# Three Doors of Fate 1.2.0 verification record

Prepared: 2026-08-25 (Asia/Seoul)

## Release identity

- Marketing version: `1.2.0`
- iOS build number: `12000`
- Bundle identifier: `com.adam.threedoorsfate`
- App Store Connect app ID: `6798086296`
- Source branch: `codex/v1.2.0-achievements`
- Base: `v1.1.2` / `6ec3d89`

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

- Python repository contracts: `107` tests, `0` failures.
- App Store local submission validator: passed.
- Public marketing, support, privacy, and root `app-ads.txt` validation: passed.
- Python `compileall`: passed.
- iOS build/export/upload helper `bash -n`: passed.
- Changed runtime/submission JSON files: `4` parsed successfully.
- `git diff --check`: passed.
- Twelve release achievement PNG pointers: canonical Git LFS pointers; all local objects matched their declared SHA-256 and byte size.

The first full-suite pass in the sparse release clone exposed missing pre-existing asset paths plus one obsolete static marker. Only the required existing App icon, iOS bridge, fonts, localized-card assets, tutorial assets, and data paths were hydrated; the obsolete marker was updated for the localized discovery component. Independent source review then identified two release gaps: non-card Block gains did not all reach the Iron Wall completion check, and persistent completion keys could be reported before startup/cloud-merge backfill. Both paths now have regression coverage. The fresh full rerun above then passed all 107 tests.

Unity EditMode regression tests were added for the centralized Block gate and persistent backfill, but Unity Editor, Unity batch-mode tests, a new Unity iOS export, Xcode archive creation, TestFlight upload, device execution, and App Store review submission were not run on this Mac. Local policy explicitly prohibits Unity use, and the data volume had less than the required 12 GiB of free space during this release pass.

No prior archive or processed build may be relabeled as `1.2.0 (12000)`. A fresh archive built from the `v1.2.0` tag is required.

## App Store submission gate

Review submission is blocked until App Store Connect exposes a processed build whose bundle identifier, marketing version, and build number are exactly:

- `com.adam.threedoorsfate`
- `1.2.0`
- `12000`

Prepared submission sources:

- `docs/submission/app-store/metadata-1.2.0.ko-KR.json`
- `docs/submission/app-store/metadata-1.2.0.en-US.json`
- `docs/submission/app-store/review-notes-1.2.0.en-US.md`
- `docs/submission/app-store/game-center-achievements-1.2.0.json`
- `docs/submission/app-store/submission-handoff-1.2.0-2026-08-25.md`
