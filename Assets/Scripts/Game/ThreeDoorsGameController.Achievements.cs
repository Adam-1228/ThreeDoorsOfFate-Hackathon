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
        private const int AchievementSlotsPerPage = 10;
        private RectTransform achievementOverlay;
        private RectTransform achievementCardsRoot;
        private RectTransform achievementDetailRoot;
        private Text achievementPageText;
        private Text achievementCompletionText;
        private Button achievementPreviousButton;
        private Button achievementNextButton;
        private IReadOnlyList<AchievementCardModel> achievementCardModels;
        private int achievementPageIndex;
        private int selectedAchievementIndex = -1;
        private readonly List<string> newlyCompletedAchievementNames = new();
        private int lastExplicitRerollLuck;
        private int sameExplicitRerollStreak;

        private sealed class AchievementCardModel
        {
            public AchievementCardModel(
                string displayName,
                string lockedDescription,
                string earnedDescription,
                string resourcePath,
                int points,
                bool completed,
                Sprite fallbackSprite = null)
            {
                DisplayName = displayName;
                LockedDescription = lockedDescription;
                EarnedDescription = earnedDescription;
                ResourcePath = resourcePath;
                Points = points;
                Completed = completed;
                FallbackSprite = fallbackSprite;
            }

            public string DisplayName { get; }
            public string LockedDescription { get; }
            public string EarnedDescription { get; }
            public string ResourcePath { get; }
            public int Points { get; }
            public bool Completed { get; }
            public Sprite FallbackSprite { get; }
        }

        private void ShowAchievements()
        {
            HideAchievements();
            TryCompletePersistentAchievements();
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

            Text heading = AddLocalizedText(
                modal,
                "업적 모달 제목",
                "achievement.heading",
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

            Button closeButton = AddLocalizedSettingsMenuButton(
                modal,
                "업적 닫기",
                "common.close",
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
                new Vector2(0.035f, 0.445f),
                new Vector2(0.965f, 0.895f));

            achievementDetailRoot = AddPanel(
                modal,
                "업적 상세 영역",
                new Color(0f, 0f, 0f, 0f));
            Image detailRootImage = achievementDetailRoot.GetComponent<Image>();
            if (detailRootImage != null)
            {
                detailRootImage.raycastTarget = false;
            }
            SetAnchors(
                achievementDetailRoot,
                new Vector2(0.035f, 0.145f),
                new Vector2(0.965f, 0.425f));

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
            selectedAchievementIndex = -1;
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
                    hardBossDoorSprite != null ? hardBossDoorSprite : bossSprite),
                new AchievementCardModel(
                    "도박사의 진엔딩",
                    "도박사의 숨겨진 결말을 찾으세요.",
                    "도박사의 진엔딩을 발견했습니다.",
                    string.Empty,
                    100,
                    gamblerEnding,
                    gamblerSelectSprite),
                new AchievementCardModel(
                    "예언가의 진엔딩",
                    "예언가의 숨겨진 결말을 찾으세요.",
                    "예언가의 진엔딩을 발견했습니다.",
                    string.Empty,
                    100,
                    oracleEnding,
                    oracleSelectSprite),
                new AchievementCardModel(
                    "추방자의 진엔딩",
                    "추방자의 숨겨진 결말을 찾으세요.",
                    "추방자의 진엔딩을 발견했습니다.",
                    string.Empty,
                    100,
                    exileEnding,
                    exileSelectSprite)
            };

            foreach (AchievementDefinition definition in AchievementProgress.NewDefinitions)
            {
                bool completed = AchievementProgress.IsCompleted(
                    PlayerPrefsProgressStore.ProductionPrefix,
                    definition);
                models.Add(new AchievementCardModel(
                    definition.DisplayName,
                    definition.LockedDescription,
                    definition.EarnedDescription,
                    definition.ImageResourcePath,
                    definition.Points,
                    completed,
                    mainTitleLogoSprite));
            }

            return models;
        }

        private void AddAchievementSlot(
            RectTransform cardsRoot,
            AchievementCardModel model,
            int absoluteIndex,
            int pageSlot)
        {
            const int columns = 5;
            const int rows = 2;
            const float left = 0.010f;
            const float right = 0.990f;
            const float columnGap = 0.012f;
            const float rowBottom = 0.020f;
            const float rowTop = 0.980f;
            const float rowGap = 0.040f;
            float slotWidth =
                (right - left - columnGap * (columns - 1)) / columns;
            float slotHeight =
                (rowTop - rowBottom - rowGap * (rows - 1)) / rows;
            int column = pageSlot % columns;
            int row = pageSlot / columns;
            float minX = left + column * (slotWidth + columnGap);
            float maxY = rowTop - row * (slotHeight + rowGap);

            Sprite slotFrame = statusSectionMediumFrameSprite != null
                ? statusSectionMediumFrameSprite
                : GetRunStatusSlotFrameSprite();
            RectTransform slot = AddPanel(
                cardsRoot,
                $"업적 슬롯 {absoluteIndex + 1}",
                Color.white,
                slotFrame);
            SetAnchors(
                slot,
                new Vector2(minX, maxY - slotHeight),
                new Vector2(minX + slotWidth, maxY));

            Image slotImage = slot.GetComponent<Image>();
            slotImage.raycastTarget = true;
            slotImage.type = GetImageType(slotFrame);
            Button button = AddSfxButton(slot.gameObject, GameSfxCue.None);
            button.targetGraphic = slotImage;
            button.colors = CreateButtonColors();
            button.interactable = model.Completed;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            if (!model.Completed)
            {
                Image blank = AddImage(
                    slot,
                    "업적 미발견 공란",
                    new Color(0.008f, 0.012f, 0.020f, 0.92f));
                blank.raycastTarget = false;
                SetAnchors(
                    blank.rectTransform,
                    new Vector2(0.135f, 0.205f),
                    new Vector2(0.865f, 0.830f));

                Text undiscovered = AddLocalizedText(
                    slot,
                    "업적 미발견",
                    "common.undiscovered",
                    16,
                    TextAnchor.MiddleCenter,
                    new Color(0.60f, 0.64f, 0.66f, 1f));
                ConfigureAchievementText(undiscovered, 11, 16, true);
                SetAnchors(
                    undiscovered.rectTransform,
                    new Vector2(0.145f, 0.300f),
                    new Vector2(0.855f, 0.735f));
                return;
            }

            Sprite artwork = string.IsNullOrWhiteSpace(model.ResourcePath)
                ? null
                : Resources.Load<Sprite>(model.ResourcePath);
            artwork ??= model.FallbackSprite;

            Image achievementImage = AddImage(
                slot,
                "업적 이미지",
                artwork != null
                    ? Color.white
                    : new Color(0.03f, 0.07f, 0.08f, 0.90f));
            achievementImage.sprite = artwork;
            achievementImage.preserveAspect = true;
            achievementImage.raycastTarget = false;
            SetAnchors(
                achievementImage.rectTransform,
                new Vector2(0.160f, 0.275f),
                new Vector2(0.840f, 0.875f));

            if (artwork == null)
            {
                Text fallback = AddText(
                    slot,
                    "업적 이미지 대체",
                    "◆",
                    32,
                    TextAnchor.MiddleCenter,
                    new Color(0.20f, 0.86f, 0.80f, 0.92f));
                ConfigureAchievementText(fallback, 20, 32, true);
                SetAnchors(
                    fallback.rectTransform,
                    new Vector2(0.160f, 0.275f),
                    new Vector2(0.840f, 0.875f));
            }

            Text title = AddText(
                slot,
                "업적 이름",
                model.DisplayName,
                15,
                TextAnchor.MiddleCenter,
                new Color(0.73f, 1f, 0.95f, 1f));
            BindLocalizedSourceText(title, model.DisplayName);
            ConfigureAchievementText(title, 10, 15, true);
            SetAnchors(
                title.rectTransform,
                new Vector2(0.105f, 0.070f),
                new Vector2(0.895f, 0.255f));

            button.onClick.AddListener(() => SelectAchievement(absoluteIndex));
            if (selectedAchievementIndex == absoluteIndex)
            {
                AddAchievementSelection(slot);
            }
        }

        private void AddAchievementSelection(RectTransform slot)
        {
            Image selection = AddImage(
                slot,
                "업적 선택 표시",
                selectionFrameSprite != null
                    ? Color.white
                    : new Color(0.14f, 0.94f, 0.86f, 0.10f));
            selection.sprite = selectionFrameSprite;
            selection.type = GetImageType(selectionFrameSprite);
            selection.raycastTarget = false;
            SetAnchors(
                selection.rectTransform,
                new Vector2(0.010f, 0.010f),
                new Vector2(0.990f, 0.990f));
            selection.rectTransform.SetAsLastSibling();
        }

        private void SelectAchievement(int absoluteIndex)
        {
            if (achievementCardModels == null
                || absoluteIndex < 0
                || absoluteIndex >= achievementCardModels.Count
                || !achievementCardModels[absoluteIndex].Completed)
            {
                return;
            }

            if (selectedAchievementIndex != absoluteIndex)
            {
                selectedAchievementIndex = absoluteIndex;
                RefreshAchievementSelectionVisuals();
            }

            RefreshAchievementDetail();
        }

        private void RefreshAchievementSelectionVisuals()
        {
            if (achievementCardsRoot == null)
            {
                return;
            }

            int firstIndex = achievementPageIndex * AchievementSlotsPerPage;
            for (int childIndex = 0;
                childIndex < achievementCardsRoot.childCount;
                childIndex += 1)
            {
                RectTransform slot =
                    achievementCardsRoot.GetChild(childIndex) as RectTransform;
                if (slot == null
                    || !slot.name.StartsWith("업적 슬롯 ", StringComparison.Ordinal))
                {
                    continue;
                }

                Transform previousSelection = slot.Find("업적 선택 표시");
                if (previousSelection != null)
                {
                    previousSelection.gameObject.SetActive(false);
                    DestroyUiObject(previousSelection.gameObject);
                }

                int absoluteIndex = firstIndex + childIndex;
                if (absoluteIndex == selectedAchievementIndex)
                {
                    AddAchievementSelection(slot);
                }
            }
        }

        private void RefreshAchievementDetail()
        {
            if (achievementDetailRoot == null)
            {
                return;
            }

            for (int childIndex = achievementDetailRoot.childCount - 1;
                childIndex >= 0;
                childIndex -= 1)
            {
                GameObject child = achievementDetailRoot.GetChild(childIndex).gameObject;
                child.SetActive(false);
                DestroyUiObject(child);
            }

            RectTransform detail = AddPanel(
                achievementDetailRoot,
                "업적 상세 패널",
                Color.white,
                statusInnerPanelFrameSprite != null
                    ? statusInnerPanelFrameSprite
                    : panelSprite);
            detail.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                detail,
                new Vector2(0.005f, 0.010f),
                new Vector2(0.995f, 0.990f));

            if (achievementCardModels == null
                || selectedAchievementIndex < 0
                || selectedAchievementIndex >= achievementCardModels.Count
                || !achievementCardModels[selectedAchievementIndex].Completed)
            {
                Text empty = AddLocalizedText(
                    detail,
                    "업적 상세 미발견",
                    "common.undiscovered",
                    19,
                    TextAnchor.MiddleCenter,
                    new Color(0.62f, 0.66f, 0.68f, 1f));
                ConfigureAchievementText(empty, 13, 19, true);
                SetAnchors(
                    empty.rectTransform,
                    new Vector2(0.080f, 0.180f),
                    new Vector2(0.920f, 0.820f));
                return;
            }

            AchievementCardModel model =
                achievementCardModels[selectedAchievementIndex];
            Sprite artwork = string.IsNullOrWhiteSpace(model.ResourcePath)
                ? null
                : Resources.Load<Sprite>(model.ResourcePath);
            artwork ??= model.FallbackSprite;

            Image image = AddImage(
                detail,
                "업적 상세 이미지",
                artwork != null
                    ? Color.white
                    : new Color(0.03f, 0.07f, 0.08f, 0.90f));
            image.sprite = artwork;
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetAnchors(
                image.rectTransform,
                new Vector2(0.040f, 0.135f),
                new Vector2(0.215f, 0.865f));

            Text title = AddText(
                detail,
                "업적 상세 제목",
                model.DisplayName,
                22,
                TextAnchor.MiddleLeft,
                new Color(0.73f, 1f, 0.95f, 1f));
            BindLocalizedSourceText(title, model.DisplayName);
            ConfigureAchievementText(title, 14, 22, true);
            SetAnchors(
                title.rectTransform,
                new Vector2(0.255f, 0.635f),
                new Vector2(0.945f, 0.880f));

            Text description = AddText(
                detail,
                "업적 상세 설명",
                model.EarnedDescription,
                17,
                TextAnchor.MiddleLeft,
                new Color(0.94f, 0.88f, 0.79f, 1f));
            BindLocalizedSourceText(description, model.EarnedDescription);
            ConfigureAchievementText(description, 12, 17, false);
            description.lineSpacing = 0.94f;
            SetAnchors(
                description.rectTransform,
                new Vector2(0.255f, 0.280f),
                new Vector2(0.945f, 0.625f));

            Text status = AddText(
                detail,
                "업적 상세 상태",
                LF(
                    "achievement.status",
                    L("achievement.status.complete"),
                    model.Points),
                15,
                TextAnchor.MiddleLeft,
                new Color(0.38f, 1f, 0.88f, 1f));
            BindLocalizedText(
                status,
                "achievement.status",
                L("achievement.status.complete"),
                model.Points);
            ConfigureAchievementText(status, 11, 15, true);
            SetAnchors(
                status.rectTransform,
                new Vector2(0.255f, 0.100f),
                new Vector2(0.945f, 0.275f));
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
                    / (float)AchievementSlotsPerPage));
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

            int startIndex = achievementPageIndex * AchievementSlotsPerPage;
            int endIndex = Mathf.Min(
                startIndex + AchievementSlotsPerPage,
                achievementCardModels.Count);
            if (selectedAchievementIndex < startIndex
                || selectedAchievementIndex >= endIndex
                || !achievementCardModels[selectedAchievementIndex].Completed)
            {
                selectedAchievementIndex = -1;
                for (int modelIndex = startIndex;
                    modelIndex < endIndex;
                    modelIndex += 1)
                {
                    if (achievementCardModels[modelIndex].Completed)
                    {
                        selectedAchievementIndex = modelIndex;
                        break;
                    }
                }
            }

            for (int modelIndex = startIndex;
                modelIndex < endIndex;
                modelIndex += 1)
            {
                AddAchievementSlot(
                    achievementCardsRoot,
                    achievementCardModels[modelIndex],
                    modelIndex,
                    modelIndex - startIndex);
            }

            RefreshAchievementDetail();

            int completedCount = achievementCardModels.Count(model => model.Completed);
            if (achievementPageText != null)
            {
                achievementPageText.text = LF(
                    "achievement.page",
                    achievementPageIndex + 1,
                    pageCount);
                BindLocalizedText(
                    achievementPageText,
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
                BindLocalizedText(
                    achievementCompletionText,
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
            achievementDetailRoot = null;
            achievementPageText = null;
            achievementCompletionText = null;
            achievementPreviousButton = null;
            achievementNextButton = null;
            achievementCardModels = null;
            achievementPageIndex = 0;
            selectedAchievementIndex = -1;
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

        private void TryCompleteCombatAwakeningAchievements()
        {
            if (gamblerCardReadingAwakened)
            {
                CompleteAchievementAndTrack(AchievementProgress.GamblerCardReading);
            }

            if (oraclePrecisePredictionAwakened)
            {
                CompleteAchievementAndTrack(AchievementProgress.OraclePrecisePrediction);
            }

            if (exileCurseEaterAwakened)
            {
                CompleteAchievementAndTrack(AchievementProgress.ExileCurseEater);
            }
        }

        private void TryCompleteCombatCardAchievements(int actualCardDamage)
        {
            if (phase != GamePhase.Combat)
            {
                return;
            }

            if (AchievementProgress.IsFateCleaverDamage(actualCardDamage))
            {
                CompleteAchievementAndTrack(AchievementProgress.FateCleaver);
            }

            if (AchievementProgress.IsFiveCardTurn(cardsPlayedThisTurn))
            {
                CompleteAchievementAndTrack(AchievementProgress.FiveCardsTurn);
            }
        }

        private void TryCompleteIronWallAchievement()
        {
            if (phase == GamePhase.Combat
                && AchievementProgress.IsIronWallBlock(playerBlock))
            {
                CompleteAchievementAndTrack(AchievementProgress.IronWall);
            }
        }

        private void ResetExplicitRerollProgress()
        {
            lastExplicitRerollLuck = 0;
            sameExplicitRerollStreak = 0;
        }

        private void RecordExplicitRerollResult(int result)
        {
            if (phase != GamePhase.Combat || result <= 0)
            {
                return;
            }

            sameExplicitRerollStreak = AchievementProgress.UpdateSameRerollStreak(
                lastExplicitRerollLuck,
                sameExplicitRerollStreak,
                result);
            RecordRunRerollStreak(sameExplicitRerollStreak);
            lastExplicitRerollLuck = result;
            if (sameExplicitRerollStreak >= 3)
            {
                CompleteAchievementAndTrack(AchievementProgress.SameRerollThree);
            }
        }

        private void TryCompleteCliffsideVictoryAchievement()
        {
            if (AchievementProgress.IsCliffsideVictory(playerHealth, playerMaxHealth))
            {
                CompleteAchievementAndTrack(AchievementProgress.CliffsideVictory);
            }
        }

        private void TryCompleteTripleContractAchievement()
        {
            HashSet<RunItemType> equippedTypes = equippedRunItemIds
                .Select(GetRunItemDefinition)
                .Where(item => item != null)
                .Select(item => item.Type)
                .ToHashSet();
            if (AchievementProgress.HasTripleContract(
                    equippedTypes.Contains(RunItemType.Relic),
                    equippedTypes.Contains(RunItemType.Blessing),
                    equippedTypes.Contains(RunItemType.Curse)))
            {
                CompleteAchievementAndTrack(AchievementProgress.TripleContract);
            }
        }

        private void TryCompleteMasterpieceAchievement()
        {
            BuildRecipe recipe = GetCurrentBuildRecipe();
            if (AchievementProgress.IsMasterpieceLevel(
                    GetBuildUpgradeLevel(recipe.Id)))
            {
                CompleteAchievementAndTrack(AchievementProgress.BuildMasterpiece);
            }
        }

        private void TryCompleteTwentiethDoorAchievement()
        {
            CharacterClass[] classes =
            {
                CharacterClass.Gambler,
                CharacterClass.Oracle,
                CharacterClass.Exile
            };
            RunDifficulty[] difficulties =
            {
                RunDifficulty.Easy,
                RunDifficulty.Normal,
                RunDifficulty.Hard
            };
            if (classes.Any(characterClass => difficulties.Any(difficulty =>
                    AchievementProgress.IsTwentiethDoorRecord(
                        GetEndlessRecord(characterClass, difficulty)))))
            {
                CompleteAchievementAndTrack(AchievementProgress.TwentiethDoor);
            }
        }

        private void TryCompleteThreeSurvivorsAchievement()
        {
            CharacterClass[] classes =
            {
                CharacterClass.Gambler,
                CharacterClass.Oracle,
                CharacterClass.Exile
            };
            int survivorCount = classes.Count(IsSurvivorTitleUnlocked);
            if (AchievementProgress.HasThreeSurvivors(survivorCount))
            {
                CompleteAchievementAndTrack(AchievementProgress.ThreeSurvivors);
            }
        }

        private void TryCompletePersistentAchievements()
        {
            AchievementProgress.BackfillPersistentPlayerPrefs(
                PlayerPrefsProgressStore.ProductionPrefix);
            TryCompleteAbyssCollectorFromSavedCharacters();
            TryCompleteBuildAchievement();
            TryCompleteTripleContractAchievement();
            TryCompleteMasterpieceAchievement();
            TryCompleteTwentiethDoorAchievement();
            TryCompleteThreeSurvivorsAchievement();
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
