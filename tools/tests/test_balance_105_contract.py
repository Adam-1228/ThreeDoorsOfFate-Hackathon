import math
import re
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
CONTROLLER = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
POLICY = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.Balance105.cs"


def unity_round(value: float) -> int:
    return math.floor(value + 0.5)


def read_number(source: str, name: str) -> float:
    match = re.search(
        rf"\b{name}\s*=\s*([0-9]+(?:\.[0-9]+)?)f?\s*;",
        source,
    )
    if match is None:
        raise AssertionError(f"Missing balance constant: {name}")
    return float(match.group(1))


class Balance105RepositoryContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.controller = CONTROLLER.read_text(encoding="utf-8")
        cls.policy = POLICY.read_text(encoding="utf-8") if POLICY.is_file() else ""

    def test_verified_normal_enemy_and_boss_outcomes_are_reproducible(self):
        self.assertTrue(POLICY.is_file(), "Missing balance-only policy source")
        boss_match = re.search(
            r'CreateScaledEnemyState\(NormalBossId,\s*"부채 심판관",\s*'
            r'(\d+),\s*(\d+),\s*(\d+),\s*true,\s*true,\s*0\)',
            self.controller,
        )
        self.assertIsNotNone(boss_match)
        boss_health, boss_attack, boss_block = map(int, boss_match.groups())

        self.assertEqual(
            (
                unity_round(boss_health * 0.96),
                unity_round(boss_attack * 0.96),
                unity_round(boss_block * 0.95),
            ),
            (132, 15, 9),
        )

        health_multiplier = read_number(
            self.policy,
            "NormalStandardEnemyHealthMultiplier",
        )
        attack_multiplier = read_number(
            self.policy,
            "NormalStandardEnemyAttackMultiplier",
        )
        block_multiplier = read_number(
            self.policy,
            "NormalStandardEnemyBlockMultiplier",
        )
        self.assertEqual(
            (
                unity_round(70 * 0.96 * health_multiplier),
                unity_round(12 * 0.96 * attack_multiplier),
                unity_round(8 * 0.95 * block_multiplier),
            ),
            (58, 10, 7),
        )

    def test_normal_nonboss_retune_and_regeneration_cap_are_wired(self):
        self.assertIn("ApplyNormalStandardEnemyRetune(", self.controller)
        self.assertIn("GetEnemyRegenerationAmount(state)", self.controller)
        self.assertEqual(
            read_number(self.policy, "NormalStandardEnemyRegenerationCap"),
            4,
        )

    def test_three_card_combat_and_shop_offers_apply_attack_guarantee(self):
        combat_rewards = re.search(
            r"private List<CardData> PickCombatRewards.*?\n        }",
            self.controller,
            re.DOTALL,
        )
        shop_cards = re.search(
            r"private List<CardData> PickShopCards.*?\n        }",
            self.controller,
            re.DOTALL,
        )
        self.assertIsNotNone(combat_rewards)
        self.assertIsNotNone(shop_cards)
        self.assertIn("EnsureAttackOffer(rewards, sources)", combat_rewards.group(0))
        self.assertIn("EnsureAttackOffer(cards, sources)", shop_cards.group(0))


if __name__ == "__main__":
    unittest.main()
