# Three Doors of Fate 1.0.3 App Store submission handoff

Prepared: 2026-08-16 (Asia/Seoul)

## Completion record

- Final App Store Connect status: `심사 대기 중` (`Waiting for Review`)
- Submitted item: iOS `1.0.3`, build `1.0.3 (7)`, one item only
- Submission row time: 2026-08-16 12:46 KST
- Final DOM verification: 2026-08-16 12:47:03 KST
- Build selection was saved and reread as `7 / 1.0.3 / App Clip: No`.
- English (U.S.) name, subtitle, all eight exposed version fields, and Korean/English What's New were saved and reread against the source files.
- English App Review notes were saved at 3,663 characters and reread exactly; Sign-in required remained `No`, and all existing contact values remained unchanged.
- `Simulated Gambling=NONE` and zero App Privacy tracking labels were reconfirmed without editing either declaration.
- The DSA trader declaration and the existing territory result (South Korea available; 174 territories unavailable) were observed read-only and left unchanged.
- Manual release and carried-forward screenshots remained unchanged. No legal, DSA, territory, privacy, age-rating, screenshot, or pricing setting was changed.
- No Resolution Center reply was sent because there was no new build-7-specific message.
- No separate export-compliance prompt appeared; the build continued to show non-exempt encryption `No`.

This is a short, browser-only handoff. Read `/Users/apple/.codex/AGENTS.md` and the complete `chrome:control-chrome` skill before acting. Use Chrome Profile 1 and its existing App Store Connect login. Do not start Unity, Xcode, a simulator, a device install, or another browser runtime.

## Authorization and exact scope

The user has explicitly authorized the following external changes for this release:

- create or use iOS version `1.0.3`;
- select processed build `1.0.3 (7)` only;
- add and save the prepared English (U.S.) App Store localization;
- apply the prepared Korean and English What's New text;
- apply the prepared English App Review notes;
- submit version `1.0.3` for App Review;
- answer an export-compliance prompt only when it exactly asks whether the app uses non-exempt encryption, using `No` because the verified archive has `ITSAppUsesNonExemptEncryption=false`.

If the browser tool requests a confirmation immediately before an external change, obey that confirmation mechanism. Do not expand the scope from this authorization.

Do not change prices, release method, territories, DSA/trader data, banking, tax, agreements, app privacy answers, age-rating answers, content rights, Game Center objects, screenshots, app previews, or any other version. DSA status and current territories may be read only. EU expansion is a separate action after direct DSA approval verification.

## App and verified build

- App Store Connect app ID: `6798086296`
- App name: `Three Doors of Fate`
- Bundle ID: `com.adam.threedoorsfate`
- Version/build: `1.0.3 (7)`
- Archive: `/Users/apple/LocalProjects/Builds/iOS/ThreeDoorsOfFate.xcarchive`
- Upload accepted: 2026-08-16 01:58:40 KST
- Apple/Xcode result: `Upload succeeded`, `Uploaded package is processing`, `EXPORT SUCCEEDED`
- Distribution log selected the Store provisioning profile for `com.adam.threedoorsfate`.
- Non-blocking upload warning: `UnityRuntime.framework` dSYM was absent; this limits symbolication for crashes inside that framework but did not block package upload.
- No iPhone install or execution was performed, by user direction.

Final archive facts already verified locally:

- bundle/version/build match `com.adam.threedoorsfate`, `1.0.3`, `7`;
- production AdMob application ID matches source configuration;
- `NSUserTrackingUsageDescription` is absent;
- `ITSAppUsesNonExemptEncryption=false`;
- 50 SKAdNetwork identifiers;
- Game Center and iCloud CloudDocuments entitlements, including `iCloud.com.adam.threedoorsfate`;
- app/framework/Google Mobile Ads/UMP privacy manifests present.

## Source text — use exactly

