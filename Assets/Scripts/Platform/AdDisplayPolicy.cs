namespace ThreeDoorsOfFate.Platform
{
    public static class AdDisplayPolicy
    {
        public const int MinimumCompletedRuns = 2;
        public const double MinimumSecondsBetweenAds = 180d;

        public static bool ShouldShowInterstitial(
            int completedRunsSinceAd,
            double nowSeconds,
            double lastShownAtSeconds,
            bool gameplayActive,
            bool adReady)
        {
            if (completedRunsSinceAd < MinimumCompletedRuns || gameplayActive || !adReady)
            {
                return false;
            }

            if (double.IsNaN(nowSeconds) || nowSeconds < 0d)
            {
                return false;
            }

            return lastShownAtSeconds <= 0d
                || nowSeconds - lastShownAtSeconds >= MinimumSecondsBetweenAds;
        }
    }
}
