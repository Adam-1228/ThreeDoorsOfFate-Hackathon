import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
CONTROLLER = ROOT / "Assets/Scripts/Game/ThreeDoorsGameController.cs"
LAYOUT_POLICY = ROOT / "Assets/Scripts/UI/PcUiPresentationPolicy.cs"


class Ui120ContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.controller = CONTROLLER.read_text(encoding="utf-8")
        cls.layout = LAYOUT_POLICY.read_text(encoding="utf-8")

    def require(self, marker: str, source: str) -> None:
        if marker not in source:
            self.fail(f"missing UI 1.2.0 contract marker: {marker}")

    def test_progress_log_reserves_a_wider_horizontal_safe_inset(self) -> None:
        self.require(
            "LogTextSafe = new UiNormalizedRect(0.090f, 0.055f, 0.930f, 0.945f)",
            self.layout,
        )
        self.require('WrapDisplayLine($"- {localizedEntry}", 16, "  ")', self.controller)

    def test_class_detail_keeps_settings_available(self) -> None:
        for marker in (
            '"캐릭터 상세 설정"',
            '"menu.settings"',
            "classDetailSettingsButton.onClick.AddListener(ToggleSettingsPanel);",
            "new Vector2(0.835f, 0.905f)",
            "new Vector2(0.965f, 0.985f)",
        ):
            with self.subTest(marker=marker):
                self.require(marker, self.controller)

    def test_shop_relic_artwork_is_clipped_behind_one_frame(self) -> None:
        for marker in (
            '"아이템 상품 그림 마스크 영역"',
            "artViewport.gameObject.AddComponent<RectMask2D>();",
            'AddImage(artViewport, "아이템 상품 아이콘"',
            '"아이템 상품 프레임 오버레이"',
            "frameOverlay.sprite = itemFrame;",
            "frameOverlay.rectTransform.SetAsLastSibling();",
        ):
            with self.subTest(marker=marker):
                self.require(marker, self.controller)

        obsolete = (
            'AddPanel(slot, "아이템 상품 프레임", Color.white, '
            "GetRunStatusSlotFrameSprite())"
        )
        if obsolete in self.controller:
            self.fail(f"obsolete shop framing remains: {obsolete}")


if __name__ == "__main__":
    unittest.main()
