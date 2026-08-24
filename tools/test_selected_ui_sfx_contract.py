from __future__ import annotations

import hashlib
import json
import re
import unittest
import wave
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[1]
LISTENING_ROOT = Path(
    "/Users/apple/Documents/game/Builds/AudioCandidates/ui-selection-confirm-20260805"
)
SELECTED = {
    "G3": {
        "destination": "Assets/Audio/SFX/SelectedUI/ui_general_select_g3.wav",
        "guid": "b627aec070784741b2a46fee20f8fb19",
        "hash": "f5ca181e3cb3a2da1f2ef561df8eb7b9703ded83218d9a09a698ff2904f78f55",
        "sceneField": "selectedGeneralUiSfxClip",
    },
    "C1": {
        "destination": "Assets/Audio/SFX/SelectedUI/ui_important_confirm_c1.wav",
        "guid": "b68c9f61860d44ae97a81d08c1d8de41",
        "hash": "d58d717f31d42d9855e92c47daaebd393b76e82fbdfba66ceac7ab3b7eb6c1d6",
        "sceneField": "selectedImportantConfirmSfxClip",
    },
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class SelectedUiSfxContractTests(unittest.TestCase):
    def test_selection_record_authorizes_exactly_g3_and_c1(self) -> None:
        selection = json.loads(
            (LISTENING_ROOT / "selection.json").read_text(encoding="utf-8")
        )

        self.assertEqual("G3", selection["selected"]["general"])
        self.assertEqual("C1", selection["selected"]["confirm"])
        self.assertTrue(selection["integrationAuthorized"])

    def test_selected_assets_match_approved_hash_and_wav_contract(self) -> None:
        for identifier, contract in SELECTED.items():
            with self.subTest(identifier=identifier):
                destination = PROJECT / contract["destination"]
                self.assertTrue(destination.is_file(), destination)
                self.assertEqual(contract["hash"], sha256(destination))
                with wave.open(str(destination), "rb") as audio:
                    self.assertEqual(1, audio.getnchannels())
                    self.assertEqual(2, audio.getsampwidth())
                    self.assertEqual(48000, audio.getframerate())
                    self.assertEqual("NONE", audio.getcomptype())

    def test_meta_guids_and_scene_bindings_are_exact(self) -> None:
        scene = (PROJECT / "Assets/Scenes/ThreeDoorsPlayable.unity").read_text(
            encoding="utf-8"
        )
        for contract in SELECTED.values():
            destination = PROJECT / contract["destination"]
            meta = destination.with_suffix(destination.suffix + ".meta").read_text(
                encoding="utf-8"
            )
            self.assertRegex(meta, rf"(?m)^guid: {contract['guid']}$")
            expected_binding = (
                f"{contract['sceneField']}: {{fileID: 8300000, "
                f"guid: {contract['guid']}, type: 3}}"
            )
            self.assertIn(expected_binding, scene)

    def test_builder_and_runtime_route_roles_with_synchronous_general_feedback(self) -> None:
        builder = (PROJECT / "Assets/Editor/PlayableGameBuilder.cs").read_text(
            encoding="utf-8"
        )
        audio_partial = (
            PROJECT / "Assets/Scripts/Game/ThreeDoorsGameController.Audio.cs"
        ).read_text(encoding="utf-8")
        controller = (
            PROJECT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
        ).read_text(encoding="utf-8")

        for contract in SELECTED.values():
            self.assertIn(contract["destination"], builder)
            self.assertIn(contract["sceneField"], builder)
            self.assertIn(contract["sceneField"], controller)
        self.assertIn("button.onClick.AddListener", audio_partial)
        self.assertNotIn("AddComponent<GameSfxButtonFeedback>", audio_partial)
        self.assertIn("SelectedUiSfxRolePolicy.Resolve", audio_partial)
        self.assertEqual(
            1,
            audio_partial.count("SelectedUiSfxRolePolicy.CanPlayGeneral"),
            "General priority is checked once immediately before synchronous playback.",
        )
        self.assertIn(
            "PlaySelectedUiSfx(selectedGeneralUiSfxClip);",
            audio_partial,
        )
        self.assertNotIn("pendingGeneralUiSfxCoroutine", audio_partial)
        self.assertNotIn("PlayGeneralUiSfxNextFrame", audio_partial)
        self.assertNotIn("StartCoroutine", audio_partial)
        self.assertIn("lastImportantUiSfxFrame", audio_partial)
        self.assertIn("importantUiSfxPriorityUntil", audio_partial)

    def test_policy_keeps_automatic_legacy_cues_silent(self) -> None:
        policy = (PROJECT / "Assets/Scripts/Audio/SelectedUiSfxRolePolicy.cs").read_text(
            encoding="utf-8"
        )

        self.assertRegex(policy, r"GameSfxCue\.UiAccept\s*=>\s*SelectedUiSfxRole\.General")
        self.assertRegex(
            policy,
            r"GameSfxCue\.ImportantConfirm\s*=>\s*SelectedUiSfxRole\.ImportantConfirm",
        )
        self.assertRegex(policy, r"GameSfxCue\.DoorOpen\s*=>\s*SelectedUiSfxRole\.ImportantConfirm")
        self.assertRegex(policy, r"GameSfxCue\.CardPlay\s*=>\s*SelectedUiSfxRole\.ImportantConfirm")
        self.assertIn("_ => SelectedUiSfxRole.None", policy)

    def test_no_unselected_candidate_hash_is_present_in_unity_assets(self) -> None:
        manifest = json.loads(
            (LISTENING_ROOT / "manifest.json").read_text(encoding="utf-8")
        )
        candidate_by_hash = {
            entry["outputSha256"]: entry["id"] for entry in manifest["candidates"]
        }
        found: dict[str, list[str]] = {}
        candidate_sizes = {
            (LISTENING_ROOT / entry["outputFile"]).stat().st_size
            for entry in manifest["candidates"]
        }
        for asset in (PROJECT / "Assets").rglob("*"):
            if not asset.is_file() or asset.stat().st_size not in candidate_sizes:
                continue
            identifier = candidate_by_hash.get(sha256(asset))
            if identifier is not None:
                found.setdefault(identifier, []).append(asset.relative_to(PROJECT).as_posix())

        self.assertEqual(
            {
                identifier: [contract["destination"]]
                for identifier, contract in SELECTED.items()
            },
            found,
        )


if __name__ == "__main__":
    unittest.main()
