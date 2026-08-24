# Three Doors of Fate 1.0.4 Verification Record

**Target:** iOS `1.0.4 (8)`  
**Unity:** `6000.4.11f1`  
**Bundle ID:** `com.adam.threedoorsfate`  
**Status:** Manually released — ASC `1.0.4 배포 준비됨`

## Pre-change safety and recovery baseline — 2026-08-17 KST

| Evidence | Result |
| --- | --- |
| Canonical project | `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon` |
| Git state | No `.git` directory; no branch/worktree recovery available |
| Initial free memory | 32%; normal pressure |
| Initial free disk | 16 GiB before backup; 15 GiB before Unity baseline |
| High-load processes | No Unity or `xcodebuild`; Xcode was normally closed before Unity |
| Source backup | `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-backup/source-before-1.0.4.tar.gz` |
| Backup size | 1.0 GiB |
| Backup archive listing | Pass (`tar -tzf`, exit 0) |
| Backup SHA-256 | `9a62ff9d9fbedd6c4ec39796f97731db036fe41b824509b804f508c92d2613d3` |
| Python baseline | Pass — 100/100, 0 failed |
| Unity EditMode baseline | Pass — 145/145, 0 failed, 0 skipped, 0 inconclusive |
| Unity PlayMode baseline | Pass — 2/2, 0 failed, 0 skipped, 0 inconclusive |

Baseline artifacts:

- `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/python-baseline.log`
- `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/editmode-baseline-results.xml`
- `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/editmode-baseline.log`
- `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/playmode-baseline-results.xml`
- `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/playmode-baseline.log`

The Unity logs contain the same non-blocking shutdown-time `.NET build-server` notice seen in the prior environment. The authoritative NUnit XML results above are Passed and contain no failed, skipped, or inconclusive tests.

## Implementation gates

| Gate | Status | Evidence |
| --- | --- | --- |
| Combination/Game Over localization | Pass | RED `0/2`, GREEN `2/2`; existing localization contract `28/28` |
| HUD and decision context | Pass | RED `0/4`, GREEN `4/4`; UI layout contract pass |
| Game Over summary/actions | Pass | RED `0/4`; final GREEN `5/5`, including hidden variant; localization contract `28/28` |
| Hand-flow practice | Pass | EditMode RED `0/4`, GREEN `4/4`; pointer RED `1/2`, GREEN `2/2`; existing guide `7/7` |
| Door diversity | Pass | 256 deterministic normal seeds plus forced-progression branch, `2/2` GREEN |
| Treasure preview | Pass | RED `0/3`, GREEN `3/3`; English sprite/name/rules and no-preview fallbacks verified |
| Achievement pagination/progress | Pass | New paging/progress `3/3`; existing completion/controller `6/6`; localization `28/28` |
| Settings icon readability | Pass | Python layout contract `5/5`; Unity Korean/English integration `2/2` |
| Version 1.0.4/build 8 contracts | Pass | Expected RED `4/4`; focused GREEN `5/5`; release-contract suite `43/43` |
| Full Python suite | Pass | 103/103, 0 failed |
| Full Unity EditMode | Pass | 171/171, 0 failed, 0 skipped, 0 inconclusive |
| Full Unity PlayMode | Pass | 3/3, 0 failed, 0 skipped, 0 inconclusive |
| Korean/English runtime matrix | Pass | 102 PNGs: 17 states × 2 languages × 3 layouts; 34 visible-text audits; English Hangul matches 0 |
| Signed iOS archive | Pass | `1.0.4 (8)`, bundle/signing/entitlements/privacy/dSYM checks passed |
| Upload and processing | Pass | Apple reported `Upload succeeded`; package entered processing at 2026-08-17 18:56:09 KST |
| App Store review submission | Pass | iOS `1.0.4 (8)`, one item, `심사 대기 중`; DOM reverified 2026-08-17 19:21:29.581 KST |

## App Store Connect processing checkpoint — 2026-08-17 KST

- Chrome Profile 1 was available and logged in, and the browser-resource preflight passed.
- At 2026-08-17 19:02:36.076 KST, the TestFlight iOS DOM still showed `버전 1.0.4` with `빌드 없음`.
- The same read-only result occurred in the initial query and two bounded 30-second follow-ups. Build `1.0.4 (8)` had not yet become selectable about 6 minutes 27 seconds after Apple accepted the upload.
- The workflow stopped at the first handoff stop condition and the global three-identical-results loop limit.
- No App Store version was created, no metadata/review contact was saved or transmitted, no build was selected, and no review item was added or submitted during this browser pass.
- Resume condition: App Store Connect/TestFlight exposes processed build `1.0.4 (8)`.

