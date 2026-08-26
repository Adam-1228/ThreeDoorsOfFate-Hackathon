using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private Text AddDecisionStateSummary()
        {
            Text summary = AddText(
                contentRoot,
                "선택 현재 상태",
                LF("decision.state", playerHealth, playerMaxHealth, gold, debt),
                18,
                TextAnchor.MiddleCenter,
                new Color(0.76f, 0.96f, 0.89f, 1f));
            summary.fontStyle = FontStyle.Bold;
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 14;
            summary.resizeTextMaxSize = 18;
            SetAnchors(
                summary.rectTransform,
                new Vector2(0.19f, 0.255f),
                new Vector2(0.81f, 0.315f));
            return summary;
        }

        private string BuildRestChoiceLabel(int healAmount)
        {
            int healthAfter = Mathf.Min(playerMaxHealth, playerHealth + healAmount);
            return LF(
                "decision.rest.heal",
                healAmount,
                healthAfter,
                playerMaxHealth);
        }

        private string BuildBloodBargainLabel()
        {
            return LF(
                "decision.event.blood",
                Mathf.Max(0, playerHealth - 6),
                playerMaxHealth,
                gold + 55);
        }

        private string BuildReadFateLabel()
        {
            int debtIncrease = Mathf.Max(0, 1 - curseReduction);
            return LF(
                "decision.event.fate",
                debtIncrease,
                debt + debtIncrease);
        }

        private static void ConfigureDecisionChoiceButton(Button button)
        {
            Text label = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (label == null)
            {
                return;
            }

            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 13;
            label.resizeTextMaxSize = 19;
            label.lineSpacing = 0.88f;
        }

        private string ResolveGameOverCause(string message)
        {
            const string defaultKorean = "동굴이 또 하나의 이름을 삼켰습니다.";
            const string deckExhaustedKorean =
                "덱과 손패가 모두 소진되었습니다. 더 이상 카드를 사용할 수 없어 패배했습니다.";
            if (message == deckExhaustedKorean
                || message == L("gameOver.deckExhausted"))
            {
                return L("gameOver.deckExhausted");
            }

            if (message == defaultKorean || message == L("gameOver.default"))
            {
                return L("gameOver.default");
            }

            return GameLocalization.TextFromSource(message ?? string.Empty);
        }

        private string BuildGameOverSummary(string cause, bool includeCause)
        {
            List<string> lines = new();
            if (includeCause)
            {
                lines.Add(cause);
            }

            lines.AddRange(new[]
            {
                LF(
                    "gameOver.summary.classDifficulty",
                    GetClassName(selectedClass),
                    GetDifficultyName(currentDifficulty)),
                LF(
                    "gameOver.summary.progress",
                    roomsCleared,
                    combatEncountersCompleted),
                LF(
                    "gameOver.summary.player",
                    playerHealth,
                    playerMaxHealth)
            });

            if (enemy != null)
            {
                lines.Add(LF(
                    "gameOver.summary.enemy",
                    GameLocalization.TextFromSource(enemy.Name),
                    enemy.Health,
                    enemy.MaxHealth));
            }

            lines.Add(LF(
                "gameOver.summary.resources",
                gold,
                debt,
                deck.Count,
                GetMaxDeckSize(),
                equippedRunItemIds.Count,
                GetRunItemSlotLimit()));

            if (newlyCompletedAchievementNames.Count > 0)
            {
                string names = string.Join(
                    ", ",
                    newlyCompletedAchievementNames.Select(
                        name => GameLocalization.TextFromSource(name)));
                lines.Add(LF("gameOver.summary.achievements", names));
            }

            return string.Join("\n", lines);
        }

        private Text AddGameOverSummary(string cause, bool hiddenVariant)
        {
            RectTransform panel = AddPanel(
                gameOverOverlay,
                "Game Over Run Summary Panel",
                new Color(0.015f, 0.025f, 0.030f, 0.86f),
                statusPanelFrameSprite != null ? statusPanelFrameSprite : panelSprite);
            SetAnchors(
                panel,
                hiddenVariant
                    ? new Vector2(0.205f, 0.205f)
                    : new Vector2(0.175f, 0.180f),
                hiddenVariant
                    ? new Vector2(0.795f, 0.600f)
                    : new Vector2(0.825f, 0.455f));

            Text summary = AddText(
                panel,
                "Game Over Run Summary",
                BuildGameOverSummary(cause, hiddenVariant),
                20,
                TextAnchor.MiddleCenter,
                new Color(0.91f, 0.90f, 0.82f, 1f));
            summary.resizeTextForBestFit = true;
            summary.resizeTextMinSize = 13;
            summary.resizeTextMaxSize = 20;
            summary.lineSpacing = 0.88f;
            SetAnchors(
                summary.rectTransform,
                new Vector2(0.055f, 0.075f),
                new Vector2(0.945f, 0.925f));
            panel.SetAsLastSibling();
            return summary;
        }

        private void AddGameOverActions()
        {
            Button retry = AddGameOverButton(
                gameOverOverlay,
                "Retry Same Run Button",
                L("gameOver.action.retrySame"),
                21);
            SetAnchors(
                retry.GetComponent<RectTransform>(),
                new Vector2(0.085f, 0.055f),
                new Vector2(0.355f, 0.165f));
            retry.onClick.AddListener(() => StartRun(selectedClass));

            Button chooseClass = AddGameOverButton(
                gameOverOverlay,
                "Choose Class Button",
                L("gameOver.action.chooseClass"),
                21);
            SetAnchors(
                chooseClass.GetComponent<RectTransform>(),
                new Vector2(0.365f, 0.055f),
                new Vector2(0.635f, 0.165f));
            chooseClass.onClick.AddListener(ShowClassSelection);

            Button mainMenu = AddGameOverButton(
                gameOverOverlay,
                "Main Menu Button",
                L("gameOver.action.mainMenu"),
                21);
            SetAnchors(
                mainMenu.GetComponent<RectTransform>(),
                new Vector2(0.645f, 0.055f),
                new Vector2(0.915f, 0.165f));
            mainMenu.onClick.AddListener(ShowMainMenu);
        }

        private void RenderTreasureOffer(
            int rewardGold,
            CardData card)
        {
            if (card == null || !CanAddCardToDeck())
            {
                AddCenteredMessage(
                    L("treasure.result.title"),
                    card == null
                        ? LF("treasure.result.goldOnly", rewardGold)
                        : LF(
                            "treasure.result.deckFull",
                            rewardGold,
                            deck.Count,
                            GetMaxDeckSize()));
                ShowContinueButton();
                return;
            }

            RectTransform resultRoot = AddPanel(
                contentRoot,
                "Treasure Card Result",
                new Color(0f, 0f, 0f, 0f));
            Stretch(resultRoot);
            Image transparentRoot = resultRoot.GetComponent<Image>();
            if (transparentRoot != null)
            {
                transparentRoot.raycastTarget = false;
            }

            Text heading = AddLocalizedText(
                resultRoot,
                "Treasure Result Heading",
                "treasure.result.title",
                32,
                TextAnchor.MiddleCenter,
                new Color(0.84f, 1f, 0.92f, 1f));
            heading.fontStyle = FontStyle.Bold;
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 20;
            heading.resizeTextMaxSize = 32;
            AddTextGlow(
                heading,
                new Color(0f, 0f, 0f, 0.92f),
                new Color(0.08f, 0.62f, 0.58f, 0.48f),
                new Vector2(1.6f, -1.8f));
            SetAnchors(
                heading.rectTransform,
                new Vector2(0.10f, 0.825f),
                new Vector2(0.90f, 0.965f));

            Image preview = AddImage(
                resultRoot,
                "Treasure Card Preview",
                Color.white);
            preview.sprite = GetLocalizedCardFullSprite(card);
            preview.preserveAspect = true;
            preview.raycastTarget = false;
            SetAnchors(
                preview.rectTransform,
                new Vector2(0.080f, 0.165f),
                new Vector2(0.410f, 0.805f));
            BindLocalizedCardSprite(preview, card);

            RectTransform details = AddPanel(
                resultRoot,
                "Treasure Card Details",
                new Color(0.020f, 0.045f, 0.050f, 0.92f),
                statusSectionWideFrameSprite != null
                    ? statusSectionWideFrameSprite
                    : panelSprite);
            SetAnchors(
                details,
                new Vector2(0.440f, 0.205f),
                new Vector2(0.930f, 0.755f));
            Image detailsImage = details.GetComponent<Image>();
            if (detailsImage != null)
            {
                detailsImage.raycastTarget = false;
            }

            Text name = AddText(
                details,
                "Treasure Card Name",
                GetLocalizedCardName(card),
                26,
                TextAnchor.MiddleCenter,
                new Color(0.80f, 1f, 0.94f, 1f));
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 17;
            name.resizeTextMaxSize = 26;
            BindLocalizedCardText(name, card, false);
            SetAnchors(
                name.rectTransform,
                new Vector2(0.080f, 0.705f),
                new Vector2(0.920f, 0.920f));

            Text rules = AddText(
                details,
                "Treasure Card Rules",
                GetLocalizedCardRules(card),
                20,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.90f, 0.80f, 1f));
            rules.resizeTextForBestFit = true;
            rules.resizeTextMinSize = 14;
            rules.resizeTextMaxSize = 20;
            rules.lineSpacing = 0.92f;
            BindLocalizedCardText(rules, card, true);
            SetAnchors(
                rules.rectTransform,
                new Vector2(0.095f, 0.250f),
                new Vector2(0.905f, 0.690f));

            Text goldText = AddLocalizedText(
                details,
                "Treasure Reward Gold",
                "treasure.result.gold",
                21,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.84f, 0.42f, 1f),
                rewardGold);
            goldText.fontStyle = FontStyle.Bold;
            goldText.resizeTextForBestFit = true;
            goldText.resizeTextMinSize = 15;
            goldText.resizeTextMaxSize = 21;
            SetAnchors(
                goldText.rectTransform,
                new Vector2(0.095f, 0.060f),
                new Vector2(0.905f, 0.230f));

            Button takeCard = AddLocalizedSettingsMenuButton(
                resultRoot,
                "Treasure Take Card",
                "treasure.action.takeCard",
                20,
                GameSfxCue.RewardClaim);
            SetAnchors(
                takeCard.GetComponent<RectTransform>(),
                new Vector2(0.440f, 0.050f),
                new Vector2(0.675f, 0.160f));
            ConfigureDecisionChoiceButton(takeCard);
            takeCard.onClick.AddListener(() =>
            {
                bool cardAdded = TryResolveTreasureCardChoice(card, true);
                AddLog(BuildTreasureLog(rewardGold, card, cardAdded));
                ShowDoors();
            });

            Button skipCard = AddLocalizedSettingsMenuButton(
                resultRoot,
                "Treasure Skip Card",
                "treasure.action.skipCard",
                20,
                GameSfxCue.UiAccept);
            SetAnchors(
                skipCard.GetComponent<RectTransform>(),
                new Vector2(0.695f, 0.050f),
                new Vector2(0.930f, 0.160f));
            ConfigureDecisionChoiceButton(skipCard);
            skipCard.onClick.AddListener(() =>
            {
                TryResolveTreasureCardChoice(card, false);
                AddLog(BuildTreasureLog(rewardGold, card, false));
                ShowDoors();
            });
        }

        private bool TryResolveTreasureCardChoice(CardData card, bool takeCard)
        {
            if (!takeCard || card == null || !TryAddCardToDeck(card, "보물"))
            {
                return false;
            }

            CheckBuildUnlocks();
            return true;
        }
    }
}
