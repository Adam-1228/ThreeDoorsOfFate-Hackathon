#if UNITY_IOS
using System;
using System.IO;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace ThreeDoorsOfFate.Editor
{
    public static class AppleReleasePostprocessor
    {
        private const string PrivacyManifestFileName = "PrivacyInfo.xcprivacy";

        [PostProcessBuild(900)]
        public static void ConfigureXcodeProject(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject project = new();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(frameworkTarget, "GameKit.framework", false);
            project.SetBuildProperty(frameworkTarget, "CLANG_ENABLE_OBJC_ARC", "YES");
            project.SetBuildProperty(mainTarget, "TARGETED_DEVICE_FAMILY", "1,2");
            AddPrivacyManifest(project, mainTarget, buildPath);
            project.WriteToFile(projectPath);

            string containerIdentifier = IOSReleaseConfiguration.GetEnvironmentOverride(
                "UNITY_IOS_ICLOUD_CONTAINER",
                IOSReleaseConfiguration.ICloudContainerIdentifier);
            if (!containerIdentifier.StartsWith("iCloud.", StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    "UNITY_IOS_ICLOUD_CONTAINER must begin with 'iCloud.'.");
            }

            ProjectCapabilityManager capabilities = new(
                projectPath,
                IOSReleaseConfiguration.EntitlementsFileName,
                targetGuid: mainTarget);
            capabilities.AddGameCenter();
            capabilities.AddiCloud(
                false,
                true,
                false,
                false,
                new[] { containerIdentifier });
            capabilities.WriteToFile();

            ConfigureInfoPlist(buildPath);
        }

        private static void AddPrivacyManifest(
            PBXProject project,
            string mainTarget,
            string buildPath)
        {
            string sourcePath = Path.Combine(
                Application.dataPath,
                "Plugins",
                "iOS",
                PrivacyManifestFileName);
            if (!File.Exists(sourcePath))
            {
                throw new BuildFailedException(
                    $"Missing iOS privacy manifest: {sourcePath}");
            }

            string destinationPath = Path.Combine(buildPath, PrivacyManifestFileName);
            File.Copy(sourcePath, destinationPath, true);
            string fileGuid = project.FindFileGuidByProjectPath(PrivacyManifestFileName);
            if (string.IsNullOrEmpty(fileGuid))
            {
                fileGuid = project.AddFile(
                    PrivacyManifestFileName,
                    PrivacyManifestFileName,
                    PBXSourceTree.Source);
                project.AddFileToBuild(mainTarget, fileGuid);
            }
        }

        private static void ConfigureInfoPlist(string buildPath)
        {
            string infoPlistPath = Path.Combine(buildPath, "Info.plist");
            PlistDocument infoPlist = new();
            infoPlist.ReadFromFile(infoPlistPath);
            infoPlist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            infoPlist.root.SetString(
                "NSUserTrackingUsageDescription",
                AdsReleaseConfiguration.TrackingUsageDescription);
            infoPlist.WriteToFile(infoPlistPath);
        }
    }
}
#endif
