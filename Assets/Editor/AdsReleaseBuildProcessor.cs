using System;
using ThreeDoorsOfFate.Ads;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ThreeDoorsOfFate.Editor
{
    public sealed class AdsReleaseBuildProcessor : IPreprocessBuildWithReport
    {
        private const string RuntimeSettingsAssetPath =
            "Assets/Resources/MobileAdsRuntimeSettings.asset";
        private const string GoogleSettingsAssetPath =
            "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            string appId = IOSReleaseConfiguration.GetEnvironmentOverride(
                AdsReleaseConfiguration.IOSAppIdEnvironmentVariable,
                AdsReleaseConfiguration.GoogleTestIOSAppId);
            string rewardedId = IOSReleaseConfiguration.GetEnvironmentOverride(
                AdsReleaseConfiguration.IOSRewardedIdEnvironmentVariable,
                AdsReleaseConfiguration.GoogleTestIOSRewardedAdUnitId);
            bool requireProduction = string.Equals(
                Environment.GetEnvironmentVariable(
                    AdsReleaseConfiguration.RequireProductionAdsEnvironmentVariable),
                "1",
                StringComparison.Ordinal);

            ValidateIdentifiers(appId, rewardedId, requireProduction);
            ApplyIdentifiers(appId, rewardedId);
        }

        public static void ValidateIdentifiers(
            string appId,
            string rewardedId,
            bool requireProduction)
        {
            bool usesOfficialTestPair = string.Equals(
                    appId,
                    AdsReleaseConfiguration.GoogleTestIOSAppId,
                    StringComparison.Ordinal)
                && string.Equals(
                    rewardedId,
                    AdsReleaseConfiguration.GoogleTestIOSRewardedAdUnitId,
                    StringComparison.Ordinal);
            bool usesProductionPair = AdsReleaseConfiguration.HasProductionIdentifiers(
                appId,
                rewardedId);

            if (!usesOfficialTestPair && !usesProductionPair)
            {
                throw new BuildFailedException(
                    "AdMob iOS identifiers must be either the official test pair or a valid production pair.");
            }

            if (requireProduction && !usesProductionPair)
            {
                throw new BuildFailedException(
                    "Production iOS builds require ADMOB_IOS_APP_ID and "
                    + "ADMOB_IOS_REWARDED_ID. Test identifiers are not allowed.");
            }
        }

        private static void ApplyIdentifiers(string appId, string rewardedId)
        {
            MobileAdsRuntimeSettings runtimeSettings =
                AssetDatabase.LoadAssetAtPath<MobileAdsRuntimeSettings>(
                    RuntimeSettingsAssetPath);
            if (runtimeSettings == null)
            {
                throw new BuildFailedException(
                    $"Missing mobile ads runtime settings: {RuntimeSettingsAssetPath}");
            }

            runtimeSettings.SetIOSRewardedAdUnitId(rewardedId);
            EditorUtility.SetDirty(runtimeSettings);

            ScriptableObject googleSettings =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(GoogleSettingsAssetPath);
            if (googleSettings == null)
            {
                throw new BuildFailedException(
                    $"Missing Google Mobile Ads settings: {GoogleSettingsAssetPath}");
            }

            SerializedObject serializedSettings = new(googleSettings);
            SetRequiredString(serializedSettings, "adMobIOSAppId", appId);
            SetRequiredString(
                serializedSettings,
                "userTrackingUsageDescription",
                AdsReleaseConfiguration.TrackingUsageDescription);
            SetRequiredString(serializedSettings, "userLanguage", "ko");
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(googleSettings);
            AssetDatabase.SaveAssets();
        }

        private static void SetRequiredString(
            SerializedObject serializedObject,
            string propertyName,
            string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new BuildFailedException(
                    $"Google Mobile Ads settings property is missing: {propertyName}");
            }

            property.stringValue = value;
        }
    }
}
