using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Platform;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class ApplePortabilityPolicyTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string ProgressMergerTypeName =
            "ThreeDoorsOfFate.Platform.PlayerProgressMerger, ThreeDoorsOfFate.Platform";
        private const string ProgressStoreTypeName =
            "ThreeDoorsOfFate.Platform.PlayerPrefsProgressStore, ThreeDoorsOfFate.Platform";
        private const string ProgressFingerprintTypeName =
            "ThreeDoorsOfFate.Platform.PlayerProgressFingerprint, ThreeDoorsOfFate.Platform";
        private const string ProgressSyncStateTypeName =
            "ThreeDoorsOfFate.Platform.PlayerProgressSyncState, ThreeDoorsOfFate.Platform";
        private const string AppleGameServicesTypeName =
            "ThreeDoorsOfFate.Platform.AppleGameServices, ThreeDoorsOfFate.Platform";
        private const string AppleGameServicesRuntimeTypeName =
            "ThreeDoorsOfFate.Platform.AppleGameServicesRuntime, ThreeDoorsOfFate.Platform";
        private const string IOSReleaseConfigurationTypeName =
            "ThreeDoorsOfFate.Platform.IOSReleaseConfiguration, ThreeDoorsOfFate.Platform";
        private const string AdDisplayPolicyTypeName =
            "ThreeDoorsOfFate.Platform.AdDisplayPolicy, ThreeDoorsOfFate.Platform";
        private const string AdsReleaseConfigurationTypeName =
            "ThreeDoorsOfFate.Platform.AdsReleaseConfiguration, ThreeDoorsOfFate.Platform";
        private const string MobileAdsServiceTypeName =
            "ThreeDoorsOfFate.Ads.MobileAdsService, ThreeDoorsOfFate.Ads";
        private const string AdsReleaseBuildProcessorTypeName =
            "ThreeDoorsOfFate.Editor.AdsReleaseBuildProcessor, Assembly-CSharp-Editor";
        private const string AppleReleasePostprocessorTypeName =
            "ThreeDoorsOfFate.Editor.AppleReleasePostprocessor, Assembly-CSharp-Editor";

        [TestCase(RuntimePlatform.IPhonePlayer, false)]
        [TestCase(RuntimePlatform.Android, false)]
        [TestCase(RuntimePlatform.OSXPlayer, true)]
        [TestCase(RuntimePlatform.WindowsPlayer, true)]
        public void SupportsDesktopWindowControls_MatchesPlatformCapabilities(
            RuntimePlatform platform,
            bool expected)
        {
            Type controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);

            MethodInfo method = controllerType.GetMethod(
                "SupportsDesktopWindowControls",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            bool actual = (bool)method.Invoke(null, new object[] { platform });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Controller_HasApplicationPauseSaveHook()
        {
            Type controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);

            MethodInfo method = controllerType.GetMethod(
                "OnApplicationPause",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void MergeJson_UsesMaximumForMonotonicIntegerProgress()
        {
            ProgressSnapshotData local = new()
            {
                schemaVersion = 1,
                revision = 2,
                updatedAtUnixSeconds = 100,
                deviceId = "iphone",
                integers = new List<ProgressIntData>
                {
                    new() { key = "ThreeDoorsOfFate.DifficultyUnlocked", value = 2 },
                    new() { key = "ThreeDoorsOfFate.EndlessRecord.Gambler.Hard", value = 7 }
                }
            };
            ProgressSnapshotData cloud = new()
            {
                schemaVersion = 1,
                revision = 3,
                updatedAtUnixSeconds = 200,
                deviceId = "ipad",
                integers = new List<ProgressIntData>
                {
                    new() { key = "ThreeDoorsOfFate.DifficultyUnlocked", value = 1 },
                    new() { key = "ThreeDoorsOfFate.EndlessRecord.Gambler.Hard", value = 10 }
                }
            };

            ProgressSnapshotData merged = Merge(local, cloud);

            Assert.That(GetInt(merged, "ThreeDoorsOfFate.DifficultyUnlocked"), Is.EqualTo(2));
            Assert.That(GetInt(merged, "ThreeDoorsOfFate.EndlessRecord.Gambler.Hard"), Is.EqualTo(10));
            Assert.That(merged.revision, Is.EqualTo(4));
        }

        [Test]
        public void MergeJson_UnionsDiscoveredRunItems()
        {
            const string discoveredKey = "ThreeDoorsOfFate.DiscoveredItems.Gambler";
            ProgressSnapshotData local = new()
            {
                schemaVersion = 1,
                revision = 2,
                updatedAtUnixSeconds = 100,
                deviceId = "iphone",
                strings = new List<ProgressStringData>
                {
                    new() { key = discoveredKey, value = ItemListJson("amber", "bell") }
                }
            };
            ProgressSnapshotData cloud = new()
            {
                schemaVersion = 1,
                revision = 3,
                updatedAtUnixSeconds = 200,
                deviceId = "ipad",
                strings = new List<ProgressStringData>
                {
                    new() { key = discoveredKey, value = ItemListJson("bell", "candle") }
                }
            };

            ProgressSnapshotData merged = Merge(local, cloud);
            ItemListData items = JsonUtility.FromJson<ItemListData>(GetString(merged, discoveredKey));

            Assert.That(items.itemIds, Is.EqualTo(new[] { "amber", "bell", "candle" }));
        }

        [Test]
        public void MergeJson_IgnoresMalformedDiscoveredItemPayload()
        {
            const string discoveredKey = "ThreeDoorsOfFate.DiscoveredItems.Oracle";
            ProgressSnapshotData local = new()
            {
                schemaVersion = 1,
                revision = 2,
                strings = new List<ProgressStringData>
                {
                    new() { key = discoveredKey, value = "{not-json" }
                }
            };
            ProgressSnapshotData cloud = new()
            {
                schemaVersion = 1,
                revision = 3,
                strings = new List<ProgressStringData>
                {
                    new() { key = discoveredKey, value = ItemListJson("candle") }
                }
            };

            ProgressSnapshotData merged = Merge(local, cloud);
            ItemListData items = JsonUtility.FromJson<ItemListData>(GetString(merged, discoveredKey));

            Assert.That(items.itemIds, Is.EqualTo(new[] { "candle" }));
        }

        [Test]
        public void CaptureJson_CollectsKnownProgressAndActiveRunKeys()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string difficultyKey = $"{prefix}DifficultyUnlocked";
            string endingKey = $"{prefix}TrueEnding.Gambler";
            string discoveredKey = $"{prefix}DiscoveredItems.Gambler";
            string runKey = $"{prefix}HardRunSave";

            try
            {
                PlayerPrefs.SetInt(difficultyKey, 2);
                PlayerPrefs.SetInt(endingKey, 1);
                PlayerPrefs.SetString(discoveredKey, ItemListJson("amber"));
                PlayerPrefs.SetString(
                    runKey,
                    "{\"version\":2,\"runId\":\"run-140\","
                    + "\"randomCursor\":17,\"roomsCleared\":5}");

                Type storeType = Type.GetType(ProgressStoreTypeName);
                Assert.That(storeType, Is.Not.Null, "PlayerPrefsProgressStore must exist.");
                MethodInfo capture = storeType.GetMethod(
                    "CaptureJson",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.That(capture, Is.Not.Null, "CaptureJson must be public and static.");

                string json = (string)capture.Invoke(
                    null,
                    new object[] { prefix, "test-device", 7L, 1234L });
                ProgressSnapshotData snapshot = JsonUtility.FromJson<ProgressSnapshotData>(json);

                Assert.That(GetInt(snapshot, difficultyKey), Is.EqualTo(2));
                Assert.That(GetInt(snapshot, endingKey), Is.EqualTo(1));
                Assert.That(GetString(snapshot, discoveredKey), Is.EqualTo(ItemListJson("amber")));
                Assert.That(GetString(snapshot, runKey), Does.Contain("\"roomsCleared\":5"));
                Assert.That(snapshot.activeRunId, Is.EqualTo("run-140"));
                Assert.That(snapshot.activeRunSchemaVersion, Is.EqualTo(2));
                Assert.That(snapshot.activeRunRandomCursor, Is.EqualTo(17));
                Assert.That(snapshot.revision, Is.EqualTo(7));
                Assert.That(snapshot.updatedAtUnixSeconds, Is.EqualTo(1234));
            }
            finally
            {
                PlayerPrefs.DeleteKey(difficultyKey);
                PlayerPrefs.DeleteKey(endingKey);
                PlayerPrefs.DeleteKey(discoveredKey);
                PlayerPrefs.DeleteKey(runKey);
            }
        }

        [Test]
        public void AchievementCompletionKeys_RoundTripThroughCaptureAndApply()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string[] keys = AchievementProgress.NewDefinitions
                .Select(definition => AchievementProgress.GetCompletionKey(
                    prefix,
                    definition.StorageSuffix))
                .ToArray();
            Assert.That(keys, Has.Length.EqualTo(16));

            try
            {
                foreach (string key in keys)
                {
                    PlayerPrefs.SetInt(key, 1);
                }

                string json = PlayerPrefsProgressStore.CaptureJson(
                    prefix,
                    "test-device",
                    4,
                    1234);
                ProgressSnapshotData snapshot = JsonUtility.FromJson<ProgressSnapshotData>(json);
                Assert.That(
                    keys.Select(key => GetInt(snapshot, key)),
                    Is.All.EqualTo(1));

                foreach (string key in keys)
                {
                    PlayerPrefs.DeleteKey(key);
                }

                PlayerPrefsProgressStore.ApplyJson(prefix, json);
                Assert.That(
                    keys.Select(key => PlayerPrefs.GetInt(key, 0)),
                    Is.All.EqualTo(1));
            }
            finally
            {
                foreach (string key in keys)
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
        }

        [Test]
        public void MergeJson_KeepsAchievementCompletionMonotonicAcrossDevices()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string localCompletionKey = AchievementProgress.GetCompletionKey(
                prefix,
                AchievementProgress.NewDefinitions[0].StorageSuffix);
            string cloudCompletionKey = AchievementProgress.GetCompletionKey(
                prefix,
                AchievementProgress.NewDefinitions[1].StorageSuffix);
            ProgressSnapshotData local = new()
            {
                schemaVersion = 1,
                revision = 2,
                integers = new List<ProgressIntData>
                {
                    new() { key = localCompletionKey, value = 1 },
                    new() { key = cloudCompletionKey, value = 0 }
                }
            };
            ProgressSnapshotData cloud = new()
            {
                schemaVersion = 1,
                revision = 3,
                integers = new List<ProgressIntData>
                {
                    new() { key = localCompletionKey, value = 0 },
                    new() { key = cloudCompletionKey, value = 1 }
                }
            };

            ProgressSnapshotData merged = Merge(local, cloud);

            Assert.That(GetInt(merged, localCompletionKey), Is.EqualTo(1));
            Assert.That(GetInt(merged, cloudCompletionKey), Is.EqualTo(1));
        }

        [Test]
        public void ContentHash_IgnoresMetadataAndEntryOrder()
        {
            ProgressSnapshotData first = new()
            {
                schemaVersion = 1,
                revision = 1,
                updatedAtUnixSeconds = 100,
                deviceId = "iphone",
                integers = new List<ProgressIntData>
                {
                    new() { key = "b", value = 2 },
                    new() { key = "a", value = 1 }
                },
                strings = new List<ProgressStringData>
                {
                    new() { key = "z", value = "last" },
                    new() { key = "c", value = "first" }
                }
            };
            ProgressSnapshotData second = new()
            {
                schemaVersion = 1,
                revision = 99,
                updatedAtUnixSeconds = 900,
                deviceId = "ipad",
                integers = first.integers.AsEnumerable().Reverse().ToList(),
                strings = first.strings.AsEnumerable().Reverse().ToList()
            };

            Assert.That(ContentHash(first), Is.EqualTo(ContentHash(second)));
        }

        [Test]
        public void AppleGameServices_UsesStableStoreIdentifiers()
        {
            Type serviceType = Type.GetType(AppleGameServicesTypeName);
            Assert.That(serviceType, Is.Not.Null, "AppleGameServices must exist.");

            Assert.That(GetPublicConstant(serviceType, "CloudSaveName"),
                Is.EqualTo("three-doors-progress-v1"));
            Assert.That(GetPublicConstant(serviceType, "EndlessLeaderboardId"),
                Is.EqualTo("com.adam.threedoorsfate.leaderboard.endless"));
            Assert.That(GetPublicConstant(serviceType, "HardDifficultyAchievementId"),
                Is.EqualTo("com.adam.threedoorsfate.achievement.hard_unlocked"));
            Assert.That(GetPublicConstant(serviceType, "AbyssCollectorAchievementId"),
                Is.EqualTo("com.adam.threedoorsfate.achievement.abyss_collector"));
            Assert.That(GetPublicConstant(serviceType, "GamblerHighRollAchievementId"),
                Is.EqualTo("com.adam.threedoorsfate.achievement.build.gambler_high_roll"));
            Assert.That(GetPublicConstant(serviceType, "OracleRiftEngineAchievementId"),
                Is.EqualTo("com.adam.threedoorsfate.achievement.build.oracle_rift_engine"));
            Assert.That(GetPublicConstant(serviceType, "ExileLastOathAchievementId"),
                Is.EqualTo("com.adam.threedoorsfate.achievement.build.exile_last_oath"));
        }

        [Test]
        public void CaptureLocalJson_IncrementsRevisionOnlyWhenContentChanges()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string difficultyKey = $"{prefix}DifficultyUnlocked";

            try
            {
                PlayerPrefs.SetInt(difficultyKey, 1);

                ProgressSnapshotData first = CaptureLocal(prefix, 100);
                ProgressSnapshotData unchanged = CaptureLocal(prefix, 200);
                PlayerPrefs.SetInt(difficultyKey, 2);
                ProgressSnapshotData changed = CaptureLocal(prefix, 300);

                Assert.That(first.revision, Is.EqualTo(1));
                Assert.That(first.updatedAtUnixSeconds, Is.EqualTo(100));
                Assert.That(first.deviceId, Is.Not.Empty);
                Assert.That(unchanged.revision, Is.EqualTo(1));
                Assert.That(unchanged.updatedAtUnixSeconds, Is.EqualTo(100));
                Assert.That(unchanged.deviceId, Is.EqualTo(first.deviceId));
                Assert.That(changed.revision, Is.EqualTo(2));
                Assert.That(changed.updatedAtUnixSeconds, Is.EqualTo(300));
                Assert.That(changed.deviceId, Is.EqualTo(first.deviceId));
            }
            finally
            {
                PlayerPrefs.DeleteKey(difficultyKey);
                PlayerPrefs.DeleteKey($"{prefix}Cloud.DeviceId");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.Revision");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.UpdatedAt");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.ContentHash");
            }
        }

        [Test]
        public void MergeAndApplyJson_PreservesProgressAndStampsCurrentDevice()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string difficultyKey = $"{prefix}DifficultyUnlocked";
            string recordKey = $"{prefix}EndlessRecord.Gambler.Hard";

            try
            {
                PlayerPrefs.SetInt(difficultyKey, 2);
                ProgressSnapshotData local = CaptureLocal(prefix, 100);
                ProgressSnapshotData cloud = new()
                {
                    schemaVersion = 1,
                    revision = 5,
                    updatedAtUnixSeconds = 200,
                    deviceId = "ipad",
                    integers = new List<ProgressIntData>
                    {
                        new() { key = difficultyKey, value = 1 },
                        new() { key = recordKey, value = 10 }
                    }
                };

                ProgressSnapshotData merged = MergeAndApply(
                    prefix,
                    JsonUtility.ToJson(local),
                    JsonUtility.ToJson(cloud),
                    300);
                ProgressSnapshotData unchanged = CaptureLocal(prefix, 400);

                Assert.That(merged.revision, Is.EqualTo(6));
                Assert.That(merged.updatedAtUnixSeconds, Is.EqualTo(300));
                Assert.That(merged.deviceId, Is.EqualTo(local.deviceId));
                Assert.That(GetInt(merged, difficultyKey), Is.EqualTo(2));
                Assert.That(GetInt(merged, recordKey), Is.EqualTo(10));
                Assert.That(PlayerPrefs.GetInt(recordKey), Is.EqualTo(10));
                Assert.That(unchanged.revision, Is.EqualTo(6));
                Assert.That(unchanged.updatedAtUnixSeconds, Is.EqualTo(300));
            }
            finally
            {
                PlayerPrefs.DeleteKey(difficultyKey);
                PlayerPrefs.DeleteKey(recordKey);
                PlayerPrefs.DeleteKey($"{prefix}Cloud.DeviceId");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.Revision");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.UpdatedAt");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.ContentHash");
            }
        }

        [Test]
        public void AdoptRemoteMetadata_UsesHighestRevisionWithoutChangingDevice()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string difficultyKey = $"{prefix}DifficultyUnlocked";

            try
            {
                PlayerPrefs.SetInt(difficultyKey, 2);
                ProgressSnapshotData local = CaptureLocal(prefix, 100);
                ProgressSnapshotData cloud = JsonUtility.FromJson<ProgressSnapshotData>(
                    JsonUtility.ToJson(local));
                cloud.revision = 9;
                cloud.updatedAtUnixSeconds = 250;
                cloud.deviceId = "ipad";

                AdoptRemoteMetadata(prefix, JsonUtility.ToJson(cloud));
                ProgressSnapshotData afterAdoption = CaptureLocal(prefix, 300);

                Assert.That(afterAdoption.revision, Is.EqualTo(9));
                Assert.That(afterAdoption.updatedAtUnixSeconds, Is.EqualTo(250));
                Assert.That(afterAdoption.deviceId, Is.EqualTo(local.deviceId));
                Assert.That(GetInt(afterAdoption, difficultyKey), Is.EqualTo(2));
            }
            finally
            {
                PlayerPrefs.DeleteKey(difficultyKey);
                PlayerPrefs.DeleteKey($"{prefix}Cloud.DeviceId");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.Revision");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.UpdatedAt");
                PlayerPrefs.DeleteKey($"{prefix}Cloud.ContentHash");
            }
        }

        [Test]
        public void CaptureGameCenterProgress_UsesHighestScoreAndCompletedMilestones()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string difficultyKey = $"{prefix}DifficultyUnlocked";
            string gamblerRecordKey = $"{prefix}EndlessRecord.Gambler.Hard";
            string oracleRecordKey = $"{prefix}EndlessRecord.Oracle.Normal";
            string oracleEndingKey = $"{prefix}TrueEnding.Oracle";

            try
            {
                PlayerPrefs.SetInt(difficultyKey, 2);
                PlayerPrefs.SetInt(gamblerRecordKey, 7);
                PlayerPrefs.SetInt(oracleRecordKey, 12);
                PlayerPrefs.SetInt(oracleEndingKey, 1);

                Type serviceType = Type.GetType(AppleGameServicesTypeName);
                MethodInfo method = serviceType?.GetMethod(
                    "CaptureGameCenterProgress",
                    BindingFlags.Public | BindingFlags.Static);
                Assert.That(method, Is.Not.Null);
                object report = method.Invoke(null, new object[] { prefix });
                Type reportType = report.GetType();
                long score = (long)reportType.GetField("endlessScore").GetValue(report);
                string[] achievements = (string[])reportType
                    .GetField("completedAchievementIds")
                    .GetValue(report);

                Assert.That(score, Is.EqualTo(12));
                Assert.That(achievements, Does.Contain(
                    "com.adam.threedoorsfate.achievement.hard_unlocked"));
                Assert.That(achievements, Does.Contain(
                    "com.adam.threedoorsfate.achievement.true_ending.oracle"));
                Assert.That(achievements, Does.Not.Contain(
                    "com.adam.threedoorsfate.achievement.true_ending.gambler"));
            }
            finally
            {
                PlayerPrefs.DeleteKey(difficultyKey);
                PlayerPrefs.DeleteKey(gamblerRecordKey);
                PlayerPrefs.DeleteKey(oracleRecordKey);
                PlayerPrefs.DeleteKey(oracleEndingKey);
            }
        }

        [Test]
        public void CaptureGameCenterProgress_ReportsOnlyCompletedNewAchievements()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            AchievementDefinition completedFirst = AchievementProgress.NewDefinitions[0];
            AchievementDefinition incomplete = AchievementProgress.NewDefinitions[1];
            AchievementDefinition completedSecond = AchievementProgress.NewDefinitions[2];
            AchievementDefinition incompleteSecond = AchievementProgress.NewDefinitions[3];
            string[] keys = AchievementProgress.GetCompletionKeys(prefix).ToArray();

            try
            {
                PlayerPrefs.SetInt(
                    AchievementProgress.GetCompletionKey(prefix, completedFirst.StorageSuffix),
                    1);
                PlayerPrefs.SetInt(
                    AchievementProgress.GetCompletionKey(prefix, completedSecond.StorageSuffix),
                    1);

                GameCenterProgressReport report = AppleGameServices.CaptureGameCenterProgress(prefix);

                Assert.That(report.completedAchievementIds, Does.Contain(completedFirst.GameCenterId));
                Assert.That(report.completedAchievementIds, Does.Contain(completedSecond.GameCenterId));
                Assert.That(report.completedAchievementIds, Does.Not.Contain(incomplete.GameCenterId));
                Assert.That(report.completedAchievementIds, Does.Not.Contain(incompleteSecond.GameCenterId));
                Assert.That(report.completedAchievementIds.Length, Is.EqualTo(2));
            }
            finally
            {
                foreach (string key in keys)
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
        }

        [Test]
        public void AppleGameServicesRuntime_HasNativeCallbackEntryPoint()
        {
            Type runtimeType = Type.GetType(AppleGameServicesRuntimeTypeName);
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(runtimeType.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            MethodInfo callback = runtimeType.GetMethod(
                "OnNativeCloudMessage",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(callback, Is.Not.Null);
            Assert.That(callback.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(
                callback.GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(string) }));
        }

        [Test]
        public void IOSReleaseConfiguration_UsesProductionIdentifiers()
        {
            Type configurationType = Type.GetType(IOSReleaseConfigurationTypeName);
            Assert.That(configurationType, Is.Not.Null);
            Assert.That(GetPublicConstant(configurationType, "BundleIdentifier"),
                Is.EqualTo("com.adam.threedoorsfate"));
            Assert.That(GetPublicConstant(configurationType, "ICloudContainerIdentifier"),
                Is.EqualTo("iCloud.com.adam.threedoorsfate"));
            Assert.That(GetPublicConstant(configurationType, "EntitlementsFileName"),
                Is.EqualTo("ThreeDoorsOfFate.entitlements"));
            Assert.That(GetPublicConstant(configurationType, "MinimumOSVersion"),
                Is.EqualTo("15.0"));
            Assert.That(GetPublicConstant(configurationType, "DefaultVersion"),
                Is.EqualTo("1.4.0"));
            Assert.That(GetPublicConstant(configurationType, "DefaultBuildNumber"),
                Is.EqualTo("14000"));
        }

        [Test]
        public void PrivacyManifest_DeclaresCurrentNoTrackingBaseline()
        {
            string manifestPath = Path.Combine(
                Application.dataPath,
                "Plugins",
                "iOS",
                "PrivacyInfo.xcprivacy");
            Assert.That(File.Exists(manifestPath), Is.True);

            string manifest = File.ReadAllText(manifestPath);
            Assert.That(manifest, Does.Contain("<key>NSPrivacyTracking</key>"));
            Assert.That(manifest, Does.Contain("<false/>"));
            Assert.That(manifest, Does.Contain("<key>NSPrivacyCollectedDataTypes</key>"));
            Assert.That(manifest, Does.Contain("<key>NSPrivacyAccessedAPITypes</key>"));
        }

        [Test]
        public void ApplePostprocessor_RemovesLegacyATTUsageDescription()
        {
            Type postprocessorType = Type.GetType(AppleReleasePostprocessorTypeName);
            Assert.That(postprocessorType, Is.Not.Null);
            MethodInfo configureInfoPlist = postprocessorType.GetMethod(
                "ConfigureInfoPlist",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(configureInfoPlist, Is.Not.Null);

            string buildPath = Path.Combine(
                Path.GetTempPath(),
                $"tdof-att-plist-{Guid.NewGuid():N}");
            Directory.CreateDirectory(buildPath);
            string infoPlistPath = Path.Combine(buildPath, "Info.plist");
            File.WriteAllText(
                infoPlistPath,
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
                + "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" "
                + "\"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n"
                + "<plist version=\"1.0\"><dict>"
                + "<key>CFBundleIdentifier</key><string>com.adam.threedoorsfate</string>"
                + "<key>NSUserTrackingUsageDescription</key><string>legacy tracking</string>"
                + "</dict></plist>");

            try
            {
                configureInfoPlist.Invoke(null, new object[] { buildPath });
                string output = File.ReadAllText(infoPlistPath);
                Assert.That(output, Does.Not.Contain("NSUserTrackingUsageDescription"));
                Assert.That(output, Does.Contain("ITSAppUsesNonExemptEncryption"));
                Assert.That(output, Does.Contain("CFBundleIdentifier"));
                Assert.That(
                    output,
                    Does.Not.Contain("PropertyList-1.0.dtd\"[]>"),
                    "The saved plist must remain readable by Apple's plist tools.");
            }
            finally
            {
                Directory.Delete(buildPath, true);
            }
        }

        [Test]
        public void ApplePostprocessor_TargetsMainAppAndCustomICloudContainer()
        {
            string postprocessorPath = Path.Combine(
                Application.dataPath,
                "Editor",
                "AppleReleasePostprocessor.cs");
            string source = File.ReadAllText(postprocessorPath);

            Assert.That(source, Does.Contain("targetGuid: mainTarget"));
            Assert.That(source, Does.Contain("capabilities.AddGameCenter()"));
            Assert.That(source, Does.Contain("capabilities.AddiCloud("));
            Assert.That(source, Does.Contain("new[] { containerIdentifier }"));
        }

        [Test]
        public void LegacyInterstitialPolicy_IsRemoved()
        {
            Type policyType = Type.GetType(AdDisplayPolicyTypeName);
            Assert.That(policyType, Is.Null);
        }

        [Test]
        public void AdsReleaseConfiguration_SeparatesTestAndProductionIdentifiers()
        {
            Type configurationType = Type.GetType(AdsReleaseConfigurationTypeName);
            Assert.That(configurationType, Is.Not.Null, "AdsReleaseConfiguration must exist.");
            string testAppId = GetPublicConstant(configurationType, "GoogleTestIOSAppId");
            string testRewardedId = GetPublicConstant(
                configurationType,
                "GoogleTestIOSRewardedAdUnitId");
            string rewardedEnvironmentVariable = GetPublicConstant(
                configurationType,
                "IOSRewardedIdEnvironmentVariable");
            MethodInfo method = configurationType.GetMethod(
                "HasProductionIdentifiers",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            Assert.That(testAppId, Is.EqualTo("ca-app-pub-3940256099942544~1458002511"));
            Assert.That(testRewardedId, Is.EqualTo(
                "ca-app-pub-3940256099942544/1712485313"));
            Assert.That(rewardedEnvironmentVariable, Is.EqualTo(
                "ADMOB_IOS_REWARDED_ID"));
            Assert.That(method.Invoke(null, new object[] { testAppId, testRewardedId }),
                Is.EqualTo(false));
            Assert.That(method.Invoke(
                    null,
                    new object[]
                    {
                        "ca-app-pub-1234567890123456~1234567890",
                        "ca-app-pub-1234567890123456/0987654321"
                    }),
                Is.EqualTo(true));
            Assert.That(method.Invoke(
                    null,
                    new object[] { "invalid", "ca-app-pub-1234567890123456/0987654321" }),
                Is.EqualTo(false));
        }

        [Test]
        public void MobileAdsService_ExposesRewardedPrivacyAndBuildTimeIdentifierInjection()
        {
            Type serviceType = Type.GetType(MobileAdsServiceTypeName);
            Assert.That(serviceType, Is.Not.Null, "MobileAdsService must exist.");
            Assert.That(serviceType.IsSubclassOf(typeof(MonoBehaviour)), Is.True);
            Assert.That(serviceType.GetMethod(
                    "RecordRunCompleted",
                    BindingFlags.Public | BindingFlags.Static),
                Is.Null);
            Assert.That(serviceType.GetMethod(
                    "RunAfterInterstitial",
                    BindingFlags.Public | BindingFlags.Static),
                Is.Null);
            Assert.That(serviceType.GetMethod(
                    "ShowRewarded",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[]
                    {
                        typeof(Func<bool>),
                        typeof(Action<ThreeDoorsOfFate.Ads.RewardedAdOutcome>)
                    },
                    null),
                Is.Not.Null);
            Assert.That(serviceType.GetProperty(
                    "IsRewardedAdReady",
                    BindingFlags.Public | BindingFlags.Static),
                Is.Not.Null);
            Assert.That(serviceType.GetEvent(
                    "RewardedAdAvailabilityChanged",
                    BindingFlags.Public | BindingFlags.Static),
                Is.Not.Null);
            Assert.That(serviceType.GetMethod(
                    "ShowPrivacyOptions",
                    BindingFlags.Public | BindingFlags.Static),
                Is.Not.Null);
            Assert.That(serviceType.GetProperty(
                    "IsPrivacyOptionsRequired",
                    BindingFlags.Public | BindingFlags.Static),
                Is.Not.Null);

            string runtimeSettingsPath = Path.Combine(
                Application.dataPath,
                "Resources",
                "MobileAdsRuntimeSettings.asset");
            string googleSettingsPath = Path.Combine(
                Application.dataPath,
                "GoogleMobileAds",
                "Resources",
                "GoogleMobileAdsSettings.asset");
            Assert.That(File.Exists(runtimeSettingsPath), Is.True);
            Assert.That(File.Exists(googleSettingsPath), Is.True);
            string runtimeSettings = File.ReadAllText(runtimeSettingsPath);
            string googleSettings = File.ReadAllText(googleSettingsPath);
            string rewardedId = GetYamlScalar(runtimeSettings, "iosRewardedAdUnitId");
            string appId = GetYamlScalar(googleSettings, "adMobIOSAppId");
            Type configurationType = Type.GetType(AdsReleaseConfigurationTypeName);
            MethodInfo hasProductionIdentifiers = configurationType?.GetMethod(
                "HasProductionIdentifiers",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(configurationType, Is.Not.Null);
            Assert.That(hasProductionIdentifiers, Is.Not.Null);
            Assert.That(
                hasProductionIdentifiers.Invoke(null, new object[] { appId, rewardedId }),
                Is.EqualTo(false),
                "The repository must retain Google's official test pair; the release processor injects production identifiers from the environment.");
            Assert.That(appId, Is.EqualTo("ca-app-pub-3940256099942544~1458002511"));
            Assert.That(rewardedId, Is.EqualTo("ca-app-pub-3940256099942544/1712485313"));
            Assert.That(googleSettings, Does.Contain("userLanguage: ko"));
            Assert.That(
                GetYamlScalar(googleSettings, "userTrackingUsageDescription"),
                Is.Empty,
                "A no-tracking build must not advertise an ATT permission prompt.");
        }

        [Test]
        public void MobileAdsService_CreatesNonTrackingRequestConfiguration()
        {
            Type serviceType = Type.GetType(MobileAdsServiceTypeName);
            Assert.That(serviceType, Is.Not.Null);
            MethodInfo createConfiguration = serviceType.GetMethod(
                "CreateNonTrackingRequestConfiguration",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(createConfiguration, Is.Not.Null);
            object configuration = createConfiguration.Invoke(null, null);

            Assert.That(
                GetPublicMemberValue(configuration, "PublisherFirstPartyIdEnabled"),
                Is.EqualTo(false));
            Assert.That(
                GetPublicMemberValue(
                    configuration,
                    "PublisherPrivacyPersonalizationState")?.ToString(),
                Is.EqualTo("Disabled"));
        }

        [Test]
        public void MobileAdsService_ChecksRequiredFormAfterSuccessfulConsentUpdate()
        {
            string servicePath = Path.Combine(
                Application.dataPath,
                "Scripts",
                "Ads",
                "MobileAdsService.cs");
            string source = File.ReadAllText(servicePath);

            Assert.That(source, Does.Contain("ShowRequiredConsentForm();"));
            Assert.That(source, Does.Contain("private void ShowRequiredConsentForm()"));
            Assert.That(source, Does.Contain(
                "ConsentForm.LoadAndShowConsentFormIfRequired"));
        }

        [Test]
        public void AdsReleaseBuildProcessor_RejectsTestIdsForProductionBuilds()
        {
            Type processorType = Type.GetType(AdsReleaseBuildProcessorTypeName);
            Assert.That(processorType, Is.Not.Null, "AdsReleaseBuildProcessor must exist.");
            MethodInfo method = processorType.GetMethod(
                "ValidateIdentifiers",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            Assert.DoesNotThrow(() => method.Invoke(
                null,
                new object[]
                {
                    "ca-app-pub-3940256099942544~1458002511",
                    "ca-app-pub-3940256099942544/1712485313",
                    false
                }));
            TargetInvocationException testIdFailure = Assert.Throws<TargetInvocationException>(
                () => method.Invoke(
                    null,
                    new object[]
                    {
                        "ca-app-pub-3940256099942544~1458002511",
                        "ca-app-pub-3940256099942544/1712485313",
                        true
                    }));
            Assert.That(testIdFailure.InnerException?.GetType().Name,
                Is.EqualTo("BuildFailedException"));
            Assert.DoesNotThrow(() => method.Invoke(
                null,
                new object[]
                {
                    "ca-app-pub-1234567890123456~1234567890",
                    "ca-app-pub-1234567890123456/0987654321",
                    true
                }));
        }

        private static ProgressSnapshotData Merge(ProgressSnapshotData local, ProgressSnapshotData cloud)
        {
            Type mergerType = Type.GetType(ProgressMergerTypeName);
            Assert.That(mergerType, Is.Not.Null, "PlayerProgressMerger must exist.");

            MethodInfo method = mergerType.GetMethod(
                "MergeJson",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "MergeJson must be public and static.");

            string mergedJson = (string)method.Invoke(
                null,
                new object[] { JsonUtility.ToJson(local), JsonUtility.ToJson(cloud) });
            return JsonUtility.FromJson<ProgressSnapshotData>(mergedJson);
        }

        private static int GetInt(ProgressSnapshotData snapshot, string key)
        {
            return snapshot.integers.Single(entry => entry.key == key).value;
        }

        private static string GetString(ProgressSnapshotData snapshot, string key)
        {
            return snapshot.strings.Single(entry => entry.key == key).value;
        }

        private static string ItemListJson(params string[] itemIds)
        {
            return JsonUtility.ToJson(new ItemListData { itemIds = itemIds.ToList() });
        }

        private static string ContentHash(ProgressSnapshotData snapshot)
        {
            Type fingerprintType = Type.GetType(ProgressFingerprintTypeName);
            Assert.That(fingerprintType, Is.Not.Null, "PlayerProgressFingerprint must exist.");
            MethodInfo method = fingerprintType.GetMethod(
                "ComputeContentHash",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { JsonUtility.ToJson(snapshot) });
        }

        private static ProgressSnapshotData CaptureLocal(string prefix, long nowUnixSeconds)
        {
            Type syncStateType = Type.GetType(ProgressSyncStateTypeName);
            Assert.That(syncStateType, Is.Not.Null, "PlayerProgressSyncState must exist.");
            MethodInfo method = syncStateType.GetMethod(
                "CaptureLocalJson",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            string json = (string)method.Invoke(null, new object[] { prefix, nowUnixSeconds });
            return JsonUtility.FromJson<ProgressSnapshotData>(json);
        }

        private static ProgressSnapshotData MergeAndApply(
            string prefix,
            string localJson,
            string cloudJson,
            long nowUnixSeconds)
        {
            Type syncStateType = Type.GetType(ProgressSyncStateTypeName);
            Assert.That(syncStateType, Is.Not.Null, "PlayerProgressSyncState must exist.");
            MethodInfo method = syncStateType.GetMethod(
                "MergeAndApplyJson",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            string json = (string)method.Invoke(
                null,
                new object[] { prefix, localJson, cloudJson, nowUnixSeconds });
            return JsonUtility.FromJson<ProgressSnapshotData>(json);
        }

        private static void AdoptRemoteMetadata(string prefix, string cloudJson)
        {
            Type syncStateType = Type.GetType(ProgressSyncStateTypeName);
            Assert.That(syncStateType, Is.Not.Null, "PlayerProgressSyncState must exist.");
            MethodInfo method = syncStateType.GetMethod(
                "AdoptRemoteMetadata",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { prefix, cloudJson });
        }

        private static string GetPublicConstant(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"{fieldName} must be public and static.");
            return (string)field.GetValue(null);
        }

        private static object GetPublicMemberValue(object instance, string memberName)
        {
            Assert.That(instance, Is.Not.Null);
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                return property.GetValue(instance);
            }

            FieldInfo field = type.GetField(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{memberName} must be public.");
            return field.GetValue(instance);
        }

        private static string GetYamlScalar(string yaml, string key)
        {
            string prefix = key + ":";
            string line = (yaml ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(candidate => candidate.Trim())
                .Single(candidate => candidate.StartsWith(prefix, StringComparison.Ordinal));
            return line.Substring(prefix.Length).Trim();
        }

        [Serializable]
        private sealed class ProgressSnapshotData
        {
            public int schemaVersion;
            public long revision;
            public long updatedAtUnixSeconds;
            public string deviceId;
            public string activeRunId;
            public int activeRunSchemaVersion;
            public int activeRunRandomCursor;
            public List<ProgressIntData> integers = new();
            public List<ProgressStringData> strings = new();
        }

        [Serializable]
        private sealed class ProgressIntData
        {
            public string key;
            public int value;
        }

        [Serializable]
        private sealed class ProgressStringData
        {
            public string key;
            public string value;
        }

        [Serializable]
        private sealed class ItemListData
        {
            public List<string> itemIds = new();
        }
    }
}
