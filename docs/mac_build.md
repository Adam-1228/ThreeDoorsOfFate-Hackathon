# macOS Build Notes

This Unity project can build a macOS standalone player from the Unity editor
menu or from a terminal.

For the combined macOS and iOS setup, device export, Simulator launch, and
Windows-to-Mac transfer workflow, see `docs/apple_build.md`.

## Requirements

- Unity Editor `6000.4.11f1`
- macOS standalone build support installed from Unity Hub
- Python 3 for the optional CLI wrapper

## Unity Editor

Open the project folder `ThreeDoorsofFate` in Unity, then run:

```text
Three Doors of Fate > Build macOS Playable
```

The output is written to:

```text
../Builds/macOS/ThreeDoorsOfFate.app
```

## Terminal

From the Unity project root:

```bash
python3 tools/unity_build.py --target macos
```

For a first-time Mac setup that installs the required Unity components and
launches the built game:

```bash
bash tools/mac_setup_and_build.sh macos
```

If Unity is installed somewhere other than the Unity Hub default path, set the
executable explicitly:

```bash
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.4.11f1/Unity.app" \
python3 tools/unity_build.py --target macos
```

The same wrapper also supports the existing targets:

```bash
python3 tools/unity_build.py --target windows
python3 tools/unity_build.py --target android
```

Use `--dry-run` to inspect the Unity command without starting the editor.
