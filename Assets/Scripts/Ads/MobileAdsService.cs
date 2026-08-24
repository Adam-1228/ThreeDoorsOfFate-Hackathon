using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ThreeDoorsOfFate.Ads
{
    public sealed class MobileAdsService : MonoBehaviour
    {
        private static MobileAdsService instance;

        private readonly RewardedAdRequestCoordinator rewardedRequest = new();
        private MobileAdsRuntimeSettings settings;
        private RewardedAd rewardedAd;
        private bool initializationRequested;
        private bool rewardedLoadRequested;
        private bool shuttingDown;

        public static event Action RewardedAdAvailabilityChanged;

        public static bool IsRewardedAdReady =>
            instance != null && instance.CanShowRewardedAd;

        public static bool IsPrivacyOptionsRequired =>
            IsRuntimeSupported
            && ConsentInformation.PrivacyOptionsRequirementStatus
                == PrivacyOptionsRequirementStatus.Required;

        private bool CanShowRewardedAd =>
            !shuttingDown
            && !rewardedRequest.IsActive
            && rewardedAd != null
            && rewardedAd.CanShowAd();

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

        public static void ShowRewarded(
            Func<bool> commitReward,
            Action<RewardedAdOutcome> completion)
        {
            if (instance == null || !instance.CanShowRewardedAd)
            {
                completion?.Invoke(RewardedAdOutcome.Unavailable);
                return;
            }

            instance.TryShowRewarded(commitReward, completion);
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
            if (settings == null || string.IsNullOrWhiteSpace(settings.IOSRewardedAdUnitId))
            {
                Debug.LogWarning("Mobile ads are disabled because runtime settings are missing.");
                return;
            }

            MobileAds.SetiOSAppPauseOnBackground(true);
            MobileAds.SetRequestConfiguration(CreateNonTrackingRequestConfiguration());
            GatherConsent();
        }

        private static RequestConfiguration CreateNonTrackingRequestConfiguration()
        {
            return new RequestConfiguration
            {
                PublisherFirstPartyIdEnabled = false,
                PublisherPrivacyPersonalizationState =
                    GoogleMobileAds.Api.PublisherPrivacyPersonalizationState.Disabled
            };
        }

        private void OnDestroy()
        {
            shuttingDown = true;
            rewardedAd?.Destroy();
            rewardedAd = null;
            if (rewardedRequest.IsActive)
            {
                rewardedRequest.Finish(true);
            }

            if (instance == this)
            {
                instance = null;
                NotifyRewardedAdAvailabilityChanged();
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

                    LoadRewardedAd();
                });
            });
        }

        private void LoadRewardedAd()
        {
            if (shuttingDown
                || settings == null
                || rewardedAd != null
                || rewardedLoadRequested)
            {
                return;
            }

            rewardedLoadRequested = true;
            RewardedAd.Load(
                settings.IOSRewardedAdUnitId,
                new AdRequest(),
                (ad, error) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        rewardedLoadRequested = false;
                        if (shuttingDown)
                        {
                            ad?.Destroy();
                            return;
                        }

                        if (error != null || ad == null)
                        {
                            Debug.LogWarning($"Rewarded ad load failed: {error}");
                            NotifyRewardedAdAvailabilityChanged();
                            return;
                        }

                        rewardedAd = ad;
                        RegisterRewardedAdEvents(ad);
                        NotifyRewardedAdAvailabilityChanged();
                    });
                });
        }

        private void RegisterRewardedAdEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
                DispatchRewardedAdCompletion(ad, false);
            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning($"Rewarded ad presentation failed: {error}");
                DispatchRewardedAdCompletion(ad, true);
            };
        }

        private void TryShowRewarded(
            Func<bool> commitReward,
            Action<RewardedAdOutcome> completion)
        {
            if (!CanShowRewardedAd
                || !rewardedRequest.TryBegin(commitReward, completion))
            {
                completion?.Invoke(RewardedAdOutcome.Unavailable);
                return;
            }

            RewardedAd ad = rewardedAd;
            rewardedAd = null;
            NotifyRewardedAdAvailabilityChanged();
            try
            {
                ad.Show(_ =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (!shuttingDown)
                        {
                            rewardedRequest.CommitReward();
                        }
                    });
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Rewarded ad could not be shown: {exception.Message}");
                FinishRewardedAd(ad, true);
            }
        }

        private void DispatchRewardedAdCompletion(RewardedAd ad, bool presentationFailed)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(
                () => FinishRewardedAd(ad, presentationFailed));
        }

        private void FinishRewardedAd(RewardedAd ad, bool presentationFailed)
        {
            if (!rewardedRequest.IsActive)
            {
                return;
            }

            ad?.Destroy();
            rewardedRequest.Finish(presentationFailed);
            NotifyRewardedAdAvailabilityChanged();
            LoadRewardedAd();
        }

        private static void NotifyRewardedAdAvailabilityChanged()
        {
            RewardedAdAvailabilityChanged?.Invoke();
        }
    }
}
