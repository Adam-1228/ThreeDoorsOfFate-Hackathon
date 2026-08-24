#!/usr/bin/env bash

set -euo pipefail

UNITY_VERSION="6000.4.11f1"
UNITY_CHANGESET="b0a1d6caadd2"
EXPECTED_BUNDLE_ID="com.adam.threedoorsfate"
EXPECTED_ICLOUD_CONTAINER="${UNITY_IOS_ICLOUD_CONTAINER:-iCloud.com.adam.threedoorsfate}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -P)"
WORKSPACE_ROOT="$(cd "$PROJECT_ROOT/.." && pwd -P)"
BUILD_ROOT="$WORKSPACE_ROOT/Builds"
DEFAULT_UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
TARGET="${1:-all}"

usage() {
    cat <<'EOF'
Usage: bash tools/mac_setup_and_build.sh [doctor|setup|macos|ios|ios-install|ios-simulator|ios-release-verify|all]

  doctor             Check the exact Unity, iOS module, Xcode, simulator, and CocoaPods versions.
  setup              Install/check Unity and Apple build dependencies.
  macos              Build and launch the macOS player.
  ios                Export the iOS device project, resolve Pods, and open the workspace.
  ios-install        Build, sign, install, and launch on a connected iPhone or iPad.
  ios-simulator      Build and launch the game in an available iOS Simulator.
  ios-release-verify Build an unsigned device player and a signed production archive.
  all                Prepare dependencies, build macOS, and export iOS device (default).
EOF
}

fail() {
    printf 'error: %s\n' "$*" >&2
    exit 1
}

require_macos() {
    [[ "$(uname -s)" == "Darwin" ]] || fail "Run this script from macOS."
}

ensure_unity_cli() {
    if command -v unity >/dev/null 2>&1; then
        return
    fi

    printf 'Installing the official Unity CLI...\n'
    curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh \
        | UNITY_CLI_CHANNEL=beta bash
    # shellcheck disable=SC1090
    source "$HOME/.unity/env"
    command -v unity >/dev/null 2>&1 \
        || fail "Unity CLI installation finished but unity is not on PATH."
}

resolve_unity_paths() {
    local configured="${UNITY_EDITOR:-$DEFAULT_UNITY_APP}"

    if [[ "$configured" == *.app ]]; then
        UNITY_APP="$configured"
        UNITY_BIN="$configured/Contents/MacOS/Unity"
    else
        UNITY_BIN="$configured"
        UNITY_APP="${configured%/Contents/MacOS/Unity}"
    fi
}

verify_unity_version() {
    [[ -x "$UNITY_BIN" ]] || fail "Unity Editor was not found: $UNITY_BIN"
    local version_output
    version_output="$("$UNITY_BIN" -version 2>&1 | tr -d '\r')"
    grep -Fxq "$UNITY_VERSION" <<<"$version_output" \
        || fail "Expected Unity $UNITY_VERSION, got: $version_output"
}

ensure_unity_editor() {
    resolve_unity_paths
    if [[ ! -x "$UNITY_BIN" ]]; then
        ensure_unity_cli
        local editor_arch
        case "$(uname -m)" in
            arm64) editor_arch="arm64" ;;
            x86_64) editor_arch="x86_64" ;;
            *) fail "Unsupported Mac architecture: $(uname -m)" ;;
        esac

        printf 'Installing Unity %s (%s)...\n' "$UNITY_VERSION" "$editor_arch"
        unity install "$UNITY_VERSION" -c "$UNITY_CHANGESET" -a "$editor_arch" -m ios
        resolve_unity_paths
    fi
    verify_unity_version
}

ensure_ios_module() {
    local ios_support="$UNITY_APP/Contents/PlaybackEngines/iOSSupport"
    if [[ ! -d "$ios_support" ]]; then
        ensure_unity_cli
        unity editors add "$UNITY_APP" >/dev/null 2>&1 || true
        printf 'Installing iOS Build Support for Unity %s...\n' "$UNITY_VERSION"
        unity install-modules -e "$UNITY_VERSION" -m ios
    fi
    [[ -d "$ios_support" ]] || fail "iOS Build Support was not installed at $ios_support"
}

ensure_xcode() {
    if ! command -v xcodebuild >/dev/null 2>&1 || ! xcodebuild -version >/dev/null 2>&1; then
        open "macappstore://itunes.apple.com/app/id497799835"
        fail "Xcode is required. Install it from the opened Mac App Store page, then rerun."
    fi

    if ! xcodebuild -checkFirstLaunchStatus >/dev/null 2>&1; then
        printf 'Completing Xcode first-launch setup (administrator password may be requested)...\n'
        sudo xcodebuild -runFirstLaunch
    fi
}

