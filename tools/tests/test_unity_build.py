from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "unity_build.py"
SPEC = importlib.util.spec_from_file_location("unity_build", MODULE_PATH)
if SPEC is None or SPEC.loader is None:
    raise RuntimeError(f"Unable to load build helper: {MODULE_PATH}")

unity_build = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(unity_build)


class UnityBuildTargetTests(unittest.TestCase):
    def test_ios_device_target_uses_device_builder(self) -> None:
        self.assertEqual(
            unity_build.BUILD_METHODS["ios"],
            "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildIOSPlayable",
        )

    def test_ios_simulator_target_uses_simulator_builder(self) -> None:
        self.assertEqual(
            unity_build.BUILD_METHODS["ios-simulator"],
            "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildIOSSimulatorPlayable",
        )

    def test_ios_command_selects_the_requested_builder(self) -> None:
        command = unity_build.build_command(
            Path("/Applications/Unity.app/Contents/MacOS/Unity"),
            Path("/tmp/ThreeDoorsofFate"),
            "ios-simulator",
            "-",
        )

        method_index = command.index("-executeMethod") + 1
        self.assertEqual(
            command[method_index],
            "ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildIOSSimulatorPlayable",
        )


if __name__ == "__main__":
    unittest.main()
