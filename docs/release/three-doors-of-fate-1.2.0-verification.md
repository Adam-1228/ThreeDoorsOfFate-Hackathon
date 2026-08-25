# Three Doors of Fate 1.2.0 verification record

Prepared: 2026-08-25 (Asia/Seoul)

## Release identity

- Marketing version: `1.2.0`
- iOS build number: `12000`
- Bundle identifier: `com.adam.threedoorsfate`
- App Store Connect app ID: `6798086296`
- Runtime release tag: `v1.2.0` / `68391b3`
- Submission-source tag: `v1.2.0+build.12000`

## Implemented scope

- 20-slot, 1,000-point achievement collection with 12 new runtime completion conditions.
- Hidden undiscovered slots and one selectable earned-achievement detail panel using the existing relic/status frame family.
- Twelve 1024×1024 achievement illustrations and Unity sprite metadata.
- Korean and English runtime localization plus a bilingual Game Center submission manifest.
- Fixed Settings access on character confirmation/class detail.
- Progress-log safe padding and wrapping adjustments.
- Shop relic artwork viewport clipping with one top frame overlay.
- Centralized every positive Block gain behind the Iron Wall milestone gate.
- Backfilled persistent achievement signals before startup reporting and after iCloud merges.
- Version, upload helper, App Store metadata, review notes, changelog, and submission validator updated for `1.2.0 (12000)`.

## Verification boundary

Fresh local verification on 2026-08-25 produced:

- Python repository contracts: `108` tests, `0` failures.
- Unity EditMode suite: `285` tests, `285` passed, `0` failed.
- App Store local submission validator: passed.
- Public marketing, support, privacy, and root `app-ads.txt` validation: passed.
- Python `compileall`: passed.
- iOS build/export/upload helper `bash -n`: passed.
- Changed runtime/submission JSON files: `4` parsed successfully.
- `git diff --check`: passed.
- Twelve release achievement PNG pointers: canonical Git LFS pointers; all local objects matched their declared SHA-256 and byte size.

The first full-suite pass in the sparse release clone exposed missing pre-existing asset paths plus one obsolete static marker. Only the required existing App icon, iOS bridge, fonts, localized-card assets, tutorial assets, and data paths were hydrated; the obsolete marker was updated for the localized discovery component. Independent source review then identified two release gaps: non-card Block gains did not all reach the Iron Wall completion check, and persistent completion keys could be reported before startup/cloud-merge backfill. Both paths now have regression coverage.

The user explicitly authorized Unity use for this submission. Unity `6000.4.11f1` produced the iOS export, and Xcode `26.6` produced a fresh arm64 archive for `1.2.0 (12000)`. Archive verification confirmed the exact bundle/version/build, production ad configuration, 50 SKAdNetwork identifiers, Game Center plus explicit iCloud entitlements, five privacy manifests, seven exported native bridge symbols, deep code signing, and matching app/UnityFramework dSYM UUIDs.

At 2026-08-25 22:18 KST, Xcode reported `Upload succeeded`, `Uploaded package is processing`, and `EXPORT SUCCEEDED` for the fresh archive. The upload returned exit code `0`. Xcode emitted one non-blocking symbol warning because the Unity installation does not provide a matching `UnityRuntime.framework` dSYM; the app and UnityFramework dSYMs were present and UUID-matched.

## App Store Review submission — 2026-08-26 KST

- App Store Connect exposed processed build `1.2.0 (12000)` as `제출 준비 완료`; no invalid-binary or export-compliance error was present.
- The exact Korean and English (U.S.) version metadata and the complete English App Review notes were saved and reread against the prepared source files.
- Twelve hidden, non-replayable Game Center achievements were created with both localizations and their matching images. The authoritative Game Center list showed `20` achievements and `1,000` points, with all 12 new identifiers in `심사 준비됨` state.
- The selected build was exactly `1.2.0 (12000)`. The archive evidence remained `ITSAppUsesNonExemptEncryption=false`; App Store Connect did not present an additional encryption questionnaire after build selection.
- The release method was explicitly changed to `자동으로 버전 출시` (automatic release after approval), superseding the earlier manual-release preparation instruction for this submission. Immediate, non-phased automatic update release and existing ratings were retained.
- Immediately before submission, the Korean and English metadata matched their JSON sources, the App Review notes matched the Markdown source, build `12000` was selected, automatic release was checked, and Game Center was reverified at 20 achievements / 1,000 points.
- Submitted at `2026-08-26 08:57 KST` by `성치용`.
- Submission ID: `8a2ce9d1-c7fa-4d48-bfc5-b95c6fbcdb96`.
- Submitted components: `13` total — iOS app `1.2.0 (12000)` plus `12` Game Center achievements.
- Authoritative post-submit App Store Connect status: `심사 대기 중` for the submission, the app version, and all 12 achievements.
- Existing territories, price, App Privacy, age rating, DSA/trader state, screenshots, review contact values, and unrelated settings were not changed.

Prepared submission sources:

- `docs/submission/app-store/metadata-1.2.0.ko-KR.json`
- `docs/submission/app-store/metadata-1.2.0.en-US.json`
- `docs/submission/app-store/review-notes-1.2.0.en-US.md`
- `docs/submission/app-store/game-center-achievements-1.2.0.json`
- `docs/submission/app-store/submission-handoff-1.2.0-2026-08-25.md`
