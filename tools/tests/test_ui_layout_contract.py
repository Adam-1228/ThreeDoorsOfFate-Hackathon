from __future__ import annotations

import json
import math
import re
import struct
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
CONTROLLER_PATH = PROJECT_ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
LOCALIZATION_CONTROLLER_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.Localization.cs"
)
REWARDED_PATH = (
    PROJECT_ROOT
    / "Assets/Scripts/Game/ThreeDoorsGameController.RewardedAds.cs"
)
FONT_PATH = PROJECT_ROOT / "Assets/Fonts/GowunBatang-Regular.ttf"
LOCALIZATION_PATH = (
    PROJECT_ROOT / "Assets/Resources/Localization/game_text.json"
)
GAME_CONTROLLER_ROOT = PROJECT_ROOT / "Assets/Scripts/Game"
HISTORY_CONTROLLER_PATH = (
    GAME_CONTROLLER_ROOT / "ThreeDoorsGameController.History140.cs"
)
HOW_TO_PLAY_QA_PATH = PROJECT_ROOT / "Assets/Editor/HowToPlaySourceQACapture.cs"
QUALITY_QA_PATH = PROJECT_ROOT / "Assets/Editor/Quality104QACapture.cs"

V140_PARTIAL_SUFFIXES = {
    "Contracts140",
    "Inspection140",
    "Checkpoint140",
    "Events140",
    "Encounter140",
    "Endless140",
    "History140",
}


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


def parse_anchor_pair(method_body: str, rect_name: str) -> tuple[float, float, float, float]:
    pattern = re.compile(
        rf"SetAnchors\(\s*{re.escape(rect_name)}\s*,\s*"
        r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*,\s*"
        r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*\)",
        re.MULTILINE,
    )
    match = pattern.search(method_body)
    if match is None:
        raise AssertionError(f"anchors not found for: {rect_name}")
    return tuple(float(value) for value in match.groups())


def parse_anchor_y_pair(method_body: str, rect_name: str) -> tuple[float, float]:
    pattern = re.compile(
        rf"SetAnchors\(\s*{re.escape(rect_name)}\s*,\s*"
        r"new Vector2\([^,]+,\s*([0-9.]+)f\)\s*,\s*"
        r"new Vector2\([^,]+,\s*([0-9.]+)f\)\s*\)",
        re.MULTILINE,
    )
    match = pattern.search(method_body)
    if match is None:
        raise AssertionError(f"vertical anchors not found for: {rect_name}")
    return tuple(float(value) for value in match.groups())


def parse_content_box_bounds(
    method_body: str, rect_name: str
) -> tuple[float, float, float, float]:
    pattern = re.compile(
        rf"RectTransform\s+{re.escape(rect_name)}\s*=\s*"
        r"AddRunStatusContentBox\(.*?"
        r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*,\s*"
        r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)",
        re.DOTALL,
    )
    match = pattern.search(method_body)
    if match is None:
        raise AssertionError(f"content-box bounds not found for: {rect_name}")
    return tuple(float(value) for value in match.groups())


def parse_class_choice_anchor_pair(
    method_body: str, character_class: str
) -> tuple[float, float, float, float]:
    pattern = re.compile(
        rf"CreateClassChoice\(\s*CharacterClass\.{re.escape(character_class)},"
        r".*?new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*,\s*"
        r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*\);",
        re.DOTALL,
    )
    match = pattern.search(method_body)
    if match is None:
        raise AssertionError(f"class-choice anchors not found for: {character_class}")
    return tuple(float(value) for value in match.groups())


def read_font_line_height_ratio(path: Path) -> float:
    data = path.read_bytes()
    table_count = struct.unpack_from(">H", data, 4)[0]
    tables: dict[str, tuple[int, int]] = {}
    for index in range(table_count):
        tag, _checksum, offset, length = struct.unpack_from(
            ">4sIII", data, 12 + index * 16
        )
        tables[tag.decode("ascii")] = (offset, length)

    head_offset, _ = tables["head"]
    hhea_offset, _ = tables["hhea"]
    units_per_em = struct.unpack_from(">H", data, head_offset + 18)[0]
    ascent, descent, line_gap = struct.unpack_from(
        ">hhh", data, hhea_offset + 4
    )
    return (ascent - descent + line_gap) / units_per_em


