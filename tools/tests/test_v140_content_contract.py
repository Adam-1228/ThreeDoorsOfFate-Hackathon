from __future__ import annotations

import json
import re
import unittest
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
DATA_ROOT = ROOT / "Assets/Resources/GameData/V140"
CARD_ROOT = ROOT / "Assets/Data/Cards/MVP"

EXPECTED_CONTRACT_IDS = {
    "gambler.high_roll",
    "gambler.house_edge",
    "gambler.reroll_control",
    "oracle.intent_reader",
    "oracle.door_prophet",
    "oracle.luck_keeper",
    "exile.low_health",
    "exile.debt_cleanser",
    "exile.counter_guard",
}
EXPECTED_EVENT_IDS = {
    "event.forgotten_altar",
    "event.echoing_vault",
    "event.wounded_cartographer",
    "event.mirror_of_names",
    "event.gambler_last_table",
    "event.oracle_blind_star",
    "event.exile_broken_chain",
    "event.compound_broker",
}
EXPECTED_ENEMY_IDS = {
    "monster_cave_lurker",
    "monster_debt_hound",
    "monster_ash_gambler",
    "monster_rune_thief",
    "monster_candle_warden",
    "monster_contract_knight",
    "monster_hollow_collector",
    "monster_rift_spider",
    "monster_curse_bearer",
    "monster_gold_mimic",
    "monster_abyss_bailiff",
    "monster_ledger_moth",
    "monster_coin_sutured_husk",
    "monster_broken_scale_acolyte",
    "monster_rift_lamprey",
    "monster_contract_marionette",
    "monster_oath_candle_revenant",
    "monster_void_tax_scribe",
    "monster_debt_pit_bruiser",
    "monster_doorless_penitent",
    "boss_gatekeeper_third_door",
    "boss_debt_adjudicator_normal",
    "boss_usurer_of_the_abyss_hard",
    "boss_bottomless_creditor_special",
}
EXPECTED_MUTATION_IDS = {
    "abyss.compound_interest",
    "abyss.iron_ledger",
    "abyss.fading_rest",
    "abyss.cracked_foresight",
    "abyss.hungry_shop",
    "abyss.sealed_hand",
}

CLASS_ENUM = {"Any": 0, "Gambler": 1, "Oracle": 2, "Exile": 3}
CATEGORY_NAME = {0: "Attack", 1: "Defense", 2: "Skill", 3: "Curse"}
LOCALIZATION_KEY = re.compile(r"^[a-z][A-Za-z0-9]*(?:[._][A-Za-z0-9]+)+$")


def load_catalog(test: unittest.TestCase, filename: str) -> dict:
    path = DATA_ROOT / filename
    test.assertTrue(path.is_file(), f"missing v1.4.0 catalog: {path}")
    return json.loads(path.read_text(encoding="utf-8"))


def read_unity_integer(source: str, field: str) -> int:
    match = re.search(rf"^  {re.escape(field)}: (\d+)$", source, re.MULTILINE)
    if match is None:
        raise AssertionError(f"missing Unity field: {field}")
    return int(match.group(1))


def load_card_metadata(card_id: str) -> dict[str, int]:
    path = CARD_ROOT / f"{card_id}.asset"
    if not path.is_file():
        raise AssertionError(f"unknown card reference: {card_id}")
    source = path.read_text(encoding="utf-8")
    return {
        "category": read_unity_integer(source, "category"),
        "rarity": read_unity_integer(source, "rarity"),
        "source": read_unity_integer(source, "source"),
        "characterClass": read_unity_integer(source, "characterClass"),
    }


def assert_localization_key(test: unittest.TestCase, value: object) -> None:
    test.assertIsInstance(value, str)
    test.assertRegex(value, LOCALIZATION_KEY)


