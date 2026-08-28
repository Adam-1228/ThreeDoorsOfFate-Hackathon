from __future__ import annotations

import json
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
SUBMISSION = ROOT / "docs/submission/app-store"

EXPECTED_WHATS_NEW = {
    "ko-KR": (
        "보스의 실제 공격 피해가 의도에 정확히 표시되며, 2~4장 카드 제안의 "
        "공격 카드 보장을 보완했습니다. 보물 카드는 금화 수령 후 직접 받거나 "
        "넘길 수 있고, 쉬움·보통 보스전의 공격 없는 손패를 전투당 한 차례 "
        "보정합니다. 동일한 재굴림 3회를 기록하는 숨은 업적도 추가했습니다."
    ),
    "en-US": (
        "Boss intents now show their exact attack damage, and Attack guarantees cover "
        "two- to four-card offers. Treasure Gold remains automatic while its card can "
        "be taken or skipped, and Easy/Normal boss fights can correct one full hand "
        "without an Attack. A hidden three-identical-rerolls achievement was added."
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


class Release130ContractTests(unittest.TestCase):
    def require(self, marker: str, source: str) -> None:
        if marker not in source:
            self.fail(f"missing version 1.3.0 release marker: {marker}")

    def test_historical_1_3_0_release_identity_remains_immutable(self) -> None:
        for locale in ("ko-KR", "en-US"):
            metadata = json.loads(
                (SUBMISSION / f"metadata-1.3.0.{locale}.json").read_text(
                    encoding="utf-8"
                )
            )
            with self.subTest(locale=locale):
                self.assertEqual(metadata["version"]["version_string"], "1.3.0")
                self.assertEqual(metadata["version"]["build_string"], "13001")

        release_notes = (ROOT / "docs/releases/v1.3.0.md").read_text(
            encoding="utf-8"
        )
        self.require("Marketing version: `1.3.0`", release_notes)
        self.require("iOS build number: `13001`", release_notes)

    def test_bilingual_metadata_and_review_notes_are_release_scoped(self) -> None:
        for locale in ("ko-KR", "en-US"):
            path = SUBMISSION / f"metadata-1.3.0.{locale}.json"
            self.assertTrue(path.is_file(), f"missing App Store metadata: {path}")
            metadata = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(metadata["version"]["version_string"], "1.3.0")
            self.assertEqual(metadata["version"]["build_string"], "13001")
            self.assertEqual(metadata["version"]["localization"], locale)
            self.assertEqual(metadata["version"]["whats_new"], EXPECTED_WHATS_NEW[locale])
            self.assertEqual(metadata["commercial"]["release_method"], "automatic")
            self.assertEqual(len(metadata["commercial"]["territories"]), 29)

        notes_path = SUBMISSION / "review-notes-1.3.0.en-US.md"
        self.assertTrue(notes_path.is_file(), f"missing review notes: {notes_path}")
        notes = notes_path.read_text(encoding="utf-8")
        self.assertLessEqual(len(notes), 4000)
        for marker in (
            "1.3.0 (13001)",
            "No account is required",
            "20 achievements",
            "12 new Game Center achievements",
            "optional rewarded ads",
            "does not request ATT permission",
        ):
            self.require(marker, notes)

    def test_game_center_manifest_adds_twelve_for_twenty_and_one_thousand_points(self) -> None:
        manifest_path = SUBMISSION / "game-center-achievements-1.3.0.json"
        self.assertTrue(manifest_path.is_file(), f"missing manifest: {manifest_path}")
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        new_items = manifest["new_achievements"]
        self.assertEqual(manifest["release_version"], "1.3.0")
        self.assertEqual(manifest["build"], "13001")
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
        self.assertEqual(
            manifest["reuses_unreleased_achievement_id"],
            "com.adam.threedoorsfate.achievement.collection.deck_50",
        )
        reroll_item = next(
            item
            for item in new_items
            if item["storage_suffix"] == "combat.same_reroll_three"
        )
        self.assertEqual(reroll_item["asc_reference_name"], "Fifty Fates")
        self.assertEqual(
            reroll_item["asc_reference_name_status"],
            "retained_internal_label_after_permanent_id_reuse",
        )
        self.assertNotIn("replaces_unreleased_achievement", manifest)

    def test_changelog_declares_version_1_3_0(self) -> None:
        changelog = (ROOT / "CHANGELOG.md").read_text(encoding="utf-8")
        self.require("## [v1.3.0] - 2026-08-26", changelog)

    def test_release_notes_identify_the_submitted_build(self) -> None:
        release_notes = (ROOT / "docs/releases/v1.3.0.md").read_text(
            encoding="utf-8"
        )
        self.require("Marketing version: `1.3.0`", release_notes)
        self.require("iOS build number: `13001`", release_notes)

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
            SUBMISSION / "submission-handoff-1.3.0-2026-08-26.md"
        ).read_text(encoding="utf-8")
        for marker in (
            "1.3.0 (13001)",
            "Do not select or relabel build `13000`",
            "20 achievements and 1,000 points",
            "Waiting for Review",
        ):
            self.require(marker, handoff)


if __name__ == "__main__":
    unittest.main()