- Korean metadata: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/metadata.ko-KR.json`
- English (U.S.) metadata: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/metadata.en-US.json`
- English review notes: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/review-notes.en-US.md`
- Age-rating basis: `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/age-rating.ko-KR.md`

The local validator passed all field limits before handoff. Do not paraphrase or add claims in App Store Connect.

## Required order

1. Before browser setup, verify free-memory state and that no Unity/Xcode/high-load process is running. Treat this handoff as a short session. Use one Chrome runtime and keep no more than three task tabs.
2. Connect only to Chrome Profile 1. Reuse an existing App Store Connect tab if suitable; otherwise open one task tab. Do not inspect cookies, passwords, profile storage, or unrelated tabs.
3. Open app `6798086296` and perform a read-only baseline check. Confirm the current live version and whether iOS version `1.0.3` already exists. If `1.0.3` exists, use it; never create a duplicate version.
4. After the first lightweight page read, check the browser runtime resource limits required by `/Users/apple/.codex/AGENTS.md`. If the Chrome control runtime exceeds its stated CPU/RSS limit, stop and report the first evidence instead of restarting repeatedly.
5. Confirm build `1.0.3 (7)` has finished processing and has no invalid-binary, export-compliance, or processing error. If it is still processing, wait in bounded intervals. Never choose build 5, 6, or another version. If processing does not finish within the no-progress limits, stop with the first status evidence.
6. Read the age-rating detail and verify `Simulated Gambling` / `모의 도박` is `None` / `없음`. Do not save this page. If it is anything else, stop immediately and report it; do not change another rating answer.
7. Read App Privacy and verify there is no tracking display (`Data Used to Track You`, `Tracking`, or equivalent). Do not edit or publish privacy answers. If tracking is shown, stop and report it.
8. Read DSA/trader verification and current territories only. Record the observed status, but do not alter them in this task.
9. Create iOS version `1.0.3` only if absent, preserving manual release and all carried-forward media/settings. If App Store Connect presents a new legal, rights, regulatory, or distribution choice not covered above, stop without guessing.
10. Add or select the English (U.S.) localization. Enter the app-level and version-level fields from `metadata.en-US.json` exactly where App Store Connect exposes them. Save and reread each localization after saving. Do not replace existing Korean metadata with English.
11. On the Korean localization, set only the `whats_new` value from `metadata.ko-KR.json` if required for this new version. Preserve the existing Korean description, keywords, URLs, and media unless the page already carried them forward.
12. Set the English What's New from `metadata.en-US.json`. If English media is optional, leave the existing fallback/carry-forward media unchanged; do not upload or delete screenshots in this task.
13. In App Review Information, preserve the authenticated account's existing real contact fields. Set Sign-in required to `No` and paste the body of `review-notes.en-US.md` exactly. Save, reread the beginning and ending, and confirm there is no truncation.
14. Select processed build `1.0.3 (7)`. If the exact export-compliance question appears, answer only the documented non-exempt-encryption question with `No`; stop on any materially different question. Save and reread the selected build.
15. Confirm immediately before submission: version `1.0.3`; build `1.0.3 (7)`; both Korean and English localizations saved; age-rating simulated gambling remains None; App Privacy still shows no tracking; manual release unchanged; no unintended territory or legal-setting change.
16. Submit for App Review using the user's authorization. If an external-change confirmation is presented, use it. Do not send a Resolution Center reply unless Apple has posted a new message specifically requiring one for build 7.
17. After submission, reread the version/submission page and record the exact displayed status and current KST time. Success requires a submitted status such as `Waiting for Review` / `심사 대기 중`, not merely a successful upload or saved draft.

## Stop conditions

Stop at the first occurrence and do not compensate by changing unrelated settings:

- Chrome Profile 1 is unavailable, logged out, or blocked by 2FA/CAPTCHA requiring the user;
- build `1.0.3 (7)` is absent after processing, invalid, rejected, or has a version/build mismatch;
- `Simulated Gambling` is not None;
- App Privacy shows tracking;
- App Store Connect requires a new agreement, legal declaration, rights document, banking/tax action, GRAC number, DSA/trader mutation, or territory decision;
- required media or metadata cannot be carried forward and the prepared sources do not resolve it;
- a different build/version is preselected and cannot be safely replaced;
- submit produces an unexpected item set or an error.

Report only the first blocking evidence and the next single user action.

## Success report

Report all of the following:

- build `1.0.3 (7)` selected;
- English (U.S.) localization and Korean/English What's New saved;
- App Review notes saved and whether a Resolution Center reply was sent (normally `No`);
- simulated gambling remains None;
- App Privacy has no tracking display;
- DSA/trader status and territories as read-only observations, with no changes;
- exact post-submit App Store Connect status and KST confirmation time;
- any non-blocking warning, including the UnityRuntime dSYM symbolication warning.
