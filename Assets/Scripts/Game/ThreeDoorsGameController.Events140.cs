using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game.V140;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const string EventCatalogResourcePath = "GameData/V140/events";

        private EventCatalog cachedEventCatalog;
        private bool eventCatalogLoadAttempted;
        private int seenRunEventSegment;

        private void ShowEvent()
        {
            if (!TryGetEventCatalog(out EventCatalog catalog))
            {
                ShowLegacyEvent();
                return;
            }

            EventDefinition definition;
            try
            {
                definition = !string.IsNullOrWhiteSpace(pendingRunEventId)
                    ? catalog.Get(pendingRunEventId)
                    : PickRunEvent();
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogWarning($"Event catalog fallback: {exception.Message}");
                ShowLegacyEvent();
                return;
            }

            pendingRunEventId = definition.Id;
            PlayNonCombatMusic();
            phase = GamePhase.Event;
            checkpointResumePhase = GamePhase.Event;
            pendingResolvedDoorTypeId = NoPendingDoorType;
            restoredRunCheckpoint = null;
            SaveRunCheckpointAtResolvedSurface();

            SetBackground(eventBackground);
            ClearContent();
            SetDefaultContentRootPlacement();
            subtitleText.text = L("event.ui.subtitle");
            BindLocalizedText(subtitleText, "event.ui.subtitle");
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(false);

            RectTransform message = AddEventMessagePanel(
                contentRoot,
                $"운명 사건 {definition.Id}");
            SetAnchors(
                message,
                new Vector2(0.095f, 0.350f),
                new Vector2(0.905f, 0.790f));

            Text heading = AddLocalizedText(
                message,
                "사건 제목",
                definition.TitleKey,
                30,
                TextAnchor.MiddleCenter,
                Color.white);
            heading.fontStyle = FontStyle.Bold;
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 20;
            heading.resizeTextMaxSize = 30;
            AddTextGlow(
                heading,
                new Color(0f, 0f, 0f, 0.86f),
                new Color(0.46f, 0.36f, 0.22f, 0.68f),
                new Vector2(2.2f, -2.6f));
            SetAnchors(
                heading.rectTransform,
                new Vector2(0.105f, 0.565f),
                new Vector2(0.895f, 0.790f));

            Text body = AddLocalizedText(
                message,
                "사건 본문",
                definition.BodyKey,
                22,
                TextAnchor.UpperCenter,
                new Color(0.88f, 0.83f, 0.74f, 1f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 15;
            body.resizeTextMaxSize = 22;
            SetAnchors(
                body.rectTransform,
                new Vector2(0.115f, 0.180f),
                new Vector2(0.885f, 0.545f));

            for (int index = 0; index < definition.Choices.Count; index += 1)
            {
                AddEventChoiceButton(definition, definition.Choices[index], index);
            }

            AddDecisionStateSummary();
            RefreshTopBar();
            RefreshLog();
        }

        private void AddEventChoiceButton(
            EventDefinition definition,
            EventChoiceDefinition choice,
            int index)
        {
            int choiceCount = definition.Choices.Count;
            float width = choiceCount == 3 ? 0.290f : 0.340f;
            float gap = choiceCount == 3 ? 0.018f : 0.055f;
            float total = width * choiceCount + gap * (choiceCount - 1);
            float left = (1f - total) * 0.5f + index * (width + gap);
            Button button = AddSettingsMenuButton(
                contentRoot,
                $"사건 선택 {index + 1}",
                BuildEventChoiceLabel(choice),
                choiceCount == 3 ? 16 : 18,
                GameSfxCue.ImportantConfirm);
            SetAnchors(
                button.GetComponent<RectTransform>(),
                new Vector2(left, 0.075f),
                new Vector2(left + width, 0.265f));
            ConfigureDecisionChoiceButton(button);
            button.interactable = CanChooseEventChoice(choice);

            UnityAction confirm = () => ApplyEventChoice(choice);
            EventEffectDefinition cardEffect = choice.Effects.FirstOrDefault(effect =>
                effect.Type == EventEffectType.AddCard);
            CardData card = cardEffect == null
                ? null
                : cardPool.FirstOrDefault(candidate =>
                    candidate != null
                    && string.Equals(
                        candidate.CardId,
                        cardEffect.CardId,
                        StringComparison.Ordinal));
            if (card != null)
            {
                button.onClick.AddListener(() => ShowCardInspection(
                    card,
                    CardInspectionMode.RewardTake,
                    L("event.ui.confirm"),
                    confirm));
                return;
            }

            button.onClick.AddListener(confirm);
        }

        private string BuildEventChoiceLabel(EventChoiceDefinition choice)
        {
            return $"{L(choice.LabelKey)}\n{BuildEventChoicePreview(choice)}";
        }

        private string BuildEventChoicePreview(EventChoiceDefinition choice)
        {
            List<string> lines = new() { L(choice.PreviewKey) };
            int maximumHealthDelta = SumEventEffect(choice, EventEffectType.MaxHealth);
            int healthDelta = SumEventEffect(choice, EventEffectType.Health);
            int goldDelta = SumEventEffect(choice, EventEffectType.Gold);
            int debtDelta = SumEventEffect(choice, EventEffectType.Debt);

            int nextMaximumHealth = Mathf.Max(1, playerMaxHealth + maximumHealthDelta);
            int nextHealth = Mathf.Clamp(playerHealth, 0, nextMaximumHealth);
            nextHealth = Mathf.Clamp(nextHealth + healthDelta, 0, nextMaximumHealth);
            if (maximumHealthDelta != 0 || healthDelta != 0)
            {
                lines.Add(LF(
                    "event.ui.preview.health",
                    playerHealth,
                    playerMaxHealth,
                    nextHealth,
                    nextMaximumHealth));
            }

            if (goldDelta != 0)
            {
                lines.Add(LF(
                    "event.ui.preview.gold",
                    gold,
                    Mathf.Max(0, gold + goldDelta)));
            }

            if (debtDelta != 0)
            {
                lines.Add(LF(
                    "event.ui.preview.debt",
                    debt,
                    Mathf.Max(0, debt + debtDelta)));
            }

            return string.Join("\n", lines.Where(line => !string.IsNullOrWhiteSpace(line)));
        }

        private bool CanChooseEventChoice(EventChoiceDefinition choice)
        {
            int maximumHealthAfter = Mathf.Max(
                1,
                playerMaxHealth + SumEventEffect(choice, EventEffectType.MaxHealth));
            int healthAfter = Mathf.Clamp(playerHealth, 0, maximumHealthAfter)
                + SumEventEffect(choice, EventEffectType.Health);
            if (healthAfter <= 0
                || gold + SumEventEffect(choice, EventEffectType.Gold) < 0)
            {
                return false;
            }

            foreach (EventEffectDefinition effect in choice.Effects)
            {
                if (effect.Type == EventEffectType.AddCard
                    && (!CanAddCardToDeck()
                        || !cardPool.Any(card => card != null
                            && string.Equals(
                                card.CardId,
                                effect.CardId,
                                StringComparison.Ordinal))))
                {
                    return false;
                }

                if (effect.Type == EventEffectType.RemoveCard
                    && !deck.Any(CanRemoveDeckCard))
                {
                    return false;
                }
            }

            return true;
        }

        private static int SumEventEffect(
            EventChoiceDefinition choice,
            EventEffectType type)
        {
            return choice.Effects
                .Where(effect => effect.Type == type)
                .Sum(effect => effect.Amount);
        }

        private EventDefinition PickRunEvent()
        {
            if (!TryGetEventCatalog(out EventCatalog catalog))
            {
                return null;
            }

            EnsureCurrentEventSegment();
            List<EventDefinition> eligible = catalog.Events
                .Where(definition => definition.IsEligible(
                    selectedClass,
                    debt,
                    seenRunEventIds))
                .ToList();
            return eligible.Count == 0
                ? catalog.GetSafeFallback()
                : eligible[RunRange(0, eligible.Count)];
        }

        private void EnsureCurrentEventSegment()
        {
            int currentSegment = Mathf.Max(0, roomsCleared / 10);
            if (seenRunEventSegment == currentSegment)
            {
                return;
            }

            seenRunEventIds.Clear();
            seenRunEventSegment = currentSegment;
        }

        private void ApplyEventChoice(EventChoiceDefinition choice)
        {
            if (choice == null || !CanChooseEventChoice(choice))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(pendingRunEventId))
            {
                seenRunEventIds.Add(pendingRunEventId);
            }

            bool requiresRemoval = false;
            RunItemDefinition discoveredItem = null;
            foreach (EventEffectDefinition effect in choice.Effects)
            {
                switch (effect.Type)
                {
                    case EventEffectType.Health:
                        if (effect.Amount >= 0)
                        {
                            Heal(effect.Amount);
                        }
                        else
                        {
                            LoseHealth(-effect.Amount, true);
                        }
                        break;
                    case EventEffectType.MaxHealth:
                        playerMaxHealth = Mathf.Max(1, playerMaxHealth + effect.Amount);
                        playerHealth = Mathf.Clamp(playerHealth, 1, playerMaxHealth);
                        break;
                    case EventEffectType.Gold:
                        gold = Mathf.Max(0, gold + effect.Amount);
                        break;
                    case EventEffectType.Debt:
                        debt = Mathf.Max(0, debt + effect.Amount);
                        break;
                    case EventEffectType.AddCard:
                        CardData card = cardPool.FirstOrDefault(candidate =>
                            candidate != null
                            && string.Equals(
                                candidate.CardId,
                                effect.CardId,
                                StringComparison.Ordinal));
                        if (card != null)
                        {
                            TryAddCardToDeck(card, L("event.ui.source"));
                        }
                        break;
                    case EventEffectType.RemoveCard:
                        requiresRemoval = true;
                        break;
                    case EventEffectType.DoorInsight:
                        doorInsightLevel = Mathf.Clamp(
                            doorInsightLevel + effect.Amount,
                            0,
                            3);
                        break;
                    case EventEffectType.ItemDiscovery:
                        discoveredItem = string.IsNullOrWhiteSpace(effect.ItemId)
                            ? PickUnlockedRunItem()
                            : GetRunItemDefinition(effect.ItemId);
                        break;
                }

                if (phase == GamePhase.GameOver)
                {
                    return;
                }
            }

            AddLog(LF("event.log.resolved", L(choice.LabelKey)));
            CheckBuildUnlocks();
            if (requiresRemoval)
            {
                ShowDeckRemovalSelection(DeckRemovalSource.Event, 0);
                return;
            }

            if (discoveredItem != null)
            {
                ShowRunItemReward(discoveredItem, ShowDoors);
                return;
            }

            ShowDoors();
        }

        private bool TryGetEventCatalog(out EventCatalog catalog)
        {
            if (!eventCatalogLoadAttempted)
            {
                eventCatalogLoadAttempted = true;
                TextAsset source = Resources.Load<TextAsset>(EventCatalogResourcePath);
                try
                {
                    cachedEventCatalog = EventCatalog.Load(source);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Event catalog fallback: {exception.Message}");
                }
            }

            catalog = cachedEventCatalog;
            return catalog != null;
        }
    }
}
