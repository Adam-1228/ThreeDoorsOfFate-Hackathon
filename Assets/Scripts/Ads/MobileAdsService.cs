using System;
using System.Globalization;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using ThreeDoorsOfFate.Platform;
using UnityEngine;

namespace ThreeDoorsOfFate.Ads
{
    public sealed class MobileAdsService : MonoBehaviour
    {
        private const string CompletedRunsKey = "ThreeDoorsOfFate.Ads.CompletedRunsSinceAd";
        private const string LastShownAtKey = "ThreeDoorsOfFate.Ads.LastShownAtUnixSeconds";

        private static MobileAdsService instance;

        private MobileAdsRuntimeSettings settings;
        private InterstitialAd interstitialAd;
        private Action pendingContinuation;
        private bool initializationRequested;
        private bool shuttingDown;

        public static bool IsPrivacyOptionsRequired =>
            IsRuntimeSupported
            && ConsentInformation.PrivacyOptionsRequirementStatus
                == PrivacyOptionsRequirementStatus.Required;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!IsRuntimeSupported || instance != null)
            {
                return;
            }

            GameObject host = new("Mobile Ads Service");
            host.AddComponent<MobileAdsService>();
        }

        public static void RecordRunCompleted()
        {
            if (!IsRuntimeSupported)
            {
                return;
            }

            int completedRuns = Mathf.Clamp(PlayerPrefs.GetInt(CompletedRunsKey, 0) + 1, 0, 100);
            PlayerPrefs.SetInt(CompletedRunsKey, completedRuns);
            PlayerPrefs.Save();
        }

        public static void RunAfterInterstitial(Action continuation, bool gameplayActive)
        {
            if (instance == null)
            {
                continuation?.Invoke();
                return;
            }

            instance.TryRunAfterInterstitial(continuation, gameplayActive);
        }

        public static void ShowPrivacyOptions()
        {
            if (instance == null || !IsPrivacyOptionsRequired)
            {
                return;
            }

            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (error != null)
                    {
                        Debug.LogWarning($"Unable to show ad privacy options: {error.Message}");
                        return;
                    }

                    if (ConsentInformation.CanRequestAds())
                    {
                        instance?.InitializeMobileAds();
                    }
                });
            });
        }

        private static bool IsRuntimeSupported =>
            !Application.isEditor && Application.platform == RuntimePlatform.IPhonePlayer;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            settings = Resources.Load<MobileAdsRuntimeSettings>(
                MobileAdsRuntimeSettings.ResourcesPath);
            if (settings == null || string.IsNullOrWhiteSpace(settings.IOSInterstitialAdUnitId))
            {
                Debug.LogWarning("Mobile ads are disabled because runtime settings are missing.");
                return;
            }

            MobileAds.SetiOSAppPauseOnBackground(true);
            GatherConsent();
        }

        private void OnDestroy()
        {
            shuttingDown = true;
            interstitialAd?.Destroy();
            interstitialAd = null;
            if (instance == this)
            {
                instance = null;
            }
        }

        private void GatherConsent()
        {
            ConsentRequestParameters request = new()
            {
                TagForUnderAgeOfConsent = false
            };
            ConsentInformation.Update(request, updateError =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (shuttingDown)
                    {
                        return;
                    }

                    if (updateError != null)
                    {
                        Debug.LogWarning($"Unable to refresh ad consent: {updateError.Message}");
                        if (ConsentInformation.CanRequestAds())
                        {
                            InitializeMobileAds();
                        }
                        return;
                    }

                    ShowRequiredConsentForm();
                });
            });
        }

        private void ShowRequiredConsentForm()
        {
            ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (showError != null)
                    {
                        Debug.LogWarning(
                            $"Unable to complete ad consent: {showError.Message}");
                    }

                    if (!shuttingDown && ConsentInformation.CanRequestAds())
                    {
                        InitializeMobileAds();
                    }
                });
            });
        }

        private void InitializeMobileAds()
        {
            if (initializationRequested || shuttingDown)
            {
                return;
            }

            initializationRequested = true;
            MobileAds.Initialize(status =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (shuttingDown)
                    {
                        return;
                    }

                    if (status == null)
                    {
                        initializationRequested = false;
                        Debug.LogWarning("Google Mobile Ads initialization failed.");
                        return;
                    }

                    LoadInterstitial();
                });
            });
        }

        private void LoadInterstitial()
        {
            if (shuttingDown || settings == null || interstitialAd != null)
            {
                return;
            }

            InterstitialAd.Load(
                settings.IOSInterstitialAdUnitId,
                new AdRequest(),
                (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (shuttingDown)
                        {
                            ad?.Destroy();
                            return;
                        }

                        if (error != null || ad == null)
                        {
                            Debug.LogWarning($"Interstitial ad load failed: {error}");
                            return;
                        }

                        interstitialAd = ad;
                        RegisterInterstitialEvents(ad);
                    });
                });
        }

        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed += () => DispatchAdCompletion(true);
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning($"Interstitial ad presentation failed: {error}");
                DispatchAdCompletion(false);
            };
        }

        private void TryRunAfterInterstitial(Action continuation, bool gameplayActive)
        {
            if (pendingContinuation != null)
            {
                continuation?.Invoke();
                return;
            }

            int completedRuns = PlayerPrefs.GetInt(CompletedRunsKey, 0);
            double nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            double lastShownAtSeconds = ReadLastShownAtSeconds();
            bool adReady = interstitialAd != null && interstitialAd.CanShowAd();
            if (!AdDisplayPolicy.ShouldShowInterstitial(
                    completedRuns,
                    nowSeconds,
                    lastShownAtSeconds,
                    gameplayActive,
                    adReady))
            {
                continuation?.Invoke();
                return;
            }

            pendingContinuation = continuation;
            try
            {
                interstitialAd.Show();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Interstitial ad could not be shown: {exception.Message}");
                FinishAdBreak(false);
            }
        }

        private void DispatchAdCompletion(bool shown)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() => FinishAdBreak(shown));
        }

        private void FinishAdBreak(bool shown)
        {
            if (shown)
            {
                PlayerPrefs.SetInt(CompletedRunsKey, 0);
                PlayerPrefs.SetString(
                    LastShownAtKey,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(
                        CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
            }

            interstitialAd?.Destroy();
            interstitialAd = null;
            Action continuation = pendingContinuation;
            pendingContinuation = null;
            continuation?.Invoke();
            LoadInterstitial();
        }

        private static double ReadLastShownAtSeconds()
        {
            string stored = PlayerPrefs.GetString(LastShownAtKey, string.Empty);
            return double.TryParse(
                stored,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value)
                ? value
                : 0d;
        }
    }
}
