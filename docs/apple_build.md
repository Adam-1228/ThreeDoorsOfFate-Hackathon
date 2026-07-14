# Apple Build and Transfer Guide

This project supports a macOS player, an iOS device Xcode export, and a fully
terminal-driven iOS Simulator launch.

## What the setup script installs

- Unity CLI, when it is missing.
- Unity Editor `6000.4.11f1`, when it is missing.
- The Unity iOS Build Support module.

Xcode must come from Apple. If it is unavailable, the script opens its Mac App
Store page and stops so installation and Apple account approval can finish.

## First command on the Mac

Extract the transfer archive, open Terminal, and run this from the Unity project
root:

```bash
bash tools/mac_setup_and_build.sh all
```

This builds and opens the macOS game, then exports and opens the iOS device
Xcode project.

Outputs:

```text
../Builds/macOS/ThreeDoorsOfFate.app
../Builds/iOS/Device/Unity-iPhone.xcodeproj
```

## Run in iOS Simulator

This path does not require an Apple development team or code signing:

```bash
bash tools/mac_setup_and_build.sh ios-simulator
```

The script generates the simulator Xcode project, compiles it with Xcode,
boots an available iPhone Simulator, installs the app, and launches it.

## Run on an iPhone or iPad

If the Apple Developer Team ID is known, pass it before the export:

```bash
UNITY_IOS_DEVELOPMENT_TEAM="YOUR_TEAM_ID" \
bash tools/mac_setup_and_build.sh ios
```

In Xcode, choose the `Unity-iPhone` target, confirm the Team under Signing &
Capabilities, select the connected device, and press Run. The bundle identifier
is `com.adam.threedoorsfate` and the minimum OS is iOS 15.0.

## Individual commands

```bash
bash tools/mac_setup_and_build.sh setup
bash tools/mac_setup_and_build.sh macos
bash tools/mac_setup_and_build.sh ios
bash tools/mac_setup_and_build.sh ios-simulator
```

The lower-level build wrapper remains available:

```bash
python3 tools/unity_build.py --target macos
python3 tools/unity_build.py --target ios
python3 tools/unity_build.py --target ios-simulator
```

## Create a fresh transfer archive on Windows

From the Unity project root in PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File tools/create_mac_transfer_package.ps1
```

The archive and its SHA-256 file are written to `../Builds/Transfer`. Unity
caches, generated IDE files, local builds, previews, backups, and Windows-only
installers are excluded. Game assets, packages, project settings, build tools,
tests, documentation, and the project continuity ledger are included.
