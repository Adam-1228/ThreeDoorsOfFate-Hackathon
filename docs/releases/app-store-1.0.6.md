# Three Doors of Fate — App Store 1.0.6

## Release contract

- App Store marketing version: `1.0.6`
- Initial upload build number: `10600`
- Bundle identifier: `com.adam.threedoorsfate`
- Apple team: `X32UC5UP4G`
- Minimum OS: `iOS 15.0`
- Devices: iPhone and iPad
- Source commit: `edad83f5235b21ebfaed708a28c379f6dfb466a0`
- Source release: GitHub `v1.1.0`

The public GitHub/WebGL source release remains `v1.1.0`. The iOS build must override the Unity project default with `UNITY_IOS_VERSION=1.0.6` and `UNITY_IOS_BUILD_NUMBER=10600`; the repository-wide `v1.1.0` browser-release metadata must not be rewritten as an iOS version.

If App Store Connect reports that build `10600` already exists for version `1.0.6`, stop before producing another archive and revise this release contract to `10601`.

## Audit evidence — 2026-08-24

- Local `main` and `origin/main` both resolve to `edad83f5235b21ebfaed708a28c379f6dfb466a0`.
- App Store Connect shows the current version as `1.0.5` with build `9`.
- TestFlight contains versions through `1.0.5` and contains no `1.0.6` build, so `1.0.6 (10600)` has no collision at audit time.
- Python repository contracts pass `10/10`.
- GitHub Actions run `32687257949` completed successfully for the synchronized `v1.1.0` WebGL release lineage.
- Xcode `26.6` is selected, first-launch setup is complete, and the iPhoneOS SDK is available.
- CocoaPods `1.16.2` is installed in the user Ruby gem directory and runs with `PATH="$HOME/.gem/ruby/2.6.0/bin:$PATH"` plus `RUBYOPT=-rlogger`.
- Unity `6000.4.11f1` is installed, but its iOS Build Support module is absent and must not be installed or executed without explicit approval for this task.

## Scope

- Improve text sizing, wrapping, safe margins, and readability across cards and game screens.
- Improve the status, synergy, owned-card, awakening, character-trait, achievement, relic, class-selection, top-guide, door-selection, option, battle, and result UI layouts.
- Include the updated interaction, card, battle, reward, shop, save, and load sound routing shipped from the same source commit.
- Include the verified Normal-mode balance adjustments and guaranteed attack-card offer behavior from the same source commit.
- Preserve the existing 72 card data assets, 48 normal renders, and 24 hard-card renders.
- Preserve Game Center, iCloud synchronization, rewarded-ad policy, privacy manifest, and iPhone/iPad support.

## App Store release notes — Korean

게임 전반의 텍스트와 UI를 정비했습니다. 카드와 전투 설명의 글자 크기, 줄바꿈, 여백을 다듬고 상태창, 카드 시너지, 보유 카드, 업적, 유물, 캐릭터 선택, 문 선택, 전투 HUD와 결과 화면의 배치와 가독성을 개선했습니다. 버튼과 주요 동작의 효과음 및 조작 피드백도 보완했으며, 보통 난이도의 전투 흐름과 카드 보상 구성을 조정했습니다.

## App Store release notes — English

Updated text and UI throughout the game. Improved font sizing, wrapping, spacing, and readability for cards and combat descriptions; refined layouts across status panels, synergies, owned cards, achievements, relics, character selection, door selection, battle HUD, and result screens; and enhanced sound and interaction feedback. Normal difficulty and card reward offers were also adjusted for a smoother run.

## Required gates

- App Store Connect must show no existing `1.0.6 (10600)` build before archive creation.
- A fresh Unity iOS export must be generated from the exact source commit above. Older Xcode projects and iCloud `dataless` build artifacts are invalid.
- Production AdMob identifiers must be injected through `ADMOB_IOS_APP_ID` and `ADMOB_IOS_INTERSTITIAL_ID`; Google test identifiers are forbidden.
- The signed archive must contain Game Center and both iCloud container entitlements, the native GameKit bridge symbols, and privacy manifests.
- The upload must preserve marketing version `1.0.6` and build `10600` without Xcode automatically changing either value.
- App Review submission is a separate external action and requires an action-time confirmation immediately before the final submission.