ensure_cocoapods() {
    if command -v pod >/dev/null 2>&1; then
        return
    fi
    command -v brew >/dev/null 2>&1 \
        || fail "CocoaPods is required. Install Homebrew, then run: brew install cocoapods"
    printf 'Installing CocoaPods...\n'
    brew install cocoapods
    command -v pod >/dev/null 2>&1 || fail "CocoaPods installation failed."
}

ensure_python() {
    command -v python3 >/dev/null 2>&1 \
        || fail "python3 is required. Install Xcode command-line tools or Python 3."
}

run_doctor() {
    resolve_unity_paths
    verify_unity_version
    [[ -d "$UNITY_APP/Contents/PlaybackEngines/iOSSupport" ]] \
        || fail "Unity iOS Build Support is missing."
    command -v unity >/dev/null 2>&1 || fail "The official Unity CLI is missing."
    unity --version
    (cd "$PROJECT_ROOT" && unity --non-interactive projects require ios)
    command -v xcode-select >/dev/null 2>&1 || fail "xcode-select is missing."
    xcode-select -p
    xcodebuild -version
    xcrun --sdk iphoneos --show-sdk-version
    xcrun --sdk iphonesimulator --show-sdk-version
    xcrun simctl list runtimes available | grep -q 'iOS' \
        || fail "No available iOS Simulator runtime was found."
    command -v pod >/dev/null 2>&1 || fail "CocoaPods is missing."
    pod --version
    printf 'Apple release doctor passed.\n'
}

run_unity_build() {
    local target="$1"
    local log_dir="$BUILD_ROOT/Logs"
    local log_file="$log_dir/apple_${target//-/_}.log"
    mkdir -p "$log_dir"

    printf 'Building %s...\n' "$target"
    if ! python3 "$SCRIPT_DIR/unity_build.py" \
        --target "$target" \
        --unity "$UNITY_BIN" \
        --log-file "$log_file"; then
        tail -n 100 "$log_file" 2>/dev/null || true
        fail "Unity $target build failed. Full log: $log_file"
    fi
    printf 'Unity build log: %s\n' "$log_file"
}

resolve_pods() {
    local xcode_root="$1"
    if [[ ! -f "$xcode_root/Podfile" ]]; then
        return
    fi
    command -v pod >/dev/null 2>&1 || fail "CocoaPods is required for this Xcode export."
    if [[ -f "$xcode_root/Podfile.lock" ]]; then
        (cd "$xcode_root" && pod install --deployment)
    else
        (cd "$xcode_root" && pod install)
    fi
}

select_xcode_container() {
    local xcode_root="$1"
    local workspace="$xcode_root/Unity-iPhone.xcworkspace"
    local project="$xcode_root/Unity-iPhone.xcodeproj"
    if [[ -d "$workspace" ]]; then
        XCODE_CONTAINER_ARGS=(-workspace "$workspace")
    elif [[ -d "$project" ]]; then
        XCODE_CONTAINER_ARGS=(-project "$project")
    else
        fail "Unity did not create an Xcode project at $xcode_root"
    fi
}

plist_value_contains() {
    local plist="$1"
    local key="$2"
    local expected="$3"
    /usr/libexec/PlistBuddy -c "Print :$key" "$plist" 2>/dev/null | grep -Fq "$expected"
}

