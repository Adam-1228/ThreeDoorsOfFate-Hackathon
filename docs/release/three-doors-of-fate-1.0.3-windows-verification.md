# Three Doors of Fate 1.0.3 Verification Record and Release Handoff

## Purpose and release boundary

This is the authoritative verification record and remaining-release handoff for
the complete Korean/English localization update, including all 72 English
cards. The filename is retained for existing contract links; Windows is now an
optional fallback rather than the required first-import environment.

- Local source/static work targets marketing version `1.0.3`, iOS `build 7`.
- Required Unity Editor version: `6000.4.11f1` exactly.
- Bundle identifier: `com.adam.threedoorsfate`.
- On 2026-08-15 the user temporarily authorized Unity on this Mac for the
  localization task. Unity `6000.4.11f1` import, compile, EditMode, and PlayMode
  validation ran on the Mac. On 2026-08-16 the user separately reauthorized the
  Unity iOS export; Xcode performed the unsigned compile, signed archive, and
  App Store Connect upload. No iOS Simulator or physical-device deployment ran.
- The verified English-card source is
  `ThreeDoors-English-Cards-1.0.2-rev2`, archive SHA-256
  `1616B2D3A2243FAA6C6E8650A74792013C3F210021D30C14CC5BF1F166439F00`.
- Mac-side source/static integration and Unity first import are complete. All
  72 English PNGs have generated `.meta` files, and those 72 files are retained
  with the release workspace.
- The canonical project is outside iCloud at
  `/Users/apple/LocalProjects/ThreeDoorsOfFate-Hackathon`; the original
  `/Users/apple/Documents/game/ThreeDoorsOfFate-Hackathon` path is a symlink to
  it. The pre-move iCloud project remains preserved as a dated backup.
- Pending rows in this document must remain explicitly pending and must not be
  reported as directly verified. The user accepted proceeding without a
  physical-device run; automated tests, representative Mac runtime captures,
  archive inspection, and App Store Connect checks are recorded separately.

## 1. Safe import and compile gate — Mac Pass

1. Keep the canonical project outside an iCloud-managed directory. If Windows
   is used as a fallback, copy or synchronize the complete project there.
2. Open it only with Unity `6000.4.11f1`.
3. Let the initial import finish and wait until the Console stops changing.
4. Record all compile errors and warnings. Any compile error is a release
   blocker; do not work around it by deleting scripts, assembly definitions,
   tests, or localization catalog entries.
5. Confirm `Assets/Resources/Localization/game_text.json` imports as a
   `TextAsset`, and confirm both `ko` and `en` resolve at runtime.
6. Confirm `Assets/Resources/Localization/english_cards.json` imports as a
   `TextAsset` and reports exactly 72 unique cards.
7. Confirm all PNGs under `Assets/Resources/Cards/EnglishLocalized` import as
   single full-rect sprites: 987x1495 source, 2048 max texture size, alpha
   enabled, clamp, bilinear, no mipmaps, and uncompressed.
8. Save and retain every generated `.meta` file for the two new Resources
   paths. Do not replace any existing Korean asset GUID. This is complete for
   all 72 English card PNGs on the Mac workspace.

Expected identity before testing:

| Field | Required value |
| --- | --- |
| Unity | `6000.4.11f1` |
| Product version | `1.0.3` |
| iOS build | `7` (`build 7`) |
| Bundle ID | `com.adam.threedoorsfate` |
| Language preference | `ThreeDoorsOfFate.Language` |
| First-launch default | Korean only on a Korean system; English otherwise |

Mac compile evidence:

- compile/import log:
  `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.3-evidence/compile-cache-flags-repaired.log`;
- result: exit code `0`, Asset Pipeline initial refresh completed in
  `95.048` seconds;
- repaired environment cause: stale macOS `hidden` flags on generated
  Render Pipelines Core and ShaderGraph package-cache subdirectories had kept
  required source files out of the compiler response; after clearing only
  those generated-cache flags and rebuilding the asset index, the previously
  omitted `AdditionalPropertiesState.cs`, `DebugState.cs`, `DebugUIDrawer.cs`,
  and `RenderPipelineEditorUtilityBridge.cs` were present in the response;