## Final automated suites and runtime QA — 2026-08-17 KST

| Evidence | Result | SHA-256 |
| --- | --- | --- |
| `python-final.log` | Pass — 103/103 | `e4b66ac4c40f7f12b473c0326f39e931d82d6d50d15d3200b02f9ebc25c2b590` |
| `editmode-final-results.xml` | Pass — 171/171 | `7eb8957fd752556f1fa9ee68e735f8809c8da45d28898840633bff5b19b70224` |
| `playmode-final-results.xml` | Pass — 3/3 | `1c4c0b985460240cafebf103ef77328873d48bb8829639427903bb65db25fb34` |

The files are under `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/`.

The final runtime matrix is under `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/runtime-matrix/` and contains:

- 17 required states in Korean and English: Settings, five guide pages, completed hand-flow practice, normal/forced doors, combat HUD with subtitle, Rest, Event, treasure, synergy, two achievement pages, and Game Over;
- three layouts per state: 1920×1080, iPhone 14 Pro Max landscape 2796×1290 with safe-area insets, and a narrower 1920×1080 safe area;
- 102 dimension-validated PNGs, 34 per-state visible-text audits, one capture timestamp, one 103-line CSV manifest, and six language/layout contact sheets;
- zero Hangul matches in the English visible-text audits;
- visual inspection of all six contact sheets and key full-resolution states, with no blank screen, clipping, or control collision observed.

The first graphics-disabled capture was rejected because it produced solid gray images. It was not used as evidence. A graphics-enabled capture exposed a tutorial-overlay cleanup issue in the QA harness; `Quality104QACapture` was corrected and the complete final matrix was recaptured. No product behavior was changed by that harness correction.

The runtime audit also found two real English regressions before final verification: the treasure activity log used the Korean card name and the shop synergy subtitle remained Korean. Tests were added first and failed `2/6` as expected in `runtime-localization-red.xml`; the localized production fixes then passed `6/6` in `runtime-localization-green.xml`. The complete 103/171/3 suites above ran after those fixes.

## Signed iOS archive and upload — 2026-08-17 KST

- Build workflow: `tools/mac_setup_and_build.sh ios-release-verify`
- Archive: `/Users/apple/LocalProjects/Builds/iOS/ThreeDoorsOfFate.xcarchive` (approximately 1.7 GiB)
- Archive identity: `com.adam.threedoorsfate`, marketing version `1.0.4`, build `8`
- Release plist: `ITSAppUsesNonExemptEncryption=false`, no `NSUserTrackingUsageDescription`, production AdMob app identifier present, 50 SKAdNetwork identifiers
- Signing: deep/strict `codesign` verification passed
- Entitlements: Game Center enabled; iCloud CloudDocuments and `iCloud.com.adam.threedoorsfate` present
- Privacy manifests: app, UnityFramework, UnityRuntime, Google Mobile Ads resources, and UMP resources present (five total)
- App binary/dSYM UUID: `343C340C-E789-3840-BAF1-E6F50AADA430` matched
- UnityFramework dSYM UUID: `EC29A0C5-D92B-3323-9B66-80E1EF757CB5` present
- Archive log: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/ios-release-verify.log`, SHA-256 `6a933763d2e2402b54599ca26295cc4cc3dc8ae282b0239f1fdf611e51d9db5d`
- Upload log: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/testflight-upload.log`, SHA-256 `9263dc029a6e8755190d427e8c674015e0e8230c7ac547cf3d37934ecab0ba84`
- Apple upload result at 2026-08-17 18:56:09 KST: `Uploaded package is processing`, `Upload succeeded`, `EXPORT SUCCEEDED`, and `TESTFLIGHT_UPLOAD_SUCCEEDED`

The first upload command stopped before export or network transfer because the production AdMob identifiers were not passed into the validator's environment. No external state changed. The corrected command supplied the already-verified production identifiers, passed the release check, and completed once.

