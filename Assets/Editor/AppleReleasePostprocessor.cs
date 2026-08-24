using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEditor.Build;
#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

namespace ThreeDoorsOfFate.Editor
{
    public static class AppleReleasePostprocessor
    {
#if UNITY_IOS
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
#endif

        private static void ConfigureInfoPlist(string buildPath)
        {
            string infoPlistPath = Path.Combine(buildPath, "Info.plist");
            XDocument infoPlist = XDocument.Load(
                infoPlistPath,
                LoadOptions.PreserveWhitespace);
            XElement dictionary = infoPlist.Root?.Element("dict")
                ?? throw new BuildFailedException("Info.plist does not contain a root dictionary.");

            RemovePlistValue(dictionary, "NSUserTrackingUsageDescription");
            SetBooleanPlistValue(
                dictionary,
                "ITSAppUsesNonExemptEncryption",
                false);
            infoPlist.Save(infoPlistPath, SaveOptions.DisableFormatting);
        }

        private static void RemovePlistValue(XElement dictionary, string key)
        {
            XElement keyElement = dictionary
                .Elements("key")
                .FirstOrDefault(element =>
                    string.Equals(element.Value, key, StringComparison.Ordinal));
            if (keyElement == null)
            {
                return;
            }

            keyElement.ElementsAfterSelf().FirstOrDefault()?.Remove();
            keyElement.Remove();
        }

        private static void SetBooleanPlistValue(
            XElement dictionary,
            string key,
            bool value)
        {
            XElement keyElement = dictionary
                .Elements("key")
                .FirstOrDefault(element =>
                    string.Equals(element.Value, key, StringComparison.Ordinal));
            XElement booleanElement = new(value ? "true" : "false");
            if (keyElement == null)
            {
                dictionary.Add(new XElement("key", key), booleanElement);
                return;
            }

            XElement currentValue = keyElement.ElementsAfterSelf().FirstOrDefault();
            if (currentValue == null)
            {
                keyElement.AddAfterSelf(booleanElement);
                return;
            }

            currentValue.ReplaceWith(booleanElement);
        }
    }
}
