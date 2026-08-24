using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Ads;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Platform;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private RectTransform rewardedRelicActionRoot;
        private Button rewardedRelicActionButton;
        private Text rewardedRelicActionLabel;
        private CharacterClass rewardedRelicActionCharacter;
        private RunItemDefinition rewardedRelicCommittedItem;
        private bool rewardedRelicRequestActive;
        private bool rewardedRelicAdsSubscribed;

        private void InitializeRewardedRelicAds()
        {
            if (rewardedRelicAdsSubscribed)
            {
                return;
            }

            MobileAdsService.RewardedAdAvailabilityChanged +=
                HandleRewardedAdAvailabilityChanged;
            rewardedRelicAdsSubscribed = true;
        }

        private void OnDestroy()
        {
            if (!rewardedRelicAdsSubscribed)
            {
                return;
            }

            MobileAdsService.RewardedAdAvailabilityChanged -=
                HandleRewardedAdAvailabilityChanged;
            rewardedRelicAdsSubscribed = false;
        }

        private void AddRewardedRelicAction(
            RectTransform actionBar,
            CharacterClass characterClass)
        {
            rewardedRelicActionCharacter = characterClass;
            Sprite frameSprite = classConfirmButtonSprite != null
                ? classConfirmButtonSprite
                : GetClassDetailActionButtonSprite();
            rewardedRelicActionRoot = AddPanel(
                actionBar,
                "보상형 유물 광고",
                Color.white,
                frameSprite);
            SetAnchors(
                rewardedRelicActionRoot,
                new Vector2(0.315f, 0.065f),
                new Vector2(0.685f, 0.935f));

            Image frame = rewardedRelicActionRoot.GetComponent<Image>();
            frame.type = Image.Type.Simple;
            frame.color = new Color(1.08f, 1.06f, 1.01f, 1f);
            frame.raycastTarget = true;

            rewardedRelicActionButton = AddSfxButton(
                rewardedRelicActionRoot.gameObject,
                GameSfxCue.ImportantConfirm);
            rewardedRelicActionButton.targetGraphic = frame;
            rewardedRelicActionButton.colors = CreateButtonColors();
            rewardedRelicActionButton.onClick.AddListener(
                () => BeginRewardedRelicAd(characterClass));

            Text playMarker = AddText(
                rewardedRelicActionRoot,
                "광고 재생 표시",
                "▶",
                13,
                TextAnchor.MiddleCenter,
                new Color(0.48f, 1f, 0.91f, 1f));
            playMarker.fontStyle = FontStyle.Bold;
            SetAnchors(
                playMarker.rectTransform,
                new Vector2(0.035f, 0.300f),
                new Vector2(0.125f, 0.700f));

            Sprite relicIcon = GetRunItemSilhouetteIcon(RunItemType.Relic);
            float labelMinX;
            if (relicIcon != null)
            {
                Image icon = AddImage(
                    rewardedRelicActionRoot,
                    "광고 유물 표시",
                    Color.white);
                icon.sprite = relicIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(
                    icon.rectTransform,
                    new Vector2(0.125f, 0.300f),
                    new Vector2(0.235f, 0.700f));
                labelMinX = 0.245f;
            }
            else
            {
                Text relicMarker = AddText(
                    rewardedRelicActionRoot,
                    "광고 유물 표시",
                    "◇",
                    16,
                    TextAnchor.MiddleCenter,
                    new Color(0.96f, 0.82f, 0.52f, 1f));
                relicMarker.fontStyle = FontStyle.Bold;
                SetAnchors(
                    relicMarker.rectTransform,
                    new Vector2(0.125f, 0.300f),
                    new Vector2(0.225f, 0.700f));
                labelMinX = 0.235f;
            }

            rewardedRelicActionLabel = AddText(
                rewardedRelicActionRoot,
                "보상형 유물 광고 라벨",
                string.Empty,
                13,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.93f, 0.78f, 1f));
            rewardedRelicActionLabel.fontStyle = FontStyle.Bold;
            rewardedRelicActionLabel.resizeTextForBestFit = true;
            rewardedRelicActionLabel.resizeTextMinSize = 9;
            rewardedRelicActionLabel.resizeTextMaxSize = 13;
            SetAnchors(
                rewardedRelicActionLabel.rectTransform,
                new Vector2(labelMinX, 0.260f),
                new Vector2(0.965f, 0.740f));

            RefreshRewardedRelicAction();
        }

        private void HandleRewardedAdAvailabilityChanged()
        {
            if (phase == GamePhase.ClassDetails
                && rewardedRelicActionRoot != null)
            {
                RefreshRewardedRelicAction();
            }
        }

        private void RefreshRewardedRelicAction()
        {
            if (rewardedRelicActionRoot == null
                || rewardedRelicActionButton == null
                || rewardedRelicActionLabel == null)
            {
                return;
            }

            RewardedRelicDailyStatus dailyStatus =
                RewardedRelicDailyLimitStore.GetStatus(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    rewardedRelicActionCharacter.ToString(),
                    DateTimeOffset.Now);
            List<RunItemDefinition> candidates = GetRewardedRelicCandidates(
                rewardedRelicActionCharacter,
                currentDifficulty);
            bool poolComplete = candidates.Count == 0;
            bool dailyComplete = dailyStatus.RemainingCount <= 0;
            bool ready = !rewardedRelicRequestActive
                && !poolComplete
                && !dailyComplete
                && MobileAdsService.IsRewardedAdReady;

            rewardedRelicActionButton.interactable = ready;

            if (poolComplete)
            {
                rewardedRelicActionLabel.text = L("rewarded.item.poolComplete");
            }
            else if (dailyComplete)
            {
                rewardedRelicActionLabel.text = L("rewarded.item.dailyComplete");
            }
            else if (rewardedRelicRequestActive)
            {
                rewardedRelicActionLabel.text = L("rewarded.item.checking");
            }
            else if (!MobileAdsService.IsRewardedAdReady)
            {
                rewardedRelicActionLabel.text = LF(
                    "rewarded.item.loading",
                    dailyStatus.RemainingCount);
            }
            else
            {
                rewardedRelicActionLabel.text = LF(
                    "rewarded.item.ready",
                    dailyStatus.RemainingCount);
            }

            Image frame = rewardedRelicActionRoot.GetComponent<Image>();
            frame.color = ready
                ? new Color(1.08f, 1.06f, 1.01f, 1f)
                : new Color(0.58f, 0.58f, 0.58f, 0.86f);
        }

        private List<RunItemDefinition> GetRewardedRelicCandidates(
            CharacterClass characterClass,
            RunDifficulty difficulty)
        {
            selectedClass = characterClass;
            LoadDiscoveredRunItemsForSelectedClass();

            IReadOnlyList<RunItemDefinition> definitions =
                GetRunItemDefinitions();
            RewardedRelicCandidate[] policyCandidates = definitions
                .Select(item => new RewardedRelicCandidate(
                    item.Id,
                    ToRewardedRelicCategory(item.Type)))
                .ToArray();
            HashSet<string> eligibleIds = RewardedRelicPolicy
                .GetEligibleUndiscovered(
                    ToRewardedRelicDifficulty(difficulty),
                    policyCandidates,
                    discoveredRunItemIds)
                .Select(candidate => candidate.ItemId)
                .ToHashSet(StringComparer.Ordinal);
            return definitions
                .Where(item => eligibleIds.Contains(item.Id))
                .ToList();
        }

        private void BeginRewardedRelicAd(CharacterClass characterClass)
        {
            if (rewardedRelicRequestActive)
            {
                return;
            }

            RunDifficulty capturedDifficulty = currentDifficulty;
            RewardedRelicDailyStatus dailyStatus =
                RewardedRelicDailyLimitStore.GetStatus(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    characterClass.ToString(),
                    DateTimeOffset.Now);
            List<RunItemDefinition> candidates = GetRewardedRelicCandidates(
                characterClass,
                capturedDifficulty);
            if (dailyStatus.RemainingCount <= 0 || candidates.Count == 0)
            {
                RefreshRewardedRelicAction();
                return;
            }

            string reservedItemId =
                candidates[Random.Range(0, candidates.Count)].Id;
            rewardedRelicCommittedItem = null;
            rewardedRelicRequestActive = true;
            RefreshRewardedRelicAction();

            MobileAdsService.ShowRewarded(
                () => CommitRewardedRelic(
                    characterClass,
                    capturedDifficulty,
                    reservedItemId),
                outcome =>
                {
                    if (this != null)
                    {
                        CompleteRewardedRelicAd(characterClass, outcome);
                    }
                });
        }

        private bool CommitRewardedRelic(
            CharacterClass characterClass,
            RunDifficulty difficulty,
            string reservedItemId)
        {
            List<RunItemDefinition> candidates = GetRewardedRelicCandidates(
                characterClass,
                difficulty);
            RewardedRelicDailyStatus dailyStatus =
                RewardedRelicDailyLimitStore.GetStatus(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    characterClass.ToString(),
                    DateTimeOffset.Now);
            if (dailyStatus.RemainingCount <= 0 || candidates.Count == 0)
            {
                return false;
            }

            RunItemDefinition item = candidates.FirstOrDefault(
                    candidate => candidate.Id == reservedItemId)
                ?? candidates[Random.Range(0, candidates.Count)];
            if (!DiscoverRunItemForSelectedClass(item))
            {
                return false;
            }

            if (!RewardedRelicDailyLimitStore.TryConsume(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    characterClass.ToString(),
                    DateTimeOffset.Now,
                    out _))
            {
                discoveredRunItemIds.Remove(item.Id);
                SaveDiscoveredRunItemsForSelectedClass();
                return false;
            }

            rewardedRelicCommittedItem = item;
            return true;
        }

        private void CompleteRewardedRelicAd(
            CharacterClass characterClass,
            RewardedAdOutcome outcome)
        {
            rewardedRelicRequestActive = false;
            RunItemDefinition committedItem = rewardedRelicCommittedItem;
            rewardedRelicCommittedItem = null;
            if (outcome == RewardedAdOutcome.RewardCommitted
                && committedItem != null)
            {
                ShowRewardedRelicResult(characterClass, committedItem);
                return;
            }

            ShowClassDetail(characterClass);
        }

        private void ShowRewardedRelicResult(
            CharacterClass characterClass,
            RunItemDefinition item)
        {
            selectedClass = characterClass;
            phase = GamePhase.ClassDetails;
            SetBackground(classSelectBackground);
            ClearContent();
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetAnchors(
                contentRoot,
                new Vector2(0.100f, 0.130f),
                new Vector2(0.900f, 0.865f));
            primaryButton.gameObject.SetActive(false);
            rewardedRelicActionRoot = null;
            rewardedRelicActionButton = null;
            rewardedRelicActionLabel = null;

            RectTransform panel = AddPanel(
                contentRoot,
                "보상형 유물 결과",
                new Color(1f, 1f, 1f, 0.88f),
                statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite);
            SetFramedModalPanelAnchors(panel);
            AddFramedModalTitle(
                contentRoot,
                "보상형 유물 결과 제목 박스",
                $"{GetRunItemTypeName(item.Type)} 획득",
                0.325f,
                0.675f);

            Sprite iconSprite = GetRunItemIcon(item);
            Vector2 bodyMin = new(0.120f, 0.400f);
            if (iconSprite != null)
            {
                Image icon = AddImage(panel, "보상형 유물 결과 아이콘", Color.white);
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(
                    icon.rectTransform,
                    new Vector2(0.145f, 0.430f),
                    new Vector2(0.315f, 0.730f));
                bodyMin.x = 0.350f;
            }

            string resultText =
                $"{GetRunItemTypeName(item.Type)} | {item.Name}\n"
                + $"{item.Effect}\n{item.Description}\n\n"
                + "보관함에 추가되었습니다. 자동으로 장착되지 않습니다.";
            Text body = AddText(
                panel,
                "보상형 유물 결과 설명",
                resultText,
                22,
                TextAnchor.MiddleCenter,
                new Color(0.88f, 0.84f, 0.76f, 1f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 14;
            body.resizeTextMaxSize = 22;
            SetAnchors(body.rectTransform, bodyMin, new Vector2(0.880f, 0.745f));

            RectTransform closeRoot = AddPanel(
                panel,
                "보상 결과 닫기",
                Color.white,
                GetClassDetailActionButtonSprite());
            SetAnchors(
                closeRoot,
                new Vector2(0.340f, 0.155f),
                new Vector2(0.660f, 0.305f));
            Image closeImage = closeRoot.GetComponent<Image>();
            closeImage.type = Image.Type.Simple;
            closeImage.raycastTarget = true;
            Button closeButton = AddSfxButton(
                closeRoot.gameObject,
                GameSfxCue.None);
            closeButton.targetGraphic = closeImage;
            closeButton.colors = CreateButtonColors();
            closeButton.onClick.AddListener(
                () => ShowClassDetail(characterClass));

            Text closeLabel = AddText(
                closeRoot,
                "보상 결과 닫기 라벨",
                "확인",
                20,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.93f, 0.78f, 1f));
            closeLabel.fontStyle = FontStyle.Bold;
            SetAnchors(
                closeLabel.rectTransform,
                new Vector2(0.120f, 0.180f),
                new Vector2(0.880f, 0.820f));
        }

        private static RewardedRelicDifficulty ToRewardedRelicDifficulty(
            RunDifficulty difficulty)
        {
            return difficulty switch
            {
                RunDifficulty.Normal => RewardedRelicDifficulty.Normal,
                RunDifficulty.Hard => RewardedRelicDifficulty.Hard,
                _ => RewardedRelicDifficulty.Easy
            };
        }

        private static RewardedRelicCategory ToRewardedRelicCategory(
            RunItemType type)
        {
            return type switch
            {
                RunItemType.Blessing => RewardedRelicCategory.Blessing,
                RunItemType.Curse => RewardedRelicCategory.Curse,
                _ => RewardedRelicCategory.Relic
            };
        }
    }
}
