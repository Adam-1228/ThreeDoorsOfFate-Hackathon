#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import json
import re
import ssl
import sys
import urllib.error
import urllib.parse
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


PROJECT_ROOT = Path(__file__).resolve().parents[1]
WEB_ROOT = Path("docs/submission/app-store/web/three-doors-of-fate")
SUBMISSION_ROOT = Path("docs/submission/app-store")
GAME_CENTER_SOURCE = Path("Assets/Scripts/Platform/AppleGameServices.cs")
GAME_CENTER_ACHIEVEMENT_ID_PATTERN = re.compile(r"^[A-Za-z0-9._]+$")
ACTIVE_VERSION = "1.2.0"
ACTIVE_BUILD = "12000"
ACTIVE_KOREAN_METADATA = "metadata-1.2.0.ko-KR.json"
ACTIVE_ENGLISH_METADATA = "metadata-1.2.0.en-US.json"
ACTIVE_REVIEW_NOTES = "review-notes-1.2.0.en-US.md"
ACTIVE_TERRITORIES = [
    "KR",
    "AT",
    "BE",
    "BG",
    "HR",
    "CY",
    "CZ",
    "DK",
    "EE",
    "FI",
    "FR",
    "DE",
    "GR",
    "HU",
    "IE",
    "IT",
    "LV",
    "LT",
    "LU",
    "MT",
    "NL",
    "PL",
    "PT",
    "RO",
    "SK",
    "SI",
    "ES",
    "SE",
    "US",
]


@dataclass(frozen=True)
class PageContract:
    relative_path: Path
    required_fragments: tuple[str, ...]


PAGE_CONTRACTS = (
    PageContract(
        Path("index.html"),
        (
            '<html lang="ko">',
            "Three Doors of Fate",
            "./support/",
            "./privacy/",
            "mailto:",
            "Game Center",
            "로컬 저장",
            "보상형 광고",
            "취소",
        ),
    ),
    PageContract(
        Path("support/index.html"),
        (
            '<html lang="ko">',
            "Three Doors of Fate",
            "mailto:",
            "Game Center",
            "로컬 저장",
            "클라우드 저장",
            "오프라인",
            "보상형 광고",
            "광고 취소",
        ),
    ),
    PageContract(
        Path("privacy/index.html"),
        (
            '<html lang="ko">',
            "Three Doors of Fate",
            "Game Center",
            "iCloud",
            "로컬 저장",
            "Google Mobile Ads",
            "대략적 위치",
            "기기 식별자",
            "광고 데이터",
            "제품 상호작용",
            "충돌 로그",
            "진단 정보",
            "성능 데이터",
            "추적",
            "mailto:",
        ),
    ),
)

FORBIDDEN_CLAIM_PATTERNS = (
    re.compile(r"사업자\s*등록(?:을|이)?\s*(?:완료|했습니다|되었습니다)"),
    re.compile(r"게임\s*(?:제작업|배급업)\s*등록(?:을|이)?\s*(?:완료|했습니다|되었습니다)"),
    re.compile(r"GRAC\s*(?:등록|등급)(?:을|이)?\s*(?:완료|했습니다|되었습니다)", re.IGNORECASE),
)

MACOS_CA_CANDIDATES = (
    Path("/etc/ssl/cert.pem"),
    Path("/private/etc/ssl/cert.pem"),
    Path("/opt/homebrew/etc/ca-certificates/cert.pem"),
    Path("/usr/local/etc/ca-certificates/cert.pem"),
)


def validate_local_assets(root: Path) -> list[str]:
    errors: list[str] = []
    page_root = root / WEB_ROOT
    for contract in PAGE_CONTRACTS:
        path = page_root / contract.relative_path
        if not path.is_file():
            errors.append(f"missing public page: {path}")
            continue
        try:
            content = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as exception:
            errors.append(f"cannot read UTF-8 page {path}: {exception}")
            continue

        for fragment in contract.required_fragments:
            if fragment not in content:
                errors.append(f"{contract.relative_path}: missing {fragment!r}")
        for pattern in FORBIDDEN_CLAIM_PATTERNS:
            match = pattern.search(content)
            if match:
                errors.append(
                    f"{contract.relative_path}: unsupported legal claim {match.group(0)!r}"
                )
    return errors