- source directories, `Packages`, `ProjectSettings`, `docs`, and `tools` had
  zero iCloud `dataless` placeholders before the successful run.

## 2. Automated Unity test gate

Run the complete Unity Test Framework suites, not only filtered localization
tests. Save both XML result files and Editor logs outside the project `Library`
folder.

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe"
$Project = "C:\path\to\ThreeDoorsOfFate-Hackathon"
$Evidence = "C:\path\to\ThreeDoorsOfFate-1.0.3-evidence"

& $Unity -batchmode -nographics -quit -projectPath $Project `
  -runTests -testPlatform EditMode `
  -testResults "$Evidence\editmode-results.xml" `
  -logFile "$Evidence\editmode.log"

& $Unity -batchmode -nographics -quit -projectPath $Project `
  -runTests -testPlatform PlayMode `
  -testResults "$Evidence\playmode-results.xml" `
  -logFile "$Evidence\playmode.log"
```

Pass requires zero failed, skipped-as-error, or inconclusive tests in both
EditMode and PlayMode. Also preserve the total test counts and the exact Unity
revision printed in the logs.

Mac results on Unity `6000.4.11f1`:

| Suite | Result | Evidence |
| --- | --- | --- |
| Python source/static contract | Pass — 100/100 | Final 2026-08-16 run; includes en-US store metadata and simulated-gambling regression checks |
| Selected UI SFX contract | Pass — 6/6 | `selected-ui-sfx-20260816-final.log` in the evidence directory |
| EditMode | Pass — 145/145, 0 failed, 0 skipped | `editmode-20260816-final-results.xml` and log |
| PlayMode | Pass — 2/2, 0 failed, 0 skipped | `playmode-20260816-final-results.xml` and log |

The first EditMode run exposed four test-isolation failures caused by static
language state leaking between fixtures. The production localization code was
not changed; the affected tests now explicitly establish and restore language
state. The full suites above are the post-fix results.

## 3. Korean / English visual and interaction matrix

Exercise every row first in Korean and then in English at the intended iPhone
landscape aspect ratios. Inspect text clipping, overlap, missing glyphs,
incorrect line wrapping, dead buttons, and untranslated non-card copy. Record a
screenshot or short capture for every row in both languages.

| Surface | Korean checks | English checks |
| --- | --- | --- |
| Main menu | Korean title/buttons; gear + `설정` is clear | English title/buttons; gear + `Settings` is clear |
| Settings | Language, display, sound, save/load, privacy, return/quit fit | Same controls fit; selection state is obvious |
| Class selection/details | Difficulty, class names, lore, traits, actions fit | All non-card descriptions/actions are English |
| Three doors | Subtitle, door labels, prophecy/hints, status action fit | Same content is English and readable |
| Run status | Stats, equipped items, collection, synergy and Korean card list fit | Card names, categories, requirements, metadata and controls are English |
| Combat | HUD, Korean cards, intent, status, feedback, End Turn, logs fit | Hand sprites and card names embedded in logs are English |
| Card preview/use | Korean hover preview and tap/use preview open, close and use correctly | Both preview paths show the matching English rendered card |
| Card/item rewards | Korean cards plus choice/skip/equip/store controls work | English card sprites and all surrounding copy are English |
| Shop/build/synergies | Purchase, sold, build status, upgrade and guide fit | Same non-card controls and explanations are English |
| Event/treasure/rest/price | Headings, choices, rewards and logs fit | Same content is English |
| Achievements | List/progress/detail/close fit | All local achievement copy is English |
| Save/load | Save, continue, unavailable/error feedback works | Same feedback is English |
| Rewarded ad | Availability/loading/error/result states render | Same states/result copy is English |
| Game over | Title, cause, retry/menu controls fit | Same non-card content is English |
| Ten-door/endless/endings | Choice, record, creditor, ending copy fits | Same content is English |
| How to Play | Five existing Korean screenshot pages only | No Korean screenshot is active; English runtime visual and copy appear on all five pages |

No card exception remains. In English mode, every card name, rules surface,
rendered full-card sprite, list/log reference, category, rarity and surrounding
control must be English. Any Korean card text in English mode is a blocker.

Status: Partial. The user accepted the Unity terms. Five 1920x1080 English
runtime captures and fifteen How to Play captures across three target
resolutions were inspected, including the main menu/Settings gear, class
selection, doors, combat, card preview, growth, and all tutorial pages. The
complete row-by-row Korean/English interaction matrix was not executed and is
not claimed as Pass.

## 4. Mid-run language-switch state preservation

Create a run with non-default state, save a before snapshot, switch Korean →
English → Korean from settings, and compare after each switch. Language changes
must re-render text only; they must not rebuild or reset gameplay.

The following values must remain exactly unchanged:

- current/max HP, Block, Action, Luck, Gold, Debt, room and difficulty;
- selected class, current enemy and turn phase;
- deck, hand, draw pile, discard pile, card costs and card IDs;
- equipped/discovered items and class-build upgrade levels;
- completed achievements, ending unlocks and endless record;
- current run save data and save availability.

Close and relaunch once after selecting each language. Confirm the local
preference persists, but `ThreeDoorsOfFate.Language` is not added to Game Center
cloud-progress keys and does not overwrite cloud gameplay data.

## 5. Game Center access-point matrix

Authenticate Game Center and inspect the Apple access point separately from the
game's gear button.

| Surface | Required Game Center state |
| --- | --- |
| Main menu | Visible at top trailing; gear-labelled Settings remains tappable |
| Achievements | Visible; moved close button remains tappable |
| Settings | Hidden |
| How to Play | Hidden |
| Class selection/details | Hidden |
| Doors/run status/combat | Hidden |
| Rewards/shop/build | Hidden |
| Event/treasure/rest/price | Hidden |
| Game over/endings | Hidden |

Return from Settings, How to Play, and Achievements to the main menu and confirm
the access point is restored only where required. Confirm no top-right control
is occluded at supported device safe areas.

## 6. Integrated English card verification — exactly 72 cards

The English-card package is already integrated in source form:

- runtime manifest: `Assets/Resources/Localization/english_cards.json`;
- 72 PNGs: `Assets/Resources/Cards/EnglishLocalized`;
- runtime resolver: `CardLocalization` keyed by stable card ID;
- live image binding: `LocalizedCardSpriteBinding`;
- Korean source data remains unchanged and is the fallback.

The Mac first import and automated resolution checks are complete. Verify the
following automated facts and finish the remaining interactive surfaces:

1. Manifest IDs equal the 72 current `CardData.CardId` values, with no missing,
   duplicate, or extra ID.
2. Every manifest PNG exists, is 987x1495, and matches its manifest SHA-256.
3. English mode resolves all 72 names, all 72 rules strings, and all 72 sprites;
   no missing-catalog or missing-sprite error appears in the Console.
4. Korean mode continues to use the existing `DisplayName`, `RulesText`, and
   `FullCardSprite`. Korean assets and serialized `CardData` values remain
   unchanged.
5. Inspect hand, reward, shop, owned-card/deck list, build requirements,
   discard flow, hover preview, tap/use preview, and use/resolve logs in both
   languages.
6. While a run is active, switch Korean → English → Korean. Existing card
   buttons, previews, `CardView` instances, deck-list names, and log card names
   must refresh without changing card IDs, order, costs, effects, or game state.
7. Confirm these five rev2 corrections contain `Action`, never `Energy`:
   `card_absorb_curse`, `card_small_contract`, `hard_exile_no_return`,
   `hard_gambler_debt_jackpot`, and `hard_skill_door_breath`.
8. The user temporarily authorized the completed Mac Unity import for this
   localization task. Windows remains an optional fallback for visual or
   release verification, not a mandatory first-import gate.

Required evidence:

- generated `.meta` files retained after import;
- Editor import settings capture for one UnifiedRendered-derived and one
  HardRendered-derived English card;
- EditMode/PlayMode XML plus Console log with no localization errors;
- a 72-card automated resolution result;
- captures of every consuming surface above in Korean and English;
- before/after state evidence for the mid-run language round trip.

## 7. iOS archive and release-configuration gate

Create the iOS export/archive only in the authorized release environment and
validate the final archive, not just source constants. Any incomplete manual
matrix row must remain disclosed as incomplete in the release decision.

Archive result on 2026-08-16: Pass for the static release-configuration checks
below. `/Users/apple/LocalProjects/Builds/iOS/ThreeDoorsOfFate.xcarchive` was
created with `ARCHIVE SUCCEEDED`; App Store Connect upload returned `Upload
succeeded` at 2026-08-16 01:58:40 KST. The upload selected the Store
provisioning profile for `com.adam.threedoorsfate`. A non-blocking warning says
the archive lacks the `UnityRuntime.framework` dSYM, limiting symbolication for
crashes inside that framework.

- `CFBundleShortVersionString` is `1.0.3`.
- `CFBundleVersion` is `7`.
- Bundle ID is `com.adam.threedoorsfate`.
- `NSUserTrackingUsageDescription` is absent.
- No ATT authorization request is made at launch or rewarded-ad entry.
- Production AdMob application/ad-unit configuration is present.
- `GADUUnityVersion` and required Google Mobile Ads SKAdNetwork entries are
  present.
- Game Center entitlement and sign-in behavior are intact.
- iCloud container `iCloud.com.adam.threedoorsfate` and save behavior are intact.
- The archived app launches, changes language, starts a run, enters combat,
  opens Settings/Achievements/How to Play, and exercises a rewarded-ad state.

Do not infer an archive Pass from source inspection. Record the archive path,
creation time, configuration, signing identity, validation output, and tester.

## 8. Evidence record

Replace Pending only after direct verification. Do not use “looks correct” as
evidence; cite an XML/log/archive/manifest path or a reproducible observation.

| Gate | Initial status | Required evidence |
| --- | --- | --- |
| Mac source/static localization contract | Pass | 100/100 Python tests on 2026-08-16 |
| Mac rev2 manifest/72 PNG hash contract | Pass | 72 IDs, dimensions and per-image hashes matched the verified manifest |
| Mac Unity import and compile | Pass | `compile-cache-flags-repaired.log`, exit 0, zero compiler errors |
| Windows import and compile | Not required | Optional fallback only; Mac exact-version gate passed |
| EditMode suite | Pass | 145/145; `editmode-20260816-final-results.xml` + log |
| PlayMode suite | Pass | 2/2; `playmode-20260816-final-results.xml` + log |
| Korean screen matrix | Pending | Full per-row captures and notes were not completed |
| English screen matrix | Partial | Five runtime plus fifteen tutorial captures; full per-row matrix remains |
| Mid-run state preservation | Pending | Before/after state dump |
| Game Center visibility/accessibility | Pending | Per-surface observations/captures |
| 72-card English manifest/import | Pass | 72 PNG + 72 generated `.meta`; 72-card EditMode resolution passed |
| Card behavior/regression | Partial | Automated tests pass; consuming-surface captures remain |
| iOS archive `1.0.3` (`build 7`) | Pass | Archive, Info.plist, codesign, and upload evidence |
| ATT absent and production AdMob intact | Pass (archive) | ATT key absent; production app ID and 50 SKAdNetwork IDs verified |
| Game Center/iCloud release behavior | Pass (entitlements) | Signed archive entitlements verified; device behavior not run |

## Completion rule

The current Mac work is **source/static, Unity compile, automated-test, archive,
and App Store upload complete**. The full manual visual/state-preservation
matrix and physical-device behavior remain unverified and are not claimed.
App Store release completion requires a processed build to be selected and the
version to reach a submitted status in App Store Connect.
