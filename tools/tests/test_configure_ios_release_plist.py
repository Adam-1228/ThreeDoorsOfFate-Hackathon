from __future__ import annotations

import os
import plistlib
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]
TOOL_PATH = PROJECT_ROOT / "tools" / "configure_ios_release_plist.py"


class ConfigureIOSReleasePlistTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temporary_directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary_directory.cleanup)
        self.temporary_root = Path(self.temporary_directory.name)
        self.plist_path = self.temporary_root / "Info.plist"
        self.skadnetwork_path = self.temporary_root / "SKAdNetworkItems.xml"
        self.admob_app_id = "ca-app-pub-1234567890123456~1234567890"
        self._write_plist(
            {
                "CFBundleIdentifier": "com.adam.threedoorsfate",
                "CFBundleShortVersionString": "1.0.0",
                "CFBundleVersion": "4",
                "NSUserTrackingUsageDescription": "legacy tracking declaration",
            }
        )
        self.skadnetwork_path.write_text(
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
            "<SKAdNetworkItems>\n"
            "  <SKAdNetworkIdentifier>cstr6suwn9.skadnetwork</SKAdNetworkIdentifier>\n"
            "</SKAdNetworkItems>\n",
            encoding="utf-8",
        )

    def test_configure_writes_complete_release_contract_without_exposing_secret(
        self,
    ) -> None:
        """Catches an exported app missing metadata required by Google Mobile Ads."""
        result = self._run_tool("configure")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn(self.admob_app_id, result.stdout + result.stderr)
        with self.plist_path.open("rb") as plist_file:
            output = plistlib.load(plist_file)
        self.assertEqual(output["CFBundleIdentifier"], "com.adam.threedoorsfate")
        self.assertEqual(output["CFBundleShortVersionString"], "1.0.2")
        self.assertEqual(output["CFBundleVersion"], "6")
        self.assertEqual(output["GADApplicationIdentifier"], self.admob_app_id)
        self.assertEqual(output["GADUUnityVersion"], "6000.4.11f1")
        self.assertFalse(output["ITSAppUsesNonExemptEncryption"])
        self.assertNotIn("NSUserTrackingUsageDescription", output)
        self.assertEqual(
            output["SKAdNetworkItems"],
            [{"SKAdNetworkIdentifier": "cstr6suwn9.skadnetwork"}],
        )

    def test_verify_accepts_only_the_configured_contract(self) -> None:
        """Catches configure and verify disagreeing about the release artifact."""
        configure_result = self._run_tool("configure")
        self.assertEqual(configure_result.returncode, 0, configure_result.stderr)

        verify_result = self._run_tool("verify")

        self.assertEqual(verify_result.returncode, 0, verify_result.stderr)
        self.assertNotIn(self.admob_app_id, verify_result.stdout + verify_result.stderr)

    def test_verify_rejects_missing_gad_application_identifier(self) -> None:
        """Catches accepting the exact launch-crashing build 4 plist shape."""
        result = self._run_tool("verify")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("GADApplicationIdentifier", result.stderr)
        self.assertNotIn(self.admob_app_id, result.stdout + result.stderr)

    def test_configure_rejects_invalid_app_id_without_partial_write(self) -> None:
        """Catches malformed production configuration corrupting the export plist."""
        original_bytes = self.plist_path.read_bytes()
        result = self._run_tool("configure", admob_app_id="invalid-app-id")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("ADMOB_IOS_APP_ID", result.stderr)
        self.assertNotIn("invalid-app-id", result.stdout + result.stderr)
        self.assertEqual(self.plist_path.read_bytes(), original_bytes)

    def test_verify_rejects_an_app_id_mismatch_without_exposing_either_value(
        self,
    ) -> None:
        """Catches uploading an archive built with a different publisher app ID."""
        configure_result = self._run_tool("configure")
        self.assertEqual(configure_result.returncode, 0, configure_result.stderr)
        other_id = "ca-app-pub-9999999999999999~1111111111"

        verify_result = self._run_tool("verify", admob_app_id=other_id)

        self.assertNotEqual(verify_result.returncode, 0)
        self.assertIn("GADApplicationIdentifier", verify_result.stderr)
        combined_output = verify_result.stdout + verify_result.stderr
        self.assertNotIn(self.admob_app_id, combined_output)
        self.assertNotIn(other_id, combined_output)

    def test_configure_rejects_duplicate_skadnetwork_ids_without_partial_write(
        self,
    ) -> None:
        """Catches ambiguous attribution metadata being silently normalized."""
        self.skadnetwork_path.write_text(
            "<SKAdNetworkItems>"
            "<SKAdNetworkIdentifier>cstr6suwn9.skadnetwork</SKAdNetworkIdentifier>"
            "<SKAdNetworkIdentifier>cstr6suwn9.skadnetwork</SKAdNetworkIdentifier>"
            "</SKAdNetworkItems>",
            encoding="utf-8",
        )
        original_bytes = self.plist_path.read_bytes()

        result = self._run_tool("configure")

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("duplicate SKAdNetworkIdentifier", result.stderr)
        self.assertEqual(self.plist_path.read_bytes(), original_bytes)

    def _run_tool(
        self,
        command: str,
        *,
        admob_app_id: str | None = None,
    ) -> subprocess.CompletedProcess[str]:
        environment = os.environ.copy()
        environment["ADMOB_IOS_APP_ID"] = admob_app_id or self.admob_app_id
        return subprocess.run(
            [
                sys.executable,
                str(TOOL_PATH),
                command,
                "--plist",
                str(self.plist_path),
                "--version",
                "1.0.2",
                "--build",
                "6",
                "--unity-version",
                "6000.4.11f1",
                "--skadnetwork-xml",
                str(self.skadnetwork_path),
            ],
            cwd=PROJECT_ROOT,
            env=environment,
            capture_output=True,
            text=True,
            check=False,
        )

    def _write_plist(self, contents: dict[str, object]) -> None:
        with self.plist_path.open("wb") as plist_file:
            plistlib.dump(contents, plist_file, sort_keys=False)


if __name__ == "__main__":
    unittest.main()
