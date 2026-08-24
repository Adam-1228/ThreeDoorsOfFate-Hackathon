# Three Doors of Fate 1.0.4 App Store submission handoff

Prepared: 2026-08-17 (Asia/Seoul)

## Completion record

- Final App Store Connect status: `심사 대기 중` (`Waiting for Review`)
- Submitted item: iOS `1.0.4`, build `1.0.4 (8)`, one item only
- Final submit click: 2026-08-17 19:20:59.697 KST
- Final DOM verification: 2026-08-17 19:21:29.581 KST
- Korean and English version metadata and exact What's New values were saved and reread.
- English App Review notes were saved at 3,986 characters and reread exactly; existing contact values and Sign-in required `No` were preserved.
- Simulated Gambling remained `없음`, App Privacy showed no tracking, DSA remained active, 29 territories remained available, and manual release remained selected.
- Resolution Center reply: none; no new build-8-specific message required one.
- No release, territory, DSA, privacy, rating, price, legal, screenshot, or unrelated setting change was made.

This is a short, browser-only handoff. Read `/Users/apple/.codex/AGENTS.md` and the complete `chrome:control-chrome` skill before acting. Use Chrome Profile 1 and its existing App Store Connect login. Do not start Unity, Xcode, a simulator, a device install, another browser runtime, or any high-load process.

## Authorization and exact scope

The user explicitly authorized completing Three Doors of Fate version `1.0.4`, including upload and App Review submission. The following external changes are in scope:

- create or use iOS version `1.0.4`;
- select processed build `1.0.4 (8)` only;
- save the prepared Korean and English (U.S.) version metadata, including both exact What's New values;
- save the prepared English App Review notes while preserving the authenticated account's existing real review-contact fields;
- add version `1.0.4` to review and submit it to Apple;
- answer an export-compliance prompt only when it exactly asks whether the app uses non-exempt encryption, using `No` because the verified archive has `ITSAppUsesNonExemptEncryption=false`.

If the browser tool requires confirmation immediately before an external change, obey that mechanism. Do not expand this authorization.

Do not release the version after approval. Do not change price, release method, territories, DSA/trader data, agreements, banking, tax, App Privacy answers, age-rating answers, content rights, Game Center objects, screenshots, app previews, phased release, pre-order state, or any other version. All existing 29 territories, manual release, DSA status, privacy disclosure, media, and legal settings must be preserved.

## App and verified build

- App Store Connect app ID: `6798086296`
- App name: `Three Doors of Fate`
- Bundle ID: `com.adam.threedoorsfate`
- Version/build: `1.0.4 (8)`
- Archive: `/Users/apple/LocalProjects/Builds/iOS/ThreeDoorsOfFate.xcarchive`
- Upload accepted: 2026-08-17 18:56:09 KST
- Apple/Xcode result: `Upload succeeded`, `Uploaded package is processing`, `EXPORT SUCCEEDED`, `TESTFLIGHT_UPLOAD_SUCCEEDED`
- Upload log: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.4-evidence/testflight-upload.log`
- Non-blocking upload warning: the archive lacked a dSYM for `UnityRuntime.framework` UUID `9F9CBA44-B75B-3EEC-B78C-E54B0AB11BAF`. This can limit symbolication for crashes inside that framework but did not block upload.
- No iPhone install or device execution was performed.

Final archive facts already verified locally:

- bundle/version/build match `com.adam.threedoorsfate`, `1.0.4`, `8`;
- production AdMob application ID is present and matches source configuration;
- `NSUserTrackingUsageDescription` is absent;
- `ITSAppUsesNonExemptEncryption=false`;
- 50 SKAdNetwork identifiers;
- Game Center and iCloud CloudDocuments entitlements, including `iCloud.com.adam.threedoorsfate`;
- app, UnityFramework, UnityRuntime, Google Mobile Ads, and UMP privacy manifests present;
- app and UnityFramework dSYMs are present and their UUIDs match their binaries;
- deep/strict code-sign verification passed.

## Source text — use exactly

- Korean metadata: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/metadata-1.0.4.ko-KR.json`
- English (U.S.) metadata: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/metadata-1.0.4.en-US.json`
- English review notes: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/review-notes-1.0.4.en-US.md`
- Age-rating basis: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/age-rating.ko-KR.md`
- Local release verification: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/release/three-doors-of-fate-1.0.4-verification.md`

The local validators and Unity tests passed. The final review-notes source is 3,986 characters (3,986 UTF-16 code units), below App Store Connect's 4,000-character limit. Do not paraphrase or add claims in App Store Connect.

Exact What's New values:

- ko-KR: `영어 모드의 카드 조합과 패배 화면 번역을 보완했습니다. 전투 HUD와 휴식·이벤트 선택 정보를 더 읽기 쉽게 정리하고, 손패 순환 연습, 다양한 문 선택, 보물 카드 미리보기, 업적 페이지와 설정 버튼 가독성을 개선했습니다.`
- en-US: `Completed the remaining English localization for card synergies and defeat screens. We also made combat and decision information easier to read, added a hand-flow practice, improved door variety and treasure previews, and refreshed achievements and the Settings control.`

