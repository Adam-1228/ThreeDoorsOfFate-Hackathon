using System;

namespace ThreeDoorsOfFate.Platform
{
    public static class IOSReleaseConfiguration
    {
        public const string BundleIdentifier = "com.adam.threedoorsfate";
        public const string ICloudContainerIdentifier = "iCloud.com.adam.threedoorsfate";
        public const string EntitlementsFileName = "ThreeDoorsOfFate.entitlements";
        public const string MinimumOSVersion = "15.0";
        public const string DefaultVersion = "1.4.0";
        public const string DefaultBuildNumber = "14000";

        public static string GetEnvironmentOverride(string variableName, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
