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

## Ordered submission procedure

1. Build a fresh archive from the verified `v1.3.0` source and confirm the archive reports bundle ID `com.adam.threedoorsfate`, version `1.3.0`, and build `13000`.
2. Validate signing, entitlements, privacy manifests, exported native bridges, dSYMs, production rewarded-ad configuration, and `ITSAppUsesNonExemptEncryption=false` from the archive itself.
3. Upload the archive and wait until App Store Connect shows processed build `1.3.0 (13000)` with no invalid-binary, compliance, or processing error.
4. Re-read the current submission. Only after step 3 succeeds, cancel the superseded 1.2.0 Waiting for Review submission.
5. Create or reuse iOS version 1.3.0, apply both localizations and the exact review notes, retain the existing screenshots, ratings, privacy disclosure, territories, price, and legal settings, and select automatic release.
6. Replace only the unapproved `Fifty Fates` draft achievement with `What Did Rerolling Change?`; keep all other identifiers, points, localizations, and artwork mappings exact.
7. Select only processed build `1.3.0 (13000)` and submit exactly 13 items: the iOS version plus the 12 new achievements.
8. Re-read the final status and record the submission ID, exact version/build, item count, release method, and KST timestamp.

Stop without guessing if a new agreement, export-compliance question, rights declaration, age-rating choice, privacy choice, or legal setting appears outside this documented scope.
