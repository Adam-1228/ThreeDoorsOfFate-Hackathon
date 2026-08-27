# Three Doors of Fate 1.4.0 verification record

Prepared: 2026-08-27 (Asia/Seoul)

## Release identity

- Marketing version: `1.4.0`
- iOS build number: `14000`
- Bundle identifier: `com.adam.threedoorsfate`
- Working branch: `codex/v1.4.0-fate-rework`
- Verified correction commit: `efbfbdb`
- Submission approval in both metadata files: `approved_for_submission: false`
- Game Center contract remains unchanged at 20 achievements / 1,000 points.

## Final automated verification

The following commands were run from the isolated v1.4.0 worktree after the
final runtime and test corrections:

```bash
python3 -m unittest discover -s tools/tests -p 'test_*.py'
```

Result: `134` tests run, `134` passed, `0` failed.

```bash
/Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$PWD" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/tdof-v140-editmode-final2.xml \
  -logFile /tmp/tdof-v140-editmode-final2.log
```

Result: `351` tests run, `351` passed, `0` failed.

```bash
/Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath "$PWD" \
  -runTests -testPlatform PlayMode \
  -testResults /tmp/tdof-v140-playmode-final.xml \
  -logFile /tmp/tdof-v140-playmode-final.log
```

Result: `6` tests run, `6` passed, `0` failed.

```bash
python3 tools/validate_app_store_submission.py --root "$PWD" --local-only
```

Result: local submission assets and App Store submission documents passed.
Exact local probes also confirmed `bundleVersion: 1.4.0`, iPhone build number
`14000`, `IOSReleaseConfiguration` defaults, and upload-helper defaults.
`git diff --check` passed.

## Save migration verification

The checkpoint suite now includes a complete disposable-namespace migration
path in addition to the existing v1 DTO and v2 round-trip tests:

1. A valid v1 Hard save is written under a unique test-only PlayerPrefs prefix.
2. It is migrated and restored without losing the two-card fixture deck.
3. The run continues to Door Selection and deterministically generates three
   pending doors.
4. The resulting v2 checkpoint is automatically persisted under the same
   disposable key.
5. Restoring the v2 checkpoint preserves the pending door IDs and the next RNG
   result exactly.
6. A completed-achievement key under the disposable prefix remains intact, and
   the temporary v1 backup is removed only after successful migration.

Focused result:
`Checkpoint140Tests.V1SaveContinuesToDoorsAndPersistsChoicesWithoutTouchingMeta`
passed `1/1`. The complete EditMode result above includes all seven checkpoint
test cases, including deck/item migration, unknown-card backup preservation,
pending choice/RNG round-trip, and Easy/Normal/Hard save availability.

## Visual QA

The release matrix used three landscape layouts:

- `16x9`: 1920×1080
- `iphone14_pro_max_landscape`: 2796×1290 with simulated safe-area insets
- `4x3`: 2048×1536

The complete bilingual matrix contains 132 rendered state/layout images
(`22` states × `2` languages × `3` layouts) plus visible-text audits:

- Artifact directory: `/tmp/tdof-v140-qa-verified.wK1SXl`
- Manifest: `/tmp/tdof-v140-qa-verified.wK1SXl/capture_manifest.csv`

Covered states include the main menu, starter contracts, five tutorial pages,
tutorial hand flow, normal and forced doors, run status, combat, shop, rest,
event, treasure, synergy guide, two achievement pages, game over, run history,
and run-history detail.

The complete matrix exposed two history layouts whose normalized transforms
were technically inside the outer object but visually overlapped the ornate
sprite borders. Those two images were rejected. The root cause was using the
large decorative frame itself as the content coordinate system. The final
implementation adds masked safe-area roots and reuses the existing thin status
content frame for both detail columns.

The corrected history and game-over surfaces were then recaptured in a narrow
final matrix:

```bash
TDOF_140_QA_DIR=/tmp/tdof-v140-history-qa-valid.skKKuA \
/Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "$PWD" \
  -executeMethod ThreeDoorsOfFate.Editor.Quality104QACapture.CaptureHistoryMatrix \
  -logFile /tmp/tdof-v140-history-qa-valid.log
```

- Artifact directory: `/tmp/tdof-v140-history-qa-valid.skKKuA`
- Manifest: `/tmp/tdof-v140-history-qa-valid.skKKuA/capture_manifest.csv`
- Result: `18` valid PNGs (`3` states × `2` languages × `3` layouts)
- Run-history list text remains inside the outer decorative safe area.
- Run-history summary and loadout text remain inside their thin inner frames.
- Korean and English long-form detail text is visible without frame overlap or
  truncation at all three layouts.
- The game-over center message and lower run summary remain separated at all
  three layouts.
- English visible-text audits across both evidence sets contain `0` Hangul
  matches.

A diagnostic capture attempted with `-nographics` produced
`RenderTexture.Create failed` and blank gray PNGs. It was rejected before
inspection and is not part of the evidence above. The valid visual command does
not use `-nographics`.

Other visual corrections proven by the matrix and integration tests:

- Starter-contract name, role, description, and trade-off text use the actual
  safe interior of the tall decorative frame.
- English status identities and encounter behavior logs resolve localized class
  roles and enemy names rather than leaking Korean source strings.
- QA state transitions immediately remove the run-status modal, refresh active
  localized bindings, and keep QA run history in a disposable PlayerPrefs key.

## Resource gates and verification boundary

- System-wide free-memory readings before Unity operations stayed between
  `58%` and `72%`, above the required `25%` floor.
- No competing Unity Editor, `xcodebuild`, or Simulator workload was running
  when a Unity operation started.
- Final filesystem reading: `4.9 GiB` free on `/System/Volumes/Data`.
- Because `4.9 GiB` is below the required `12 GiB` floor, no Unity iOS export,
  Xcode archive, signing, device install, or upload was attempted.

This record verifies source, runtime tests, local release contracts, migration,
and desktop-rendered UI only. It does not claim a fresh iOS binary or physical
device pass.

## Deferred external actions

App Store submission has not been performed for 1.4.0. No TestFlight upload,
App Store review request, Git tag, GitHub release, archive, or release-branch
push was created as part of this work. Version 1.4.0 remains a locally verified
release candidate while 1.3.0 is given the intended live observation period and
until the user provides a separate submission decision.