Non-blocking warning: Apple accepted the package but Xcode could not upload a dSYM for `UnityRuntime.framework` UUID `9F9CBA44-B75B-3EEC-B78C-E54B0AB11BAF`. This can limit symbolication of crashes inside that prebuilt framework; it did not block archive validation or upload.

To restore the required 12 GiB high-load safety margin before export/upload, only the generated and reproducible `/Users/apple/LocalProjects/Builds/iOS/DerivedData-Device` directory was removed after the archive passed independent verification. The signed archive and evidence were retained.

## App Store Review submission — 2026-08-17 KST

- App Store Connect app: `Three Doors of Fate`, app ID `6798086296`
- Submitted item: exactly one — iOS app `1.0.4`, build `1.0.4 (8)`
- Final submit click: 2026-08-17 19:20:59.697 KST
- Authoritative post-submit DOM verification: 2026-08-17 19:21:29.581 KST
- Exact final status: `심사 대기 중`
- Submission detail: `제출된 항목(1개)` and `iOS 앱 1.0.4 / 1.0.4 (8) / 앱 버전 / 심사 대기 중`
- Korean and English (U.S.) version metadata and both exact What's New values were saved and reread against their source JSON files.
- English App Review notes were saved and reread at exactly 3,986 characters, SHA-256 `e0767e39ebe57c6f09af21d4ef5a873059bf16dcecc796d02063f80c58a30f1f`.
- Existing authenticated review-contact values were preserved without exposure or modification; Sign-in required remained `No`.
- Resolution Center reply: none; there was no new build-8-specific message requiring one.
- Read-only final baselines: Simulated Gambling `없음`; App Privacy showed no tracking; DSA active; exactly 29 territories; `수동으로 버전 출시` retained.
- No release-after-approval action, territory/privacy/rating/DSA/price/legal/media change, or unrelated setting mutation was performed.

## Manual release — 2026-08-18 KST

- Released item: `Three Doors of Fate` iOS `1.0.4 (8)`
- Release scope: exactly 29 territories — South Korea, EU 27, and the United States
- Final `이 버전 출시` click: 2026-08-18 08:54:20.503 KST
- Authoritative ASC verification: 2026-08-18 08:54:33.735 KST
- Exact ASC status: `1.0.4 배포 준비됨`
- Manual release and `모든 사용자에게 즉시 업데이트 출시` remained selected; no phased-release or other option was changed.
- DSA remained `활성화됨`; metadata, territories, pricing, DSA, App Privacy, age rating, and all unrelated settings remained unchanged.
- A final read-only ASC recheck at 2026-08-18 08:56:05.893 KST still showed `iOS 앱 버전 1.0.4`, build row `8 / 1.0.4`, status `1.0.4 배포 준비됨`, zero `이 버전 출시` buttons, and the checked/disabled manual-release setting.
- The public product page loaded without error at 2026-08-18 08:56:20.329 KST but still displayed version `1.0.3`. Storefront propagation of 1.0.4 was therefore pending at the checkpoint; this did not contradict the authoritative ASC release state.

### Combination and Game Over localization checkpoint

- RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-localization-red.xml` — 2 failed as expected.
- GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-localization-green.xml` — 2 passed, 0 failed.
- `game_text.json`: `19e54c3383a6d4fae4c9caf6ff241df38b9994ca07b47d6a36eea7b50d2e5ce9`
- `ThreeDoorsGameController.cs`: `06f84aac32bfde0842a160ca2ecb4069b64edd0e719f40cfa13d69bcd0b3968f`
- `Quality104LocalizationTests.cs`: `39eb6c7bfc4fa48dae4a0b0fdb9c104e9cec20d65dafbce5197ebb903856fc01`

### HUD and decision context checkpoint

- RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-runui-red.xml` — 4 failed as expected.
- GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-runui-green.xml` — 4 passed, 0 failed.
- `ThreeDoorsGameController.cs`: `e5ad2d1548647e9c7f292c0fe1f7ce424104b9d0c1b77dea0911ab0d3f4f1d26`
- `ThreeDoorsGameController.Quality104.cs`: `0f24aa9765b16676feada811e32469f4db6c558954fb97b18b62113d77fa4a8e`
- `game_text.json`: `df4876fed21ade9eb76bac0284d036d61d8af25d2371f52cd0840bf1cfa8f498`
- `Quality104RunUiTests.cs`: `9e278aea2f0147a6845a9e60f08919affcce2f87d479404edc8817f767bb1f5d`

