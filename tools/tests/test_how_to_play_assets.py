from __future__ import annotations

import hashlib
import json
import struct
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
TUTORIAL_ROOT = PROJECT_ROOT / "Assets/Resources/Tutorial"
EXPECTED_ASSETS = (
    "how_to_play_01_class.png",
    "how_to_play_02_doors.png",
    "how_to_play_03_combat.png",
    "how_to_play_04_card_use.png",
    "how_to_play_05_growth.png",
)
PROVENANCE_PATH = (
    PROJECT_ROOT / "docs/tutorial/how-to-play-image-provenance.json"
)


def read_png_dimensions(path: Path) -> tuple[int, int]:
    header = path.read_bytes()[:24]
    if len(header) != 24 or header[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"not a complete PNG header: {path}")
    if header[12:16] != b"IHDR":
        raise ValueError(f"PNG does not start with IHDR: {path}")
    return struct.unpack(">II", header[16:24])


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class HowToPlayAssetContractTests(unittest.TestCase):
    def test_exactly_five_ordered_tutorial_images_exist(self) -> None:
        actual = tuple(
            path.name for path in sorted(TUTORIAL_ROOT.glob("*.png"))
        )
        self.assertEqual(EXPECTED_ASSETS, actual)

    def test_tutorial_images_are_consistent_landscape_pngs(self) -> None:
        dimensions: list[tuple[int, int]] = []
        total_bytes = 0
        for name in EXPECTED_ASSETS:
            path = TUTORIAL_ROOT / name
            self.assertTrue(path.is_file(), f"missing tutorial image: {path}")
            dimensions.append(read_png_dimensions(path))
            total_bytes += path.stat().st_size

        for width, height in dimensions:
            with self.subTest(width=width, height=height):
                self.assertGreaterEqual(width, 1280)
                self.assertGreaterEqual(height, 720)
                self.assertGreater(width / height, 1.70)
                self.assertLess(width / height, 2.25)
        self.assertLessEqual(total_bytes, 30 * 1024 * 1024)

    def test_sprite_meta_and_provenance_match_output_bytes(self) -> None:
        self.assertTrue(
            PROVENANCE_PATH.is_file(),
            f"missing provenance manifest: {PROVENANCE_PATH}",
        )
        provenance = json.loads(PROVENANCE_PATH.read_text(encoding="utf-8"))
        self.assertEqual(1, provenance["schemaVersion"])
        self.assertEqual("2026-08-08", provenance["createdOn"])

        outputs = provenance["outputs"]
        self.assertEqual(
            EXPECTED_ASSETS,
            tuple(entry["asset"] for entry in outputs),
        )
        by_asset = {entry["asset"]: entry for entry in outputs}

        for name in EXPECTED_ASSETS:
            with self.subTest(asset=name):
                image_path = TUTORIAL_ROOT / name
                meta_path = TUTORIAL_ROOT / f"{name}.meta"
                self.assertTrue(
                    image_path.is_file(),
                    f"missing tutorial image: {image_path}",
                )
                self.assertTrue(
                    meta_path.is_file(),
                    f"missing Unity sprite metadata: {meta_path}",
                )
                self.assertEqual(sha256(image_path), by_asset[name]["sha256"])
                self.assertFalse(by_asset[name]["generatedKoreanExplanation"])

                meta = meta_path.read_text(encoding="utf-8")
                self.assertIn("enableMipMap: 0", meta)
                self.assertIn("spriteMode: 1", meta)
                self.assertIn("textureType: 8", meta)


if __name__ == "__main__":
    unittest.main()
