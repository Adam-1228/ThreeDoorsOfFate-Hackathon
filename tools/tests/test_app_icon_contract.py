from __future__ import annotations

import hashlib
import json
import struct
import unittest
from pathlib import Path


PROJECT = Path(__file__).resolve().parents[2]
ICON_RELATIVE = Path(
    "Assets/Art/Branding/AppIcon/three_doors_app_icon_1024.png"
)
ICON = PROJECT / ICON_RELATIVE
META = ICON.with_suffix(ICON.suffix + ".meta")
PROVENANCE = PROJECT / "docs/art/app_icon_provenance.json"
EXPECTED_META_GUID = "f1454d7d40b174ed19f443eba524129b"
EXPECTED_REFERENCES = {
    "Assets/Art/Backgrounds/bg_main_menu_three_doors.png",
    "Assets/Art/UI/MainMenu/title_logo_three_doors.png",
    "Assets/Art/Branding/AppIcon/three_doors_app_icon_1024.png",
}


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_png_ihdr(path: Path) -> tuple[int, int, int, int]:
    header = path.read_bytes()[:33]
    if len(header) != 33 or header[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a complete PNG header")
    length = struct.unpack(">I", header[8:12])[0]
    if length != 13 or header[12:16] != b"IHDR":
        raise ValueError("PNG does not start with a 13-byte IHDR")
    width, height, bit_depth, color_type, _, _, _ = struct.unpack(
        ">IIBBBBB", header[16:29]
    )
    return width, height, bit_depth, color_type


class AppIconContractTests(unittest.TestCase):
    def test_production_icon_is_opaque_1024_rgb_png(self) -> None:
        self.assertTrue(ICON.is_file(), ICON)
        self.assertEqual((1024, 1024, 8, 2), read_png_ihdr(ICON))

    def test_provenance_matches_production_bytes_and_preserved_guid(self) -> None:
        self.assertTrue(
            PROVENANCE.is_file(),
            "Generate docs/art/app_icon_provenance.json before integrating the icon.",
        )
        provenance = json.loads(PROVENANCE.read_text(encoding="utf-8"))

        self.assertEqual("single-sealed-fate-door-v1", provenance["designId"])
        self.assertEqual("OpenAI built-in ImageGen", provenance["generator"])
        self.assertEqual(ICON_RELATIVE.as_posix(), provenance["productionPath"])
        self.assertEqual(sha256(ICON), provenance["productionSha256"])
        self.assertNotEqual(
            provenance["previousSha256"],
            provenance["productionSha256"],
        )

        self.assertTrue(provenance["previousPath"].strip())
        self.assertRegex(provenance["previousSha256"], r"^[0-9a-f]{64}$")
        self.assertEqual(EXPECTED_META_GUID, provenance["metaGuid"])
        self.assertEqual(EXPECTED_REFERENCES, set(provenance["referencePaths"]))
        self.assertTrue(provenance["finalPrompt"].strip())

        meta = META.read_text(encoding="utf-8")
        self.assertIn(f"guid: {EXPECTED_META_GUID}\n", meta)


if __name__ == "__main__":
    unittest.main()
