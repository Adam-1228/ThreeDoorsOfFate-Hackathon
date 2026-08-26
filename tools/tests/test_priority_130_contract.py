from __future__ import annotations

import re
import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
CONTROLLER = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
BALANCE = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.Balance105.cs"
QUALITY = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.Quality104.cs"
ACHIEVEMENTS = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.Achievements.cs"


def method(source: str, signature: str) -> str:
    start = source.find(signature)
    if start < 0:
        raise AssertionError(f"missing method: {signature}")
    next_method = source.find("\n        private ", start + len(signature))
    return source[start:] if next_method < 0 else source[start:next_method]


class Priority130ContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.controller = CONTROLLER.read_text(encoding="utf-8")
        cls.balance = BALANCE.read_text(encoding="utf-8")
        cls.quality = QUALITY.read_text(encoding="utf-8")
        cls.achievements = ACHIEVEMENTS.read_text(encoding="utf-8")

    def test_two_to_four_card_offers_share_the_attack_guarantee(self) -> None:
        ensure = method(self.balance, "private void EnsureAttackOffer(")
        self.assertIn("offers.Count < 2", ensure)
        self.assertIn("offers.Count > 4", ensure)
        self.assertNotIn("offers.Count != 3", ensure)

        combat = method(self.controller, "private List<CardData> PickCombatRewards(")
        shop = method(self.controller, "private List<CardData> PickShopCards(")
        self.assertIn("EnsureAttackOffer(rewards, sources);", combat)
        self.assertIn("EnsureAttackOffer(cards, sources);", shop)
        self.assertNotRegex(combat, r"if\s*\(count\s*==\s*3\)")
        self.assertNotRegex(shop, r"if\s*\(count\s*==\s*3\)")

    def test_boss_low_luck_damage_has_one_preview_and_resolution_source(self) -> None:
        self.assertIn("private int GetEnemyIntentAttackDamage(", self.balance)
        prepare = method(self.controller, "private void PrepareEnemyIntent()")
        render = method(self.controller, "private void RenderCombat()")
        refresh = method(
            self.controller,
            "private void RefreshEnemyIntentLabelForCurrentLuck()",
        )
        resolve = method(self.controller, "private void ResolveEnemyIntent()")
        self.assertIn("RefreshEnemyIntentLabelForCurrentLuck();", prepare)
        self.assertIn("RefreshEnemyIntentLabelForCurrentLuck();", render)
        self.assertIn("GetEnemyIntentAttackDamage(enemy)", refresh)
        self.assertIn("GetEnemyIntentAttackDamage(enemy)", resolve)
        self.assertNotIn("enemy.IntentAttack + 5", resolve)

    def test_boss_hand_smoothing_is_once_per_combat_and_hard_excluded(self) -> None:
        smoothing = method(self.balance, "private bool TrySmoothBossNoAttackHand()")
        for marker in (
            "bossNoAttackHandSmoothingUsedThisCombat",
            "currentDifficulty == RunDifficulty.Hard",
            "hand.Count != StartingHandSize",
            "CardCategory.Attack",
        ):
            self.assertIn(marker, smoothing)
        start = method(self.controller, "private void StartCombat(EnemyState newEnemy)")
        end = method(self.controller, "private void EndTurn()")
        self.assertIn("bossNoAttackHandSmoothingUsedThisCombat = false;", start)
        self.assertIn("TrySmoothBossNoAttackHand();", start)
        self.assertIn("TrySmoothBossNoAttackHand();", end)

    def test_boss_hand_smoothing_log_is_localized(self) -> None:
        catalog = json.loads(
            (ROOT / "Assets/Resources/Localization/game_text.json").read_text(
                encoding="utf-8"
            )
        )
        entries = {entry["ko"]: entry["en"] for entry in catalog["entries"]}
        source = "운명 보정: 보스전 손패에 공격 카드 1장을 배치했습니다."
        self.assertEqual(
            "Fate correction: placed 1 Attack card into your hand for the Boss battle.",
            entries.get(source),
        )

    def test_treasure_gold_is_automatic_but_card_is_explicit(self) -> None:
        show = method(self.controller, "private void ShowTreasure()")
        self.assertIn("gold += rewardGold;", show)
        self.assertIn("RenderTreasureOffer(rewardGold, card);", show)
        self.assertNotIn("TryAddCardToDeck", show)
        self.assertIn("private void RenderTreasureOffer(", self.quality)
        self.assertIn("TryResolveTreasureCardChoice", self.quality)
        self.assertIn('"treasure.action.takeCard"', self.quality)
        self.assertIn('"treasure.action.skipCard"', self.quality)

    def test_only_explicit_rerolls_advance_the_comic_achievement(self) -> None:
        effect = method(self.controller, "private void ApplyEffect(CardEffectDefinition effect)")
        turn_roll = method(self.controller, "private void RollLuckForTurn()")
        start = method(self.controller, "private void StartCombat(EnemyState newEnemy)")
        self.assertIn("RecordExplicitRerollResult(luck);", effect)
        self.assertNotIn("RecordExplicitRerollResult", turn_roll)
        self.assertIn("ResetExplicitRerollProgress();", start)
        self.assertIn("private void RecordExplicitRerollResult(int result)", self.achievements)


if __name__ == "__main__":
    unittest.main()
