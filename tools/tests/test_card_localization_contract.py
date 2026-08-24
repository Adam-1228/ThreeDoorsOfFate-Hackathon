from __future__ import annotations

import hashlib
import json
import re
import struct
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
CARD_DATA_ROOT = PROJECT_ROOT / "Assets/Data/Cards/MVP"
CARD_IMAGE_ROOT = (
    PROJECT_ROOT / "Assets/Resources/Cards/EnglishLocalized"
)
CARD_MANIFEST_PATH = (
    PROJECT_ROOT / "Assets/Resources/Localization/english_cards.json"
)
CARD_LOCALIZATION_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Localization/CardLocalization.cs"
)
SPRITE_BINDING_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Localization/LocalizedCardSpriteBinding.cs"
)
GAME_LOCALIZATION_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Localization/GameLocalization.cs"
)
CONTROLLER_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
)
CONTROLLER_LOCALIZATION_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.Localization.cs"
)
CARD_VIEW_PATH = PROJECT_ROOT / "Assets/Scripts/UI/CardView.cs"
IMPORTER_PATH = PROJECT_ROOT / "Assets/Editor/CardSpriteImportPostprocessor.cs"
UNITY_TEST_PATH = (
    PROJECT_ROOT / "Assets/Tests/EditMode/CardLocalizationTests.cs"
)
WINDOWS_HANDOFF_PATH = (
    PROJECT_ROOT
    / "docs/release/three-doors-of-fate-1.0.3-windows-verification.md"
)

EXPECTED_PACKAGE = "ThreeDoors-English-Cards-1.0.2-rev2"
EXPECTED_ARCHIVE_SHA256 = (
    "1616B2D3A2243FAA6C6E8650A74792013C3F210021D30C14CC5BF1F166439F00"
)
EXPECTED_CORRECTIONS = {
    "card_absorb_curse": "Reduce Debt by 1. Gain 1 Action.",
    "card_small_contract": "Lose 3 HP. Gain 1 Action.",
    "hard_exile_no_return": (
        "Reduce Curse or Debt by 1. If successful, gain 1 Action."
    ),
    "hard_gambler_debt_jackpot": (
        "Gain 20 Gold. If Luck is 5 or higher, gain 1 Action."
    ),
    "hard_skill_door_breath": (
        "Draw 2 cards. If Luck is 4 or higher, gain 1 Action."
    ),
}


def read_manifest() -> dict:
    return json.loads(CARD_MANIFEST_PATH.read_text(encoding="utf-8"))


def png_dimensions(path: Path) -> tuple[int, int]:
    with path.open("rb") as handle:
        signature = handle.read(24)
    if len(signature) != 24 or signature[:8] != b"\x89PNG\r\n\x1a\n":
        raise AssertionError(f"not a PNG: {path}")
    return struct.unpack(">II", signature[16:24])


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