def validate_submission_documents(root: Path) -> list[str]:
    errors: list[str] = []
    submission_root = root / SUBMISSION_ROOT
    metadata_path = submission_root / ACTIVE_KOREAN_METADATA
    try:
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as exception:
        return [f"cannot read release metadata {metadata_path}: {exception}"]

    expected_values = (
        (("app_record", "platform"), "iOS"),
        (("app_record", "name"), "Three Doors of Fate"),
        (("app_record", "primary_language"), "ko-KR"),
        (("app_record", "bundle_id"), "com.adam.threedoorsfate"),
        (("app_record", "sku"), "TDOF-IOS-2026"),
        (("version", "version_string"), ACTIVE_VERSION),
        (("version", "build_string"), ACTIVE_BUILD),
        (
            ("version", "support_url"),
            "https://adam-1228.github.io/three-doors-of-fate/support/",
        ),
        (
            ("version", "privacy_policy_url"),
            "https://adam-1228.github.io/three-doors-of-fate/privacy/",
        ),
        (("commercial", "price"), "free"),
        (("commercial", "distribution_method"), "public"),
        (("commercial", "territories"), ACTIVE_TERRITORIES),
        (("commercial", "release_method"), "manual"),
        (("commercial", "preorder"), False),
        (("commercial", "in_app_purchases"), []),
        (("commercial", "subscriptions"), []),
        (("review", "sign_in_required"), False),
        (("build", "game_center"), True),
        (("build", "icloud"), True),
        (("build", "rewarded_ads_only"), True),
        (("build", "production_ads_required"), True),
    )
    for key_path, expected in expected_values:
        current: object = metadata
        for key in key_path:
            if not isinstance(current, dict) or key not in current:
                errors.append(f"metadata missing {'.'.join(key_path)}")
                break
            current = current[key]
        else:
            if current != expected:
                errors.append(
                    f"metadata {'.'.join(key_path)} must be {expected!r}, got {current!r}"
                )

    app_record = metadata.get("app_record", {})
    version = metadata.get("version", {})
    character_limits = (
        ("name", app_record.get("name"), 2, 30),
        ("subtitle", version.get("subtitle"), 1, 30),
        ("promotional_text", version.get("promotional_text"), 0, 170),
        ("description", version.get("description"), 1, 4000),
    )
    for label, value, minimum, maximum in character_limits:
        if not isinstance(value, str):
            errors.append(f"metadata {label} must be text")
        elif len(value) < minimum:
            errors.append(f"metadata {label} is shorter than {minimum} characters")
        elif len(value) > maximum:
            errors.append(f"metadata {label} exceeds {maximum} characters")

    keywords = version.get("keywords")
    if not isinstance(keywords, str):
        errors.append("metadata keywords must be text")
    else:
        keyword_bytes = len(keywords.encode("utf-8"))
        if keyword_bytes > 100:
            errors.append(f"metadata keywords exceed 100 bytes ({keyword_bytes})")
        for keyword in keywords.split(","):
            if len(keyword.strip()) <= 2:
                errors.append(
                    f"metadata keyword must be longer than two characters: {keyword!r}"
                )

    english_metadata_path = submission_root / ACTIVE_ENGLISH_METADATA
    try:
        english_metadata = json.loads(
            english_metadata_path.read_text(encoding="utf-8")
        )
    except (OSError, UnicodeError, json.JSONDecodeError) as exception:
        errors.append(
            f"cannot read release metadata {english_metadata_path}: {exception}"
        )
    else:
        english_expected_values = (
            (("app_record", "platform"), "iOS"),
            (("app_record", "name"), "Three Doors of Fate"),
            (("app_record", "primary_language"), "ko-KR"),
            (("app_record", "bundle_id"), "com.adam.threedoorsfate"),
            (("app_record", "sku"), "TDOF-IOS-2026"),
            (("version", "version_string"), ACTIVE_VERSION),
            (("version", "build_string"), ACTIVE_BUILD),
            (("version", "localization"), "en-US"),
            (
                ("version", "support_url"),
                "https://adam-1228.github.io/three-doors-of-fate/support/",
            ),
            (
                ("version", "privacy_policy_url"),
                "https://adam-1228.github.io/three-doors-of-fate/privacy/",
            ),
            (("commercial", "territories"), ACTIVE_TERRITORIES),
            (("commercial", "release_method"), "manual"),
            (("commercial", "preorder"), False),
            (("review", "sign_in_required"), False),
            (
                ("review", "review_notes_file"),
                f"docs/submission/app-store/{ACTIVE_REVIEW_NOTES}",
            ),
        )
        for key_path, expected in english_expected_values:
            current: object = english_metadata
            for key in key_path:
                if not isinstance(current, dict) or key not in current:
                    errors.append(
                        f"metadata.en-US missing {'.'.join(key_path)}"
                    )
                    break
                current = current[key]
            else:
                if current != expected:
                    errors.append(
                        "metadata.en-US "
                        f"{'.'.join(key_path)} must be {expected!r}, got {current!r}"
                    )

        english_app_record = english_metadata.get("app_record", {})
        english_version = english_metadata.get("version", {})
        english_character_limits = (
            ("name", english_app_record.get("name"), 2, 30),
            ("subtitle", english_version.get("subtitle"), 1, 30),
            (
                "promotional_text",
                english_version.get("promotional_text"),
                0,
                170,
            ),
            ("description", english_version.get("description"), 1, 4000),
            ("whats_new", english_version.get("whats_new"), 1, 4000),
        )
        for label, value, minimum, maximum in english_character_limits:
            if not isinstance(value, str):
                errors.append(f"metadata.en-US {label} must be text")
            elif len(value) < minimum:
                errors.append(
                    f"metadata.en-US {label} is shorter than {minimum} characters"
                )
            elif len(value) > maximum:
                errors.append(
                    f"metadata.en-US {label} exceeds {maximum} characters"
                )

        english_keywords = english_version.get("keywords")
        if not isinstance(english_keywords, str):
            errors.append("metadata.en-US keywords must be text")
        else:
            english_keyword_bytes = len(english_keywords.encode("utf-8"))
            if english_keyword_bytes > 100:
                errors.append(
                    "metadata.en-US keywords exceed 100 bytes "
                    f"({english_keyword_bytes})"
                )
            for keyword in english_keywords.split(","):
                if len(keyword.strip()) <= 2:
                    errors.append(
                        "metadata.en-US keyword must be longer than two "
                        f"characters: {keyword!r}"
                    )

    document_contracts = {
        "privacy-data-use.ko-KR.md": (
            "Gameplay Content",
            "Google Mobile Ads",
            "Coarse Location",
            "Device ID",
            "Advertising Data",
            "Product Interaction",
            "Crash Data",
            "Performance Data",
            "Other Diagnostic Data",
            "PrivacyInfo.xcprivacy",
        ),
        "age-rating.ko-KR.md": (
            "Advertising: Yes",
            "Horror/Fear Themes",
            "Cartoon or Fantasy Violence",
            "Realistic Violence",
            "Simulated Gambling: None",
            "Gambling: No",
            "Loot Boxes: No",
            "GRAC",
        ),
        "review-notes.ko-KR.md": (
            "Sign-in required",
            "Game Center",
            "로컬 저장",
            "선택형 보상 광고 버튼",
            "인앱 구매와 구독이 없습니다",
        ),
        ACTIVE_REVIEW_NOTES: (
            f"{ACTIVE_VERSION} ({ACTIVE_BUILD})",
            "No account is required",
            "20 achievements",
            "12 new Game Center achievements",
            "Settings",
            "Progress Log",
            "Game Center",
            "optional rewarded ads",
            "does not request ATT permission",
        ),
        "game-center.ko-KR.md": (
            "com.adam.threedoorsfate.leaderboard.endless",
            "com.adam.threedoorsfate.achievement.hard_unlocked",
            "com.adam.threedoorsfate.achievement.true_ending.gambler",
            "com.adam.threedoorsfate.achievement.true_ending.oracle",
            "com.adam.threedoorsfate.achievement.true_ending.exile",
            "three-doors-progress-v1",
            "iCloud.com.adam.threedoorsfate",
        ),
    }
    for file_name, required_fragments in document_contracts.items():
        path = submission_root / file_name
        try:
            content = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as exception:
            errors.append(f"cannot read submission document {path}: {exception}")
            continue
        for fragment in required_fragments:
            if fragment not in content:
                errors.append(f"{file_name}: missing {fragment!r}")

    game_center_source_path = root / GAME_CENTER_SOURCE
    try:
        game_center_source = game_center_source_path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exception:
        errors.append(
            f"cannot read Game Center source {game_center_source_path}: {exception}"
        )
    else:
        achievement_ids = re.findall(
            r'"(com\.adam\.threedoorsfate\.achievement\.[^"]+)"',
            game_center_source,
        )
        for achievement_id in achievement_ids:
            if not GAME_CENTER_ACHIEVEMENT_ID_PATTERN.fullmatch(achievement_id):
                errors.append(
                    f"unsupported Game Center achievement ID: {achievement_id!r}"
                )
    return errors


