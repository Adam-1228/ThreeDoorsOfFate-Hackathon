#!/usr/bin/env python3

from __future__ import annotations

import argparse
import os
import plistlib
import re
import stat
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Sequence


ADMOB_APP_ID_PATTERN = re.compile(r"ca-app-pub-[0-9]+~[0-9]+")
VERSION_PATTERN = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+")
BUILD_PATTERN = re.compile(r"[1-9][0-9]*")
SKADNETWORK_ID_PATTERN = re.compile(r"[a-z0-9]{10}\.skadnetwork")


class ReleasePlistError(Exception):
    pass


def parse_arguments(argv: Sequence[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Configure or verify the Three Doors of Fate iOS release plist."
    )
    parser.add_argument("command", choices=("configure", "verify"))
    parser.add_argument("--plist", required=True, type=Path)
    parser.add_argument("--version", required=True)
    parser.add_argument("--build", required=True)
    parser.add_argument("--unity-version", required=True)
    parser.add_argument("--skadnetwork-xml", required=True, type=Path)
    return parser.parse_args(argv)


def validate_release_inputs(arguments: argparse.Namespace) -> str:
    admob_app_id = os.environ.get("ADMOB_IOS_APP_ID", "").strip()
    if ADMOB_APP_ID_PATTERN.fullmatch(admob_app_id) is None:
        raise ReleasePlistError(
            "ADMOB_IOS_APP_ID is missing or is not a production-format AdMob app ID."
        )
    if VERSION_PATTERN.fullmatch(arguments.version) is None:
        raise ReleasePlistError("The release version must use three numeric components.")
    if BUILD_PATTERN.fullmatch(arguments.build) is None:
        raise ReleasePlistError("The release build must be a positive integer.")
    if not arguments.unity_version.strip():
        raise ReleasePlistError("The Unity version must not be empty.")
    return admob_app_id


def read_skadnetwork_items(xml_path: Path) -> list[dict[str, str]]:
    try:
        root = ET.parse(xml_path).getroot()
    except (OSError, ET.ParseError) as error:
        raise ReleasePlistError(
            "The Google Mobile Ads SKAdNetwork metadata could not be read."
        ) from error

    identifiers: list[str] = []
    seen: set[str] = set()
    for element in root.iter("SKAdNetworkIdentifier"):
        identifier = (element.text or "").strip()
        if SKADNETWORK_ID_PATTERN.fullmatch(identifier) is None:
            raise ReleasePlistError(
                "The Google Mobile Ads metadata contains an invalid SKAdNetworkIdentifier."
            )
        if identifier in seen:
            raise ReleasePlistError(
                "The Google Mobile Ads metadata contains a duplicate SKAdNetworkIdentifier."
            )
        seen.add(identifier)
        identifiers.append(identifier)

    if not identifiers:
        raise ReleasePlistError(
            "The Google Mobile Ads metadata contains no SKAdNetworkIdentifier values."
        )
    return [{"SKAdNetworkIdentifier": identifier} for identifier in identifiers]


def load_plist(plist_path: Path) -> tuple[dict[str, object], plistlib.PlistFormat]:
    try:
        original_bytes = plist_path.read_bytes()
        plist = plistlib.loads(original_bytes)
    except (OSError, plistlib.InvalidFileException) as error:
        raise ReleasePlistError("The iOS Info.plist could not be read.") from error
    if not isinstance(plist, dict):
        raise ReleasePlistError("The iOS Info.plist root must be a dictionary.")
    plist_format = (
        plistlib.FMT_BINARY if original_bytes.startswith(b"bplist") else plistlib.FMT_XML
    )
    return plist, plist_format


def configure_plist(
    arguments: argparse.Namespace,
    plist: dict[str, object],
    admob_app_id: str,
    skadnetwork_items: list[dict[str, str]],
) -> None:
    configured = dict(plist)
    configured["CFBundleShortVersionString"] = arguments.version
    configured["CFBundleVersion"] = arguments.build
    configured["GADApplicationIdentifier"] = admob_app_id
    configured["GADUUnityVersion"] = arguments.unity_version.strip()
    configured["ITSAppUsesNonExemptEncryption"] = False
    configured["SKAdNetworkItems"] = skadnetwork_items
    configured.pop("NSUserTrackingUsageDescription", None)
    write_plist_atomically(arguments.plist, configured)


def write_plist_atomically(plist_path: Path, plist: dict[str, object]) -> None:
    _, plist_format = load_plist(plist_path)
    source_mode = stat.S_IMODE(plist_path.stat().st_mode)
    descriptor, temporary_name = tempfile.mkstemp(
        prefix=f".{plist_path.name}.",
        suffix=".tmp",
        dir=plist_path.parent,
    )
    temporary_path = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "wb") as temporary_file:
            plistlib.dump(
                plist,
                temporary_file,
                fmt=plist_format,
                sort_keys=False,
            )
            temporary_file.flush()
            os.fsync(temporary_file.fileno())
        os.chmod(temporary_path, source_mode)
        os.replace(temporary_path, plist_path)
    except Exception:
        temporary_path.unlink(missing_ok=True)
        raise


def verify_plist(
    arguments: argparse.Namespace,
    plist: dict[str, object],
    admob_app_id: str,
    skadnetwork_items: list[dict[str, str]],
) -> None:
    failures: list[str] = []
    if plist.get("CFBundleShortVersionString") != arguments.version:
        failures.append("CFBundleShortVersionString does not match the release version.")
    if plist.get("CFBundleVersion") != arguments.build:
        failures.append("CFBundleVersion does not match the release build.")
    if plist.get("GADApplicationIdentifier") != admob_app_id:
        failures.append(
            "GADApplicationIdentifier is missing or does not match ADMOB_IOS_APP_ID."
        )
    if plist.get("GADUUnityVersion") != arguments.unity_version.strip():
        failures.append("GADUUnityVersion does not match the release Unity version.")
    if plist.get("ITSAppUsesNonExemptEncryption") is not False:
        failures.append("ITSAppUsesNonExemptEncryption must be false.")
    if "NSUserTrackingUsageDescription" in plist:
        failures.append("NSUserTrackingUsageDescription must be absent.")
    if plist.get("SKAdNetworkItems") != skadnetwork_items:
        failures.append("SKAdNetworkItems do not match the packaged Google metadata.")
    if failures:
        raise ReleasePlistError(" ".join(failures))


def run(arguments: argparse.Namespace) -> None:
    admob_app_id = validate_release_inputs(arguments)
    skadnetwork_items = read_skadnetwork_items(arguments.skadnetwork_xml)
    plist, _ = load_plist(arguments.plist)
    if arguments.command == "configure":
        configure_plist(arguments, plist, admob_app_id, skadnetwork_items)
        print(
            f"Configured iOS release plist for {arguments.version} "
            f"({arguments.build}) with {len(skadnetwork_items)} SKAdNetwork identifiers."
        )
        return
    verify_plist(arguments, plist, admob_app_id, skadnetwork_items)
    print(
        f"Verified iOS release plist for {arguments.version} "
        f"({arguments.build}) with {len(skadnetwork_items)} SKAdNetwork identifiers."
    )


def main(argv: Sequence[str] | None = None) -> int:
    try:
        run(parse_arguments(argv))
    except (ReleasePlistError, OSError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
