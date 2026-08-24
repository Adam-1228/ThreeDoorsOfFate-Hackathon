using System;

namespace ThreeDoorsOfFate.Platform
{
    public static class AdsReleaseConfiguration
    {
        private const string AdMobIdentifierPrefix = "ca-app-pub-";
        private const string GoogleTestPublisherPrefix = "ca-app-pub-3940256099942544";

        public const string GoogleTestIOSAppId =
            "ca-app-pub-3940256099942544~1458002511";
        public const string GoogleTestIOSInterstitialAdUnitId =
            "ca-app-pub-3940256099942544/4411468910";
        public const string IOSAppIdEnvironmentVariable = "ADMOB_IOS_APP_ID";
        public const string IOSInterstitialIdEnvironmentVariable =
            "ADMOB_IOS_INTERSTITIAL_ID";
        public const string RequireProductionAdsEnvironmentVariable =
            "UNITY_IOS_REQUIRE_PRODUCTION_ADS";
        public const string TrackingUsageDescription =
            "맞춤형 광고 제공과 광고 성과 측정을 위해 기기 식별자 사용을 요청합니다.";

        public static bool HasProductionIdentifiers(string appId, string interstitialAdUnitId)
        {
            return IsAdMobIdentifier(appId, '~')
                && IsAdMobIdentifier(interstitialAdUnitId, '/')
                && !appId.StartsWith(GoogleTestPublisherPrefix, StringComparison.Ordinal)
                && !interstitialAdUnitId.StartsWith(
                    GoogleTestPublisherPrefix,
                    StringComparison.Ordinal);
        }

        private static bool IsAdMobIdentifier(string value, char separator)
        {
            if (string.IsNullOrWhiteSpace(value)
                || !value.StartsWith(AdMobIdentifierPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            int separatorIndex = value.IndexOf(separator, AdMobIdentifierPrefix.Length);
            if (separatorIndex <= AdMobIdentifierPrefix.Length
                || separatorIndex == value.Length - 1
                || value.IndexOf(separator, separatorIndex + 1) >= 0)
            {
                return false;
            }

            return ContainsOnlyDigits(
                    value,
                    AdMobIdentifierPrefix.Length,
                    separatorIndex - AdMobIdentifierPrefix.Length)
                && ContainsOnlyDigits(
                    value,
                    separatorIndex + 1,
                    value.Length - separatorIndex - 1);
        }

        private static bool ContainsOnlyDigits(string value, int startIndex, int length)
        {
            for (int index = startIndex; index < startIndex + length; index += 1)
            {
                if (value[index] < '0' || value[index] > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
