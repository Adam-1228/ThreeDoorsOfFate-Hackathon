from __future__ import annotations

import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[2]


class AppleReleaseScriptPolicyTests(unittest.TestCase):
    def test_gamekit_achievement_reporting_uses_current_completion_selector(self) -> None:
        source = (
            PROJECT_ROOT / "Assets" / "Plugins" / "iOS" / "ThreeDoorsGameKitBridge.mm"
        ).read_text(encoding="utf-8")

        self.assertIn(
            "reportAchievements:@[achievement] withCompletionHandler:",
            source,
        )
        self.assertNotIn(
            "reportAchievements:@[achievement] completionHandler:",
            source,
        )
        self.assertIn(
            "TDOF_EXPORT void TDOF_GameCenterSetAccessPointVisible(int visible)",
            source,
        )
        self.assertIn("[GKAccessPoint shared].active = visible != 0;", source)

    def test_mac_release_verification_covers_ios_dependencies_and_outputs(self) -> None:
        source = (PROJECT_ROOT / "tools" / "mac_setup_and_build.sh").read_text(
            encoding="utf-8"
        )

        for required in (
            "doctor",
            "ios-release-verify",
            "pod install",
            "Unity-iPhone.xcworkspace",
            "generic/platform=iOS",
            "CODE_SIGNING_ALLOWED=NO",
            "UNITY_IOS_REQUIRE_PRODUCTION_ADS",
            "ThreeDoorsOfFate.entitlements",
            "PrivacyInfo.xcprivacy",
            "TDOF_CloudInitialize",
            "com.apple.developer.icloud-container-identifiers",
            "PRODUCT_BUNDLE_IDENTIFIER = $EXPECTED_BUNDLE_ID",
        ):
            self.assertIn(required, source)

    def test_native_symbol_validation_is_safe_with_pipefail(self) -> None:
        source = (PROJECT_ROOT / "tools" / "mac_setup_and_build.sh").read_text(
            encoding="utf-8"
        )

        self.assertIn(
            '(set +o pipefail; nm -gU "$binary" | grep -Fq "_$symbol")',
            source,
        )
        self.assertNotIn(
            'nm -gU "$binary" | grep -Fq "_$symbol" || fail',
            source,
        )

    def test_simulator_supports_build_only_and_cocoapods_workspace(self) -> None:
        source = (PROJECT_ROOT / "tools" / "run_ios_simulator.sh").read_text(
            encoding="utf-8"
        )

        self.assertIn("--build-only", source)
        self.assertIn("pod install", source)
        self.assertIn("Unity-iPhone.xcworkspace", source)
        self.assertIn("generic/platform=iOS Simulator", source)

    def test_ios_install_builds_signs_installs_and_launches_connected_device(self) -> None:
        source = (PROJECT_ROOT / "tools" / "mac_setup_and_build.sh").read_text(
            encoding="utf-8"
        )

        for required in (
            "ios-install",
            "UNITY_IOS_DEVICE_ID",
            "xcrun xctrace list devices",
            "-allowProvisioningUpdates",
            'destination "id=$device_id"',
            "xcrun devicectl device install app",
            "xcrun devicectl device process launch",
            "Installed and launched",
        ):
            self.assertIn(required, source)

    def test_transfer_package_is_verified_before_atomic_publish(self) -> None:
        source = (PROJECT_ROOT / "tools" / "create_mac_transfer_package.ps1").read_text(
            encoding="utf-8"
        )

        self.assertIn("$temporaryArchivePath", source)
        self.assertIn("$manifestPath", source)
        self.assertIn('\"status\"', source)
        self.assertIn('\"--porcelain=v1\"', source)
        self.assertIn("tar -tf", source)
        self.assertIn("Move-Item", source)


if __name__ == "__main__":
    unittest.main()