class V140ContentContractTests(unittest.TestCase):
    def test_catalogs_have_stable_schema_counts_and_ids(self) -> None:
        starter = load_catalog(self, "starter_contracts.json")
        events = load_catalog(self, "events.json")
        enemies = load_catalog(self, "enemy_behaviors.json")
        mutations = load_catalog(self, "endless_mutations.json")

        for catalog in (starter, events, enemies, mutations):
            self.assertEqual(1, catalog.get("schemaVersion"))

        self.assertEqual(3, len(starter["baseDecks"]))
        self.assertEqual(EXPECTED_CONTRACT_IDS, {item["id"] for item in starter["contracts"]})
        self.assertEqual(EXPECTED_EVENT_IDS, {item["id"] for item in events["events"]})
        self.assertEqual(EXPECTED_ENEMY_IDS, {item["enemyId"] for item in enemies["behaviors"]})
        self.assertEqual(EXPECTED_MUTATION_IDS, {item["id"] for item in mutations["mutations"]})

    def test_base_decks_are_legal_deterministic_twenty_four_card_lists(self) -> None:
        catalog = load_catalog(self, "starter_contracts.json")
        base_decks = catalog["baseDecks"]
        self.assertEqual({"Gambler", "Oracle", "Exile"}, {item["characterClass"] for item in base_decks})

        for base_deck in base_decks:
            with self.subTest(character_class=base_deck["characterClass"]):
                expected_class = CLASS_ENUM[base_deck["characterClass"]]
                category_counts: Counter[str] = Counter()
                class_card_count = 0
                total = 0
                seen_card_ids: set[str] = set()

                for entry in base_deck["cards"]:
                    card_id = entry["cardId"]
                    count = entry["count"]
                    self.assertNotIn(card_id, seen_card_ids)
                    self.assertGreater(count, 0)
                    seen_card_ids.add(card_id)
                    metadata = load_card_metadata(card_id)
                    self.assertEqual(0, metadata["rarity"], f"starter card must be common: {card_id}")
                    self.assertNotEqual(3, metadata["category"], f"starter card cannot be a curse: {card_id}")
                    self.assertFalse(card_id.startswith("hard_"), f"starter card cannot be hard-only: {card_id}")
                    self.assertIn(metadata["characterClass"], (0, expected_class))
                    category_counts[CATEGORY_NAME[metadata["category"]]] += count
                    if metadata["characterClass"] == expected_class:
                        class_card_count += count
                    total += count

                self.assertEqual(24, total)
                self.assertEqual({"Attack": 10, "Defense": 8, "Skill": 6}, dict(category_counts))
                self.assertGreaterEqual(class_card_count, 4)

    def test_contracts_stay_within_swap_and_resource_limits(self) -> None:
        catalog = load_catalog(self, "starter_contracts.json")
        grouped = Counter(contract["characterClass"] for contract in catalog["contracts"])
        self.assertEqual({"Gambler": 3, "Oracle": 3, "Exile": 3}, dict(grouped))

        for contract in catalog["contracts"]:
            with self.subTest(contract=contract["id"]):
                expected_prefix = contract["characterClass"].lower() + "."
                self.assertTrue(contract["id"].startswith(expected_prefix))
                assert_localization_key(self, contract["nameKey"])
                assert_localization_key(self, contract["roleKey"])
                assert_localization_key(self, contract["descriptionKey"])
                self.assertLessEqual(sum(swap["count"] for swap in contract["swaps"]), 4)
                self.assertGreaterEqual(contract["startingGoldDelta"], -10)
                self.assertLessEqual(contract["startingGoldDelta"], 10)
                self.assertGreaterEqual(contract["startingHealthDelta"], -6)
                self.assertLessEqual(contract["startingHealthDelta"], 6)
                self.assertGreaterEqual(contract["startingLuckDelta"], -1)
                self.assertLessEqual(contract["startingLuckDelta"], 1)
                self.assertGreaterEqual(contract["startingDebtDelta"], -1)
                self.assertLessEqual(contract["startingDebtDelta"], 1)
                self.assertLessEqual(
                    abs(contract["startingLuckDelta"]) + abs(contract["startingDebtDelta"]),
                    1,
                )
                for swap in contract["swaps"]:
                    self.assertGreater(swap["count"], 0)
                    removed = load_card_metadata(swap["removeCardId"])
                    added = load_card_metadata(swap["addCardId"])
                    self.assertEqual(removed["category"], added["category"])
                    self.assertEqual(0, added["rarity"])
                    self.assertNotEqual(3, added["category"])

    def test_events_cover_universal_class_and_debt_conditions(self) -> None:
        catalog = load_catalog(self, "events.json")
        events = catalog["events"]
        classes = Counter(event["requiredClass"] for event in events)
        universal_events = [
            event
            for event in events
            if event["requiredClass"] == "Any" and event["minimumDebt"] < 4
        ]
        self.assertEqual(4, len(universal_events))
        self.assertEqual(5, classes["Any"])
        self.assertEqual(1, classes["Gambler"])
        self.assertEqual(1, classes["Oracle"])
        self.assertEqual(1, classes["Exile"])
        debt_events = [event for event in events if event["minimumDebt"] >= 4]
        self.assertEqual(["event.compound_broker"], [event["id"] for event in debt_events])

        allowed_effects = {
            "Health",
            "MaxHealth",
            "Gold",
            "Debt",
            "AddCard",
            "RemoveCard",
            "DoorInsight",
            "DiscoverItem",
        }
        for event in events:
            with self.subTest(event=event["id"]):
                assert_localization_key(self, event["titleKey"])
                assert_localization_key(self, event["bodyKey"])
                self.assertTrue(event["oncePerTenDoors"])
                self.assertIn(len(event["choices"]), (2, 3))
                for choice in event["choices"]:
                    assert_localization_key(self, choice["labelKey"])
                    assert_localization_key(self, choice["previewKey"])
                    self.assertGreaterEqual(len(choice["effects"]), 1)
                    for effect in choice["effects"]:
                        self.assertIn(effect["type"], allowed_effects)
                        if effect.get("cardId"):
                            load_card_metadata(effect["cardId"])

    def test_every_enemy_has_two_unique_actions_and_boss_phase_thresholds(self) -> None:
        catalog = load_catalog(self, "enemy_behaviors.json")
        allowed_archetypes = {"Attack", "Guard", "Collector", "Disruptor", "Regenerator"}
        for behavior in catalog["behaviors"]:
            with self.subTest(enemy=behavior["enemyId"]):
                self.assertIn(behavior["archetype"], allowed_archetypes)
                action_ids = [action["id"] for action in behavior["actions"]]
                self.assertEqual(len(action_ids), len(set(action_ids)))
                self.assertGreaterEqual(
                    sum(1 for action in behavior["actions"] if action["unique"]),
                    2,
                )
                for action in behavior["actions"]:
                    assert_localization_key(self, action["nameKey"])
                    self.assertGreater(action["weight"], 0)

                if behavior["enemyId"].startswith("boss_"):
                    self.assertEqual(
                        [100, 70, 35],
                        [phase["maximumHealthPercent"] for phase in behavior["phases"]],
                    )
                    known_actions = set(action_ids)
                    for phase in behavior["phases"]:
                        self.assertGreaterEqual(len(phase["actionIds"]), 2)
                        self.assertTrue(set(phase["actionIds"]).issubset(known_actions))
                else:
                    self.assertEqual([], behavior["phases"])

    def test_endless_mutations_pair_clamped_risks_and_rewards(self) -> None:
        catalog = load_catalog(self, "endless_mutations.json")
        allowed_effects = {
            "EnemyAttackMultiplier",
            "CombatGoldMultiplier",
            "EnemyBlockMultiplier",
            "RareCardWeightMultiplier",
            "RestHealingMultiplier",
            "RemovalCostMultiplier",
            "DebtGainBonus",
            "DoorInsightBonus",
            "ShopPriceMultiplier",
            "ShopOfferBonus",
            "OpeningHandPenalty",
            "FirstTurnActionBonus",
        }
        for mutation in catalog["mutations"]:
            with self.subTest(mutation=mutation["id"]):
                assert_localization_key(self, mutation["nameKey"])
                assert_localization_key(self, mutation["riskKey"])
                assert_localization_key(self, mutation["rewardKey"])
                self.assertEqual(1, len(mutation["risks"]))
                self.assertEqual(1, len(mutation["rewards"]))
                for effect in mutation["risks"] + mutation["rewards"]:
                    self.assertIn(effect["type"], allowed_effects)
                    self.assertLessEqual(effect["minimum"], effect["value"])
                    self.assertLessEqual(effect["value"], effect["maximum"])


if __name__ == "__main__":
    unittest.main()
