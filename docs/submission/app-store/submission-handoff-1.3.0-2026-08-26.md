# Three Doors of Fate 1.3.0 App Store submission handoff

## Authorized scope

The user explicitly authorized building and submitting version `1.3.0 (13000)` for App Review and automatic release after approval. The target app is Apple ID `6798086296`, bundle ID `com.adam.threedoorsfate`.

## Immutable release identity

- Exact version/build: `1.3.0 (13000)`
- Release method: automatic after approval, immediate and non-phased
- Achievement total: 20 achievements and 1,000 points
- Submission items: iOS `1.3.0 (13000)` plus 12 new Game Center achievements

Do not select or relabel a prior build. Do not cancel the current `1.2.0 (12000)` Waiting for Review submission until the fresh `1.3.0 (13000)` build has uploaded, processed successfully, and can be selected. After that proof exists, cancel only the superseded 1.2.0 submission, remove the unreleased `Fifty Fates` achievement from the draft set, create the replacement achievement from `game-center-achievements-1.3.0.json`, and submit the exact 1.3.0 item set.

## Release sources

- Korean metadata: `metadata-1.3.0.ko-KR.json`
- English metadata: `metadata-1.3.0.en-US.json`
- Review notes: `review-notes-1.3.0.en-US.md`
- Game Center manifest: `game-center-achievements-1.3.0.json`

## Verified upload checkpoint — 2026-08-26 KST

- Fresh archive: `/Users/apple/tdof-v120-release-clone/.worktrees/Builds/iOS/ThreeDoorsOfFate.xcarchive`
- Archive identity: bundle ID `com.adam.threedoorsfate`, version `1.3.0`, build `13000`
- Archive checks passed: strict code-sign verification, Game Center entitlement, iCloud containers, 5 privacy manifests, production AdMob app/rewarded IDs, 50 SKAdNetwork identifiers, and 7 required native bridge symbols in the unsigned release product
- App Store Connect upload: `Upload succeeded` at `2026-08-26 12:48:56 KST`; build-upload ID `bafa9635-9aeb-4195-9681-41d0c21c986b`
- Current checkpoint: Apple reported `Uploaded package is processing`; processing completion has not yet been independently confirmed
- Non-blocking upload warning: Apple did not receive a dSYM for Unity's prebuilt `UnityRuntime.framework`; the app binary and symbols upload otherwise succeeded

The remaining work is a short browser-only App Store Connect task. Use Chrome Profile 1 and its existing signed-in App Store Connect tab, or open `https://appstoreconnect.apple.com/apps/6798086296/distribution`. Do not start Unity, Xcode, a simulator, a device installation, another build, or a second browser runtime. Prefer DOM/text inspection and avoid screenshots unless a visual decision is unavoidable.

## Ordered submission procedure

1. Treat the archive, validation, and upload checkpoint above as completed evidence; do not rebuild or re-upload build `13000`.
2. Wait until App Store Connect shows processed build `1.3.0 (13000)` with no invalid-binary, compliance, or processing error.
3. Re-read the current submission. Only after step 2 succeeds, cancel the superseded 1.2.0 Waiting for Review submission.
4. Create or reuse iOS version 1.3.0, apply both localizations and the exact review notes, retain the existing screenshots, ratings, privacy disclosure, territories, price, and legal settings, and select automatic release.
5. Replace only the unapproved `Fifty Fates` draft achievement with `What Did Rerolling Change?`; keep all other identifiers, points, localizations, and artwork mappings exact.
6. Select only processed build `1.3.0 (13000)` and submit exactly 13 items: the iOS version plus the 12 new achievements.
7. Re-read the final status and record the submission ID, exact version/build, item count, release method, and KST timestamp.

Stop without guessing if a new agreement, export-compliance question, rights declaration, age-rating choice, privacy choice, or legal setting appears outside this documented scope.
