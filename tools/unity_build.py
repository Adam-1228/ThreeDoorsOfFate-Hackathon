from __future__ import annotations

import argparse
import os
import platform
import re
import subprocess
import sys
from collections.abc import Sequence
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[1]

BUILD_METHODS = {
    "windows": "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildWindowsPlayable",
    "macos": "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildMacOSPlayable",
    "ios": "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildIOSPlayable",
    "ios-simulator": "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildIOSSimulatorPlayable",
    "android": "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildAndroidLandscape",
}


def read_editor_version(project_root: Path) -> str | None:
    version_file = project_root / "ProjectSettings" / "ProjectVersion.txt"
    if not version_file.exists():
        return None

    match = re.search(r"^m_EditorVersion:\s*(.+)$", version_file.read_text(encoding="utf-8"), re.MULTILINE)
    return match.group(1).strip() if match else None


def unity_candidates(editor_version: str | None) -> list[Path]:
    env_candidates = [
        os.environ.get("UNITY_EDITOR"),
        os.environ.get("UNITY_PATH"),
        os.environ.get("UNITY_EXECUTABLE"),
    ]
    candidates = [Path(value) for value in env_candidates if value]

    system = platform.system()
    if system == "Darwin":
        if editor_version:
            candidates.append(Path("/Applications/Unity/Hub/Editor") / editor_version / "Unity.app/Contents/MacOS/Unity")
        candidates.append(Path("/Applications/Unity/Unity.app/Contents/MacOS/Unity"))
    elif system == "Windows":
        program_files = [
            os.environ.get("ProgramFiles"),
            os.environ.get("ProgramFiles(x86)"),
        ]
        for root in program_files:
            if root and editor_version:
                candidates.append(Path(root) / "Unity/Hub/Editor" / editor_version / "Editor/Unity.exe")
        candidates.append(Path("C:/Program Files/Unity/Hub/Editor") / (editor_version or "") / "Editor/Unity.exe")
    else:
        if editor_version:
            candidates.append(Path("/opt/unity/Hub/Editor") / editor_version / "Editor/Unity")
        candidates.append(Path("/usr/bin/unity-editor"))
        candidates.append(Path("/usr/bin/unityhub"))

    return candidates


def resolve_unity_executable(override: Path | None, editor_version: str | None) -> Path:
    if override is not None:
        override = normalize_unity_path(override)
        if override.exists():
            return override
        raise FileNotFoundError(f"Unity executable not found: {override}")

    for candidate in unity_candidates(editor_version):
        candidate = normalize_unity_path(candidate)
        if candidate.exists():
            return candidate

    searched = "\n".join(f"- {candidate}" for candidate in unity_candidates(editor_version))
    raise FileNotFoundError(
        "Unity executable was not found. Set UNITY_EDITOR or pass --unity.\n"
        f"Searched:\n{searched}"
    )


def normalize_unity_path(path: Path) -> Path:
    if path.suffix == ".app":
        return path / "Contents" / "MacOS" / "Unity"
    return path


def build_command(
    unity: Path,
    project_root: Path,
    target: str,
    log_file: str,
) -> list[str]:
    method = BUILD_METHODS[target]
    command = [
        str(unity),
        "-batchmode",
        "-quit",
        "-nographics",
        "-projectPath",
        str(project_root),
    ]
    if target in {"ios", "ios-simulator"}:
        command.extend(["-buildTarget", "iOS"])
    command.extend([
        "-executeMethod",
        method,
        "-logFile",
        log_file,
    ])
    return command


def parse_args(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Run Three Doors of Fate Unity builds.")
    parser.add_argument("--target", choices=sorted(BUILD_METHODS), default=host_default_target())
    parser.add_argument("--project-root", type=Path, default=PROJECT_ROOT)
    parser.add_argument("--unity", type=Path, default=None, help="Path to the Unity executable.")
    parser.add_argument("--log-file", default="-", help="Unity log path, or '-' for stdout.")
    parser.add_argument("--dry-run", action="store_true", help="Print the command without running Unity.")
    return parser.parse_args(argv)


def host_default_target() -> str:
    system = platform.system()
    if system == "Darwin":
        return "macos"
    if system == "Windows":
        return "windows"
    return "windows"


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(argv)
    project_root = args.project_root.resolve()
    editor_version = read_editor_version(project_root)
    unity = resolve_unity_executable(args.unity, editor_version)
    command = build_command(unity, project_root, args.target, args.log_file)

    if args.dry_run:
        print(" ".join(f'"{part}"' if " " in part else part for part in command))
        return 0

    completed = subprocess.run(command, check=False)
    return completed.returncode


if __name__ == "__main__":
    sys.exit(main())
