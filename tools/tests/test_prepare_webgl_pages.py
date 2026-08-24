from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
TOOL_PATH = PROJECT_ROOT / "tools" / "prepare_webgl_pages.py"


class PrepareWebglPagesTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary_directory.cleanup)
        self.site_root = Path(self.temporary_directory.name)
        self.index_path = self.site_root / "index.html"
        self.original_html = """<!DOCTYPE html>
<html lang="en-us">
  <head><title>Unity Web Player | Three Doors of Fate</title></head>
  <body>
    <div id="unity-container" class="unity-desktop">
      <canvas id="unity-canvas" width="960" height="600"></canvas>
    </div>
    <script>var config = { productVersion: "1.1.1" };</script>
  </body>
</html>
"""
        self.index_path.write_text(self.original_html, encoding="utf-8")

    def run_tool(
        self,
        expected_version: str | None = None,
    ) -> subprocess.CompletedProcess[str]:
        command = [
            sys.executable,
            str(TOOL_PATH),
            "--site-root",
            str(self.site_root),
        ]
        if expected_version is not None:
            command.extend(("--expected-version", expected_version))
        return subprocess.run(
            command,
            cwd=PROJECT_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_adds_loading_notice_and_play_guide_once(self) -> None:
        """Catches a deployment that hides instructions until after Unity loads."""
        first_result = self.run_tool(expected_version="1.1.1")
        second_result = self.run_tool(expected_version="1.1.1")

        self.assertEqual(0, first_result.returncode, first_result.stderr)
        self.assertEqual(0, second_result.returncode, second_result.stderr)

        deployed_html = self.index_path.read_text(encoding="utf-8")
        self.assertEqual(1, deployed_html.count('data-tdf-pages-shell="v1"'))
        self.assertEqual(1, deployed_html.count('id="how-to-play"'))
        self.assertLess(
            deployed_html.index('class="tdf-loading-notice"'),
            deployed_html.index('id="unity-container"'),
        )
        self.assertLess(
            deployed_html.index('id="unity-container"'),
            deployed_html.index('id="how-to-play"'),
        )
        for expected in (
            "첫 실행은 대용량 게임 데이터를 내려받습니다",
            "게임 시작 → 난이도 → 직업 선택",
            "카드를 클릭해 사용하고 턴 종료",
            "체력·금화·빚·덱을 관리",
            'aria-labelledby="how-to-play-title"',
            "@media (max-width: 760px)",
        ):
            with self.subTest(expected=expected):
                self.assertIn(expected, deployed_html)

    def test_rejects_non_unity_page_without_partial_write(self) -> None:
        """Catches silently publishing the guide into the wrong index page."""
        malformed_html = "<html><head></head><body>not unity</body></html>"
        self.index_path.write_text(malformed_html, encoding="utf-8")

        result = self.run_tool()

        self.assertNotEqual(0, result.returncode)
        self.assertIn("unity-container", result.stderr)
        self.assertEqual(malformed_html, self.index_path.read_text(encoding="utf-8"))

    def test_rejects_wrong_product_version_without_partial_write(self) -> None:
        """Catches deploying a stale release asset under a newer release tag."""
        stale_html = self.original_html.replace(
            'productVersion: "1.1.1"',
            'productVersion: "1.1.0"',
        )
        self.index_path.write_text(stale_html, encoding="utf-8")

        result = self.run_tool(expected_version="1.1.1")

        self.assertNotEqual(0, result.returncode)
        self.assertIn("expected product version 1.1.1", result.stderr)
        self.assertEqual(stale_html, self.index_path.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
