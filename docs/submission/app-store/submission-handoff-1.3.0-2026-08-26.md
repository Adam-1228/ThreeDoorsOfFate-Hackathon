# Three Doors of Fate 1.3.0 App Store submission handoff

## Authorized scope

The user explicitly authorized building and submitting version `1.3.0 (13001)` for App Review and automatic release after approval. The target app is Apple ID `6798086296`, bundle ID `com.adam.threedoorsfate`.

## Immutable release identity

- Exact version/build: `1.3.0 (13001)`
- Release method: automatic after approval, immediate and non-phased
- Achievement total: 20 achievements and 1,000 points
- Submission items: iOS `1.3.0 (13001)` plus 12 new-to-players Game Center achievements

Do not select or relabel build `13000`; it reports a replacement Game Center ID that App Store Connect cannot accept within the permanent 1,000-point limit. Build `13001` reuses the unreleased `Fifty Fates` permanent ID while retaining the new reroll achievement's player-facing title, descriptions, image, hidden behavior, and 15 points. The superseded `1.2.0 (12000)` review submission has already been canceled by the developer.

## Release sources

- Korean metadata: `metadata-1.3.0.ko-KR.json`
- English metadata: `metadata-1.3.0.en-US.json`
- Review notes: `review-notes-1.3.0.en-US.md`
- Game Center manifest: `game-center-achievements-1.3.0.json`

## Superseded upload checkpoint — 2026-08-26 KST

- Superseded archive: `/Users/apple/tdof-v120-release-clone/.worktrees/Builds/iOS/ThreeDoorsOfFate.xcarchive`
- Superseded archive identity: bundle ID `com.adam.threedoorsfate`, version `1.3.0`, build `13000`
- Archive checks passed: strict code-sign verification, Game Center entitlement, iCloud containers, 5 privacy manifests, production AdMob app/rewarded IDs, 50 SKAdNetwork identifiers, and 7 required native bridge symbols in the unsigned release product
- App Store Connect upload: `Upload succeeded` at `2026-08-26 12:48:56 KST`; build-upload ID `bafa9635-9aeb-4195-9681-41d0c21c986b`
- Apple later exposed build `13000` as processed and validated, but it must not be selected because its Game Center ID no longer matches the approved permanent-ID recovery plan
- Non-blocking upload warning: Apple did not receive a dSYM for Unity's prebuilt `UnityRuntime.framework`; the app binary and symbols upload otherwise succeeded

## Final build and review checkpoint — 2026-08-26 KST

- Final archive identity: bundle ID `com.adam.threedoorsfate`, version `1.3.0`, build `13001`, minimum iOS `15.0`
- Final archive checks passed: strict code-sign verification, Game Center entitlement, iCloud and ubiquity containers, 5 privacy manifests, production AdMob app/rewarded IDs, and 50 SKAdNetwork identifiers
- App Store Connect upload completed at `2026-08-26 16:32:47 KST`; build-upload ID `ecad7e5d-cfa0-4f4a-af68-3861c89bc309`
- Apple exposed build `1.3.0 (13001)` as processed, validated, and `Ready to Submit`; builds `12000` and `13000` were not selected
- One accidental partial two-item submission, ID `925cfd96-1378-4bec-9bfa-0ca4f2f8292a`, was immediately canceled and reached the final `Deleted` state before the review set was rebuilt
- Final App Review submission ID: `83ac4a1d-2f70-43fb-8ead-8f930ed2494c`
- Final submission state: `Waiting for Review` / `심사 대기 중` at `2026-08-26 18:10 KST`
- Final submitted set: exactly 13 items — iOS `1.3.0 (13001)` plus the 12 new-to-players Game Center achievements
- Release remains automatic after approval, immediate, and non-phased
- Non-blocking upload warning: Apple did not receive a dSYM for Unity's prebuilt `UnityRuntime.framework`; the app binary and other symbols uploaded successfully

## Completed achievement recovery

- Permanently reuse achievement ID `com.adam.threedoorsfate.achievement.collection.deck_50`.
- Keep storage suffix `combat.same_reroll_three`, the generated `achievement_same_reroll_three.png` artwork, 15 points, hidden behavior, and non-repeatable behavior.
- The old App Store Connect record was unarchived and its en-US/ko-KR player-facing titles, descriptions, image, hidden setting, and replayability were updated from `game-center-achievements-1.3.0.json`.
- The internal-only App Store Connect identification label remains `Fifty Fates` because its detail form did not become editable during the submission window. This does not alter the permanent achievement ID or the new player-facing metadata.
- The incomplete zero-point replacement draft using ID `com.adam.threedoorsfate.achievement.combat.same_reroll_three` was absent and was not submitted.
- The visible catalog was verified at exactly 20 achievements and the permanent total at 1,000 points.

## Completed submission procedure

1. Built, archived, validated, and uploaded exact build `1.3.0 (13001)` without reusing build `13000`.
2. Verified App Store Connect processing completed with no invalid-binary, compliance, or processing error.
3. Completed the permanent-ID achievement recovery and verified 20 achievements / 1,000 points.
4. Applied both localizations and the exact review notes while retaining existing screenshots, ratings, privacy disclosure, territories, price, legal settings, and automatic release.
5. Selected only processed build `1.3.0 (13001)` and submitted exactly 13 items.
6. Re-read and recorded the final `Waiting for Review` state, submission ID, version/build, item count, release method, and KST timestamp.

Stop without guessing if a new agreement, export-compliance question, rights declaration, age-rating choice, privacy choice, or legal setting appears outside this documented scope.

Completion was independently confirmed at `Waiting for Review` / `심사 대기 중`; the uploaded build or a saved draft alone was not treated as completion.
