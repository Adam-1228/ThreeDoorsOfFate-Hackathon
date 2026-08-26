#!/usr/bin/env bash

set -euo pipefail

EXPECTED_BUNDLE_ID="${TDOF_EXPECTED_BUNDLE_ID:-com.adam.threedoorsfate}"
EXPECTED_VERSION="${TDOF_EXPECTED_VERSION:-1.3.0}"
EXPECTED_BUILD="${TDOF_EXPECTED_BUILD:-13000}"
EXPECTED_UNITY_VERSION="${TDOF_EXPECTED_UNITY_VERSION:-6000.4.11f1}"
ARCHIVE_PATH="${1:-}"
EXPORT_PATH="${2:-}"
EXPORT_OPTIONS_PLIST=""
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -P)"

usage() {
    printf 'Usage: bash tools/upload_testflight.sh <archive.xcarchive> <new-export-directory>\n'
}

fail() {
    printf 'error: %s\n' "$*" >&2
    exit 1
}

resolve_skadnetwork_xml() {
    local configured="${TDOF_SKADNETWORK_XML:-}"
    if [[ -n "$configured" ]]; then
        [[ -f "$configured" ]] \
            || fail "Configured Google Mobile Ads SKAdNetwork metadata was not found."
        printf '%s\n' "$configured"
        return
    fi

    local candidate
    local matches=()
    while IFS= read -r candidate; do
        matches+=("$candidate")
    done < <(
        find "$PROJECT_ROOT/Library/PackageCache" \
            -path '*/GoogleMobileAds/Editor/GoogleMobileAdsSKAdNetworkItems.xml' \
            -type f -print 2>/dev/null
    )
    [[ "${#matches[@]}" -eq 1 ]] \
        || fail "Expected exactly one packaged Google Mobile Ads SKAdNetwork metadata file."
    printf '%s\n' "${matches[0]}"
}

verify_release_plist() {
    local info_plist="$1"
    local skadnetwork_xml
    skadnetwork_xml="$(resolve_skadnetwork_xml)"
    python3 "$SCRIPT_DIR/configure_ios_release_plist.py" verify \
        --plist "$info_plist" \
        --version "$EXPECTED_VERSION" \
        --build "$EXPECTED_BUILD" \
        --unity-version "$EXPECTED_UNITY_VERSION" \
        --skadnetwork-xml "$skadnetwork_xml"
}

cleanup() {
    if [[ -n "$EXPORT_OPTIONS_PLIST" ]]; then
        rm -f "$EXPORT_OPTIONS_PLIST"
    fi
}

trap cleanup EXIT

[[ -n "$ARCHIVE_PATH" && -n "$EXPORT_PATH" ]] || { usage; exit 2; }
[[ -d "$ARCHIVE_PATH" && "$ARCHIVE_PATH" == *.xcarchive ]] \
    || fail "A readable .xcarchive directory is required: $ARCHIVE_PATH"
[[ ! -e "$EXPORT_PATH" && ! -L "$EXPORT_PATH" ]] \
    || fail "Refusing to overwrite the TestFlight export path: $EXPORT_PATH"

APP_PATH="$ARCHIVE_PATH/Products/Applications/Three Doors of Fate.app"
if [[ ! -d "$APP_PATH" ]]; then
    APP_PATH="$(find "$ARCHIVE_PATH/Products/Applications" -maxdepth 1 -name '*.app' -print -quit 2>/dev/null || true)"
fi
[[ -n "$APP_PATH" && -d "$APP_PATH" ]] \
    || fail "No app bundle was found in the archive."
INFO_PLIST="$APP_PATH/Info.plist"
[[ -f "$INFO_PLIST" ]] || fail "The archived app has no Info.plist."

BUNDLE_ID="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$INFO_PLIST" 2>/dev/null || true)"
[[ "$BUNDLE_ID" == "$EXPECTED_BUNDLE_ID" ]] \
    || fail "Archive bundle identifier does not match the release app."
APP_VERSION="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$INFO_PLIST" 2>/dev/null || true)"
APP_BUILD="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' "$INFO_PLIST" 2>/dev/null || true)"
[[ "$APP_VERSION" == "$EXPECTED_VERSION" ]] \
    || fail "Archive version does not match the expected release version."
[[ "$APP_BUILD" == "$EXPECTED_BUILD" ]] \
    || fail "Archive build does not match the expected release build."
verify_release_plist "$INFO_PLIST"

mkdir -p "$(dirname "$EXPORT_PATH")"
TEMPORARY_SEED="$(mktemp "${TMPDIR:-/private/tmp}/tdof-testflight-export.XXXXXX")"
EXPORT_OPTIONS_PLIST="${TEMPORARY_SEED}.plist"
mv "$TEMPORARY_SEED" "$EXPORT_OPTIONS_PLIST"
plutil -create xml1 "$EXPORT_OPTIONS_PLIST"
/usr/libexec/PlistBuddy -c 'Add :method string app-store-connect' "$EXPORT_OPTIONS_PLIST"
/usr/libexec/PlistBuddy -c 'Add :destination string upload' "$EXPORT_OPTIONS_PLIST"
/usr/libexec/PlistBuddy -c 'Add :signingStyle string automatic' "$EXPORT_OPTIONS_PLIST"
/usr/libexec/PlistBuddy -c 'Add :manageAppVersionAndBuildNumber bool false' "$EXPORT_OPTIONS_PLIST"
/usr/libexec/PlistBuddy -c 'Add :uploadSymbols bool true' "$EXPORT_OPTIONS_PLIST"
/usr/libexec/PlistBuddy -c 'Add :stripSwiftSymbols bool true' "$EXPORT_OPTIONS_PLIST"
plutil -lint "$EXPORT_OPTIONS_PLIST" >/dev/null

xcodebuild \
    -exportArchive \
    -archivePath "$ARCHIVE_PATH" \
    -exportPath "$EXPORT_PATH" \
    -exportOptionsPlist "$EXPORT_OPTIONS_PLIST" \
    -allowProvisioningUpdates

printf 'TESTFLIGHT_UPLOAD_SUCCEEDED archive=%s export=%s\n' \
    "$ARCHIVE_PATH" "$EXPORT_PATH"
