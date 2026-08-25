from __future__ import annotations

import contextlib
import functools
import http.server
import json
import shutil
import subprocess
import sys
import tempfile
import threading
import unittest
from pathlib import Path
from typing import Iterator

from tools import validate_app_store_submission


PROJECT_ROOT = Path(__file__).resolve().parents[2]
VALIDATOR = PROJECT_ROOT / "tools" / "validate_app_store_submission.py"
SUBMISSION_ROOT = PROJECT_ROOT / "docs" / "submission" / "app-store"
KOREAN_104_METADATA = SUBMISSION_ROOT / "metadata-1.0.4.ko-KR.json"
ENGLISH_104_METADATA = SUBMISSION_ROOT / "metadata-1.0.4.en-US.json"
REVIEW_NOTES_104 = SUBMISSION_ROOT / "review-notes-1.0.4.en-US.md"
KOREAN_ACTIVE_METADATA = SUBMISSION_ROOT / "metadata-1.2.0.ko-KR.json"
ENGLISH_ACTIVE_METADATA = SUBMISSION_ROOT / "metadata-1.2.0.en-US.json"
ACTIVE_REVIEW_NOTES = SUBMISSION_ROOT / "review-notes-1.2.0.en-US.md"
PUBLIC_PAGE_ROOT = (
    PROJECT_ROOT / "docs" / "submission" / "app-store" / "web" / "three-doors-of-fate"
)


class QuietStaticHandler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, format: str, *args: object) -> None:
        return


class WrongAppAdsContentTypeHandler(QuietStaticHandler):
    def guess_type(self, path: str) -> str:
        if path.endswith("app-ads.txt"):
            return "application/octet-stream"
        return super().guess_type(path)


@contextlib.contextmanager
def serve_directory(
    directory: Path,
    handler_class: type[QuietStaticHandler] = QuietStaticHandler,
) -> Iterator[str]:
    handler = functools.partial(handler_class, directory=str(directory))
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        host, port = server.server_address
        yield f"http://{host}:{port}"
    finally:
        server.shutdown()
        thread.join(timeout=5)
        server.server_close()


