# Three Doors of Fate 1.2.0 App Store submission handoff

Prepared: 2026-08-25 (Asia/Seoul)

## Authorization and target

The user explicitly authorized creating and submitting version `1.2.0` for App Review.

- App: `Three Doors of Fate`
- App Store Connect app ID: `6798086296`
- Bundle ID: `com.adam.threedoorsfate`
- Exact version/build: `1.2.0 (12000)`
- Release method to preserve: manual
- Existing availability to preserve: 29 territories (South Korea, EU 27, and United States)

Do not select or relabel a prior build. Submission requires the newly uploaded build reporting exactly `1.2.0 (12000)`. Runtime source is pinned by `v1.2.0`; submission build hardening is pinned by `v1.2.0+build.12000`.

## Prepared sources

- Korean metadata: `metadata-1.2.0.ko-KR.json`
- English (U.S.) metadata: `metadata-1.2.0.en-US.json`
- English review notes: `review-notes-1.2.0.en-US.md`
- 12 new Game Center achievements: `game-center-achievements-1.2.0.json`
- Local verification boundary: `../../release/three-doors-of-fate-1.2.0-verification.md`

Use these values exactly. Do not paraphrase the What's New text or review notes.

## Required order

1. Build and archive `v1.2.0` on an authorized Unity build machine, then verify bundle ID `com.adam.threedoorsfate`, version `1.2.0`, and build `12000` from the archive itself.
2. Verify signing, export compliance, privacy manifests, entitlements, production ad configuration, dSYMs, and deep code signing before upload.
3. Upload the archive and wait until App Store Connect shows processed build `1.2.0 (12000)` with no invalid-binary or compliance error.
4. Re-read the current App Store baseline. Preserve manual release, 29 territories, DSA/trader state, App Privacy, age rating, screenshots, pricing, agreements, and review contact values.
5. Create or reuse iOS version `1.2.0`, then save and reread the exact Korean and English metadata.
6. Create the 12 new hidden, non-replayable Game Center achievements from the manifest. Upload each matching image and verify the final total is 20 achievements and 1,000 points.
7. Paste the complete English review notes and preserve Sign-in required `No` plus the authenticated account's existing real contact details.
8. Select only processed build `1.2.0 (12000)`. If asked whether the verified archive uses non-exempt encryption, answer from that archive's actual plist value rather than assuming.
9. Immediately before submission, verify the exact version/build, both localizations, Game Center item set, manual release, 29 territories, privacy/rating state, and review notes.
10. Add the version and required Game Center components for review, submit, then reread the resulting status. Completion requires `Waiting for Review` / `심사 대기 중` or a later valid review state.

## Stop conditions

Stop without changing unrelated settings if the exact build is absent, processing fails, points would exceed 1,000, an achievement identifier already exists with conflicting immutable fields, the submission contains unexpected items, or App Store Connect asks for a new legal/privacy/rating/territory decision not covered by the prepared evidence.

## Completed local and transport stages

- Fresh Unity EditMode result: `285/285` passed.
- Fresh Python contracts: `108/108` passed.
- Signed arm64 archive: `/Users/apple/Builds/iOS/ThreeDoorsOfFate.xcarchive`.
- Archive contract, entitlements, privacy manifests, production ads, native bridge symbols, and app/UnityFramework dSYM UUIDs: verified.
- App Store Connect upload at 2026-08-25 22:18 KST: `Upload succeeded`; package reported as processing; exit code `0`.
- Non-blocking Xcode warning: the installed Unity package did not include a matching `UnityRuntime.framework` dSYM. App and UnityFramework dSYMs are present and UUID-matched.

## Remaining blocker

App Store Connect must finish processing build `1.2.0 (12000)` before it can be selected. App Store Connect API credentials are not present, and this long image-heavy task must not start a new browser-control runtime. Final metadata/Game Center setup and the review-submission click therefore belong in a short dedicated task after the processed build is visible. Completion still requires `Waiting for Review` / `심사 대기 중` or a later valid review state.
