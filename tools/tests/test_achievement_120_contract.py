from __future__ import annotations

import re
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
ACHIEVEMENT_SOURCE = (
    PROJECT_ROOT / "Assets/Scripts/Platform/AchievementProgress.cs"
)
CONTROLLER_SOURCE = PROJECT_ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
ACHIEVEMENT_CONTROLLER_SOURCE = (
    PROJECT_ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.Achievements.cs"
)
PERSISTENCE_SOURCE = (
    PROJECT_ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.Persistence.cs"
)

EXPECTED = {
    "combat.gambler_card_reading": (
        "com.adam.threedoorsfate.achievement.combat.gambler_card_reading",
        "Achievements/achievement_gambler_card_reading",
        10,
    ),
    "combat.oracle_precise_prediction": (
        "com.adam.threedoorsfate.achievement.combat.oracle_precise_prediction",
        "Achievements/achievement_oracle_precise_prediction",
        10,
    ),
    "combat.exile_curse_eater": (
        "com.adam.threedoorsfate.achievement.combat.exile_curse_eater",
        "Achievements/achievement_exile_curse_eater",
        10,
    ),
    "combat.fate_cleaver_50": (
        "com.adam.threedoorsfate.achievement.combat.fate_cleaver_50",
        "Achievements/achievement_fate_cleaver_50",
        15,
    ),
    "combat.iron_wall_40": (
        "com.adam.threedoorsfate.achievement.combat.iron_wall_40",
        "Achievements/achievement_iron_wall_40",
        15,
    ),
    "combat.five_cards_turn": (
        "com.adam.threedoorsfate.achievement.combat.five_cards_turn",
        "Achievements/achievement_five_cards_turn",
        15,
    ),
    "collection.deck_50": (
        "com.adam.threedoorsfate.achievement.collection.deck_50",
        "Achievements/achievement_deck_50",
        15,
    ),
    "combat.cliffside_victory": (
        "com.adam.threedoorsfate.achievement.combat.cliffside_victory",
        "Achievements/achievement_cliffside_victory",
        20,
    ),
    "collection.triple_contract": (
        "com.adam.threedoorsfate.achievement.collection.triple_contract",
        "Achievements/achievement_triple_contract",
        20,
    ),
    "build.masterpiece": (
        "com.adam.threedoorsfate.achievement.build.masterpiece",
        "Achievements/achievement_build_masterpiece",
        20,
    ),
    "endless.twentieth_door": (
        "com.adam.threedoorsfate.achievement.endless.twentieth_door",
        "Achievements/achievement_twentieth_door",
        25,
    ),
    "meta.three_survivors": (
        "com.adam.threedoorsfate.achievement.meta.three_survivors",
        "Achievements/achievement_three_survivors",
        25,
    ),
}

DEFINITION_PATTERN = re.compile(
    r"AchievementDefinition\s+\w+Definition\s*=\s*new\(\s*"
    r'"(?P<id>[^"]+)"\s*,\s*'
    r'"(?P<suffix>[^"]+)"\s*,\s*'
    r'"[^"]*"\s*,\s*'
    r'"[^"]*"\s*,\s*'
    r'"[^"]*"\s*,\s*'
    r'"(?P<resource>[^"]+)"\s*,\s*'
    r"(?P<points>\d+)\s*\);",
    re.DOTALL,
)


def read_definitions() -> dict[str, tuple[str, str, int]]:
    source = ACHIEVEMENT_SOURCE.read_text(encoding="utf-8")
    return {
        match.group("suffix"): (
            match.group("id"),
            match.group("resource"),
            int(match.group("points")),
        )
        for match in DEFINITION_PATTERN.finditer(source)
    }


