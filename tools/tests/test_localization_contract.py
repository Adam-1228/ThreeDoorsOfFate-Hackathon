from __future__ import annotations

import json
import re
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
CATALOG_PATH = (
    PROJECT_ROOT / "Assets/Resources/Localization/game_text.json"
)
LANGUAGE_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Localization/GameLanguage.cs"
)
RUNTIME_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Localization/GameLocalization.cs"
)
BINDING_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Localization/LocalizedTextBinding.cs"
)
CONTROLLER_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
)
CONTROLLER_LOCALIZATION_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.Localization.cs"
)
HOW_TO_PLAY_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.HowToPlay.cs"
)
ACHIEVEMENTS_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.Achievements.cs"
)
PERSISTENCE_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.Persistence.cs"
)
REWARDED_ADS_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.RewardedAds.cs"
)
ACHIEVEMENT_PROGRESS_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Platform/AchievementProgress.cs"
)
PLAYER_PREFS_PROGRESS_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Platform/PlayerPrefsProgressStore.cs"
)
APPLE_RUNTIME_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Platform/AppleGameServicesRuntime.cs"
)
IOS_RELEASE_CONFIGURATION_PATH = (
    PROJECT_ROOT / "Assets/Scripts/Platform/IOSReleaseConfiguration.cs"
)
PROJECT_SETTINGS_PATH = PROJECT_ROOT / "ProjectSettings/ProjectSettings.asset"
CARD_VIEW_PATH = PROJECT_ROOT / "Assets/Scripts/UI/CardView.cs"
WINDOWS_HANDOFF_PATH = (
    PROJECT_ROOT
    / "docs/release/three-doors-of-fate-1.0.3-windows-verification.md"
)


def placeholders(value: str) -> set[str]:
    return set(re.findall(r"\{\d+(?::[^{}]+)?\}", value))


def extract_method(source: str, method_name: str) -> str:
    signature = re.search(
        rf"\b(?:private|public|protected|internal)\s+"
        rf"(?:static\s+)?[A-Za-z0-9_<>,.\[\]?]+\s+"
        rf"{re.escape(method_name)}\s*\(",
        source,
    )
    if signature is None:
        raise AssertionError(f"method not found: {method_name}")

    opening_brace = source.find("{", signature.end())
    if opening_brace < 0:
        raise AssertionError(f"method body not found: {method_name}")

    depth = 0
    for index in range(opening_brace, len(source)):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                return source[opening_brace : index + 1]

    raise AssertionError(f"unterminated method body: {method_name}")


def _decode_csharp_escape(source: str, index: int) -> tuple[str, int]:
    if index + 1 >= len(source):
        return "\\", index + 1
    escaped = source[index + 1]
    return {
        "n": "\n",
        "r": "\r",
        "t": "\t",
        '"': '"',
        "\\": "\\",
    }.get(escaped, escaped), index + 2


def _skip_csharp_quoted(source: str, index: int, quote: str) -> int:
    index += 1
    while index < len(source):
        if source[index] == "\\":
            index += 2
            continue
        if source[index] == quote:
            return index + 1
        index += 1
    return index


def _skip_interpolation_expression(source: str, index: int) -> int:
    depth = 1
    while index < len(source) and depth > 0:
        if source[index] in ('"', "'"):
            index = _skip_csharp_quoted(source, index, source[index])
            continue
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
        index += 1
    return index


def extract_csharp_string_values(source: str) -> list[str]:
    values: list[str] = []
    index = 0
    while index < len(source):
        interpolated = source.startswith('$"', index)
        if interpolated:
            index += 2
        elif source[index] == '"':
            index += 1
        else:
            index += 1
            continue

        value: list[str] = []
        placeholder_index = 0
        while index < len(source):
            if source[index] == "\\":
                decoded, index = _decode_csharp_escape(source, index)
                value.append(decoded)
                continue
            if source[index] == '"':
                index += 1
                break
            if interpolated and source.startswith("{{", index):
                value.append("{")
                index += 2
                continue
            if interpolated and source.startswith("}}", index):
                value.append("}")
                index += 2
                continue
            if interpolated and source[index] == "{":
                index = _skip_interpolation_expression(source, index + 1)
                value.append(f"{{{placeholder_index}}}")
                placeholder_index += 1
                continue
            value.append(source[index])
            index += 1
        values.append("".join(value))
    return values


