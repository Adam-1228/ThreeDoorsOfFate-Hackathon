#!/usr/bin/env bash

set -euo pipefail

UNITY_VERSION="6000.4.11f1"
UNITY_CHANGESET="b0a1d6caadd2"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -P)"
WORKSPACE_ROOT="$(cd "$PROJECT_ROOT/.." && pwd -P)"
BUILD_ROOT="$WORKSPACE_ROOT/Builds"
DEFAULT_UNITY_APP="/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app"
TARGET="${1:-all}"

usage() {
    cat <<'EOF'
Usage: bash tools/mac_setup_and_build.sh [setup|macos|ios|ios-simulator|all]

  setup          Install/check Unity, iOS Build Support, and Xcode setup.
  macos          Build and launch the macOS player.
  ios            Export the iOS device Xcode project and open it.
  ios-simulator  Build and launch the game in an available iOS Simulator.
  all            Prepare dependencies, build macOS, and export iOS device (default).
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
    # The official installer writes this environment file.
    # shellcheck disable=SC1090
    source "$HOME/.unity/env"
    command -v unity >/dev/null 2>&1 || fail "Unity CLI installation finished but unity is not on PATH."
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

ensure_unity_editor() {
    resolve_unity_paths
    if [[ -x "$UNITY_BIN" ]]; then
        return
    fi

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
    [[ -x "$UNITY_BIN" ]] || fail "Unity Editor was not found after installation: $UNITY_BIN"
}

ensure_ios_module() {
    local ios_support="$UNITY_APP/Contents/PlaybackEngines/iOSSupport"
    if [[ -d "$ios_support" ]]; then
        return
    fi

    ensure_unity_cli
    unity editors add "$UNITY_APP" >/dev/null 2>&1 || true
    printf 'Installing iOS Build Support for Unity %s...\n' "$UNITY_VERSION"
    unity install-modules -e "$UNITY_VERSION" -m ios
    [[ -d "$ios_support" ]] || fail "iOS Build Support was not installed at $ios_support"
}

ensure_xcode() {
    if ! command -v xcodebuild >/dev/null 2>&1 || ! xcodebuild -version >/dev/null 2>&1; then
        open "macappstore://itunes.apple.com/app/id497799835"
        fail "Xcode is required. The Mac App Store page was opened; install Xcode, then rerun this command."
    fi

    if ! xcodebuild -checkFirstLaunchStatus >/dev/null 2>&1; then
        printf 'Completing the Xcode first-launch setup (administrator password may be requested)...\n'
        sudo xcodebuild -runFirstLaunch
    fi
}

ensure_python() {
    command -v python3 >/dev/null 2>&1 || fail "python3 is required. Install Xcode command-line tools or Python 3."
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

run_macos() {
    run_unity_build macos
    open "$BUILD_ROOT/macOS/ThreeDoorsOfFate.app"
}

export_ios_device() {
    run_unity_build ios
    open "$BUILD_ROOT/iOS/Device/Unity-iPhone.xcodeproj"
}

main() {
    case "$TARGET" in
        -h|--help)
            usage
            return
            ;;
        setup|macos|ios|ios-simulator|all) ;;
        *) usage; fail "Unknown target: $TARGET" ;;
    esac

    require_macos
    ensure_unity_editor
    ensure_python

    if [[ "$TARGET" != "macos" ]]; then
        ensure_ios_module
        ensure_xcode
    fi

    export UNITY_EDITOR="$UNITY_BIN"
    case "$TARGET" in
        setup) printf 'Apple build dependencies are ready.\n' ;;
        macos) run_macos ;;
        ios) export_ios_device ;;
        ios-simulator) bash "$SCRIPT_DIR/run_ios_simulator.sh" ;;
        all)
            run_macos
            export_ios_device
            ;;
    esac
}

main "$@"