### Game Over summary and actions checkpoint

- RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-gameover-red.xml` — 4 failed as expected.
- Final GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-gameover-final.xml` — 5 passed, 0 failed, including the hidden-background path.
- Localization contract: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-gameover-localization-contract-final.log` — 28 passed, 0 failed.
- `ThreeDoorsGameController.cs`: `1f13b4a3b0b070f7c8634e4162d005abfb30a0e6b06fc7b21994a1b7211299f0`
- `ThreeDoorsGameController.Quality104.cs`: `8ec94c16e941a0ea41fbe0c1c6796135f34acdaa10601c4c9400331ed75d5073`
- `ThreeDoorsGameController.Achievements.cs`: `2a018b954ecad111638fed0e4e19b26a22779ca6c23ad1cd37425501ab02293a`
- `game_text.json`: `72896291cbb96ab24da522968e48abc7c702afc7064afbb35f665b6eb00a546b`
- `Quality104GameOverTests.cs`: `842d7aa8b1973edd306e2b42955f4bc627fcb7c77a20ca7c998857414befad18`

### Interactive hand-flow practice checkpoint

- EditMode RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-handflow-edit-red.xml` — 4 failed as expected.
- PlayMode RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-handflow-play-red.xml` — existing pointer test passed; new practice test failed as expected.
- EditMode GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-handflow-edit-final.xml` — 4 passed, 0 failed.
- PlayMode GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-handflow-play-green.xml` — 2 passed, 0 failed.
- Existing five-page guide integration: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-handflow-existing-integration.xml` — 7 passed, 0 failed.
- Localization contract: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-handflow-localization-contract-final.log` — 28 passed, 0 failed.
- `ThreeDoorsGameController.HowToPlay.cs`: `86e12bcf71c17cbaa52a6b46b4cf380571a4a3d90b67a012424ed1ff7c92045f`
- `game_text.json`: `d536ad13b355a56d979e9397893ca385f90bec4ccc086ef441b9b61502bb8851`
- `Quality104HandFlowTutorialTests.cs`: `cf09f3d1ba38f76892a027d7638a2b1a6f8066c0c56e190075e153de8b4a547d`
- `HowToPlayPointerTests.cs`: `87b9f705ce4f289b10d06069848e7d876ab9167555f168ed9631233c0dba5cde`

### Door diversity checkpoint

- RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-door-diversity-red2.xml` — normal generation failed with duplicate Battles and no-safe-choice seeds; forced progression passed.
- GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-door-diversity-green.xml` — 2 passed, 0 failed across 256 normal seeds and the forced-progression/boss-eligibility path.
- `ThreeDoorsGameController.cs`: `59d49d47fd1370c1f8e7d0d014a3cff564c38d5f52ffb9b729dbb80107e95f93`
- `Quality104DoorDiversityTests.cs`: `087a35418daf51a1cd499149964c93bf8a232b9e49823310b940fd1257835387`

### Localized treasure-card preview checkpoint

- RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-treasure-red2.xml` — 3 failed as expected because the renderer did not exist.
- GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-treasure-green.xml` — 3 passed, 0 failed.
- Localization contract: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-treasure-localization-contract-final.log` — 28 passed, 0 failed.
- The success test resolved `card_absolute_barrier` to its imported English sprite, `Absolute Barrier`, and `Gain 22 Block.`; rendering left Gold and deck contents unchanged. Null-card and skipped/full-deck branches created no preview.
- `ThreeDoorsGameController.cs`: `f674d72a050c4a5a26e6fea52d3fd73cc3f9044e87275822b1fbcd85c4f05095`
- `ThreeDoorsGameController.Quality104.cs`: `ed9321f6532ce7c55f9c7c7dd1ecdd9896e594a5ca4eeffeddfc01dd8f479e59`
- `game_text.json`: `8e6cf965bef424d00349f726ff861a2a6f90cd9b4f340ab3a6b89d897af02d68`
- `Quality104TreasureTests.cs`: `dbccda64dcf6014c0e1b0951cdba251736570dd470f58282b0d26ac173de47a1`

### Achievement pagination and progress checkpoint