def extract_call_arguments(source: str, call_name: str) -> list[list[str]]:
    calls: list[list[str]] = []
    for match in re.finditer(rf"\b{re.escape(call_name)}\s*\(", source):
        opening = source.find("(", match.start())
        paren_depth = 1
        bracket_depth = 0
        brace_depth = 0
        argument_start = opening + 1
        arguments: list[str] = []
        index = opening + 1
        while index < len(source) and paren_depth > 0:
            if source.startswith('$"', index):
                string_start = index
                index += 2
                interpolation_depth = 0
                while index < len(source):
                    if source[index] == "\\":
                        index += 2
                        continue
                    if source[index] == "{" and not source.startswith("{{", index):
                        interpolation_depth += 1
                    elif source[index] == "}" and interpolation_depth > 0:
                        interpolation_depth -= 1
                    elif source[index] == '"' and interpolation_depth == 0:
                        index += 1
                        break
                    index += 1
                if index <= string_start:
                    break
                continue
            if source[index] in ('"', "'"):
                index = _skip_csharp_quoted(source, index, source[index])
                continue
            character = source[index]
            if character == "(":
                paren_depth += 1
            elif character == ")":
                paren_depth -= 1
                if paren_depth == 0:
                    arguments.append(source[argument_start:index].strip())
                    calls.append(arguments)
                    break
            elif character == "[":
                bracket_depth += 1
            elif character == "]":
                bracket_depth -= 1
            elif character == "{":
                brace_depth += 1
            elif character == "}":
                brace_depth -= 1
            elif (
                character == ","
                and paren_depth == 1
                and bracket_depth == 0
                and brace_depth == 0
            ):
                arguments.append(source[argument_start:index].strip())
                argument_start = index + 1
            index += 1
    return calls


def extract_text_assignment_expressions(source: str) -> list[str]:
    expressions: list[str] = []
    for match in re.finditer(r"\.text\s*=", source):
        index = match.end()
        start = index
        paren_depth = bracket_depth = brace_depth = 0
        while index < len(source):
            if source.startswith('$"', index):
                index = _skip_csharp_quoted(source, index + 1, '"')
                continue
            if source[index] in ('"', "'"):
                index = _skip_csharp_quoted(source, index, source[index])
                continue
            character = source[index]
            if character == "(":
                paren_depth += 1
            elif character == ")":
                paren_depth -= 1
            elif character == "[":
                bracket_depth += 1
            elif character == "]":
                bracket_depth -= 1
            elif character == "{":
                brace_depth += 1
            elif character == "}":
                brace_depth -= 1
            elif character == ";" and not any(
                (paren_depth, bracket_depth, brace_depth)
            ):
                expressions.append(source[start:index])
                break
            index += 1
    return expressions


def iter_json_strings(value: object):
    if isinstance(value, str):
        yield value
    elif isinstance(value, list):
        for item in value:
            yield from iter_json_strings(item)
    elif isinstance(value, dict):
        for item in value.values():
            yield from iter_json_strings(item)