class CardPackageContractTests(unittest.TestCase):
    def test_runtime_manifest_is_the_verified_rev2_package(self) -> None:
        payload = read_manifest()
        self.assertEqual(1, payload["schema_version"])
        self.assertEqual(EXPECTED_PACKAGE, payload["package_name"])
        self.assertEqual("1.0.2", payload["package_version"])
        self.assertEqual(72, payload["english_png_count"])
        self.assertEqual(72, payload["english_text_record_count"])
        self.assertEqual([], payload["missing_card_ids"])
        self.assertEqual([], payload["duplicate_card_ids"])

    def test_manifest_ids_exactly_match_project_card_data_ids(self) -> None:
        payload = read_manifest()
        manifest_ids = [entry["card_id"] for entry in payload["cards"]]
        project_ids: list[str] = []
        for path in sorted(CARD_DATA_ROOT.glob("*.asset")):
            match = re.search(
                r"(?m)^  cardId: (?P<card_id>\S+)\s*$",
                path.read_text(encoding="utf-8"),
            )
            self.assertIsNotNone(match, str(path))
            project_ids.append(match.group("card_id"))

        self.assertEqual(72, len(manifest_ids))
        self.assertEqual(72, len(set(manifest_ids)))
        self.assertEqual(set(project_ids), set(manifest_ids))

    def test_all_manifest_images_exist_with_exact_dimensions_and_hashes(self) -> None:
        payload = read_manifest()
        entries = payload["cards"]
        expected_names = {
            Path(entry["image_relative_path"]).name for entry in entries
        }
        actual_paths = sorted(CARD_IMAGE_ROOT.glob("*.png"))

        self.assertEqual(72, len(actual_paths))
        self.assertEqual(expected_names, {path.name for path in actual_paths})
        for entry in entries:
            path = CARD_IMAGE_ROOT / Path(entry["image_relative_path"]).name
            with self.subTest(card_id=entry["card_id"]):
                self.assertEqual((987, 1495), png_dimensions(path))
                self.assertEqual((entry["width"], entry["height"]), png_dimensions(path))
                self.assertEqual(entry["sha256"], sha256(path))

    def test_rev2_action_corrections_are_present_and_energy_is_absent(self) -> None:
        by_id = {
            entry["card_id"]: entry
            for entry in read_manifest()["cards"]
        }
        self.assertEqual(set(EXPECTED_CORRECTIONS), set(EXPECTED_CORRECTIONS) & set(by_id))
        for card_id, exact_rules in EXPECTED_CORRECTIONS.items():
            rules = by_id[card_id]["english_rules_text"]
            with self.subTest(card_id=card_id):
                self.assertIn("Action", rules)
                self.assertNotIn("Energy", rules)
                if exact_rules is not None:
                    self.assertEqual(exact_rules, rules)

        all_rules = "\n".join(
            entry["english_rules_text"] for entry in by_id.values()
        )
        self.assertNotIn("Energy", all_rules)


