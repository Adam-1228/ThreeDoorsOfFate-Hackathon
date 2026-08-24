using ThreeDoorsOfFate.Platform;
using UnityEngine;

namespace ThreeDoorsOfFate.Ads
{
    public sealed class MobileAdsRuntimeSettings : ScriptableObject
    {
        public const string ResourcesPath = "MobileAdsRuntimeSettings";

        [SerializeField]
        private string iosRewardedAdUnitId =
            AdsReleaseConfiguration.GoogleTestIOSRewardedAdUnitId;

        public string IOSRewardedAdUnitId => iosRewardedAdUnitId;

        public void SetIOSRewardedAdUnitId(string value)
        {
            iosRewardedAdUnitId = value == null ? string.Empty : value.Trim();
        }
    }
}
