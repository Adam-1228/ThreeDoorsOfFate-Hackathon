#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -P)"
WORKSPACE_ROOT="$(cd "$PROJECT_ROOT/.." && pwd -P)"
BUILD_ROOT="$WORKSPACE_ROOT/Builds"
XCODE_ROOT="$BUILD_ROOT/iOS/Simulator"
DERIVED_DATA="$BUILD_ROOT/iOS/DerivedData-Simulator"
LOG_FILE="$BUILD_ROOT/Logs/apple_ios_simulator.log"
BUNDLE_ID="${IOS_BUNDLE_ID:-com.adam.threedoorsfate}"
MODE="${1:-launch}"

fail() {
    printf 'error: %s\n' "$*" >&2
    exit 1
}

case "$MODE" in
    launch|--build-only) ;;
    *) fail "Usage: bash tools/run_ios_simulator.sh [--build-only]" ;;
esac

[[ "$(uname -s)" == "Darwin" ]] || fail "Run this script from macOS."
command -v python3 >/dev/null 2>&1 || fail "python3 is required."
command -v xcodebuild >/dev/null 2>&1 || fail "Xcode is required."
xcodebuild -version >/dev/null 2>&1 \
    || fail "Select a full Xcode installation with xcode-select."
command -v xcrun >/dev/null 2>&1 || fail "Xcode command-line tools are required."
command -v pod >/dev/null 2>&1 || fail "CocoaPods is required. Run: brew install cocoapods"

mkdir -p "$(dirname "$LOG_FILE")"
python3 "$SCRIPT_DIR/unity_build.py" \
    --target ios-simulator \
    --log-file "$LOG_FILE"

if [[ -f "$XCODE_ROOT/Podfile" ]]; then
    if [[ -f "$XCODE_ROOT/Podfile.lock" ]]; then
        (cd "$XCODE_ROOT" && pod install --deployment)
    else
        (cd "$XCODE_ROOT" && pod install)
    fi
fi

XCODE_WORKSPACE="$XCODE_ROOT/Unity-iPhone.xcworkspace"
XCODE_PROJECT="$XCODE_ROOT/Unity-iPhone.xcodeproj"
if [[ -d "$XCODE_WORKSPACE" ]]; then
    XCODE_CONTAINER_ARGS=(-workspace "$XCODE_WORKSPACE")
elif [[ -d "$XCODE_PROJECT" ]]; then
    XCODE_CONTAINER_ARGS=(-project "$XCODE_PROJECT")
else
    fail "Unity did not create an Xcode project under $XCODE_ROOT"
fi

xcodebuild \
    "${XCODE_CONTAINER_ARGS[@]}" \
    -scheme Unity-iPhone \
    -configuration Release \
    -sdk iphonesimulator \
    -destination 'generic/platform=iOS Simulator' \
    -derivedDataPath "$DERIVED_DATA" \
    CODE_SIGNING_ALLOWED=NO \
    build

while IFS= read -r -d '' manifest; do
    plutil -lint "$manifest" >/dev/null
done < <(find "$DERIVED_DATA/Build/Products" -name PrivacyInfo.xcprivacy -print0)

if [[ "$MODE" == "--build-only" ]]; then
    printf 'iOS Simulator build verified: %s\n' "$DERIVED_DATA"
    exit 0
fi

DEVICE_ID="$(xcrun simctl list devices booted \
    | sed -nE 's/.*\(([0-9A-Fa-f-]{36})\) \(Booted\).*/\1/p' \
    | head -n 1)"

if [[ -z "$DEVICE_ID" ]]; then
    DEVICE_ID="$(xcrun simctl list devices available \
        | sed -nE '/iPhone/s/.*\(([0-9A-Fa-f-]{36})\).*/\1/p' \
        | head -n 1)"
    [[ -n "$DEVICE_ID" ]] || fail "No available iPhone Simulator runtime was found in Xcode."
    xcrun simctl boot "$DEVICE_ID"
fi

open -a Simulator
xcrun simctl bootstatus "$DEVICE_ID" -b

APP_PATH="$(find "$DERIVED_DATA/Build/Products/Release-iphonesimulator" \
    -maxdepth 1 -type d -name '*.app' -print -quit)"
[[ -n "$APP_PATH" ]] || fail "The Simulator app bundle was not produced."

xcrun simctl install "$DEVICE_ID" "$APP_PATH"
xcrun simctl launch "$DEVICE_ID" "$BUNDLE_ID"
printf 'Launched %s on Simulator %s\n' "$BUNDLE_ID" "$DEVICE_ID"