def build_https_context(
    default_cafile: Path | str | None = None,
    fallback_candidates: tuple[Path, ...] = MACOS_CA_CANDIDATES,
) -> ssl.SSLContext:
    selected_default = default_cafile
    if selected_default is None:
        selected_default = ssl.get_default_verify_paths().cafile
    if selected_default is not None and Path(selected_default).is_file():
        return ssl.create_default_context(cafile=str(selected_default))
    for candidate in fallback_candidates:
        if candidate.is_file():
            return ssl.create_default_context(cafile=str(candidate))
    return ssl.create_default_context()


def _fetch(url: str, expected_content_type: str) -> tuple[str, str]:
    request = urllib.request.Request(
        url,
        headers={"User-Agent": "ThreeDoorsOfFate-ReleaseValidator/1.0"},
    )
    with urllib.request.urlopen(
        request,
        timeout=20,
        context=build_https_context(),
    ) as response:
        status = getattr(response, "status", None)
        content_type = response.headers.get_content_type()
        body = response.read()
    if status != 200:
        raise ValueError(f"HTTP {status}")
    if not body:
        raise ValueError("empty response")
    if content_type != expected_content_type:
        raise ValueError(
            f"expected {expected_content_type}, received {content_type or 'unknown'}"
        )
    return hashlib.sha256(body).hexdigest(), content_type


