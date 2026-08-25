from __future__ import annotations

import json
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
SUBMISSION = ROOT / "docs/submission/app-store"

EXPECTED_WHATS_NEW = {
    "ko-KR": (
        "업적을 총 20개로 확장하고, 미발견 항목은 숨겨 두는 유물형 업적 "
        "갤러리와 달성 상세 패널을 추가했습니다. 캐릭터 확인 화면에서 설정을 "
        "열 수 있게 했으며, 진행 기록 여백과 상점 유물 이미지 프레임 표시를 "
        "개선했습니다."
    ),
    "en-US": (
        "Expanded Achievements to 20 with a relic-style discovery gallery that keeps "
        "undiscovered entries hidden and shows details after completion. Settings are "
        "now available from character confirmation, and we improved progress-log "
        "padding and shop relic framing."
    ),
}

NEW_ACHIEVEMENT_IDS = {
    "com.adam.threedoorsfate.achievement.combat.gambler_card_reading": 10,
    "com.adam.threedoorsfate.achievement.combat.oracle_precise_prediction": 10,
    "com.adam.threedoorsfate.achievement.combat.exile_curse_eater": 10,
    "com.adam.threedoorsfate.achievement.combat.fate_cleaver_50": 15,
    "com.adam.threedoorsfate.achievement.combat.iron_wall_40": 15,
    "com.adam.threedoorsfate.achievement.combat.five_cards_turn": 15,
    "com.adam.threedoorsfate.achievement.collection.deck_50": 15,
    "com.adam.threedoorsfate.achievement.combat.cliffside_victory": 20,
    "com.adam.threedoorsfate.achievement.collection.triple_contract": 20,
    "com.adam.threedoorsfate.achievement.build.masterpiece": 20,
    "com.adam.threedoorsfate.achievement.endless.twentieth_door": 25,
    "com.adam.threedoorsfate.achievement.meta.three_survivors": 25,
}


class Release120ContractTests(unittest.TestCase):
    def require(self, marker: str, source: str) -> None:
        if marker not in source:
            self.fail(f"missing version 1.2.0 release marker: {marker}")

    def test_project_and_upload_defaults_are_1_2_0_build_12000(self) -> None:
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
            ("bundleVersion: 1.2.0", project),
            ("iPhone: 12000", project),
            ('DefaultVersion = "1.2.0"', release),
            ('DefaultBuildNumber = "12000"', release),
            ('EXPECTED_VERSION="${TDOF_EXPECTED_VERSION:-1.2.0}"', upload),
            ('EXPECTED_BUILD="${TDOF_EXPECTED_BUILD:-12000}"', upload),
            ('ACTIVE_VERSION = "1.2.0"', validator),
            ('ACTIVE_BUILD = "12000"', validator),
        ):
            with self.subTest(marker=marker):
                self.require(marker, source)

    def test_bilingual_metadata_and_review_notes_are_release_scoped(self) -> None:
        for locale in ("ko-KR", "en-US"):
            path = SUBMISSION / f"metadata-1.2.0.{locale}.json"
            self.assertTrue(path.is_file(), f"missing App Store metadata: {path}")
            metadata = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(metadata["version"]["version_string"], "1.2.0")
            self.assertEqual(metadata["version"]["build_string"], "12000")
            self.assertEqual(metadata["version"]["localization"], locale)
            self.assertEqual(metadata["version"]["whats_new"], EXPECTED_WHATS_NEW[locale])
            self.assertEqual(metadata["commercial"]["release_method"], "manual")
            self.assertEqual(len(metadata["commercial"]["territories"]), 29)

        notes_path = SUBMISSION / "review-notes-1.2.0.en-US.md"
        self.assertTrue(notes_path.is_file(), f"missing review notes: {notes_path}")
        notes = notes_path.read_text(encoding="utf-8")
        self.assertLessEqual(len(notes), 4000)
        for marker in (
            "1.2.0 (12000)",
            "No account is required",
            "20 achievements",
            "12 new Game Center achievements",
            "optional rewarded ads",
            "does not request ATT permission",
        ):
            self.require(marker, notes)

    def test_game_center_manifest_adds_twelve_for_twenty_and_one_thousand_points(self) -> None:
        manifest_path = SUBMISSION / "game-center-achievements-1.2.0.json"
        self.assertTrue(manifest_path.is_file(), f"missing manifest: {manifest_path}")
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        new_items = manifest["new_achievements"]
        self.assertEqual(manifest["release_version"], "1.2.0")
        self.assertEqual(manifest["build"], "12000")
        self.assertEqual(manifest["total_achievement_count"], 20)
        self.assertEqual(manifest["total_points"], 1000)
        self.assertEqual(len(new_items), 12)
        self.assertEqual(
            {item["reference_name"]: item["points"] for item in new_items},
            NEW_ACHIEVEMENT_IDS,
        )
        self.assertEqual(sum(item["points"] for item in new_items), 200)
        self.assertTrue(all(item["is_hidden"] for item in new_items))
        self.assertTrue(all(item["image"].endswith(".png") for item in new_items))

    def test_changelog_declares_version_1_2_0(self) -> None:
        changelog = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
        self.require("## [v1.2.0] - 2026-08-25", changelog)

    def test_readme_promotes_the_app_store_and_current_webgl_release(self) -> None:
        readme = (ROOT / "README.md").read_text(encoding="utf-8")
        self.require(
            "https://apps.apple.com/kr/app/three-doors-of-fate/id6798086296",
            readme,
        )
        self.require("v1.1.2 WebGL", readme)
        self.assertNotIn("iOS 실기기·스토어 제출은 별도 검증이 필요", readme)

    def test_submission_handoff_requires_a_fresh_exact_build(self) -> None:
        handoff = (
            SUBMISSION / "submission-handoff-1.2.0-2026-08-25.md"
        ).read_text(encoding="utf-8")
        for marker in (
            "1.2.0 (12000)",
            "Do not select or relabel a prior build",
            "20 achievements and 1,000 points",
            "Waiting for Review",
        ):
            self.require(marker, handoff)


if __name__ == "__main__":
    unittest.main()
