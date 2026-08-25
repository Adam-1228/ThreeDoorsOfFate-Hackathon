import hashlib
import pathlib
import re
import struct
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
ART_ROOT = ROOT / "Assets/Resources/Achievements"
DEFINITION_SOURCE = ROOT / "Assets/Scripts/Platform/AchievementProgress.cs"

ARTWORK_NAMES = (
    "achievement_gambler_card_reading",
    "achievement_oracle_precise_prediction",
    "achievement_exile_curse_eater",
    "achievement_fate_cleaver_50",
    "achievement_iron_wall_40",
    "achievement_five_cards_turn",
    "achievement_deck_50",
    "achievement_cliffside_victory",
    "achievement_triple_contract",
    "achievement_build_masterpiece",
    "achievement_twentieth_door",
    "achievement_three_survivors",
)


def read_png_header(path: pathlib.Path) -> tuple[int, int, int, int]:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n" or data[12:16] != b"IHDR":
        raise AssertionError(f"not a PNG with an IHDR header: {path}")
    width, height, bit_depth, color_type = struct.unpack(">IIBB", data[16:26])
    return width, height, bit_depth, color_type


class AchievementArtwork120Tests(unittest.TestCase):
    def test_all_twelve_images_are_opaque_rgb_1024_squares(self) -> None:
        for name in ARTWORK_NAMES:
            with self.subTest(name=name):
                path = ART_ROOT / f"{name}.png"
                self.assertTrue(path.is_file(), f"missing achievement image: {path}")
                self.assertEqual(read_png_header(path), (1024, 1024, 8, 2))

    def test_images_are_distinct_and_bound_to_definitions(self) -> None:
        source = DEFINITION_SOURCE.read_text(encoding="utf-8")
        digests = set()
        for name in ARTWORK_NAMES:
            path = ART_ROOT / f"{name}.png"
            self.assertTrue(path.is_file(), f"missing achievement image: {path}")
            digests.add(hashlib.sha256(path.read_bytes()).hexdigest())
            self.assertIn(f'"Achievements/{name}"', source)

        self.assertEqual(len(digests), len(ARTWORK_NAMES))

    def test_unity_sprite_import_metadata_is_present_and_unique(self) -> None:
        guids = set()
        for name in ARTWORK_NAMES:
            meta_path = ART_ROOT / f"{name}.png.meta"
            self.assertTrue(meta_path.is_file(), f"missing Unity metadata: {meta_path}")
            meta = meta_path.read_text(encoding="utf-8")
            self.assertIn("textureType: 8", meta)
            self.assertIn("spriteMode: 1", meta)
            self.assertIn("enableMipMap: 0", meta)
            match = re.search(r"^guid: ([0-9a-f]{32})$", meta, re.MULTILINE)
            self.assertIsNotNone(match, meta_path)
            guids.add(match.group(1))

        self.assertEqual(len(guids), len(ARTWORK_NAMES))


if __name__ == "__main__":
    unittest.main()
