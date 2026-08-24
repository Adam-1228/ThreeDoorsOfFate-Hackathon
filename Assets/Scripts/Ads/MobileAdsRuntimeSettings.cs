using ThreeDoorsOfFate.Platform;
using UnityEngine;

namespace ThreeDoorsOfFate.Ads
{
    public sealed class MobileAdsRuntimeSettings : ScriptableObject
    {
        public const string ResourcesPath = "MobileAdsRuntimeSettings";

        [SerializeField]
        private string iosInterstitialAdUnitId =
            AdsReleaseConfiguration.GoogleTestIOSInterstitialAdUnitId;

        public string IOSInterstitialAdUnitId => iosInterstitialAdUnitId;

        public void SetIOSInterstitialAdUnitId(string value)
        {
            iosInterstitialAdUnitId = value == null ? string.Empty : value.Trim();
        }
    }
}