class UiLayoutContractTests(unittest.TestCase):
    def test_v140_controller_is_split_into_focused_partials(self) -> None:
        for suffix in V140_PARTIAL_SUFFIXES:
            path = GAME_CONTROLLER_ROOT / f"ThreeDoorsGameController.{suffix}.cs"
            with self.subTest(path=path):
                self.assertTrue(path.is_file(), f"missing v1.4.0 partial: {path}")

    def test_v140_main_menu_fits_five_and_six_safe_buttons(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        menu = extract_method(source, "ShowMainMenu")
        placement = extract_method(source, "GetMainMenuButtonRect")

        self.assertIn("supportsDesktopWindowControls ? 6 : 5", menu)
        self.assertIn('"menu.runHistory"', menu)
        self.assertIn("Mathf.Clamp(count, 1, 6)", placement)
        for count, width, gap in ((5, 0.16, 0.025), (6, 0.135, 0.018)):
            with self.subTest(count=count):
                total = count * width + (count - 1) * gap
                self.assertLessEqual(total, 0.90)
                self.assertGreaterEqual(0.5 - total * 0.5, 0.05 - 1e-6)
                self.assertLessEqual(0.5 + total * 0.5, 0.95 + 1e-6)

    def test_mobile_qa_captures_reflow_all_five_menu_routes(self) -> None:
        expected_names = (
            '"게임시작"',
            '"플레이 방법"',
            '"운명 기록"',
            '"업적"',
            '"설정"',
        )
        for path in (HOW_TO_PLAY_QA_PATH, QUALITY_QA_PATH):
            source = path.read_text(encoding="utf-8")
            body = extract_method(source, "ConfigureMobileMainMenuButtons")
            with self.subTest(path=path):
                for name in expected_names:
                    self.assertIn(name, body)
                self.assertIn("new object[] { button, index, 5 }", body)
                self.assertIn('FindDescendant(contentRoot, "게임종료")', body)

    def test_v140_release_qa_captures_required_surfaces_at_three_aspect_ratios(self) -> None:
        source = QUALITY_QA_PATH.read_text(encoding="utf-8")
        start_known_run = extract_method(source, "StartKnownRun")
        capture_state = extract_method(source, "CaptureState")

        for marker in (
            'new("16x9", 1920, 1080',
            '"iphone14_pro_max_landscape"',
            'new("4x3", 2048, 1536',
            '"starter_contracts"',
            '"run_status"',
            '"shop"',
            '"run_history"',
            '"run_history_detail"',
            '"TDOF_140_QA_DIR"',
            'RunHistoryStore.GetStorageKey(qaRunHistoryPrefix)',
        ):
            with self.subTest(marker=marker):
                self.assertIn(marker, source)

        self.assertIn("HideRunStatusPanelImmediately(", start_known_run)
        self.assertIn("RefreshLocalizedBindings(canvas);", capture_state)

    def test_v140_history_qa_has_narrow_recheck_entrypoint(self) -> None:
        source = QUALITY_QA_PATH.read_text(encoding="utf-8")
        history_capture = extract_method(source, "CaptureHistoryLanguage")

        self.assertIn("CaptureHistoryMatrix", source)
        for marker in ('"game_over"', '"run_history"', '"run_history_detail"'):
            with self.subTest(marker=marker):
                self.assertIn(marker, history_capture)
        self.assertIn("CaptureState(", history_capture)

    def test_shop_cards_require_inspection_before_purchase(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        shop = extract_method(source, "ShowShop")
        self.assertIn("CardInspectionMode.ShopBuy", shop)
        self.assertIn("cardButton.onClick.AddListener(inspectPurchase);", shop)
        self.assertIn("buy.onClick.AddListener(inspectPurchase);", shop)
        self.assertNotIn("cardButton.onClick.AddListener(purchase);", shop)
        self.assertNotIn("buy.onClick.AddListener(purchase);", shop)

    def test_run_history_frames_and_columns_stay_in_normalized_bounds(self) -> None:
        source = HISTORY_CONTROLLER_PATH.read_text(encoding="utf-8")
        history = extract_method(source, "ShowRunHistory")
        detail = extract_method(source, "ShowRunHistoryDetail")

        outer = parse_anchor_pair(history, "outer")
        detail_outer = parse_anchor_pair(detail, "outer")
        summary = parse_content_box_bounds(detail, "summaryPanel")
        loadout = parse_content_box_bounds(detail, "loadoutPanel")
        for rect in (outer, detail_outer, summary, loadout):
            with self.subTest(rect=rect):
                self.assertLess(rect[0], rect[2])
                self.assertLess(rect[1], rect[3])
                for value in rect:
                    self.assertGreaterEqual(value, 0.0)
                    self.assertLessEqual(value, 1.0)
        self.assertLessEqual(summary[2], loadout[0])
        self.assertIn('"운명 기록 목록 안전영역"', history)
        self.assertIn("PcUiLayoutPolicy.StatusDetailBody", history)
        self.assertIn('"운명 기록 상세 안전영역"', detail)
        self.assertIn("PcUiLayoutPolicy.StatusDetailBody", detail)
        self.assertGreaterEqual(detail.count("statusInnerPanelFrameSprite"), 2)

    def test_main_menu_settings_icon_and_label_use_separate_readable_columns(self) -> None:
        source = LOCALIZATION_CONTROLLER_PATH.read_text(encoding="utf-8")
        body = extract_method(source, "AddMainMenuIconButton")
        icon = parse_anchor_pair(body, "iconImage.rectTransform")
        label = parse_anchor_pair(body, "label.rectTransform")

        self.assertGreaterEqual(icon[2] - icon[0], 0.30)
        self.assertGreaterEqual(icon[3] - icon[1], 0.72)
        self.assertLessEqual(icon[2], label[0])
        for value in (*icon, *label):
            self.assertGreaterEqual(value, 0.0)
            self.assertLessEqual(value, 1.0)
        self.assertIn("label.resizeTextForBestFit = true;", body)
        minimum = re.search(r"label\.resizeTextMinSize\s*=\s*(\d+);", body)
        self.assertIsNotNone(minimum)
        self.assertGreaterEqual(int(minimum.group(1)), 16)

    def test_run_hud_splits_progress_and_resources_without_overlap(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        body = extract_method(source, "BuildShell")

        progress = parse_anchor_pair(body, "runProgressText.rectTransform")
        resources = parse_anchor_pair(body, "runResourcesText.rectTransform")
        self.assertLessEqual(resources[3], progress[1])

        for field_name in ("playerStatsText", "runProgressText", "runResourcesText"):
            self.assertIn(f"{field_name}.resizeTextForBestFit = true;", body)
            match = re.search(
                rf"{re.escape(field_name)}\.resizeTextMinSize\s*=\s*(\d+);",
                body,
            )
            self.assertIsNotNone(match)
            self.assertGreaterEqual(int(match.group(1)), 12)

    def test_settings_language_row_uses_two_nonoverlapping_equal_buttons(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        body = extract_method(source, "ShowSettingsPanel")

        self.assertIn("koreanLanguageButton", body)
        self.assertIn("englishLanguageButton", body)
        korean_match = re.search(
            r"SetAnchors\(\s*koreanLanguageButton\.GetComponent<RectTransform>\(\),\s*"
            r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\),\s*"
            r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*\)",
            body,
            re.MULTILINE,
        )
        english_match = re.search(
            r"SetAnchors\(\s*englishLanguageButton\.GetComponent<RectTransform>\(\),\s*"
            r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\),\s*"
            r"new Vector2\(([0-9.]+)f,\s*([0-9.]+)f\)\s*\)",
            body,
            re.MULTILINE,
        )
        self.assertIsNotNone(korean_match)
        self.assertIsNotNone(english_match)

        korean = tuple(float(value) for value in korean_match.groups())
        english = tuple(float(value) for value in english_match.groups())
        self.assertLessEqual(korean[2], english[0])
        self.assertAlmostEqual(korean[2] - korean[0], english[2] - english[0])
        self.assertEqual(korean[1], english[1])
        self.assertEqual(korean[3], english[3])

    def test_character_name_rect_fits_embedded_font_on_iphone_14_pro_max(self) -> None:
        source = CONTROLLER_PATH.read_text(encoding="utf-8")
        body = extract_method(source, "AddClassSelectionNameBox")

        self.assertIn("nameText.resizeTextForBestFit = true;", body)
        min_size_match = re.search(
            r"nameText\.resizeTextMinSize\s*=\s*(\d+);", body
        )
        max_size_match = re.search(
            r"nameText\.resizeTextMaxSize\s*=\s*(\d+);", body
        )
        self.assertIsNotNone(min_size_match)
        self.assertIsNotNone(max_size_match)
        self.assertGreaterEqual(int(min_size_match.group(1)), 16)

        _, label_min_y, _, label_max_y = parse_anchor_pair(body, "labelBox")
        _, text_min_y, _, text_max_y = parse_anchor_pair(
            body, "nameText.rectTransform"
        )
        self.assertLessEqual(text_min_y, 0.1201)
        self.assertGreaterEqual(text_max_y, 0.8799)

        screen_width = 2796
        screen_height = 1290
        safe_height = 1164
        scale_factor = math.sqrt(
            (screen_width / 1920.0) * (screen_height / 1080.0)
        )
        logical_safe_height = safe_height / scale_factor
        selection_body = extract_method(source, "ShowClassSelection")
        _, content_min_y, _, content_max_y = parse_anchor_pair(
            selection_body, "contentRoot"
        )
        _, choice_min_y, _, choice_max_y = parse_class_choice_anchor_pair(
            selection_body, "Gambler"
        )
        available_height = (
            logical_safe_height
            * (content_max_y - content_min_y)
            * (choice_max_y - choice_min_y)
            * (label_max_y - label_min_y)
            * (text_max_y - text_min_y)
        )
        required_height = (
            int(max_size_match.group(1)) * read_font_line_height_ratio(FONT_PATH)
        )
        self.assertGreaterEqual(
            available_height,
            required_height,
            "The iPhone text rect must fit one full Gowun Batang line.",
        )

    def test_reward_action_uses_one_safe_status_line(self) -> None:
        source = REWARDED_PATH.read_text(encoding="utf-8")
        self.assertNotIn("rewardedRelicRemainingLabel", source)

        add_body = extract_method(source, "AddRewardedRelicAction")
        label_start = add_body.index("rewardedRelicActionLabel = AddText")
        label_block = add_body[label_start : add_body.index(
            "RefreshRewardedRelicAction();", label_start
        )]
        self.assertIn("TextAnchor.MiddleCenter", label_block)
        self.assertIn("rewardedRelicActionLabel.resizeTextForBestFit = true;", label_block)
        self.assertIn("rewardedRelicActionLabel.resizeTextMinSize = 9;", label_block)
        self.assertIn("rewardedRelicActionLabel.resizeTextMaxSize = 13;", label_block)
        min_y, max_y = parse_anchor_y_pair(
            add_body, "rewardedRelicActionLabel.rectTransform"
        )
        self.assertGreaterEqual(min_y, 0.25)
        self.assertLessEqual(max_y, 0.75)

        refresh_body = extract_method(source, "RefreshRewardedRelicAction")
        self.assertRegex(
            refresh_body,
            re.compile(
                r'rewardedRelicActionLabel\.text\s*=\s*LF\(\s*'
                r'"rewarded\.item\.loading",\s*'
                r'dailyStatus\.RemainingCount\s*\);',
                re.DOTALL,
            ),
        )
        self.assertRegex(
            refresh_body,
            re.compile(
                r'rewardedRelicActionLabel\.text\s*=\s*LF\(\s*'
                r'"rewarded\.item\.ready",\s*'
                r'dailyStatus\.RemainingCount\s*\);',
                re.DOTALL,
            ),
        )

        catalog = json.loads(LOCALIZATION_PATH.read_text(encoding="utf-8"))
        entries = {entry["key"]: entry for entry in catalog["entries"]}
        for key in ("rewarded.item.loading", "rewarded.item.ready"):
            self.assertIn(key, entries)
            self.assertNotIn("\n", entries[key]["ko"])
            self.assertNotIn("\n", entries[key]["en"])

        controller_source = CONTROLLER_PATH.read_text(encoding="utf-8")
        add_text_body = extract_method(controller_source, "AddText")
        self.assertIn(
            "uiText.verticalOverflow = VerticalWrapMode.Truncate;",
            add_text_body,
        )


if __name__ == "__main__":
    unittest.main()
