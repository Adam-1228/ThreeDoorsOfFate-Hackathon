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

## Approved achievement recovery

- Permanently reuse achievement ID `com.adam.threedoorsfate.achievement.collection.deck_50`.
- Keep storage suffix `combat.same_reroll_three`, the generated `achievement_same_reroll_three.png` artwork, 15 points, hidden behavior, and non-repeatable behavior.
- Unarchive the old App Store Connect record and replace its reference name and en-US/ko-KR player-facing metadata and image with the corresponding entry in `game-center-achievements-1.3.0.json`.
- Delete the incomplete zero-point replacement draft using ID `com.adam.threedoorsfate.achievement.combat.same_reroll_three`; do not submit it.
- Confirm the visible catalog returns to exactly 20 achievements and the permanent total remains 1,000 points.

The remaining App Store Connect work is a short browser-only task after build `13001` is uploaded and processed. Use Chrome Profile 1 and its existing signed-in App Store Connect tab, or open `https://appstoreconnect.apple.com/apps/6798086296/distribution`. Do not start Unity, Xcode, a simulator, a device installation, another build, or a second browser runtime. Prefer DOM/text inspection and avoid screenshots unless a visual decision is unavoidable.

## Ordered submission procedure

1. Build, archive, validate, and upload exact build `1.3.0 (13001)`; do not reuse build `13000`.
2. Wait until App Store Connect shows processed build `1.3.0 (13001)` with no invalid-binary, compliance, or processing error.
3. Complete and verify the approved achievement recovery above before selecting review items.
4. Create or reuse iOS version 1.3.0, apply both localizations and the exact review notes, retain the existing screenshots, ratings, privacy disclosure, territories, price, and legal settings, and select automatic release.
5. Select only processed build `1.3.0 (13001)` and submit exactly 13 items: the iOS version plus the 12 new-to-players achievements.
6. Re-read the final status and record the submission ID, exact version/build, item count, release method, and KST timestamp.

Stop without guessing if a new agreement, export-compliance question, rights declaration, age-rating choice, privacy choice, or legal setting appears outside this documented scope.

Completion requires the new submission and app version to show `Waiting for Review` / `심사 대기 중` or a later valid review state; an uploaded or saved draft is not sufficient.