## Expected read-only baseline

These values were last confirmed during the 1.0.3 release workflow and must be re-read, not assumed or edited:

- current public version: iOS `1.0.3`, build `7`, Ready for Distribution;
- availability: 29 countries or regions — South Korea, all EU 27, and the United States;
- DSA: active for the EU 27;
- release method: manual;
- age-rating detail: Simulated Gambling / 모의 도박 = None / 없음;
- App Privacy: no tracking display;
- no active build-specific Resolution Center message is expected.

## Required order

1. Before browser setup, verify memory state and confirm no Unity, Xcode, simulator, device deployment, or other high-load process is running. Treat this handoff as a short session. Use one Chrome runtime and no more than three task tabs.
2. Connect only to Chrome Profile 1. Reuse a suitable existing App Store Connect tab when possible. Do not inspect cookies, passwords, profile storage, or unrelated tabs.
3. Open app `6798086296` and perform a read-only baseline check. Confirm the public version/build, 29-country availability, DSA active state, and manual release. Never modify those settings.
4. After the first lightweight page read, perform the browser-runtime CPU/RSS audit required by `/Users/apple/.codex/AGENTS.md`. Stop on its stated limit rather than repeatedly restarting.
5. Confirm build `1.0.4 (8)` finished processing and has no invalid-binary, export-compliance, or processing error. Wait only in bounded intervals. Never select build 7 or another build. If processing makes no progress within the global limits, stop with the first status evidence.
6. Read age-rating detail and verify Simulated Gambling / 모의 도박 remains None / 없음. Do not save. Stop if it differs.
7. Read App Privacy and verify no tracking display (`Data Used to Track You`, `Tracking`, or equivalent). Do not edit or publish. Stop if tracking is shown.
8. Read DSA and territories only, verifying DSA active and exactly 29 available countries/regions (KR + EU27 + US). Do not change them.
9. Create iOS version `1.0.4` only if absent; if present, reuse it. Preserve manual release and all carried-forward screenshots/media/settings. Stop on any new legal, rights, regulatory, or distribution choice outside the exact export-compliance case above.
10. Verify app-level Korean and English names/subtitles/URLs already match the source JSON. Change only a differing field that is explicitly present in the source, then save and reread it. Never replace the Korean localization with English.
11. On the Korean version localization, enter the exact `whats_new` value. Preserve carried-forward screenshots. Only fill other exposed version fields from the ko-KR JSON if App Store Connect requires them or they differ; never paraphrase.
12. On English (U.S.), enter the exact version fields and exact `whats_new` from the en-US JSON. Preserve carried-forward/fallback screenshots; do not upload or delete media.
13. Save each changed localization separately and reread all saved values against the JSON. Confirm no truncation or validation error.
14. In App Review Information, preserve all existing authenticated-account contact values exactly. Keep Sign-in required `No`. Paste the complete body of `review-notes-1.0.4.en-US.md` exactly, save, and reread its beginning/end and character count. Never invent a contact value.
15. Select only processed build `1.0.4 (8)`. If the exact non-exempt-encryption prompt appears, answer `No`; stop on any materially different question. Save and reread the selected row/version/build.
16. Immediately before submission, verify version `1.0.4`, build `1.0.4 (8)`, both localizations, exact What's New, review notes, Simulated Gambling=None, no App Privacy tracking, manual release, 29 territories, and an item set containing only iOS `1.0.4 (8)`.
17. Add for Review and submit using the user's authorization and any browser confirmation mechanism. Do not send a Resolution Center reply unless Apple posted a new message specifically requiring one for build 8; if such a message exists, stop and report it instead of guessing a response.
18. Reread the version/submission page and record the exact status plus current KST timestamp. Success requires a submitted status such as Waiting for Review / 심사 대기 중, not merely an upload or saved draft.

## Stop conditions

Stop at the first occurrence and do not compensate by changing unrelated settings:

- Chrome Profile 1 is unavailable, logged out, or blocked by 2FA/CAPTCHA requiring the user;
- build `1.0.4 (8)` is absent after bounded processing, invalid, rejected, or mismatched;
- Simulated Gambling is not None;
- App Privacy shows tracking;
- availability is not exactly the existing 29 countries/regions, DSA is not active, or manual release is not retained;
- App Store Connect requires a new agreement, legal declaration, rights document, banking/tax action, GRAC number, DSA/trader mutation, territory decision, or a materially different export-compliance answer;
- required media/metadata cannot be carried forward and the prepared source does not resolve it;
- a different build/version is preselected and cannot be safely replaced;
- submit presents an unexpected item set or error;
- resource or browser-runtime safety thresholds are exceeded.

Report only the first blocking evidence and the next single user action.

## Success report

Report all of the following:

- build `1.0.4 (8)` selected and saved;
- Korean and English (U.S.) localizations, exact What's New, and App Review notes saved;
- whether any Resolution Center reply was sent (normally `No`);
- Simulated Gambling remains None and App Privacy has no tracking display;
- DSA active, exactly 29 territories, and manual release observed read-only with no changes;
- exact post-submit App Store Connect status and KST verification time;
- non-blocking UnityRuntime dSYM warning.