class AppStoreSubmissionAssetTests(unittest.TestCase):
    def test_https_context_uses_readable_macos_ca_bundle_when_default_is_missing(self) -> None:
        """Catches Framework Python failing HTTPS despite a valid macOS CA bundle."""
        builder = getattr(
            validate_app_store_submission,
            "build_https_context",
            None,
        )
        self.assertIsNotNone(builder)

        context = builder(
            default_cafile=Path("/definitely/missing/python-cert.pem"),
            fallback_candidates=(Path("/etc/ssl/cert.pem"),),
        )
        self.assertGreater(len(context.get_ca_certs()), 0)

    def test_project_public_pages_satisfy_release_disclosure_contract(self) -> None:
        """Catches missing pages or disclosures that would make public support incomplete."""
        result = subprocess.run(
            [
                sys.executable,
                str(VALIDATOR),
                "--root",
                str(PROJECT_ROOT),
                "--local-only",
            ],
            cwd=PROJECT_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(
            result.returncode,
            0,
            f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )
        self.assertIn("Local submission assets passed", result.stdout)

    def test_release_metadata_documents_satisfy_submission_contract(self) -> None:
        """Catches a missing App Store field, privacy basis, rating, or review path."""
        result = subprocess.run(
            [
                sys.executable,
                str(VALIDATOR),
                "--root",
                str(PROJECT_ROOT),
                "--local-only",
            ],
            cwd=PROJECT_ROOT,
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(
            result.returncode,
            0,
            f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )
        self.assertIn("App Store submission documents passed", result.stdout)

    def test_version_1_0_4_submission_copy_is_exact_and_scoped(self) -> None:
        """Catches stale release identity, territory scope, or review guidance."""
        for path in (
            KOREAN_104_METADATA,
            ENGLISH_104_METADATA,
            REVIEW_NOTES_104,
        ):
            self.assertTrue(path.is_file(), f"missing 1.0.4 submission file: {path}")

        korean = json.loads(KOREAN_104_METADATA.read_text(encoding="utf-8"))
        english = json.loads(ENGLISH_104_METADATA.read_text(encoding="utf-8"))
        expected_territories = {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI",
            "FR", "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU",
            "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
            "KR", "US",
        }
        expected_whats_new = {
            "ko-KR": "영어 모드의 카드 조합과 패배 화면 번역을 보완했습니다. 전투 HUD와 휴식·이벤트 선택 정보를 더 읽기 쉽게 정리하고, 손패 순환 연습, 다양한 문 선택, 보물 카드 미리보기, 업적 페이지와 설정 버튼 가독성을 개선했습니다.",
            "en-US": "Completed the remaining English localization for card synergies and defeat screens. We also made combat and decision information easier to read, added a hand-flow practice, improved door variety and treasure previews, and refreshed achievements and the Settings control.",
        }
        for localization, metadata in (("ko-KR", korean), ("en-US", english)):
            with self.subTest(localization=localization):
                self.assertEqual(metadata["version"]["version_string"], "1.0.4")
                self.assertEqual(metadata["version"]["build_string"], "8")
                self.assertEqual(
                    metadata["version"]["whats_new"],
                    expected_whats_new[localization],
                )
                self.assertEqual(
                    set(metadata["commercial"]["territories"]),
                    expected_territories,
                )
                self.assertEqual(len(metadata["commercial"]["territories"]), 29)
                self.assertEqual(metadata["commercial"]["release_method"], "manual")

        notes = REVIEW_NOTES_104.read_text(encoding="utf-8")
        for fragment in (
            "1.0.4 (8)",
            "No account is required",
            "Settings",
            "optional rewarded ads",
            "does not request ATT permission",
            "Game Over",
            "How to Play",
        ):
            self.assertIn(fragment, notes)

    def test_local_validator_requires_english_release_metadata(self) -> None:
        """Catches shipping an English game localization without en-US store copy."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture_root = Path(temporary_directory)
            fixture_submission = (
                fixture_root / "docs" / "submission" / "app-store"
            )
            shutil.copytree(
                PROJECT_ROOT / "docs" / "submission" / "app-store",
                fixture_submission,
            )
            fixture_game_center_source = (
                fixture_root
                / "Assets"
                / "Scripts"
                / "Platform"
                / "AppleGameServices.cs"
            )
            fixture_game_center_source.parent.mkdir(parents=True)
            shutil.copy2(
                PROJECT_ROOT
                / "Assets"
                / "Scripts"
                / "Platform"
                / "AppleGameServices.cs",
                fixture_game_center_source,
            )
            (fixture_submission / ENGLISH_ACTIVE_METADATA.name).unlink(missing_ok=True)
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--root",
                    str(fixture_root),
                    "--local-only",
                ],
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn(ENGLISH_ACTIVE_METADATA.name, result.stderr)

    def test_age_rating_contract_keeps_simulated_gambling_at_none(self) -> None:
        """Catches reintroducing the individual-developer gambling restriction."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture_root = Path(temporary_directory)
            fixture_submission = (
                fixture_root / "docs" / "submission" / "app-store"
            )
            shutil.copytree(
                PROJECT_ROOT / "docs" / "submission" / "app-store",
                fixture_submission,
            )
            fixture_game_center_source = (
                fixture_root
                / "Assets"
                / "Scripts"
                / "Platform"
                / "AppleGameServices.cs"
            )
            fixture_game_center_source.parent.mkdir(parents=True)
            shutil.copy2(
                PROJECT_ROOT
                / "Assets"
                / "Scripts"
                / "Platform"
                / "AppleGameServices.cs",
                fixture_game_center_source,
            )
            age_rating_path = fixture_submission / "age-rating.ko-KR.md"
            age_rating = age_rating_path.read_text(encoding="utf-8")
            age_rating_path.write_text(
                age_rating.replace(
                    "Simulated Gambling: None",
                    "Simulated Gambling: Infrequent",
                ),
                encoding="utf-8",
            )
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--root",
                    str(fixture_root),
                    "--local-only",
                ],
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Simulated Gambling: None", result.stderr)

    def test_metadata_validator_rejects_app_store_field_limit_overflow(self) -> None:
        """Catches metadata that App Store Connect refuses because a field is too long."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture_root = Path(temporary_directory)
            fixture_submission = (
                fixture_root / "docs" / "submission" / "app-store"
            )
            shutil.copytree(
                PROJECT_ROOT / "docs" / "submission" / "app-store",
                fixture_submission,
            )
            metadata_path = fixture_submission / KOREAN_ACTIVE_METADATA.name
            metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            metadata["version"]["subtitle"] = "가" * 31
            metadata_path.write_text(
                json.dumps(metadata, ensure_ascii=False, indent=2),
                encoding="utf-8",
            )
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--root",
                    str(fixture_root),
                    "--local-only",
                ],
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("subtitle exceeds 30 characters", result.stderr)

    def test_metadata_validator_rejects_preorder_instead_of_manual_korea_release(self) -> None:
        """Catches accidentally turning the approved Korea-only manual release into pre-order."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture_root = Path(temporary_directory)
            fixture_submission = (
                fixture_root / "docs" / "submission" / "app-store"
            )
            shutil.copytree(
                PROJECT_ROOT / "docs" / "submission" / "app-store",
                fixture_submission,
            )
            metadata_path = fixture_submission / KOREAN_ACTIVE_METADATA.name
            metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            metadata["commercial"]["preorder"] = True
            metadata_path.write_text(
                json.dumps(metadata, ensure_ascii=False, indent=2),
                encoding="utf-8",
            )
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--root",
                    str(fixture_root),
                    "--local-only",
                ],
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("commercial.preorder must be False", result.stderr)

    def test_local_validator_rejects_invalid_game_center_identifier_characters(self) -> None:
        """Catches achievement IDs that App Store Connect refuses to create."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture_root = Path(temporary_directory)
            fixture_submission = (
                fixture_root / "docs" / "submission" / "app-store"
            )
            shutil.copytree(
                PROJECT_ROOT / "docs" / "submission" / "app-store",
                fixture_submission,
            )
            source_path = (
                fixture_root
                / "Assets"
                / "Scripts"
                / "Platform"
                / "AppleGameServices.cs"
            )
            source_path.parent.mkdir(parents=True)
            source = (
                PROJECT_ROOT
                / "Assets"
                / "Scripts"
                / "Platform"
                / "AppleGameServices.cs"
            ).read_text(encoding="utf-8")
            source_path.write_text(
                source.replace("achievement.hard_unlocked", "achievement.hard-unlocked"),
                encoding="utf-8",
            )
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--root",
                    str(fixture_root),
                    "--local-only",
                ],
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("unsupported Game Center achievement ID", result.stderr)

    def test_deployed_pages_and_root_app_ads_are_reachable(self) -> None:
        """Catches a wrong deploy path, missing app-ads.txt, or failed HTTP response."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            site_root = Path(temporary_directory)
            shutil.copytree(PUBLIC_PAGE_ROOT, site_root / "three-doors-of-fate")
            (site_root / "app-ads.txt").write_text(
                "google.com, pub-test, DIRECT, f08c47fec0942fa0\n",
                encoding="utf-8",
            )
            with serve_directory(site_root) as origin:
                result = subprocess.run(
                    [
                        sys.executable,
                        str(VALIDATOR),
                        "--root",
                        str(PROJECT_ROOT),
                        "--base-url",
                        f"{origin}/three-doors-of-fate",
                    ],
                    cwd=PROJECT_ROOT,
                    capture_output=True,
                    text=True,
                    check=False,
                )

        self.assertEqual(
            result.returncode,
            0,
            f"stdout:\n{result.stdout}\nstderr:\n{result.stderr}",
        )
        self.assertIn("Remote submission assets passed", result.stdout)

    def test_remote_validator_rejects_wrong_app_ads_content_type(self) -> None:
        """Catches GitHub hosting app-ads.txt as binary instead of public text."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            site_root = Path(temporary_directory)
            shutil.copytree(PUBLIC_PAGE_ROOT, site_root / "three-doors-of-fate")
            (site_root / "app-ads.txt").write_text(
                "google.com, pub-test, DIRECT, f08c47fec0942fa0\n",
                encoding="utf-8",
            )
            with serve_directory(
                site_root,
                WrongAppAdsContentTypeHandler,
            ) as origin:
                result = subprocess.run(
                    [
                        sys.executable,
                        str(VALIDATOR),
                        "--root",
                        str(PROJECT_ROOT),
                        "--base-url",
                        f"{origin}/three-doors-of-fate",
                    ],
                    cwd=PROJECT_ROOT,
                    capture_output=True,
                    text=True,
                    check=False,
                )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("expected text/plain", result.stderr)

    def test_local_validator_rejects_unverified_registration_completion_claim(self) -> None:
        """Catches a public claim that pending Korean registrations are complete."""
        with tempfile.TemporaryDirectory() as temporary_directory:
            fixture_root = Path(temporary_directory)
            fixture_pages = (
                fixture_root
                / "docs"
                / "submission"
                / "app-store"
                / "web"
                / "three-doors-of-fate"
            )
            shutil.copytree(PUBLIC_PAGE_ROOT, fixture_pages)
            index_path = fixture_pages / "index.html"
            index_path.write_text(
                index_path.read_text(encoding="utf-8").replace(
                    "</article>",
                    "<p>사업자등록을 완료했습니다.</p></article>",
                ),
                encoding="utf-8",
            )
            result = subprocess.run(
                [
                    sys.executable,
                    str(VALIDATOR),
                    "--root",
                    str(fixture_root),
                    "--local-only",
                ],
                cwd=PROJECT_ROOT,
                capture_output=True,
                text=True,
                check=False,
            )

        self.assertNotEqual(result.returncode, 0)
        self.assertIn("unsupported legal claim", result.stderr)


if __name__ == "__main__":
    unittest.main()