validate_export() {
    local xcode_root="$1"
    local pbxproj="$xcode_root/Unity-iPhone.xcodeproj/project.pbxproj"
    local entitlements="$xcode_root/ThreeDoorsOfFate.entitlements"
    local privacy_manifest="$xcode_root/PrivacyInfo.xcprivacy"
    local info_plist="$xcode_root/Info.plist"

    [[ -f "$pbxproj" ]] || fail "Missing Xcode project file: $pbxproj"
    [[ -f "$entitlements" ]] || fail "Missing entitlements: $entitlements"
    [[ -f "$privacy_manifest" ]] || fail "Missing root app privacy manifest."
    [[ -f "$info_plist" ]] || fail "Missing Info.plist."
    plutil -lint "$entitlements" "$privacy_manifest" "$info_plist" >/dev/null
    grep -Eq 'TARGETED_DEVICE_FAMILY = "?1,2"?;' "$pbxproj" \
        || fail "The Xcode project is not configured for both iPhone and iPad."
    grep -Fq 'CODE_SIGN_ENTITLEMENTS = ThreeDoorsOfFate.entitlements;' "$pbxproj" \
        || fail "The app target does not reference ThreeDoorsOfFate.entitlements."
    grep -Fq 'ThreeDoorsGameKitBridge.mm' "$pbxproj" \
        || fail "The GameKit native bridge is missing from the Xcode project."
    plist_value_contains "$entitlements" 'com.apple.developer.game-center' 'true' \
        || fail "Game Center entitlement is missing."
    plist_value_contains "$entitlements" 'com.apple.developer.icloud-services' 'CloudDocuments' \
        || fail "iCloud CloudDocuments entitlement is missing."
    plist_value_contains "$entitlements" 'com.apple.developer.ubiquity-container-identifiers' "$EXPECTED_ICLOUD_CONTAINER" \
        || fail "Expected iCloud container is missing: $EXPECTED_ICLOUD_CONTAINER"
    plist_value_contains "$entitlements" 'com.apple.developer.icloud-container-identifiers' "$EXPECTED_ICLOUD_CONTAINER" \
        || fail "Expected iCloud Documents container is missing: $EXPECTED_ICLOUD_CONTAINER"
    plist_value_contains "$info_plist" 'CFBundleIdentifier' "$EXPECTED_BUNDLE_ID" \
        || grep -Fq "PRODUCT_BUNDLE_IDENTIFIER = $EXPECTED_BUNDLE_ID;" "$pbxproj" \
        || grep -Fq "PRODUCT_BUNDLE_IDENTIFIER = \"$EXPECTED_BUNDLE_ID\";" "$pbxproj" \
        || fail "Expected bundle identifier is missing: $EXPECTED_BUNDLE_ID"
}

validate_native_symbols() {
    local products_root="$1"
    local binary
    binary="$(find "$products_root" -type f -path '*/UnityFramework.framework/UnityFramework' -print -quit)"
    [[ -n "$binary" ]] || fail "UnityFramework binary was not produced."
    local symbols=(
        TDOF_CloudInitialize
        TDOF_GameCenterAuthenticate
        TDOF_GameCenterReportScore
        TDOF_GameCenterReportAchievement
        TDOF_CloudFetch
        TDOF_CloudSave
        TDOF_CloudResolve
    )
    local symbol
    for symbol in "${symbols[@]}"; do
        nm -gU "$binary" | grep -Fq "_$symbol" || fail "Missing native symbol: $symbol"
    done
}

validate_bundled_privacy_manifests() {
    local root="$1"
    local found=0
    while IFS= read -r -d '' manifest; do
        plutil -lint "$manifest" >/dev/null
        found=1
    done < <(find "$root" -name PrivacyInfo.xcprivacy -print0)
    [[ "$found" -eq 1 ]] || fail "No bundled privacy manifests were found under $root"
}

run_unsigned_device_compile() {
    local xcode_root="$1"
    local derived_data="$BUILD_ROOT/iOS/DerivedData-Device"
    select_xcode_container "$xcode_root"
    xcodebuild \
        "${XCODE_CONTAINER_ARGS[@]}" \
        -scheme Unity-iPhone \
        -configuration Release \
        -sdk iphoneos \
        -destination 'generic/platform=iOS' \
        -derivedDataPath "$derived_data" \
        CODE_SIGNING_ALLOWED=NO \
        build
    validate_native_symbols "$derived_data/Build/Products"
    validate_bundled_privacy_manifests "$derived_data/Build/Products"
}

run_macos() {
    run_unity_build macos
    open "$BUILD_ROOT/macOS/ThreeDoorsOfFate.app"
}

export_ios_device() {
    local xcode_root="$BUILD_ROOT/iOS/Device"
    run_unity_build ios
    resolve_pods "$xcode_root"
    validate_export "$xcode_root"
    select_xcode_container "$xcode_root"
    open "${XCODE_CONTAINER_ARGS[1]}"
}