class CardRuntimeContractTests(unittest.TestCase):
    def test_card_localization_has_id_lookup_fallback_cache_and_source_translation(self) -> None:
        source = CARD_LOCALIZATION_PATH.read_text(encoding="utf-8")
        for token in (
            'CatalogResourcePath = "Localization/english_cards"',
            'EnglishCardResourceRoot = "Cards/EnglishLocalized/"',
            "GetName(",
            "GetRules(",
            "GetFullCardSprite(",
            "RegisterKoreanSource(",
            "TryTranslateKoreanSource(",
            "Resources.Load<Sprite>",
            "SpriteCache",
            "ReportedMissing",
            "GameLocalization.IsEnglish",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

    def test_game_localization_translates_captured_card_names(self) -> None:
        source = GAME_LOCALIZATION_PATH.read_text(encoding="utf-8")
        self.assertRegex(
            source,
            re.compile(
                r"public static bool IsEnglish\s*\{.*?EnsureInitialized\(\)",
                re.DOTALL,
            ),
        )
        self.assertGreaterEqual(
            source.count("CardLocalization.TryTranslateKoreanSource"),
            2,
        )
        self.assertIn(
            "CardLocalization.TryTranslateKoreanSourcesInText",
            source,
        )
        self.assertIn("TryTranslateCompositeLines", source)

        catalog = json.loads(
            (
                PROJECT_ROOT
                / "Assets/Resources/Localization/game_text.json"
            ).read_text(encoding="utf-8")
        )
        by_korean = {entry["ko"]: entry["en"] for entry in catalog["entries"]}
        self.assertEqual(
            "{0} {1}  {2}",
            by_korean.get("{0} {1}  {2}"),
        )
        self.assertEqual("Owned: {0}", by_korean.get("보유: {0}"))
        self.assertEqual("Required: {0}", by_korean.get("필요: {0}"))

    def test_controller_registers_cards_before_building_visible_ui(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        awake_start = source.index("private void Awake()")
        awake_end = source.index("private void Update()", awake_start)
        awake = source[awake_start:awake_end]
        self.assertIn("RegisterCardLocalizationSources();", awake)
        self.assertLess(
            awake.index("GameLocalization.Initialize"),
            awake.index("RegisterCardLocalizationSources"),
        )
        self.assertLess(
            awake.index("RegisterCardLocalizationSources"),
            awake.index("BuildShell"),
        )

    def test_controller_card_boundary_localizes_name_rules_and_sprite(self) -> None:
        source = CONTROLLER_LOCALIZATION_PATH.read_text(encoding="utf-8")
        for token in (
            "CardLocalization.GetName(card.CardId, card.DisplayName)",
            "CardLocalization.GetRules(card.CardId, card.RulesText)",
            "CardLocalization.GetFullCardSprite(card.CardId, card.FullCardSprite)",
            "CardLocalization.RegisterKoreanSource(card.CardId, card.DisplayName)",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

    def test_card_buttons_and_previews_are_language_live(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        create_start = source.index("private Button CreateCardButton(")
        create_end = source.index("private void AddCardPreviewHandlers", create_start)
        create_card = source[create_start:create_end]
        self.assertIn("GetLocalizedCardFullSprite(card)", create_card)
        self.assertIn("BindLocalizedCardSprite(frame, card)", create_card)
        self.assertIn("BindLocalizedCardText(name, card, false)", create_card)
        self.assertIn("BindLocalizedCardText(rules, card, true)", create_card)

        self.assertIn(
            "private void AddCardPreviewHandlers(GameObject target, CardData card)",
            source,
        )
        self.assertIn(
            "private void ShowCardPreview(CardData card, RectTransform previewTarget)",
            source,
        )
        self.assertIn(
            "private void SelectCombatCardForPreview(int handIndex, CardData card)",
            source,
        )
        self.assertNotIn("Sprite previewSprite = card.FullCardSprite", source)

    def test_sprite_binding_refreshes_on_language_change(self) -> None:
        source = SPRITE_BINDING_PATH.read_text(encoding="utf-8")
        for token in (
            "GameLocalization.LanguageChanged += Refresh",
            "GameLocalization.LanguageChanged -= Refresh",
            "CardLocalization.GetFullCardSprite",
            "public void Configure(",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

    def test_card_view_refreshes_name_rules_and_metadata(self) -> None:
        source = CARD_VIEW_PATH.read_text(encoding="utf-8")
        refresh_start = source.index("private void RefreshLocalizedLabels()")
        refresh_end = source.index("private static Color GetCategoryColor", refresh_start)
        refresh = source[refresh_start:refresh_end]
        self.assertIn(
            "CardLocalization.GetName(cardData.CardId, cardData.DisplayName)",
            refresh,
        )
        self.assertIn(
            "CardLocalization.GetRules(cardData.CardId, cardData.RulesText)",
            refresh,
        )
        self.assertNotIn("SetText(nameText, value.DisplayName)", source)
        self.assertNotIn("SetText(rulesText, value.RulesText)", source)

    def test_unity_editmode_contract_covers_round_trip_and_72_cards(self) -> None:
        source = UNITY_TEST_PATH.read_text(encoding="utf-8")
        for token in (
            "72",
            "GameLanguage.Korean",
            "GameLanguage.English",
            "CardLocalization.GetName",
            "CardLocalization.GetRules",
            "CardLocalization.TryTranslateKoreanSource",
            'Resources.LoadAll<Sprite>("Cards/EnglishLocalized")',
            "AssetDatabase.FindAssets",
            "TextureImporterType.Sprite",
            "TextureImporterCompression.Uncompressed",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)


class CardImportAndHandoffContractTests(unittest.TestCase):
    def test_resources_english_cards_use_full_rendered_sprite_settings(self) -> None:
        source = IMPORTER_PATH.read_text(encoding="utf-8")
        self.assertIn(
            'EnglishLocalizedCardRoot = "Assets/Resources/Cards/EnglishLocalized/"',
            source,
        )
        self.assertIn("path.StartsWith(EnglishLocalizedCardRoot", source)
        full_rendered_start = source.index(
            "private static bool IsFullRenderedCardArt"
        )
        full_rendered_end = source.index(
            "private static bool IsDoorArt", full_rendered_start
        )
        self.assertIn(
            "path.StartsWith(EnglishLocalizedCardRoot",
            source[full_rendered_start:full_rendered_end],
        )

    def test_windows_handoff_names_exact_package_and_runtime_matrix(self) -> None:
        source = WINDOWS_HANDOFF_PATH.read_text(encoding="utf-8")
        for token in (
            EXPECTED_PACKAGE,
            EXPECTED_ARCHIVE_SHA256,
            "Assets/Resources/Cards/EnglishLocalized",
            "Assets/Resources/Localization/english_cards.json",
            "generated `.meta`",
            "hover preview",
            "tap/use preview",
            "card_absorb_curse",
            "hard_skill_door_breath",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)


if __name__ == "__main__":
    unittest.main()