class LocalizationContractTests(unittest.TestCase):
    def assert_file_exists(self, path: Path) -> None:
        self.assertTrue(path.is_file(), f"required file is missing: {path}")

    def test_required_localization_files_exist(self) -> None:
        for path in (CATALOG_PATH, LANGUAGE_PATH, RUNTIME_PATH, BINDING_PATH):
            with self.subTest(path=path):
                self.assert_file_exists(path)

    def test_catalog_entries_are_unique_bilingual_and_placeholder_safe(self) -> None:
        self.assert_file_exists(CATALOG_PATH)
        payload = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
        entries = payload.get("entries")
        self.assertIsInstance(entries, list)
        self.assertGreater(len(entries), 0)

        keys = [entry.get("key", "") for entry in entries]
        self.assertEqual(len(keys), len(set(keys)))
        for entry in entries:
            with self.subTest(key=entry.get("key")):
                self.assertTrue(entry.get("key", "").strip())
                self.assertTrue(entry.get("ko", "").strip())
                self.assertTrue(entry.get("en", "").strip())
                self.assertEqual(
                    placeholders(entry["ko"]),
                    placeholders(entry["en"]),
                )

    def test_language_policy_has_saved_override_and_korean_only_system_default(self) -> None:
        self.assert_file_exists(LANGUAGE_PATH)
        source = LANGUAGE_PATH.read_text(encoding="utf-8")
        self.assertIn('PreferenceKey = "ThreeDoorsOfFate.Language"', source)
        self.assertRegex(
            source,
            re.compile(
                r"Resolve\(\s*string\s+savedValue\s*,\s*"
                r"SystemLanguage\s+systemLanguage\s*\)",
                re.MULTILINE,
            ),
        )
        self.assertIn('savedValue == "ko"', source)
        self.assertIn('savedValue == "en"', source)
        self.assertIn("systemLanguage == SystemLanguage.Korean", source)
        self.assertIn("GameLanguage.English", source)

    def test_runtime_exposes_lookup_persistence_and_change_event(self) -> None:
        self.assert_file_exists(RUNTIME_PATH)
        source = RUNTIME_PATH.read_text(encoding="utf-8")
        for token in (
            "public static GameLanguage CurrentLanguage",
            "public static bool IsEnglish",
            "public static event Action LanguageChanged",
            "public static void Initialize(SystemLanguage systemLanguage)",
            "public static void SetLanguage(GameLanguage language)",
            "public static string Text(string key)",
            "public static string Format(string key, params object[] args)",
            "PlayerPrefs.Save();",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)
        self.assertRegex(
            source,
            re.compile(
                r"PlayerPrefs\.SetString\(\s*"
                r"GameLanguagePolicy\.PreferenceKey",
                re.MULTILINE,
            ),
        )

    def test_runtime_supports_catalog_backed_static_source_lookup(self) -> None:
        self.assert_file_exists(RUNTIME_PATH)
        source = RUNTIME_PATH.read_text(encoding="utf-8")
        self.assertIn("public static string TextFromSource(string source)", source)
        self.assertIn("EntriesByKoreanSource", source)

    def test_runtime_supports_catalog_backed_formatted_source_lookup(self) -> None:
        self.assert_file_exists(RUNTIME_PATH)
        source = RUNTIME_PATH.read_text(encoding="utf-8")
        for token in (
            "SourcePatterns",
            "TryTranslateFormattedSource",
            "pattern.TryTranslate",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

    def test_shared_text_builders_bind_static_source_through_catalog(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        add_text = extract_method(source, "AddText")
        set_label = extract_method(source, "SetButtonLabel")

        self.assertIn("GameLocalization.TextFromSource(text)", add_text)
        self.assertIn("BindLocalizedSourceText(uiText, text)", add_text)
        self.assertIn("GameLocalization.TextFromSource(label)", set_label)
        self.assertIn("BindLocalizedSourceText(text, label)", set_label)

    def test_log_rendering_translates_each_canonical_entry_before_wrapping(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        refresh_log = extract_method(source, "RefreshLog")
        self.assertIn(
            "GameLocalization.TextFromSource(combatLog[i])",
            refresh_log,
        )

    def test_live_text_binding_refreshes_and_unsubscribes(self) -> None:
        self.assert_file_exists(BINDING_PATH)
        source = BINDING_PATH.read_text(encoding="utf-8")
        self.assertIn("Configure(Text target, Func<string> resolver)", source)
        self.assertIn("GameLocalization.LanguageChanged += Refresh", source)
        self.assertIn("GameLocalization.LanguageChanged -= Refresh", source)
        self.assertIn("target.text = resolver()", source)

    def test_live_text_binding_captures_later_direct_assignments(self) -> None:
        self.assert_file_exists(BINDING_PATH)
        source = BINDING_PATH.read_text(encoding="utf-8")
        self.assertIn("private string renderedText", source)
        self.assertIn("private void LateUpdate()", source)
        self.assertIn("target.text == renderedText", source)
        self.assertIn("GameLocalization.TextFromSource(source)", source)

    def test_localization_initializes_before_runtime_ui(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        awake = extract_method(source, "Awake")
        self.assertIn("GameLocalization.Initialize", awake)
        self.assertLess(
            awake.index("GameLocalization.Initialize"),
            awake.index("BuildShell"),
        )

    def test_settings_has_persistent_korean_and_english_choices(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        settings = extract_method(source, "ShowSettingsPanel")
        for token in (
            '"settings.language"',
            "GameLanguage.Korean",
            "GameLanguage.English",
            "SetGameLanguage",
            "UpdateLanguageSelectionState",
        ):
            with self.subTest(token=token):
                self.assertIn(token, settings)

    def test_main_menu_settings_entry_has_gear_and_localized_label(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        menu = extract_method(source, "ShowMainMenu")
        self.assertIn("AddMainMenuIconButton", menu)
        self.assertIn('"menu.settings"', menu)
        self.assertIn("settingsIconSprite", menu)
        self.assertNotRegex(
            menu,
            re.compile(
                r'AddMainMenuButton\([^;]+"옵션"\s*,\s*"옵션"',
                re.DOTALL,
            ),
        )

    def test_controller_localization_helpers_define_card_boundary(self) -> None:
        self.assert_file_exists(CONTROLLER_LOCALIZATION_PATH)
        source = CONTROLLER_LOCALIZATION_PATH.read_text(encoding="utf-8")
        for token in (
            "private static string L(string key)",
            "private static string LF(string key, params object[] args)",
            "LocalizedTextBinding",
            "SetGameLanguage(GameLanguage language)",
            "GetLocalizedCardName(CardData card)",
            "CardLocalization.GetName(card.CardId, card.DisplayName)",
            "GetLocalizedCardRules(CardData card)",
            "CardLocalization.GetRules(card.CardId, card.RulesText)",
            "GetLocalizedCardFullSprite(CardData card)",
            "CardLocalization.GetFullCardSprite(card.CardId, card.FullCardSprite)",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

    def test_precombat_player_visible_calls_are_localized(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        precombat = source[: source.index("private void RenderCombat()")]
        visible_values: set[str] = set()

        visible_argument_positions = {
            "AddText": (2,),
            "AddButton": (2,),
            "SetButtonLabel": (1,),
            "AddSettingsMenuButton": (2,),
            "AddClassDetailButton": (2,),
            "AddClassDetailActionButton": (2,),
            "AddMainMenuButton": (2,),
            "AddOptionToggleButton": (2,),
            "AddDoorChoiceLabelBox": (2,),
            "AddDoorChoiceButton": (2,),
            "AddRunStatusLabelBox": (2,),
            "AddRunStatusTextButton": (2,),
            "AddRunStatusCard": (1,),
            "AddLog": (0,),
            "ShowRunStatusDetail": (0, 1),
        }
        for call_name, positions in visible_argument_positions.items():
            for arguments in extract_call_arguments(precombat, call_name):
                for position in positions:
                    if position >= len(arguments):
                        continue
                    visible_values.update(
                        value
                        for value in extract_csharp_string_values(
                            arguments[position]
                        )
                        if re.search(r"[가-힣]", value)
                    )

        for expression in extract_text_assignment_expressions(precombat):
            visible_values.update(
                value
                for value in extract_csharp_string_values(expression)
                if re.search(r"[가-힣]", value)
            )

        source_data = precombat[
            precombat.index("private const string SurvivorTitleText") :
            precombat.index('[Header("Cards")]')
        ]
        visible_values.update(
            value
            for value in extract_csharp_string_values(source_data)
            if re.search(r"[가-힣]", value)
        )

        display_string_methods = (
            "GetDifficultyName",
            "GetDifficultyDescription",
            "GetClassInfoSectionTitle",
            "GetRunItemCollectionTitle",
            "CreateBossDoorOption",
            "CreateDoorOption",
            "GetDoorHint",
            "GetBossDoorHint",
            "GetBossRewardForecastText",
            "GetPostCombatSustainHint",
            "GetRunItemDiscoveryHint",
            "GetUnlockedRunItemTypeSummary",
            "CreateBossEnemy",
            "GetCurrentBossDoorName",
            "CreateDebtClearBoss",
        )
        for method_name in display_string_methods:
            body = extract_method(precombat, method_name)
            visible_values.update(
                value
                for value in extract_csharp_string_values(body)
                if re.search(r"[가-힣]", value)
            )

        catalog_values = {
            entry["ko"]
            for entry in json.loads(
                CATALOG_PATH.read_text(encoding="utf-8")
            )["entries"]
        }
        missing = sorted(visible_values - catalog_values)
        self.assertEqual([], missing, "unlocalized precombat sources")

    def test_combat_and_postcombat_player_visible_calls_are_localized(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        postcombat = source[source.index("private void RenderCombat()") :]
        visible_values: set[str] = set()

        visible_argument_positions = {
            "AddText": (2,),
            "AddButton": (2,),
            "SetButtonLabel": (1,),
            "AddSettingsMenuButton": (2,),
            "AddClassDetailButton": (2,),
            "AddClassDetailActionButton": (2,),
            "AddShopActionButton": (2,),
            "AddShopLabelBox": (2,),
            "AddShopPanelText": (2,),
            "AddStatusSectionTitle": (1,),
            "AddFramedModalTitle": (2,),
            "AddDetailText": (2,),
            "AddStatusMeterBar": (2,),
            "AddRunStatusTextButton": (2,),
            "AddRunStatusCard": (1,),
            "AddGameOverButton": (2,),
            "AddPostTenChoice": (1, 2),
            "AddShopSoldSlot": (1, 2),
            "AddLog": (0,),
            "TriggerCombatFeedback": (0,),
            "AddCenteredMessage": (0, 1),
            "ShowRunStatusDetail": (0, 1),
        }
        for call_name, positions in visible_argument_positions.items():
            for arguments in extract_call_arguments(postcombat, call_name):
                for position in positions:
                    if position >= len(arguments):
                        continue
                    visible_values.update(
                        value
                        for value in extract_csharp_string_values(
                            arguments[position]
                        )
                        if re.search(r"[가-힣]", value)
                    )

        for expression in extract_text_assignment_expressions(postcombat):
            visible_values.update(
                value
                for value in extract_csharp_string_values(expression)
                if re.search(r"[가-힣]", value)
            )

        display_string_methods = (
            "BuildActiveCombinationHudText",
            "GetCombinationImpactName",
            "GetEquippedRunItemNames",
            "BuildEquippedRunItemsText",
            "GetRunItemSlotUnlockLabel",
            "GetRunItemTypeName",
            "GetMainMenuEndlessRecordText",
            "BuildEnemyIntentLabel",
            "BuildEnemyCardPool",
            "AddBossUniqueCards",
            "GetJourneyEndingMessage",
            "GetBuildRecipe",
            "GetCombinationRecipes",
            "GetHardCombinationRecipes",
            "GetBuildStatusLabel",
            "BuildRunOverviewText",
            "BuildRunSummaryText",
            "BuildRunJudgementText",
            "BuildStatusDetailText",
            "BuildCombinationOverviewText",
            "BuildCombinationStatusText",
            "BuildShopCombinationText",
            "BuildCombinationColumnText",
            "BuildDeckOverviewText",
            "BuildDeckOverviewCompactText",
            "BuildDeckListText",
            "BuildCharacterTraitSummaryText",
            "BuildCharacterTraitText",
            "GetHardClassTraitName",
            "GetHardClassTraitText",
            "BuildCombatAwakeningSummaryText",
            "BuildCombatAwakeningText",
            "BuildDecisionHintText",
            "GetCardCategoryName",
            "GetClassProfile",
            "GetClassName",
        )
        for method_name in display_string_methods:
            body = extract_method(postcombat, method_name)
            visible_values.update(
                value
                for value in extract_csharp_string_values(body)
                if re.search(r"[가-힣]", value)
            )

        catalog_values = {
            entry["ko"]
            for entry in json.loads(
                CATALOG_PATH.read_text(encoding="utf-8")
            )["entries"]
        }
        visible_values.update({
            "장착 슬롯이 가득 찼습니다. 교체할 아이템을 고르거나 새 아이템을 보관하세요.",
            "{0} 슬롯은 현재 난이도에서 잠겨 있습니다. 보관한 뒤 상위 난이도에서 장착할 수 있습니다.",
            "금화 {0}로 모든 빚을 청산하고 직업별 진엔딩을 영구 해금합니다.",
            "빚이 없습니다. 직업별 진엔딩을 영구 해금합니다.",
            "덱과 손패가 모두 소진되었습니다. 더 이상 카드를 사용할 수 없어 패배했습니다.",
            "동굴이 또 하나의 이름을 삼켰습니다.",
            "완성됨\n강화 최대 {0}/{1}",
            "완성됨\n강화 {0}/{1}\n다음 {2}금",
            "필요 카드\n{0}",
            "필요 카드를 모으면\n강화가 열립니다",
            "현재 빚 {0} / 금화 {1} / 무한 최고 기록 {2}문",
            "해금 확인",
            "다음 여정으로",
        })
        missing = sorted(visible_values - catalog_values)
        self.maxDiff = None
        self.assertEqual([], missing, "unlocalized combat/postcombat sources")

    def test_card_text_routes_through_the_explicit_localization_boundary(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        populate_card = extract_method(source, "PopulateCardPresentation")
        self.assertIn("GetLocalizedCardName(card)", populate_card)
        self.assertIn("GetLocalizedCardRules(card)", populate_card)

    def test_card_view_localizes_name_rules_and_metadata(self) -> None:
        source = CARD_VIEW_PATH.read_text(encoding="utf-8")
        refresh = extract_method(source, "RefreshLocalizedLabels")
        for token in (
            "GameLocalization.LanguageChanged += RefreshLocalizedLabels",
            "GameLocalization.LanguageChanged -= RefreshLocalizedLabels",
            'GameLocalization.Text("card.category.attack")',
            'GameLocalization.Text("card.category.defense")',
            'GameLocalization.Text("card.category.skill")',
            'GameLocalization.Text("card.category.curse")',
            'GameLocalization.Text("card.rarity.common")',
            'GameLocalization.Text("card.rarity.rare")',
            'GameLocalization.Text("card.rarity.curse")',
            'GameLocalization.Format("card.meta", category, rarity)',
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

        self.assertIn(
            "CardLocalization.GetName(cardData.CardId, cardData.DisplayName)",
            refresh,
        )
        self.assertIn(
            "CardLocalization.GetRules(cardData.CardId, cardData.RulesText)",
            refresh,
        )

    def test_journey_fallback_titles_are_bilingual_catalog_entries(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        method = extract_method(source, "GetJourneyEndingTitleText")
        self.assertIn('L("ending.title.', method)
        self.assertNotIn('return "The ', method)

    def test_how_to_play_uses_keys_and_never_shows_korean_screenshots_in_english(self) -> None:
        source = HOW_TO_PLAY_PATH.read_text(encoding="utf-8")
        for token in (
            "HowToPlayTitleKeys",
            "HowToPlayCaptionKeys",
            "HowToPlayEnglishStepKeys",
            "howToPlayEnglishVisualRoot",
            "GameLocalization.IsEnglish",
            "BuildEnglishHowToPlayVisual",
            "showKoreanScreenshot",
        ):
            with self.subTest(token=token):
                self.assertIn(token, source)

        page = extract_method(source, "ShowHowToPlayPage")
        self.assertIn("!GameLocalization.IsEnglish", page)
        self.assertIn("bool showHandFlowPractice = howToPlayPageIndex == 3", page)
        self.assertIn("&& !showHandFlowPractice", page)
        self.assertIn("howToPlayImage.gameObject.SetActive(showKoreanScreenshot)", page)
        self.assertRegex(
            page,
            re.compile(
                r"howToPlayEnglishVisualRoot\.gameObject\.SetActive\(\s*"
                r"GameLocalization\.IsEnglish\s*\|\|\s*showHandFlowPractice\s*\)",
                re.MULTILINE,
            ),
        )
        self.assertIn("BuildHandFlowPractice()", page)
        self.assertNotIn("HowToPlayTitles[", page)
        self.assertNotIn("HowToPlayCaptions[", page)

    def test_tutorial_achievement_persistence_and_rewarded_ad_copy_is_catalogued(self) -> None:
        visible_values: set[str] = set()

        how_to_play = HOW_TO_PLAY_PATH.read_text(encoding="utf-8")
        for method_name in ("ShowHowToPlay", "ShowHowToPlayPage"):
            method = extract_method(how_to_play, method_name)
            for call_name, positions in {
                "AddText": (2,),
                "AddSettingsMenuButton": (2,),
                "SetButtonLabel": (1,),
            }.items():
                for arguments in extract_call_arguments(method, call_name):
                    for position in positions:
                        if position < len(arguments):
                            visible_values.update(
                                value
                                for value in extract_csharp_string_values(
                                    arguments[position]
                                )
                                if re.search(r"[가-힣]", value)
                            )

        achievements = ACHIEVEMENTS_PATH.read_text(encoding="utf-8")
        for method_name in (
            "ShowAchievements",
            "GetAchievementCardModels",
            "AddAchievementSlot",
            "RefreshAchievementDetail",
        ):
            method = extract_method(achievements, method_name)
            visible_values.update(
                value
                for value in extract_csharp_string_values(method)
                if re.search(r"[가-힣]", value)
                and not value.startswith("업적 ")
            )

        achievement_progress = ACHIEVEMENT_PROGRESS_PATH.read_text(
            encoding="utf-8"
        )
        visible_values.update(
            value
            for value in extract_csharp_string_values(achievement_progress)
            if re.search(r"[가-힣]", value)
        )

        persistence = PERSISTENCE_PATH.read_text(encoding="utf-8")
        for method_name in (
            "AddHardRunSaveLoadControls",
            "SaveRunFromSettingsPanel",
            "ContinueSavedRun",
        ):
            method = extract_method(persistence, method_name)
            for call_name, positions in {
                "AddSettingsMenuButton": (2,),
                "AddLog": (0,),
            }.items():
                for arguments in extract_call_arguments(method, call_name):
                    for position in positions:
                        if position < len(arguments):
                            visible_values.update(
                                value
                                for value in extract_csharp_string_values(
                                    arguments[position]
                                )
                                if re.search(r"[가-힣]", value)
                            )

        rewarded_ads = REWARDED_ADS_PATH.read_text(encoding="utf-8")
        for method_name in (
            "RefreshRewardedRelicAction",
            "ShowRewardedRelicResult",
        ):
            method = extract_method(rewarded_ads, method_name)
            visible_values.update(
                value
                for expression in extract_text_assignment_expressions(method)
                for value in extract_csharp_string_values(expression)
                if re.search(r"[가-힣]", value)
            )
            for call_name, positions in {
                "AddText": (2,),
                "AddFramedModalTitle": (2,),
            }.items():
                for arguments in extract_call_arguments(method, call_name):
                    for position in positions:
                        if position < len(arguments):
                            visible_values.update(
                                value
                                for value in extract_csharp_string_values(
                                    arguments[position]
                                )
                                if re.search(r"[가-힣]", value)
                            )

        visible_values.update({
            "{0} | {1}\n{2}\n{3}\n\n보관함에 추가되었습니다. 자동으로 장착되지 않습니다.",
        })
        catalog_values = {
            entry["ko"]
            for entry in json.loads(
                CATALOG_PATH.read_text(encoding="utf-8")
            )["entries"]
        }
        missing = sorted(visible_values - catalog_values)
        self.maxDiff = None
        self.assertEqual([], missing, "unlocalized secondary-surface sources")

    def test_controller_routes_game_center_access_point_by_surface(self) -> None:
        controller = CONTROLLER_PATH.read_text(encoding="utf-8")
        how_to_play = HOW_TO_PLAY_PATH.read_text(encoding="utf-8")
        achievements = ACHIEVEMENTS_PATH.read_text(encoding="utf-8")

        main_menu = extract_method(controller, "ShowMainMenu")
        clear_content = extract_method(controller, "ClearContent")
        settings = extract_method(controller, "ShowSettingsPanel")
        tutorial = extract_method(how_to_play, "ShowHowToPlay")
        achievement_surface = extract_method(achievements, "ShowAchievements")

        show_call = "AppleGameServicesRuntime.SetAccessPointVisible(true);"
        hide_call = "AppleGameServicesRuntime.SetAccessPointVisible(false);"
        self.assertIn(show_call, main_menu)
        self.assertGreater(main_menu.index(show_call), main_menu.index("ClearContent();"))
        self.assertIn(show_call, achievement_surface)
        self.assertIn(hide_call, clear_content)
        self.assertIn(hide_call, settings)
        self.assertIn(hide_call, tutorial)

    def test_every_literal_localization_reference_exists(self) -> None:
        payload = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
        catalog_keys = {entry["key"] for entry in payload["entries"]}
        referenced_keys: set[str] = set()
        source_paths = tuple(
            sorted((PROJECT_ROOT / "Assets/Scripts").rglob("*.cs"))
        )
        key_positions = {
            "L": (0,),
            "LF": (0,),
            "AddLocalizedText": (2,),
            "AddLocalizedMainMenuButton": (2,),
            "AddLocalizedSettingsMenuButton": (2,),
            "AddLocalizedOptionToggleButton": (2,),
            "AddMainMenuIconButton": (2,),
            "BindLocalizedText": (1,),
        }
        for path in source_paths:
            source = path.read_text(encoding="utf-8-sig")
            for call_name, positions in key_positions.items():
                for arguments in extract_call_arguments(source, call_name):
                    for position in positions:
                        if position >= len(arguments):
                            continue
                        referenced_keys.update(
                            value
                            for value in extract_csharp_string_values(
                                arguments[position]
                            )
                            if re.fullmatch(r"[A-Za-z][A-Za-z0-9_.-]+", value)
                        )

        self.assertTrue(referenced_keys)
        self.assertEqual(
            [],
            sorted(referenced_keys - catalog_keys),
            "literal localization references missing from catalog",
        )

    def test_every_catalog_entry_is_reachable_from_runtime_source_or_data(self) -> None:
        payload = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
        source_paths = tuple(
            sorted((PROJECT_ROOT / "Assets/Scripts").rglob("*.cs"))
        )
        source_blob_parts: list[str] = []
        source_values: set[str] = set()
        for path in source_paths:
            source = path.read_text(encoding="utf-8-sig")
            source_blob_parts.append(source)
            source_values.update(extract_csharp_string_values(source))

        for path in sorted((PROJECT_ROOT / "Assets/Data").rglob("*.json")):
            data = json.loads(path.read_text(encoding="utf-8"))
            source_values.update(iter_json_strings(data))

        source_blob = "\n".join(source_blob_parts)
        reserved_keys = set(payload.get("reservedKeys", []))
        unused = sorted(
            entry["key"]
            for entry in payload["entries"]
            if entry["key"] not in source_blob
            and entry["ko"] not in source_values
            and entry["ko"] not in source_blob
            and entry["key"] not in reserved_keys
        )
        self.assertEqual([], unused, "unused or unmarked catalog entries")

    def test_player_visible_sources_have_no_untranslated_english_fallbacks(self) -> None:
        payload = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
        catalog_sources = {entry["ko"] for entry in payload["entries"]}
        catalog_keys = {entry["key"] for entry in payload["entries"]}
        visible_values: set[str] = set()
        visible_argument_positions = {
            "AddText": (2,),
            "AddButton": (2,),
            "SetButtonLabel": (1,),
            "AddSettingsMenuButton": (2,),
            "AddClassDetailButton": (2,),
            "AddClassDetailActionButton": (2,),
            "AddMainMenuButton": (2,),
            "AddOptionToggleButton": (2,),
            "AddDoorChoiceLabelBox": (2,),
            "AddDoorChoiceButton": (2,),
            "AddRunStatusLabelBox": (2,),
            "AddRunStatusTextButton": (2,),
            "AddRunStatusCard": (1,),
            "AddShopActionButton": (2,),
            "AddShopLabelBox": (2,),
            "AddShopPanelText": (2,),
            "AddStatusSectionTitle": (1,),
            "AddFramedModalTitle": (2,),
            "AddDetailText": (2,),
            "AddStatusMeterBar": (2,),
            "AddGameOverButton": (2,),
            "AddPostTenChoice": (1, 2),
            "AddShopSoldSlot": (1, 2),
            "AddLog": (0,),
            "TriggerCombatFeedback": (0,),
            "AddCenteredMessage": (0, 1),
            "ShowRunStatusDetail": (0, 1),
        }
        for path in sorted(
            (PROJECT_ROOT / "Assets/Scripts/Game").glob(
                "ThreeDoorsGameController*.cs"
            )
        ):
            source = path.read_text(encoding="utf-8-sig")
            for call_name, positions in visible_argument_positions.items():
                for arguments in extract_call_arguments(source, call_name):
                    for position in positions:
                        if position < len(arguments):
                            visible_values.update(
                                extract_csharp_string_values(arguments[position])
                            )
            for expression in extract_text_assignment_expressions(source):
                visible_values.update(extract_csharp_string_values(expression))

        untranslated = sorted(
            value
            for value in visible_values
            if re.search(r"[A-Za-z]", value)
            and not re.search(r"[가-힣]", value)
            and value not in catalog_sources
            and value not in catalog_keys
        )
        self.maxDiff = None
        self.assertEqual(
            [],
            untranslated,
            "player-visible English source bypasses Korean localization",
        )

    def test_language_preference_is_not_part_of_cloud_progress(self) -> None:
        source = PLAYER_PREFS_PROGRESS_PATH.read_text(encoding="utf-8")
        integer_keys = extract_method(source, "GetIntegerKeys")
        string_keys = extract_method(source, "GetStringKeys")
        for method in (integer_keys, string_keys):
            self.assertNotIn("ThreeDoorsOfFate.Language", method)
            self.assertNotIn("GameLanguagePolicy.PreferenceKey", method)
            self.assertNotRegex(method, re.compile(r'Language[}".]'))

    def test_release_version_matches_ios_1_3_0_defaults(self) -> None:
        project_settings = PROJECT_SETTINGS_PATH.read_text(encoding="utf-8")
        release_configuration = IOS_RELEASE_CONFIGURATION_PATH.read_text(
            encoding="utf-8"
        )
        self.assertRegex(
            project_settings,
            re.compile(r"(?m)^  bundleVersion: 1\.3\.0$"),
        )
        self.assertRegex(
            project_settings,
            re.compile(r"(?m)^    iPhone: 13001$"),
        )
        self.assertIn('DefaultVersion = "1.3.0"', release_configuration)
        self.assertIn('DefaultBuildNumber = "13001"', release_configuration)

    def test_windows_verification_handoff_covers_external_release_boundary(self) -> None:
        self.assert_file_exists(WINDOWS_HANDOFF_PATH)
        handoff = WINDOWS_HANDOFF_PATH.read_text(encoding="utf-8")
        required_tokens = (
            "6000.4.11f1",
            "1.0.3",
            "build 7",
            "EditMode",
            "PlayMode",
            "Korean",
            "English",
            "Game Center",
            "72",
            "DisplayName",
            "RulesText",
            "FullCardSprite",
            "NSUserTrackingUsageDescription",
        )
        for token in required_tokens:
            with self.subTest(token=token):
                self.assertIn(token, handoff)


if __name__ == "__main__":
    unittest.main()