resolve_development_team() {
    local xcode_root="$1"
    local configured="${UNITY_IOS_DEVELOPMENT_TEAM:-}"
    if [[ -n "$configured" ]]; then
        printf '%s\n' "$configured"
        return
    fi

    local pbxproj="$xcode_root/Unity-iPhone.xcodeproj/project.pbxproj"
    if [[ -f "$pbxproj" ]]; then
        configured="$(sed -nE 's/^[[:space:]]*DEVELOPMENT_TEAM = ([A-Z0-9]+);/\1/p' "$pbxproj" | head -n 1)"
        if [[ -n "$configured" ]]; then
            printf '%s\n' "$configured"
            return
        fi
    fi

    local profile_root profile temporary_plist
    local profile_roots=(
        "$HOME/Library/MobileDevice/Provisioning Profiles"
        "$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"
    )
    for profile_root in "${profile_roots[@]}"; do
        [[ -d "$profile_root" ]] || continue
        while IFS= read -r -d '' profile; do
            temporary_plist="$(mktemp)"
            if security cms -D -i "$profile" >"$temporary_plist" 2>/dev/null; then
                configured="$(/usr/libexec/PlistBuddy -c 'Print :TeamIdentifier:0' "$temporary_plist" 2>/dev/null || true)"
            fi
            rm -f "$temporary_plist"
            if [[ -n "$configured" ]]; then
                printf '%s\n' "$configured"
                return
            fi
        done < <(find "$profile_root" -type f \( -name '*.mobileprovision' -o -name '*.provisionprofile' \) -print0)
    done

    return 1
}

resolve_connected_device_id() {
    local configured="${UNITY_IOS_DEVICE_ID:-}"
    if [[ -n "$configured" ]]; then
        printf '%s\n' "$configured"
        return
    fi

    local device_id
    device_id="$(
        xcrun xctrace list devices 2>/dev/null \
            | sed -nE '/Simulator/d; /(iPhone|iPad)/ s/.*\(([0-9A-Fa-f-]{16,})\)$/\1/p' \
            | head -n 1
    )"
    [[ -n "$device_id" ]] \
        || fail "No connected iPhone or iPad was found. Unlock it, trust this Mac, and keep Developer Mode enabled."
    printf '%s\n' "$device_id"
}

verify_signed_device_app() {
    local app_path="$1"
    local entitlements_output="$BUILD_ROOT/iOS/device-build-entitlements.plist"
    local profile_output="$BUILD_ROOT/iOS/device-build-profile.plist"

    codesign --verify --deep --strict "$app_path"
    codesign -d --entitlements :- "$app_path" >"$entitlements_output" 2>/dev/null
    plutil -lint "$entitlements_output" >/dev/null
    plist_value_contains "$entitlements_output" 'com.apple.developer.game-center' 'true' \
        || fail "Signed device app is missing Game Center entitlement."
    plist_value_contains "$entitlements_output" 'com.apple.developer.icloud-services' 'CloudDocuments' \
        || fail "Signed device app is missing iCloud CloudDocuments entitlement."
    plist_value_contains "$entitlements_output" 'com.apple.developer.ubiquity-container-identifiers' "$EXPECTED_ICLOUD_CONTAINER" \
        || fail "Signed device app is missing the expected iCloud container."
    [[ -f "$app_path/embedded.mobileprovision" ]] \
        || fail "Signed device app has no embedded provisioning profile."
    security cms -D -i "$app_path/embedded.mobileprovision" >"$profile_output"
    plutil -lint "$profile_output" >/dev/null
    validate_bundled_privacy_manifests "$app_path"
}

run_ios_device_install() {
    local xcode_root="$BUILD_ROOT/iOS/Device"
    local derived_data="$BUILD_ROOT/iOS/DerivedData-Install"
    local team_id device_id app_path

    run_unity_build ios
    resolve_pods "$xcode_root"
    validate_export "$xcode_root"

    team_id="$(resolve_development_team "$xcode_root" || true)"
    [[ -n "$team_id" ]] \
        || fail "Apple development team ID was not found. Set UNITY_IOS_DEVELOPMENT_TEAM and rerun ios-install."
    device_id="$(resolve_connected_device_id)"
    command -v xcrun >/dev/null 2>&1 || fail "xcrun is required."
    xcrun --find devicectl >/dev/null 2>&1 \
        || fail "Xcode devicectl is required to install the app."

    printf 'Building for connected device %s with team %s...\n' "$device_id" "$team_id"
    select_xcode_container "$xcode_root"
    xcodebuild \
        "${XCODE_CONTAINER_ARGS[@]}" \
        -scheme Unity-iPhone \
        -configuration Release \
        -sdk iphoneos \
        -destination "id=$device_id" \
        -derivedDataPath "$derived_data" \
        -allowProvisioningUpdates \
        -allowProvisioningDeviceRegistration \
        DEVELOPMENT_TEAM="$team_id" \
        CODE_SIGN_STYLE=Automatic \
        build

    app_path="$(find "$derived_data/Build/Products/Release-iphoneos" -maxdepth 1 -name '*.app' -print -quit)"
    [[ -n "$app_path" && -d "$app_path" ]] \
        || fail "Signed iOS app was not produced under $derived_data."
    validate_native_symbols "$derived_data/Build/Products"
    verify_signed_device_app "$app_path"

    xcrun devicectl device install app --device "$device_id" "$app_path"
    xcrun devicectl device process launch --device "$device_id" "$EXPECTED_BUNDLE_ID"
    printf 'Installed and launched %s on device %s.\n' "$EXPECTED_BUNDLE_ID" "$device_id"
}

