using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const int AchievementCardsPerPage = 4;
        private RectTransform achievementOverlay;
        private RectTransform achievementCardsRoot;
        private Text achievementPageText;
        private Text achievementCompletionText;
        private Button achievementPreviousButton;
        private Button achievementNextButton;
        private IReadOnlyList<AchievementCardModel> achievementCardModels;
        private int achievementPageIndex;
        private readonly List<string> newlyCompletedAchievementNames = new();

        private sealed class AchievementCardModel
        {
            public AchievementCardModel(
                string displayName,
                string lockedDescription,
                string earnedDescription,
                string resourcePath,
                int points,
                bool completed,
                int progressCurrent,
                int progressTarget,
                Sprite fallbackSprite = null)
            {
                DisplayName = displayName;
                LockedDescription = lockedDescription;
                EarnedDescription = earnedDescription;
                ResourcePath = resourcePath;
                Points = points;
                Completed = completed;
                ProgressCurrent = progressCurrent;
                ProgressTarget = Mathf.Max(1, progressTarget);
                FallbackSprite = fallbackSprite;
            }

            public string DisplayName { get; }
            public string LockedDescription { get; }
            public string EarnedDescription { get; }
            public string ResourcePath { get; }
            public int Points { get; }
            public bool Completed { get; }
            public int ProgressCurrent { get; }
            public int ProgressTarget { get; }
            public Sprite FallbackSprite { get; }
        }

        private void ShowAchievements()
        {
            HideAchievements();
            AppleGameServicesRuntime.SetAccessPointVisible(true);
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(false);
            }

            Image overlayImage = AddImage(
                root,
                "업적 오버레이",
                new Color(0f, 0f, 0f, 0.78f));
            overlayImage.raycastTarget = true;
            achievementOverlay = overlayImage.rectTransform;
            Stretch(achievementOverlay);
            AddClickBlocker(overlayImage);

            Sprite modalSprite = mainOptionsPanelSprite != null
                ? mainOptionsPanelSprite
                : statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite;
            RectTransform modal = AddPanel(
                achievementOverlay,
                "업적 모달",
                Color.white,
                modalSprite);
            SetAnchors(modal, new Vector2(0.018f, 0.026f), new Vector2(0.982f, 0.974f));
            AddClickBlocker(modal.GetComponent<Image>());

            Text heading = AddText(
                modal,
                "업적 모달 제목",
                "운명에 새겨진 기록",
                32,
                TextAnchor.MiddleCenter,
                new Color(0.76f, 1f, 0.96f, 1f));
            ConfigureAchievementText(heading, 19, 32, true);
            AddTextGlow(
                heading,
                new Color(0f, 0f, 0f, 0.92f),
                new Color(0.08f, 0.62f, 0.58f, 0.42f),
                new Vector2(1.1f, -1.1f));
            SetAnchors(heading.rectTransform, new Vector2(0.15f, 0.905f), new Vector2(0.85f, 0.980f));

            Button closeButton = AddSettingsMenuButton(
                modal,
                "업적 닫기",
                "닫기",
                18,
                GameSfxCue.UiAccept);
            SetAnchors(
                closeButton.GetComponent<RectTransform>(),
                new Vector2(0.875f, 0.910f),
                new Vector2(0.955f, 0.975f));
            closeButton.onClick.AddListener(HideAchievements);

            achievementCompletionText = AddText(
                modal,
                "업적 달성 요약",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                new Color(0.94f, 0.88f, 0.70f, 1f));
            achievementCompletionText.fontStyle = FontStyle.Bold;
            achievementCompletionText.resizeTextForBestFit = true;
            achievementCompletionText.resizeTextMinSize = 13;
            achievementCompletionText.resizeTextMaxSize = 18;
            SetAnchors(
                achievementCompletionText.rectTransform,
                new Vector2(0.045f, 0.910f),
                new Vector2(0.245f, 0.975f));

            achievementCardsRoot = AddPanel(
                modal,
                "업적 목록 페이지",
                new Color(0f, 0f, 0f, 0f));
            Image cardsRootImage = achievementCardsRoot.GetComponent<Image>();
            if (cardsRootImage != null)
            {
                cardsRootImage.raycastTarget = false;
            }
            SetAnchors(
                achievementCardsRoot,
                new Vector2(0.035f, 0.145f),
                new Vector2(0.965f, 0.895f));

            achievementPreviousButton = AddLocalizedSettingsMenuButton(
                modal,
                "업적 이전 페이지",
                "achievement.previous",
                18,
                GameSfxCue.UiAccept);
            SetAnchors(
                achievementPreviousButton.GetComponent<RectTransform>(),
                new Vector2(0.135f, 0.045f),
                new Vector2(0.335f, 0.125f));
            achievementPreviousButton.onClick.AddListener(
                () => ShowAchievementPage(achievementPageIndex - 1));

            achievementPageText = AddText(
                modal,
                "업적 페이지 번호",
                string.Empty,
                19,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 1f, 0.94f, 1f));
            achievementPageText.fontStyle = FontStyle.Bold;
            SetAnchors(
                achievementPageText.rectTransform,
                new Vector2(0.410f, 0.050f),
                new Vector2(0.590f, 0.120f));

            achievementNextButton = AddLocalizedSettingsMenuButton(
                modal,
                "업적 다음 페이지",
                "achievement.next",
                18,
                GameSfxCue.UiAccept);
            SetAnchors(
                achievementNextButton.GetComponent<RectTransform>(),
                new Vector2(0.665f, 0.045f),
                new Vector2(0.865f, 0.125f));
            achievementNextButton.onClick.AddListener(
                () => ShowAchievementPage(achievementPageIndex + 1));

            achievementCardModels = GetAchievementCardModels();
            ShowAchievementPage(0);

            achievementOverlay.SetAsLastSibling();
        }

        private IReadOnlyList<AchievementCardModel> GetAchievementCardModels()
        {
            bool hardUnlocked = PlayerPrefs.GetInt(DifficultyUnlockKey, 0) >= 2;
            bool gamblerEnding = PlayerPrefs.GetInt(
                GetTrueEndingKey(CharacterClass.Gambler),
                0) > 0;
            bool oracleEnding = PlayerPrefs.GetInt(
                GetTrueEndingKey(CharacterClass.Oracle),
                0) > 0;
            bool exileEnding = PlayerPrefs.GetInt(
                GetTrueEndingKey(CharacterClass.Exile),
                0) > 0;
            List<AchievementCardModel> models = new()
            {
                new AchievementCardModel(
                    "심연으로 가는 문",
                    "어려움 난이도를 해금하세요.",
                    "어려움 난이도를 해금했습니다.",
                    string.Empty,
                    100,
                    hardUnlocked,
                    hardUnlocked ? 1 : 0,
                    1,
                    hardBossDoorSprite != null ? hardBossDoorSprite : bossSprite),
                new AchievementCardModel(
                    "도박사의 진엔딩",
                    "도박사의 숨겨진 결말을 찾으세요.",
                    "도박사의 진엔딩을 발견했습니다.",
                    string.Empty,
                    100,
                    gamblerEnding,
                    gamblerEnding ? 1 : 0,
                    1,
                    gamblerSelectSprite),
                new AchievementCardModel(
                    "예언가의 진엔딩",
                    "예언가의 숨겨진 결말을 찾으세요.",
                    "예언가의 진엔딩을 발견했습니다.",
                    string.Empty,
                    100,
                    oracleEnding,
                    oracleEnding ? 1 : 0,
                    1,
                    oracleSelectSprite),
                new AchievementCardModel(
                    "추방자의 진엔딩",
                    "추방자의 숨겨진 결말을 찾으세요.",
                    "추방자의 진엔딩을 발견했습니다.",
                    string.Empty,
                    100,
                    exileEnding,
                    exileEnding ? 1 : 0,
                    1,
                    exileSelectSprite)
            };

            foreach (AchievementDefinition definition in AchievementProgress.NewDefinitions)
            {
                bool completed = AchievementProgress.IsCompleted(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    definition);
                (int progressCurrent, int progressTarget) =
                    GetAchievementProgress(definition, completed);
                models.Add(new AchievementCardModel(
                    definition.DisplayName,
                    definition.LockedDescription,
                    definition.EarnedDescription,
                    definition.ImageResourcePath,
                    definition.Points,
                    completed,
                    progressCurrent,
                    progressTarget,
                    mainTitleLogoSprite));
            }

            return models;
        }

        private void AddAchievementCard(
            RectTransform cardsRoot,
            AchievementCardModel model,
            int absoluteIndex,
            int pageSlot)
        {
            const float left = 0.010f;
            const float right = 0.990f;
            const float columnGap = 0.020f;
            const float rowBottom = 0.015f;
            const float rowTop = 0.985f;
            const float rowGap = 0.025f;
            float cardWidth = (right - left - columnGap) / 2f;
            float cardHeight = (rowTop - rowBottom - rowGap) / 2f;
            int column = pageSlot % 2;
            int row = pageSlot / 2;
            float minX = left + column * (cardWidth + columnGap);
            float maxY = rowTop - row * (cardHeight + rowGap);

            RectTransform card = AddPanel(
                cardsRoot,
                $"업적 카드 {absoluteIndex + 1}",
                model.Completed
                    ? Color.white
                    : new Color(0.72f, 0.75f, 0.78f, 0.96f),
                statusCategoryCardFrameSprite != null
                    ? statusCategoryCardFrameSprite
                    : panelSprite);
            SetAnchors(
                card,
                new Vector2(minX, maxY - cardHeight),
                new Vector2(minX + cardWidth, maxY));

            Sprite artwork = string.IsNullOrWhiteSpace(model.ResourcePath)
                ? null
                : Resources.Load<Sprite>(model.ResourcePath);
            artwork ??= model.FallbackSprite;

            RectTransform artworkSlot = AddPanel(
                card,
                "업적 이미지 슬롯",
                Color.white,
                statusInnerPanelFrameSprite != null
                    ? statusInnerPanelFrameSprite
                    : panelSprite);
            artworkSlot.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                artworkSlot,
                new Vector2(0.035f, 0.140f),
                new Vector2(0.315f, 0.860f));

            Image image = AddImage(
                artworkSlot,
                "업적 이미지",
                artwork != null
                    ? model.Completed
                        ? Color.white
                        : new Color(0.52f, 0.54f, 0.58f, 0.96f)
                    : new Color(0.04f, 0.08f, 0.09f, 0.68f));
            image.sprite = artwork;
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetAnchors(image.rectTransform, new Vector2(0.070f, 0.075f), new Vector2(0.930f, 0.925f));

            if (artwork == null)
            {
                Text fallback = AddText(
                    artworkSlot,
                    "업적 이미지 대체",
                    model.Completed ? "◆" : "◇",
                    42,
                    TextAnchor.MiddleCenter,
                    new Color(0.20f, 0.86f, 0.80f, model.Completed ? 0.92f : 0.50f));
                ConfigureAchievementText(fallback, 24, 42, true);
                SetAnchors(fallback.rectTransform, new Vector2(0.070f, 0.075f), new Vector2(0.930f, 0.925f));
            }

            RectTransform infoPanel = AddPanel(
                card,
                "업적 정보 패널",
                Color.white,
                statusInnerPanelFrameSprite != null
                    ? statusInnerPanelFrameSprite
                    : panelSprite);
            infoPanel.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                infoPanel,
                new Vector2(0.345f, 0.120f),
                new Vector2(0.960f, 0.880f));

            Text title = AddText(
                infoPanel,
                "업적 제목",
                model.DisplayName,
                22,
                TextAnchor.MiddleLeft,
                model.Completed
                    ? new Color(0.73f, 1f, 0.95f, 1f)
                    : new Color(0.79f, 0.76f, 0.70f, 1f));
            ConfigureAchievementText(title, 14, 22, true);
            SetAnchors(title.rectTransform, new Vector2(0.075f, 0.710f), new Vector2(0.925f, 0.920f));

            Text description = AddText(
                infoPanel,
                "업적 설명",
                model.Completed ? model.EarnedDescription : model.LockedDescription,
                16,
                TextAnchor.MiddleLeft,
                new Color(0.94f, 0.88f, 0.79f, 1f));
            ConfigureAchievementText(description, 11, 16, false);
            description.lineSpacing = 0.94f;
            SetAnchors(description.rectTransform, new Vector2(0.075f, 0.350f), new Vector2(0.925f, 0.680f));

            Text status = AddText(
                infoPanel,
                "업적 상태",
                LF(
                    "achievement.status",
                    L(model.Completed
                        ? "achievement.status.complete"
                        : "achievement.status.incomplete"),
                    model.Points),
                14,
                TextAnchor.MiddleCenter,
                model.Completed
                    ? new Color(0.38f, 1f, 0.88f, 1f)
                    : new Color(0.66f, 0.62f, 0.58f, 1f));
            ConfigureAchievementText(status, 10, 14, true);
            SetAnchors(status.rectTransform, new Vector2(0.075f, 0.120f), new Vector2(0.600f, 0.310f));

            Text progress = AddText(
                infoPanel,
                "업적 진행",
                LF(
                    "achievement.progress",
                    model.ProgressCurrent,
                    model.ProgressTarget),
                15,
                TextAnchor.MiddleCenter,
                new Color(0.70f, 0.96f, 0.90f, 1f));
            ConfigureAchievementText(progress, 11, 15, true);
            SetAnchors(progress.rectTransform, new Vector2(0.620f, 0.120f), new Vector2(0.925f, 0.310f));
        }

        private void ShowAchievementPage(int requestedPage)
        {
            if (achievementCardsRoot == null || achievementCardModels == null)
            {
                return;
            }

            int pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    achievementCardModels.Count
                    / (float)AchievementCardsPerPage));
            achievementPageIndex = Mathf.Clamp(
                requestedPage,
                0,
                pageCount - 1);

            for (int index = achievementCardsRoot.childCount - 1;
                index >= 0;
                index -= 1)
            {
                GameObject child = achievementCardsRoot.GetChild(index).gameObject;
                child.SetActive(false);
                DestroyUiObject(child);
            }

            int startIndex = achievementPageIndex * AchievementCardsPerPage;
            int endIndex = Mathf.Min(
                startIndex + AchievementCardsPerPage,
                achievementCardModels.Count);
            for (int modelIndex = startIndex;
                modelIndex < endIndex;
                modelIndex += 1)
            {
                AddAchievementCard(
                    achievementCardsRoot,
                    achievementCardModels[modelIndex],
                    modelIndex,
                    modelIndex - startIndex);
            }

            int completedCount = achievementCardModels.Count(model => model.Completed);
            if (achievementPageText != null)
            {
                achievementPageText.text = LF(
                    "achievement.page",
                    achievementPageIndex + 1,
                    pageCount);
            }

            if (achievementCompletionText != null)
            {
                achievementCompletionText.text = LF(
                    "achievement.summary",
                    completedCount,
                    achievementCardModels.Count);
            }

            if (achievementPreviousButton != null)
            {
                achievementPreviousButton.interactable = achievementPageIndex > 0;
            }

            if (achievementNextButton != null)
            {
                achievementNextButton.interactable = achievementPageIndex < pageCount - 1;
            }
        }

        private (int Current, int Target) GetAchievementProgress(
            AchievementDefinition definition,
            bool completed)
        {
            if (definition == null)
            {
                return (completed ? 1 : 0, 1);
            }

            if (definition.StorageSuffix ==
                AchievementProgress.AbyssCollector.StorageSuffix)
            {
                return (GetAbyssCollectorProgress(), 30);
            }

            if (definition.StorageSuffix.StartsWith(
                    "build.",
                    StringComparison.Ordinal)
                && IsAchievementRunActive())
            {
                BuildRecipe recipe = GetCurrentBuildRecipe();
                AchievementDefinition currentDefinition =
                    AchievementProgress.GetDefinitionForBuild(recipe.Id);
                if (currentDefinition != null
                    && currentDefinition.StorageSuffix == definition.StorageSuffix)
                {
                    HashSet<string> requiredIds = new(
                        recipe.RequiredCardIds,
                        StringComparer.Ordinal);
                    int ownedRequired = deck
                        .Where(card => card != null)
                        .Select(card => card.CardId)
                        .Where(requiredIds.Contains)
                        .Distinct(StringComparer.Ordinal)
                        .Count();
                    return (ownedRequired, requiredIds.Count);
                }
            }

            return (completed ? 1 : 0, 1);
        }

        private int GetAbyssCollectorProgress()
        {
            HashSet<string> catalogIds = new(
                GetRunItemDefinitions()
                    .Select(item => item.Id)
                    .Where(itemId => !string.IsNullOrWhiteSpace(itemId)),
                StringComparer.Ordinal);
            if (catalogIds.Count == 0)
            {
                return 0;
            }

            int bestCount = 0;
            CharacterClass[] classes =
            {
                CharacterClass.Gambler,
                CharacterClass.Oracle,
                CharacterClass.Exile
            };
            foreach (CharacterClass characterClass in classes)
            {
                HashSet<string> characterItems = ReadSavedItemIds(
                    GetDiscoveredItemKey(characterClass));
                characterItems.UnionWith(ReadSavedItemIds(
                    GetEquippedItemKey(characterClass)));
                if (IsAchievementRunActive() && selectedClass == characterClass)
                {
                    characterItems.UnionWith(discoveredRunItemIds);
                    characterItems.UnionWith(equippedRunItemIds);
                }

                int validCount = characterItems.Count(catalogIds.Contains);
                bestCount = Mathf.Max(bestCount, validCount);
            }

            return Mathf.Clamp(bestCount, 0, 30);
        }

        private bool IsAchievementRunActive()
        {
            return phase == GamePhase.DoorSelection
                || phase == GamePhase.Combat
                || phase == GamePhase.Reward
                || phase == GamePhase.Shop
                || phase == GamePhase.Event
                || phase == GamePhase.Rest
                || phase == GamePhase.Treasure
                || phase == GamePhase.Curse;
        }

        private static void ConfigureAchievementText(
            Text text,
            int minimumSize,
            int maximumSize,
            bool bold)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignByGeometry = false;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        }

        private void HideAchievements()
        {
            if (achievementOverlay != null)
            {
                achievementOverlay.gameObject.SetActive(false);
                DestroyUiObject(achievementOverlay.gameObject);
            }

            achievementOverlay = null;
            achievementCardsRoot = null;
            achievementPageText = null;
            achievementCompletionText = null;
            achievementPreviousButton = null;
            achievementNextButton = null;
            achievementCardModels = null;
            achievementPageIndex = 0;
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(true);
            }
        }

        private void TryCompleteAbyssCollectorForSelectedClass()
        {
            IEnumerable<string> catalogIds = GetRunItemDefinitions().Select(item => item.Id);
            if (AchievementProgress.IsCollectionComplete(discoveredRunItemIds, catalogIds))
            {
                CompleteAchievementAndTrack(AchievementProgress.AbyssCollector);
            }
        }

        private void TryCompleteAbyssCollectorFromSavedCharacters()
        {
            string[] catalogIds = GetRunItemDefinitions()
                .Select(item => item.Id)
                .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (catalogIds.Length == 0)
            {
                return;
            }

            CharacterClass[] classes =
            {
                CharacterClass.Gambler,
                CharacterClass.Oracle,
                CharacterClass.Exile
            };
            foreach (CharacterClass characterClass in classes)
            {
                HashSet<string> characterItems = ReadSavedItemIds(
                    GetDiscoveredItemKey(characterClass));
                characterItems.UnionWith(ReadSavedItemIds(GetEquippedItemKey(characterClass)));
                if (!AchievementProgress.IsCollectionComplete(characterItems, catalogIds))
                {
                    continue;
                }

                CompleteAchievementAndTrack(AchievementProgress.AbyssCollector);
                return;
            }
        }

        private static HashSet<string> ReadSavedItemIds(string key)
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            try
            {
                EquippedItemSaveData saveData = JsonUtility.FromJson<EquippedItemSaveData>(json);
                return saveData?.itemIds == null
                    ? new HashSet<string>(StringComparer.Ordinal)
                    : new HashSet<string>(
                        saveData.itemIds.Where(itemId => !string.IsNullOrWhiteSpace(itemId)),
                        StringComparer.Ordinal);
            }
            catch (ArgumentException)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private void TryCompleteBuildAchievement()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (!AchievementProgress.IsBuildComplete(
                    recipe.Id,
                    deck.Where(card => card != null).Select(card => card.CardId)))
            {
                return;
            }

            AchievementDefinition definition =
                AchievementProgress.GetDefinitionForBuild(recipe.Id);
            if (definition != null)
            {
                CompleteAchievementAndTrack(definition);
            }
        }

        private void CompleteAchievementAndTrack(AchievementDefinition definition)
        {
            if (definition != null
                && AchievementProgress.Complete(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    definition))
            {
                newlyCompletedAchievementNames.Add(definition.DisplayName);
            }
        }
    }
}
