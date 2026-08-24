# Three Doors of Fate — iOS release readiness (2026-08-05)

## Verified locally

- Project: `/Users/apple/Documents/game/ThreeDoorsOfFate-Hackathon`
- Unity: `6000.4.11f1` with matching iOS Build Support
- Xcode: `26.6`; CocoaPods: `1.16.2`
- Bundle identifier: `com.adam.threedoorsfate`
- Marketing version/build: `1.0.0 (1)`
- Python release/layout tests: `17/17` passed
- Unity EditMode tests: `132/132` passed
- Signed iPhone Release build: `codesign --verify --deep --strict` passed
- Required Game Center, iCloud, privacy-manifest, app-icon, and native-symbol checks passed
- Connected iPhone install succeeded
- Foreground launch remained active for the full 12-second console observation window

## Stable native build location

`tools/mac_setup_and_build.sh` and `PlayableGameBuilder.cs` now honor:

```bash
UNITY_IOS_NATIVE_BUILD_ROOT=/Volumes/TDOF-iOS-Build
```

Use it for Unity Xcode export, DerivedData, device builds, and release archives on this Mac. This avoids signing failures caused by File Provider extended attributes under `Documents`.

## External account audit

- App Store Connect login: ready
- Registered Bundle ID: available for `com.adam.threedoorsfate`
- App Store Connect app record: not created yet
- New-app form prepared, not submitted:
  - Platform: iOS
  - Name: Three Doors of Fate
  - Primary language: Korean
  - Bundle ID: `com.adam.threedoorsfate`
  - SKU: `TDOF-IOS-2026`
  - Access: full
- Free Apps Agreement: active
- Paid Apps Agreement: waiting for user information; not required for an ad-supported free app without paid downloads or in-app purchases
- EU distribution: trader-status information still requires an account-owner decision
- AdMob login: ready
- Three Doors of Fate AdMob app: not created yet
- Unpublished iOS app form prepared, not submitted

## Blocking release inputs

1. Confirm creation of the prepared App Store Connect record.
2. Confirm creation of the prepared unpublished AdMob iOS app and one rewarded ad unit.
3. Provide/publish a privacy-policy URL and complete App Store privacy answers, including Google Mobile Ads SDK data practices.
4. Decide EU trader/non-trader status or exclude EU storefronts until the status is complete.
5. After production AdMob identifiers exist, run `ios-release-verify` with private environment variables and the APFS native build root.
6. Use Xcode Organizer/cloud-managed distribution signing to validate and upload the archive. App review submission remains a separate metadata/legal checkpoint.

No production archive or App Store upload was attempted with Google's test advertising identifiers.