verify_signed_archive() {
    local archive_path="$1"
    local app_path="$archive_path/Products/Applications/Three Doors of Fate.app"
    [[ -d "$app_path" ]] || app_path="$(find "$archive_path/Products/Applications" -maxdepth 1 -name '*.app' -print -quit)"
    [[ -n "$app_path" && -d "$app_path" ]] || fail "Signed app was not found in the archive."
    local entitlements_output="$BUILD_ROOT/iOS/signed-entitlements.plist"
    local profile_output="$BUILD_ROOT/iOS/embedded-profile.plist"
    codesign -d --entitlements :- "$app_path" >"$entitlements_output" 2>/dev/null
    plutil -lint "$entitlements_output" >/dev/null
    plist_value_contains "$entitlements_output" 'com.apple.developer.game-center' 'true' \
        || fail "Signed app is missing Game Center entitlement."
    plist_value_contains "$entitlements_output" 'com.apple.developer.icloud-services' 'CloudDocuments' \
        || fail "Signed app is missing iCloud CloudDocuments entitlement."
    plist_value_contains "$entitlements_output" 'com.apple.developer.ubiquity-container-identifiers' "$EXPECTED_ICLOUD_CONTAINER" \
        || fail "Signed app is missing the expected iCloud container."
    plist_value_contains "$entitlements_output" 'com.apple.developer.icloud-container-identifiers' "$EXPECTED_ICLOUD_CONTAINER" \
        || fail "Signed app is missing the expected iCloud Documents container."
    [[ -f "$app_path/embedded.mobileprovision" ]] || fail "Archive has no embedded provisioning profile."
    security cms -D -i "$app_path/embedded.mobileprovision" >"$profile_output"
    plutil -lint "$profile_output" >/dev/null
    validate_bundled_privacy_manifests "$app_path"
}

run_ios_release_verify() {
    local team_id="${UNITY_IOS_DEVELOPMENT_TEAM:-}"
    [[ -n "$team_id" ]] || fail "Set UNITY_IOS_DEVELOPMENT_TEAM to your Apple team ID."
    [[ -n "${ADMOB_IOS_APP_ID:-}" && -n "${ADMOB_IOS_INTERSTITIAL_ID:-}" ]] \
        || fail "Set ADMOB_IOS_APP_ID and ADMOB_IOS_INTERSTITIAL_ID to production IDs."
    export UNITY_IOS_REQUIRE_PRODUCTION_ADS=1
    local xcode_root="$BUILD_ROOT/iOS/Device"
    local archive_path="$BUILD_ROOT/iOS/ThreeDoorsOfFate.xcarchive"
    run_unity_build ios
    resolve_pods "$xcode_root"
    validate_export "$xcode_root"
    run_unsigned_device_compile "$xcode_root"
    select_xcode_container "$xcode_root"
    xcodebuild \
        "${XCODE_CONTAINER_ARGS[@]}" \
        -scheme Unity-iPhone \
        -configuration Release \
        -destination 'generic/platform=iOS' \
        -archivePath "$archive_path" \
        DEVELOPMENT_TEAM="$team_id" \
        CODE_SIGN_STYLE=Automatic \
        archive
    verify_signed_archive "$archive_path"
    printf 'Signed iOS release archive verified: %s\n' "$archive_path"
}

main() {
    case "$TARGET" in
        -h|--help)
            usage
            return
            ;;
        doctor|setup|macos|ios|ios-install|ios-simulator|ios-release-verify|all) ;;
        *) usage; fail "Unknown target: $TARGET" ;;
    esac

    require_macos
    if [[ "$TARGET" == "doctor" ]]; then
        run_doctor
        return
    fi

    ensure_unity_editor
    ensure_python
    if [[ "$TARGET" != "macos" ]]; then
        ensure_ios_module
        ensure_xcode
        ensure_cocoapods
    fi

    export UNITY_EDITOR="$UNITY_BIN"
    case "$TARGET" in
        setup) run_doctor ;;
        macos) run_macos ;;
        ios) export_ios_device ;;
        ios-install) run_ios_device_install ;;
        ios-simulator) bash "$SCRIPT_DIR/run_ios_simulator.sh" ;;
        ios-release-verify) run_ios_release_verify ;;
        all)
            run_macos
            export_ios_device
            ;;
    esac
}

main "$@"
