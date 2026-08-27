from __future__ import annotations

import json
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
SUBMISSION = ROOT / "docs/submission/app-store"


class Release140ContractTests(unittest.TestCase):
    def require(self, marker: str, source: str) -> None:
        if marker not in source:
            self.fail(f"missing version 1.4.0 release marker: {marker}")

    def test_project_and_upload_defaults_are_1_4_0_build_14000(self) -> None:
        project = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text(
            encoding="utf-8"
        )
        release = (
            ROOT / "Assets/Scripts/Platform/IOSReleaseConfiguration.cs"
        ).read_text(encoding="utf-8")
        upload = (ROOT / "tools/upload_testflight.sh").read_text(encoding="utf-8")
        validator = (
            ROOT / "tools/validate_app_store_submission.py"
        ).read_text(encoding="utf-8")

        for marker, source in (
            ("bundleVersion: 1.4.0", project),
            ("iPhone: 14000", project),
            ('DefaultVersion = "1.4.0"', release),
            ('DefaultBuildNumber = "14000"', release),
            ('EXPECTED_VERSION="${TDOF_EXPECTED_VERSION:-1.4.0}"', upload),
            ('EXPECTED_BUILD="${TDOF_EXPECTED_BUILD:-14000}"', upload),
            ('ACTIVE_VERSION = "1.4.0"', validator),
            ('ACTIVE_BUILD = "14000"', validator),
        ):
            with self.subTest(marker=marker):
                self.require(marker, source)

    def test_release_candidate_metadata_is_bilingual_and_draft_scoped(self) -> None:
        for locale in ("ko-KR", "en-US"):
            path = SUBMISSION / f"metadata-1.4.0.{locale}.json"
            self.assertTrue(path.is_file(), f"missing App Store metadata: {path}")
            metadata = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(metadata["version"]["version_string"], "1.4.0")
            self.assertEqual(metadata["version"]["build_string"], "14000")
            self.assertEqual(metadata["version"]["localization"], locale)
            self.assertEqual(metadata["commercial"]["release_method"], "automatic")
            self.assertFalse(metadata["submission"]["approved_for_submission"])
            whats_new = metadata["version"]["whats_new"]
            for marker in (
                "24",
                "contract" if locale == "en-US" else "계약",
                "history" if locale == "en-US" else "기록",
            ):
                self.assertIn(marker, whats_new.lower())

        notes_path = SUBMISSION / "review-notes-1.4.0.en-US.md"
        self.assertTrue(notes_path.is_file(), f"missing review notes: {notes_path}")
        notes = notes_path.read_text(encoding="utf-8")
        self.assertLessEqual(len(notes), 4000)
        for marker in (
            "1.4.0 (14000)",
            "No account is required",
            "optional rewarded ads",
            "20 achievements",
            "release candidate",
        ):
            self.require(marker, notes)

    def test_documentation_marks_1_4_0_as_unsubmitted_release_candidate(self) -> None:
        changelog = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
        release_notes_path = ROOT / "docs/releases/v1.4.0.md"
        self.assertTrue(release_notes_path.is_file())
        release_notes = release_notes_path.read_text(encoding="utf-8")
        readme = (ROOT / "README.md").read_text(encoding="utf-8")

        self.require("## [v1.4.0] - Unreleased", changelog)
        self.require("Marketing version: `1.4.0`", release_notes)
        self.require("iOS build number: `14000`", release_notes)
        self.require("awaiting separate submission approval", release_notes)
        self.require("v1.4.0 release candidate", readme)

    def test_game_center_remains_twenty_achievements_and_one_thousand_points(self) -> None:
        manifest_path = SUBMISSION / "game-center-achievements-1.3.0.json"
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(manifest["total_achievement_count"], 20)
        self.assertEqual(manifest["total_points"], 1000)


if __name__ == "__main__":
    unittest.main()