def validate_remote_assets(base_url: str) -> tuple[list[str], list[str]]:
    errors: list[str] = []
    evidence: list[str] = []
    normalized_base = base_url.rstrip("/") + "/"
    parsed = urllib.parse.urlsplit(normalized_base)
    if parsed.scheme not in {"http", "https"} or not parsed.netloc:
        return [f"invalid base URL: {base_url}"], evidence

    endpoints = (
        (normalized_base, "text/html"),
        (urllib.parse.urljoin(normalized_base, "support/"), "text/html"),
        (urllib.parse.urljoin(normalized_base, "privacy/"), "text/html"),
        (
            urllib.parse.urlunsplit(
                (parsed.scheme, parsed.netloc, "/app-ads.txt", "", "")
            ),
            "text/plain",
        ),
    )
    for url, expected_content_type in endpoints:
        try:
            digest, content_type = _fetch(url, expected_content_type)
            evidence.append(f"{url} {content_type} sha256={digest}")
        except (urllib.error.URLError, OSError, ValueError) as exception:
            errors.append(f"{url}: {exception}")
    return errors, evidence


def _print_errors(errors: Iterable[str]) -> None:
    for error in errors:
        print(f"error: {error}", file=sys.stderr)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate Three Doors of Fate App Store public assets."
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=PROJECT_ROOT,
        help="Project root containing docs/submission/app-store.",
    )
    parser.add_argument(
        "--local-only",
        action="store_true",
        help="Validate local sources without making network requests.",
    )
    parser.add_argument(
        "--base-url",
        help="Deployed game page URL; also verifies the site-root app-ads.txt.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    local_errors = validate_local_assets(args.root.resolve())
    if local_errors:
        _print_errors(local_errors)
        return 1
    print("Local submission assets passed")

    document_errors = validate_submission_documents(args.root.resolve())
    if document_errors:
        _print_errors(document_errors)
        return 1
    print("App Store submission documents passed")

    if args.local_only:
        return 0
    if not args.base_url:
        print("error: --base-url is required unless --local-only is used", file=sys.stderr)
        return 2

    remote_errors, evidence = validate_remote_assets(args.base_url)
    if remote_errors:
        _print_errors(remote_errors)
        return 1
    for line in evidence:
        print(line)
    print("Remote submission assets passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
