using System.Collections.Generic;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private static readonly string[] HowToPlayTitleKeys =
        {
            "tutorial.page.class.title",
            "tutorial.page.doors.title",
            "tutorial.page.combat.title",
            "tutorial.page.cards.title",
            "tutorial.page.growth.title"
        };

        private static readonly string[] HowToPlayCaptionKeys =
        {
            "tutorial.page.class.caption",
            "tutorial.page.doors.caption",
            "tutorial.page.combat.caption",
            "tutorial.page.cards.caption",
            "tutorial.page.growth.caption"
        };

        private static readonly string[][] HowToPlayEnglishStepKeys =
        {
            new[]
            {
                "tutorial.page.class.step1",
                "tutorial.page.class.step2",
                "tutorial.page.class.step3"
            },
            new[]
            {
                "tutorial.page.doors.step1",
                "tutorial.page.doors.step2",
                "tutorial.page.doors.step3"
            },
            new[]
            {
                "tutorial.page.combat.step1",
                "tutorial.page.combat.step2",
                "tutorial.page.combat.step3"
            },
            new[]
            {
                "tutorial.page.cards.step1",
                "tutorial.page.cards.step2",
                "tutorial.page.cards.step3"
            },
            new[]
            {
                "tutorial.page.growth.step1",
                "tutorial.page.growth.step2",
                "tutorial.page.growth.step3"
            }
        };

        private static readonly string[] HowToPlaySpriteResourcePaths =
        {
            "Tutorial/how_to_play_01_class",
            "Tutorial/how_to_play_02_doors",
            "Tutorial/how_to_play_03_combat",
            "Tutorial/how_to_play_04_card_use",
            "Tutorial/how_to_play_05_growth"
        };

        [System.NonSerialized] private List<Sprite> howToPlaySprites;
        private RectTransform howToPlayOverlay;
        private Image howToPlayImage;
        private RectTransform howToPlayEnglishVisualRoot;
        private Text howToPlayTitleText;
        private Text howToPlayCaptionText;
        private Text howToPlayProgressText;
        private Text howToPlayMissingImageText;
        private Button howToPlayPreviousButton;
        private Button howToPlayNextButton;
        private Button howToPlayCloseButton;
        private int howToPlayPageIndex;
        private readonly List<Button> handFlowPracticeCardButtons = new();
        private Text handFlowPracticeStatusText;
        private Text handFlowPracticeCountsText;
        private Button handFlowPracticeEndTurnButton;
        private Button handFlowPracticeUseButton;
        private int handFlowPracticeHandCount;
        private int handFlowPracticeDrawCount;
        private int handFlowPracticeDiscardCount;
        private int handFlowPracticeSelectedIndex = -1;
        private int handFlowPracticeStep;

        private void ShowHowToPlay()
        {
            HideHowToPlay();
            AppleGameServicesRuntime.SetAccessPointVisible(false);
            EnsureHowToPlaySpritesLoaded();
            howToPlayPageIndex = 0;
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(false);
            }

            Image overlayImage = AddImage(
                root,
                "플레이 방법 오버레이",
                new Color(0f, 0f, 0f, 0.72f));
            overlayImage.raycastTarget = true;
            howToPlayOverlay = overlayImage.rectTransform;
            Stretch(howToPlayOverlay);
            AddClickBlocker(overlayImage);

            Sprite modalSprite = mainOptionsPanelSprite != null
                ? mainOptionsPanelSprite
                : statusPanelFrameSprite != null
                    ? statusPanelFrameSprite
                    : panelSprite;
            RectTransform modal = AddPanel(
                howToPlayOverlay,
                "플레이 방법 모달",
                Color.white,
                modalSprite);
            SetAnchors(
                modal,
                new Vector2(0.045f, 0.045f),
                new Vector2(0.955f, 0.955f));
            AddClickBlocker(modal.GetComponent<Image>());

            howToPlayTitleText = AddText(
                modal,
                "플레이 방법 제목",
                string.Empty,
                32,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 1f, 0.96f, 1f));
            howToPlayTitleText.fontStyle = FontStyle.Bold;
            howToPlayTitleText.resizeTextForBestFit = true;
            howToPlayTitleText.resizeTextMinSize = 20;
            howToPlayTitleText.resizeTextMaxSize = 32;
            AddTextGlow(
                howToPlayTitleText,
                new Color(0f, 0f, 0f, 0.92f),
                new Color(0.08f, 0.62f, 0.58f, 0.42f),
                new Vector2(1.2f, -1.2f));
            SetAnchors(
                howToPlayTitleText.rectTransform,
                new Vector2(0.12f, 0.865f),
                new Vector2(0.88f, 0.955f));

            howToPlayCloseButton = AddSettingsMenuButton(
                modal,
                "플레이 방법 닫기",
                "닫기",
                18,
                GameSfxCue.UiAccept);
            SetAnchors(
                howToPlayCloseButton.GetComponent<RectTransform>(),
                new Vector2(0.875f, 0.875f),
                new Vector2(0.955f, 0.955f));
            howToPlayCloseButton.onClick.AddListener(HideHowToPlay);

            howToPlayImage = AddImage(modal, "플레이 방법 이미지", Color.white);
            howToPlayImage.preserveAspect = true;
            howToPlayImage.raycastTarget = false;
            SetAnchors(
                howToPlayImage.rectTransform,
                new Vector2(0.075f, 0.265f),
                new Vector2(0.925f, 0.855f));

            howToPlayEnglishVisualRoot = AddPanel(
                modal,
                "영문 플레이 방법 시각 안내",
                new Color(0.025f, 0.055f, 0.060f, 0.96f),
                statusSectionWideFrameSprite != null
                    ? statusSectionWideFrameSprite
                    : panelSprite);
            SetAnchors(
                howToPlayEnglishVisualRoot,
                new Vector2(0.075f, 0.265f),
                new Vector2(0.925f, 0.855f));
            Image englishVisualFrame =
                howToPlayEnglishVisualRoot.GetComponent<Image>();
            if (englishVisualFrame != null)
            {
                englishVisualFrame.raycastTarget = false;
            }

            howToPlayMissingImageText = AddText(
                modal,
                "플레이 방법 이미지 누락",
                "설명 이미지를 불러오지 못했습니다.",
                26,
                TextAnchor.MiddleCenter,
                new Color(0.88f, 0.82f, 0.72f, 1f));
            howToPlayMissingImageText.fontStyle = FontStyle.Bold;
            howToPlayMissingImageText.resizeTextForBestFit = true;
            howToPlayMissingImageText.resizeTextMinSize = 16;
            howToPlayMissingImageText.resizeTextMaxSize = 26;
            SetAnchors(
                howToPlayMissingImageText.rectTransform,
                new Vector2(0.075f, 0.265f),
                new Vector2(0.925f, 0.855f));

            howToPlayCaptionText = AddText(
                modal,
                "플레이 방법 설명",
                string.Empty,
                23,
                TextAnchor.MiddleCenter,
                new Color(0.96f, 0.90f, 0.80f, 1f));
            howToPlayCaptionText.resizeTextForBestFit = true;
            howToPlayCaptionText.resizeTextMinSize = 15;
            howToPlayCaptionText.resizeTextMaxSize = 23;
            howToPlayCaptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            howToPlayCaptionText.verticalOverflow = VerticalWrapMode.Truncate;
            howToPlayCaptionText.lineSpacing = 0.95f;
            SetAnchors(
                howToPlayCaptionText.rectTransform,
                new Vector2(0.105f, 0.145f),
                new Vector2(0.895f, 0.255f));

            howToPlayPreviousButton = AddSettingsMenuButton(
                modal,
                "플레이 방법 이전",
                "이전",
                24,
                GameSfxCue.UiAccept);
            SetAnchors(
                howToPlayPreviousButton.GetComponent<RectTransform>(),
                new Vector2(0.070f, 0.045f),
                new Vector2(0.270f, 0.125f));
            howToPlayPreviousButton.onClick.AddListener(
                () => ShowHowToPlayPage(howToPlayPageIndex - 1));

            howToPlayProgressText = AddText(
                modal,
                "플레이 방법 페이지",
                string.Empty,
                22,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 1f, 0.94f, 1f));
            howToPlayProgressText.fontStyle = FontStyle.Bold;
            SetAnchors(
                howToPlayProgressText.rectTransform,
                new Vector2(0.405f, 0.050f),
                new Vector2(0.595f, 0.120f));

            howToPlayNextButton = AddSettingsMenuButton(
                modal,
                "플레이 방법 다음",
                L("common.next"),
                24,
                GameSfxCue.UiAccept);
            SetAnchors(
                howToPlayNextButton.GetComponent<RectTransform>(),
                new Vector2(0.730f, 0.045f),
                new Vector2(0.930f, 0.125f));
            howToPlayNextButton.onClick.AddListener(AdvanceHowToPlay);

            howToPlayOverlay.SetAsLastSibling();
            ShowHowToPlayPage(0);
        }

        private void EnsureHowToPlaySpritesLoaded()
        {
            if (howToPlaySprites != null && howToPlaySprites.Count > 0)
            {
                return;
            }

            howToPlaySprites = new List<Sprite>(HowToPlaySpriteResourcePaths.Length);
            foreach (string resourcePath in HowToPlaySpriteResourcePaths)
            {
                howToPlaySprites.Add(Resources.Load<Sprite>(resourcePath));
            }
        }

        private void AddClickBlocker(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.raycastTarget = true;
            Button blocker = AddSfxButton(image.gameObject, GameSfxCue.None);
            blocker.targetGraphic = image;
            blocker.transition = Selectable.Transition.None;
            blocker.colors = CreateStaticButtonColors();
            Navigation navigation = blocker.navigation;
            navigation.mode = Navigation.Mode.None;
            blocker.navigation = navigation;
        }

        private void AdvanceHowToPlay()
        {
            if (howToPlayPageIndex >= HowToPlayTitleKeys.Length - 1)
            {
                HideHowToPlay();
                return;
            }

            ShowHowToPlayPage(howToPlayPageIndex + 1);
        }

        private void ShowHowToPlayPage(int index)
        {
            if (howToPlayOverlay == null
                || howToPlayImage == null
                || howToPlayEnglishVisualRoot == null
                || howToPlayTitleText == null
                || howToPlayCaptionText == null
                || howToPlayProgressText == null
                || howToPlayMissingImageText == null
                || howToPlayPreviousButton == null
                || howToPlayNextButton == null)
            {
                return;
            }

            howToPlayPageIndex = Mathf.Clamp(
                index,
                0,
                HowToPlayTitleKeys.Length - 1);
            Sprite sprite = howToPlaySprites != null
                && howToPlayPageIndex < howToPlaySprites.Count
                    ? howToPlaySprites[howToPlayPageIndex]
                    : null;

            BindLocalizedText(
                howToPlayTitleText,
                HowToPlayTitleKeys[howToPlayPageIndex]);
            BindLocalizedText(
                howToPlayCaptionText,
                HowToPlayCaptionKeys[howToPlayPageIndex]);
            howToPlayProgressText.text =
                $"{howToPlayPageIndex + 1} / {HowToPlayTitleKeys.Length}";
            howToPlayImage.sprite = sprite;
            bool showHandFlowPractice = howToPlayPageIndex == 3;
            bool showKoreanScreenshot =
                !GameLocalization.IsEnglish
                && !showHandFlowPractice
                && sprite != null;
            howToPlayImage.gameObject.SetActive(showKoreanScreenshot);
            howToPlayMissingImageText.gameObject.SetActive(
                !GameLocalization.IsEnglish
                && !showHandFlowPractice
                && sprite == null);
            howToPlayEnglishVisualRoot.gameObject.SetActive(
                GameLocalization.IsEnglish || showHandFlowPractice);
            if (showHandFlowPractice)
            {
                BuildHandFlowPractice();
            }
            else if (GameLocalization.IsEnglish)
            {
                BuildEnglishHowToPlayVisual(howToPlayPageIndex);
            }

            howToPlayPreviousButton.interactable = howToPlayPageIndex > 0;
            howToPlayNextButton.interactable = true;
            SetButtonLabel(
                howToPlayNextButton,
                howToPlayPageIndex == HowToPlayTitleKeys.Length - 1
                    ? L("common.complete")
                    : L("common.next"));
        }

        private void BuildEnglishHowToPlayVisual(int pageIndex)
        {
            if (howToPlayEnglishVisualRoot == null
                || pageIndex < 0
                || pageIndex >= HowToPlayEnglishStepKeys.Length)
            {
                return;
            }

            ClearHowToPlayVisualRoot();

            Sprite[] icons = GetEnglishHowToPlayIcons(pageIndex);
            string[] stepKeys = HowToPlayEnglishStepKeys[pageIndex];
            for (int stepIndex = 0; stepIndex < stepKeys.Length; stepIndex += 1)
            {
                AddEnglishHowToPlayStep(
                    howToPlayEnglishVisualRoot,
                    pageIndex,
                    stepIndex,
                    stepKeys[stepIndex],
                    stepIndex < icons.Length ? icons[stepIndex] : null);
            }
        }

        private void BuildHandFlowPractice()
        {
            if (howToPlayEnglishVisualRoot == null)
            {
                return;
            }

            ClearHowToPlayVisualRoot();
            ResetHandFlowPractice();

            RectTransform practice = AddPanel(
                howToPlayEnglishVisualRoot,
                "손패 순환 연습",
                new Color(0f, 0f, 0f, 0f));
            Stretch(practice);
            Image practiceImage = practice.GetComponent<Image>();
            if (practiceImage != null)
            {
                practiceImage.raycastTarget = false;
            }

            handFlowPracticeStatusText = AddText(
                practice,
                "손패 순환 연습 안내",
                L("tutorial.practice.step.endTurn"),
                21,
                TextAnchor.MiddleCenter,
                new Color(0.78f, 1f, 0.94f, 1f));
            handFlowPracticeStatusText.fontStyle = FontStyle.Bold;
            handFlowPracticeStatusText.resizeTextForBestFit = true;
            handFlowPracticeStatusText.resizeTextMinSize = 14;
            handFlowPracticeStatusText.resizeTextMaxSize = 21;
            SetAnchors(
                handFlowPracticeStatusText.rectTransform,
                new Vector2(0.055f, 0.835f),
                new Vector2(0.945f, 0.965f));

            for (int index = 0; index < 3; index += 1)
            {
                float minX = 0.105f + index * 0.295f;
                RectTransform cardRoot = AddPanel(
                    practice,
                    $"Practice Card {index + 1}",
                    Color.white,
                    cardBackSprite != null
                        ? cardBackSprite
                        : statusSectionMediumFrameSprite != null
                            ? statusSectionMediumFrameSprite
                            : panelSprite);
                SetAnchors(
                    cardRoot,
                    new Vector2(minX, 0.345f),
                    new Vector2(minX + 0.200f, 0.790f));
                Image cardImage = cardRoot.GetComponent<Image>();
                cardImage.preserveAspect = true;
                cardImage.raycastTarget = true;

                Button cardButton = AddSfxButton(
                    cardRoot.gameObject,
                    GameSfxCue.UiAccept);
                cardButton.targetGraphic = cardImage;
                cardButton.colors = CreateButtonColors();
                int capturedIndex = index;
                cardButton.onClick.AddListener(
                    () => SelectPracticeCard(capturedIndex));
                handFlowPracticeCardButtons.Add(cardButton);

                Text label = AddText(
                    cardRoot,
                    "연습 카드 라벨",
                    LF("tutorial.practice.card", index + 1),
                    17,
                    TextAnchor.MiddleCenter,
                    new Color(1f, 0.94f, 0.78f, 1f));
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 12;
                label.resizeTextMaxSize = 17;
                AddTextGlow(
                    label,
                    new Color(0f, 0f, 0f, 0.94f),
                    new Color(0.08f, 0.62f, 0.58f, 0.44f),
                    new Vector2(1.2f, -1.3f));
                SetAnchors(
                    label.rectTransform,
                    new Vector2(0.08f, 0.045f),
                    new Vector2(0.92f, 0.245f));
            }

            handFlowPracticeCountsText = AddText(
                practice,
                "손패 순환 연습 수량",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.90f, 0.80f, 1f));
            handFlowPracticeCountsText.fontStyle = FontStyle.Bold;
            handFlowPracticeCountsText.resizeTextForBestFit = true;
            handFlowPracticeCountsText.resizeTextMinSize = 13;
            handFlowPracticeCountsText.resizeTextMaxSize = 18;
            SetAnchors(
                handFlowPracticeCountsText.rectTransform,
                new Vector2(0.225f, 0.250f),
                new Vector2(0.775f, 0.335f));

            handFlowPracticeEndTurnButton = AddSettingsMenuButton(
                practice,
                "연습 턴 종료",
                L("tutorial.practice.endTurn"),
                20,
                GameSfxCue.UiAccept);
            SetAnchors(
                handFlowPracticeEndTurnButton.GetComponent<RectTransform>(),
                new Vector2(0.165f, 0.055f),
                new Vector2(0.435f, 0.205f));
            handFlowPracticeEndTurnButton.onClick.AddListener(
                HandlePracticeEndTurn);

            handFlowPracticeUseButton = AddSettingsMenuButton(
                practice,
                "연습 카드 사용",
                L("tutorial.practice.use"),
                20,
                GameSfxCue.UiAccept);
            SetAnchors(
                handFlowPracticeUseButton.GetComponent<RectTransform>(),
                new Vector2(0.565f, 0.055f),
                new Vector2(0.835f, 0.205f));
            handFlowPracticeUseButton.onClick.AddListener(
                UseSelectedPracticeCard);

            RefreshHandFlowPractice();
        }

        private void ResetHandFlowPractice()
        {
            handFlowPracticeHandCount = 3;
            handFlowPracticeDrawCount = 2;
            handFlowPracticeDiscardCount = 0;
            handFlowPracticeSelectedIndex = -1;
            handFlowPracticeStep = 0;
        }

        private void HandlePracticeEndTurn()
        {
            if (handFlowPracticeStep != 0)
            {
                return;
            }

            handFlowPracticeStep = 1;
            RefreshHandFlowPractice();
        }

        private void SelectPracticeCard(int index)
        {
            if (handFlowPracticeStep < 1
                || handFlowPracticeStep >= 3
                || index < 0
                || index >= handFlowPracticeHandCount)
            {
                return;
            }

            handFlowPracticeSelectedIndex = index;
            handFlowPracticeStep = 2;
            RefreshHandFlowPractice();
        }

        private void UseSelectedPracticeCard()
        {
            if (handFlowPracticeStep != 2
                || handFlowPracticeSelectedIndex < 0
                || handFlowPracticeSelectedIndex >= handFlowPracticeHandCount)
            {
                return;
            }

            handFlowPracticeHandCount = Mathf.Max(
                0,
                handFlowPracticeHandCount - 1);
            handFlowPracticeDiscardCount += 1;
            if (handFlowPracticeDrawCount > 0
                && handFlowPracticeHandCount < 3)
            {
                handFlowPracticeDrawCount -= 1;
                handFlowPracticeHandCount += 1;
            }

            handFlowPracticeSelectedIndex = -1;
            handFlowPracticeStep = 3;
            RefreshHandFlowPractice();
        }

        private void RefreshHandFlowPractice()
        {
            if (handFlowPracticeStatusText != null)
            {
                string statusKey = handFlowPracticeStep switch
                {
                    1 => "tutorial.practice.step.select",
                    2 => "tutorial.practice.step.use",
                    3 => "tutorial.practice.step.complete",
                    _ => "tutorial.practice.step.endTurn"
                };
                handFlowPracticeStatusText.text = L(statusKey);
                handFlowPracticeStatusText.color = handFlowPracticeStep == 3
                    ? new Color(0.45f, 1f, 0.70f, 1f)
                    : new Color(0.78f, 1f, 0.94f, 1f);
            }

            if (handFlowPracticeCountsText != null)
            {
                handFlowPracticeCountsText.text = LF(
                    "tutorial.practice.counts",
                    handFlowPracticeHandCount,
                    3,
                    handFlowPracticeDrawCount,
                    handFlowPracticeDiscardCount);
            }

            for (int index = 0;
                index < handFlowPracticeCardButtons.Count;
                index += 1)
            {
                Button card = handFlowPracticeCardButtons[index];
                if (card == null)
                {
                    continue;
                }

                card.interactable = handFlowPracticeStep >= 1
                    && handFlowPracticeStep < 3
                    && index < handFlowPracticeHandCount;
                bool selected = index == handFlowPracticeSelectedIndex;
                card.transform.localScale = selected
                    ? Vector3.one * 1.055f
                    : Vector3.one;
                if (card.targetGraphic is Image image)
                {
                    image.color = selected
                        ? new Color(0.62f, 1f, 0.88f, 1f)
                        : Color.white;
                }
            }

            if (handFlowPracticeEndTurnButton != null)
            {
                handFlowPracticeEndTurnButton.interactable =
                    handFlowPracticeStep == 0;
            }

            if (handFlowPracticeUseButton != null)
            {
                handFlowPracticeUseButton.interactable =
                    handFlowPracticeStep == 2
                    && handFlowPracticeSelectedIndex >= 0;
            }
        }

        private void ClearHowToPlayVisualRoot()
        {
            if (howToPlayEnglishVisualRoot != null)
            {
                for (int childIndex = howToPlayEnglishVisualRoot.childCount - 1;
                    childIndex >= 0;
                    childIndex -= 1)
                {
                    GameObject child =
                        howToPlayEnglishVisualRoot.GetChild(childIndex).gameObject;
                    child.SetActive(false);
                    DestroyUiObject(child);
                }
            }

            ClearHandFlowPracticeReferences();
        }

        private void ClearHandFlowPracticeReferences()
        {
            handFlowPracticeCardButtons.Clear();
            handFlowPracticeStatusText = null;
            handFlowPracticeCountsText = null;
            handFlowPracticeEndTurnButton = null;
            handFlowPracticeUseButton = null;
        }

        private Sprite[] GetEnglishHowToPlayIcons(int pageIndex)
        {
            switch (pageIndex)
            {
                case 0:
                    return new[]
                    {
                        gamblerSelectSprite,
                        oracleSelectSprite,
                        exileSelectSprite
                    };
                case 1:
                    return new[]
                    {
                        easyBossDoorSprite,
                        normalBossDoorSprite,
                        hardBossDoorSprite
                    };
                case 2:
                    return new[]
                    {
                        enemyStatusFrameSprite,
                        diceSprites != null && diceSprites.Count > 0
                            ? diceSprites[0]
                            : null,
                        classConfirmButtonSprite
                    };
                case 3:
                    return new[]
                    {
                        cardBackSprite,
                        cardBackSprite,
                        classConfirmButtonSprite
                    };
                default:
                    return new[]
                    {
                        GetRunItemSilhouetteIcon(RunItemType.Relic),
                        GetRunItemSilhouetteIcon(RunItemType.Blessing),
                        GetRunItemSilhouetteIcon(RunItemType.Curse)
                    };
            }
        }

        private void AddEnglishHowToPlayStep(
            RectTransform parent,
            int pageIndex,
            int stepIndex,
            string textKey,
            Sprite iconSprite)
        {
            const float left = 0.035f;
            const float right = 0.965f;
            const float gap = 0.025f;
            float width = (right - left - gap * 2f) / 3f;
            float minX = left + stepIndex * (width + gap);

            RectTransform step = AddPanel(
                parent,
                $"영문 플레이 방법 {pageIndex + 1}-{stepIndex + 1}",
                new Color(1f, 1f, 1f, 0.92f),
                panelSprite != null
                    ? panelSprite
                    : statusSectionMediumFrameSprite);
            SetAnchors(
                step,
                new Vector2(minX, 0.075f),
                new Vector2(minX + width, 0.925f));
            Image stepFrame = step.GetComponent<Image>();
            if (stepFrame != null)
            {
                stepFrame.raycastTarget = false;
            }

            Text number = AddText(
                step,
                "영문 플레이 방법 순서",
                (stepIndex + 1).ToString(),
                22,
                TextAnchor.MiddleCenter,
                new Color(0.34f, 1f, 0.88f, 1f));
            number.fontStyle = FontStyle.Bold;
            SetAnchors(
                number.rectTransform,
                new Vector2(0.075f, 0.810f),
                new Vector2(0.250f, 0.955f));

            float textTop = 0.76f;
            if (iconSprite != null)
            {
                Image icon = AddImage(
                    step,
                    "영문 플레이 방법 아이콘",
                    Color.white);
                icon.sprite = iconSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                SetAnchors(
                    icon.rectTransform,
                    new Vector2(0.190f, 0.455f),
                    new Vector2(0.810f, 0.850f));
                textTop = 0.43f;
            }

            Text label = AddLocalizedText(
                step,
                "영문 플레이 방법 안내",
                textKey,
                20,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.90f, 0.80f, 1f));
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 20;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            SetAnchors(
                label.rectTransform,
                new Vector2(0.095f, 0.180f),
                new Vector2(0.905f, textTop));
        }

        private void HideHowToPlay()
        {
            if (howToPlayOverlay != null)
            {
                howToPlayOverlay.gameObject.SetActive(false);
                DestroyUiObject(howToPlayOverlay.gameObject);
            }

            howToPlayOverlay = null;
            howToPlayImage = null;
            howToPlayEnglishVisualRoot = null;
            howToPlayTitleText = null;
            howToPlayCaptionText = null;
            howToPlayProgressText = null;
            howToPlayMissingImageText = null;
            howToPlayPreviousButton = null;
            howToPlayNextButton = null;
            howToPlayCloseButton = null;
            ClearHandFlowPracticeReferences();
            ResetHandFlowPractice();
            howToPlayPageIndex = 0;
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(true);
            }

            if (phase == GamePhase.MainMenu
                && achievementOverlay == null
                && settingsOverlay == null)
            {
                AppleGameServicesRuntime.SetAccessPointVisible(true);
            }
        }
    }
}