- New-test RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-achievement-paging-red.xml` — 3 failed as expected.
- Existing-test RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-achievement-existing-red.xml` — 5 passed and the old eight-card layout failed the new four-card requirement.
- New-test GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-achievement-paging-green.xml` — 3 passed, 0 failed.
- Existing-test GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-achievement-existing-green.xml` — 6 passed, 0 failed.
- Localization contract: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-achievement-localization-contract.log` — 28 passed, 0 failed.
- Verified four cards per page in a 2×2 grid, endpoint clamping, 11pt description floor, bilingual page/completion summaries, per-character collector maximum `17/30`, and distinct active-build progress `2/3` without adding a saved key.
- `ThreeDoorsGameController.Achievements.cs`: `ea66b666a7407f64e5f01e90849f7c2f294d1d187a035365f1a570a53f34fec8`
- `game_text.json`: `1ddf613f399529eb05bcbf48416647ec066ce5818e6c4cceee16a4c65c7628c6`
- `AchievementControllerIntegrationTests.cs`: `f40c77afde2b55ee4c7bdf27773f307eba9453d49581eedad19cd40fbba66f8e`
- `Quality104AchievementPagingTests.cs`: `404b39580f00b67ed81651da91c6ae2ccedda4d66bb69ab34c0e79fefc677b79`

### Settings icon readability checkpoint

- Python RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-settings-layout-red.log` — the previous 22% icon width failed the 30% floor.
- Unity RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-settings-icon-red.xml` — both Korean and English cases failed at 22% width.
- Python GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-settings-layout-green.log` — 5 passed, 0 failed.
- Unity GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-settings-icon-green.xml` — 2 passed, 0 failed.
- Verified a 31% × 76% non-raycast gear column, a separate non-overlapping label column, minimum 16pt best-fit text for `설정` and `Settings`, and the existing button interaction/SFX structure.
- `ThreeDoorsGameController.Localization.cs`: `72d614f0c8e14cd72dcb8ef4b3fc3eeb689c45c5c2d080e85882004c2c3957b6`
- `Quality104SettingsIconTests.cs`: `be6ad0ddf744e12f7ddc68450f0a37a83860470e90b23cf8aba8c441fc91d6e9`
- `test_ui_layout_contract.py`: `95be80f6fc728810bab491f5acb95234647b6173ed31e3ca475bbe38d095009d`

### Version 1.0.4 (8) release-contract checkpoint

- RED: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-release-contract-red.log` — all 4 selected tests failed on the old `1.0.3 (7)` identity or missing versioned submission files.
- Focused GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-release-contract-green.log` — 5 passed, 0 failed.
- Release-contract GREEN: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/quality104-release-contract-full-green.log` — 43 passed, 0 failed.
- The active Unity project, C# release configuration, build helper, TestFlight upload helper, and submission validator all require `1.0.4 (8)`; prior 1.0.3 evidence files remain unchanged.
- Korean and English metadata preserve manual release and enumerate exactly 29 territories: Korea, the EU 27, and the United States.
- Exact bilingual What's New copy and a versioned English App Review note cover every modified surface, optional Game Center, optional non-tracking rewarded ads, and no required account.
- `ProjectSettings.asset`: `2c0959ff19639c400a42b9b626d2deb3a59f36982e6b34cce6361d03833df426`
- `IOSReleaseConfiguration.cs`: `d07f67301979209d20f1bcd1bad70e0b2ba91e676f14da7fe1bb0a4dcffef0ff`
- `mac_setup_and_build.sh`: `afce798064d67ce2ae76703501b819e358b2fb408f4e032f547c19fbbcca2a1e`
- `upload_testflight.sh`: `1ee418ca889f274f6dd970193eba360a854303aeb1705a80857b7937a3b09530`
- `validate_app_store_submission.py`: `2424faaab9d742dc9a370c1a274a6efd4c12d257982a7fd9013fb4361839f173`
- `metadata-1.0.4.ko-KR.json`: `62a65ef7a2e1cfd27dddb8555fde068547ed53d388fcdf024bf08c20a8fd6b58`
- `metadata-1.0.4.en-US.json`: `9b2f91680ab5b86a13d4b71f7036e566c236f37549cbef5757cae3da343f6270`
- `review-notes-1.0.4.en-US.md`: `e0767e39ebe57c6f09af21d4ef5a873059bf16dcecc796d02063f80c58a30f1f`