class Achievement120CatalogContractTests(unittest.TestCase):
    def test_all_twelve_release_definitions_exist_exactly(self) -> None:
        definitions = read_definitions()
        actual = {
            suffix: definitions.get(suffix)
            for suffix in EXPECTED
        }
        self.assertEqual(EXPECTED, actual)

    def test_release_definitions_add_exactly_two_hundred_points(self) -> None:
        definitions = read_definitions()
        selected = [definitions.get(suffix) for suffix in EXPECTED]
        self.assertNotIn(None, selected)
        points = [entry[2] for entry in selected if entry is not None]
        self.assertEqual(12, len(points))
        self.assertEqual(200, sum(points))

    def test_release_ids_and_resource_paths_are_unique(self) -> None:
        definitions = read_definitions()
        selected = [definitions.get(suffix) for suffix in EXPECTED]
        self.assertNotIn(None, selected)
        selected = [entry for entry in selected if entry is not None]
        self.assertEqual(12, len({entry[0] for entry in selected}))
        self.assertEqual(12, len({entry[1] for entry in selected}))

    def test_every_release_achievement_has_a_runtime_completion_route(self) -> None:
        source = "\n".join(
            path.read_text(encoding="utf-8")
            for path in (
                CONTROLLER_SOURCE,
                ACHIEVEMENT_CONTROLLER_SOURCE,
                PERSISTENCE_SOURCE,
            )
        )
        properties = (
            "GamblerCardReading",
            "OraclePrecisePrediction",
            "ExileCurseEater",
            "FateCleaver",
            "IronWall",
            "FiveCardsTurn",
            "DeckFifty",
            "CliffsideVictory",
            "TripleContract",
            "BuildMasterpiece",
            "TwentiethDoor",
            "ThreeSurvivors",
        )
        for property_name in properties:
            with self.subTest(property_name=property_name):
                marker = (
                    "CompleteAchievementAndTrack("
                    f"AchievementProgress.{property_name})"
                )
                if marker not in source:
                    self.fail(f"missing runtime completion route: {marker}")

    def test_runtime_transitions_call_the_milestone_checks(self) -> None:
        controller = CONTROLLER_SOURCE.read_text(encoding="utf-8")
        persistence = PERSISTENCE_SOURCE.read_text(encoding="utf-8")
        required_calls = (
            "TryCompleteCombatAwakeningAchievements();",
            "TryCompleteCombatCardAchievements(actualCardDamage);",
            "TryCompleteCliffsideVictoryAchievement();",
            "TryCompleteDeckFiftyAchievement();",
            "TryCompleteTripleContractAchievement();",
            "TryCompleteMasterpieceAchievement();",
            "TryCompleteTwentiethDoorAchievement();",
            "TryCompleteThreeSurvivorsAchievement();",
        )
        combined = controller + "\n" + persistence
        for call in required_calls:
            with self.subTest(call=call):
                if call not in combined:
                    self.fail(f"missing runtime transition call: {call}")
        if "TryCompletePersistentAchievements();" not in persistence:
            self.fail("hard-run restore does not backfill persistent achievements")

    def test_gallery_uses_two_pages_of_ten_hidden_discovery_slots(self) -> None:
        source = ACHIEVEMENT_CONTROLLER_SOURCE.read_text(encoding="utf-8")
        required = (
            "private const int AchievementSlotsPerPage = 10;",
            "const int columns = 5;",
            "const int rows = 2;",
            '"업적 슬롯 {absoluteIndex + 1}"',
            '"업적 미발견"',
            '"common.undiscovered"',
            '"업적 상세 패널"',
            '"업적 상세 제목"',
            '"업적 상세 설명"',
            '"업적 상세 상태"',
            '"업적 선택 표시"',
            "statusSectionMediumFrameSprite",
            "statusInnerPanelFrameSprite",
            "selectionFrameSprite",
        )
        for marker in required:
            with self.subTest(marker=marker):
                if marker not in source:
                    self.fail(f"missing achievement gallery contract: {marker}")
        for forbidden in (
            "private void AddAchievementCard(",
            '"업적 진행"',
            '"업적 정보 패널"',
        ):
            if forbidden in source:
                self.fail(f"obsolete gallery marker remains: {forbidden}")


if __name__ == "__main__":
    unittest.main()
