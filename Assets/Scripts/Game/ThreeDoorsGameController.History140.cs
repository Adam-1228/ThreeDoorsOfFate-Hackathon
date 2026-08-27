using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game.V140;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const string RunHistoryGameVersion = "1.4.0";

        private string runHistoryKeyPrefix =
            PlayerPrefsProgressStore.ProductionPrefix;
        private readonly List<RunHistoryEntry> displayedRunHistoryEntries = new();
        private long runStartedAtUnixSeconds;
        private bool runHistoryRecordedThisRun;
        private int runHistoryCardsPlayed;
        private int runHistoryDamageDealt;
        private int runHistoryDamageTaken;
        private int runHistoryBossesDefeated;
        private int runHistoryZeroGoldShopVisits;
        private int runHistoryMaximumSameRerollStreak;
        private int runHistoryLowLuckRolls;

        private void ResetRunHistoryTracking()
        {
            runStartedAtUnixSeconds =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            runHistoryRecordedThisRun = false;
            runHistoryCardsPlayed = 0;
            runHistoryDamageDealt = 0;
            runHistoryDamageTaken = 0;
            runHistoryBossesDefeated = 0;
            runHistoryZeroGoldShopVisits = 0;
            runHistoryMaximumSameRerollStreak = 0;
            runHistoryLowLuckRolls = 0;
        }

        private void RecordRunCardPlayed()
        {
            runHistoryCardsPlayed += 1;
        }

        private void RecordRunDamageDealt(int amount)
        {
            runHistoryDamageDealt += Mathf.Max(0, amount);
        }

        private void RecordRunDamageTaken(int amount)
        {
            runHistoryDamageTaken += Mathf.Max(0, amount);
        }

        private void RecordRunBossDefeated()
        {
            runHistoryBossesDefeated += 1;
        }

        private void RecordRunShopVisit()
        {
            if (gold <= 0)
            {
                runHistoryZeroGoldShopVisits += 1;
            }
        }

        private void RecordRunLowLuckRoll(int rolledLuck)
        {
            if (rolledLuck <= 2)
            {
                runHistoryLowLuckRolls += 1;
            }
        }

        private void RecordRunRerollStreak(int streak)
        {
            runHistoryMaximumSameRerollStreak = Mathf.Max(
                runHistoryMaximumSameRerollStreak,
                Mathf.Max(0, streak));
        }

        private void RecordCompletedRunHistory(
            bool victory,
            string endingKind,
            string causeKey,
            string causeFallback)
        {
            if (runHistoryRecordedThisRun
                || string.IsNullOrWhiteSpace(activeRunId)
                || runStartedAtUnixSeconds <= 0)
            {
                return;
            }

            long finishedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            RunHistoryEntry entry = new()
            {
                RunId = activeRunId,
                GameVersion = RunHistoryGameVersion,
                StartedAtUnixSeconds = runStartedAtUnixSeconds,
                FinishedAtUnixSeconds = finishedAt,
                CharacterClass = selectedClass.ToString(),
                Difficulty = currentDifficulty.ToString(),
                StarterContractId = selectedStarterContractId ?? string.Empty,
                EndingKind = endingKind ?? string.Empty,
                EndingCauseKey = causeKey ?? string.Empty,
                EndingCauseFallback = causeFallback ?? string.Empty,
                Victory = victory,
                DoorsCleared = Mathf.Max(0, roomsCleared),
                BattlesDefeated = Mathf.Max(0, combatEncountersCompleted),
                BossesDefeated = Mathf.Max(0, runHistoryBossesDefeated),
                FinalHealth = Mathf.Max(0, playerHealth),
                FinalMaxHealth = Mathf.Max(0, playerMaxHealth),
                FinalGold = Mathf.Max(0, gold),
                FinalDebt = Mathf.Max(0, debt),
                CardsPlayed = Mathf.Max(0, runHistoryCardsPlayed),
                DamageDealt = Mathf.Max(0, runHistoryDamageDealt),
                DamageTaken = Mathf.Max(0, runHistoryDamageTaken),
                CardsRemoved = Mathf.Max(0, cardsRemovedThisRun),
                ZeroGoldShopVisits = Mathf.Max(
                    0,
                    runHistoryZeroGoldShopVisits),
                MaximumSameRerollStreak = Mathf.Max(
                    0,
                    runHistoryMaximumSameRerollStreak),
                LowLuckRolls = Mathf.Max(0, runHistoryLowLuckRolls),
                FinalDeckCardIds = deck
                    .Where(card => card != null
                        && !string.IsNullOrWhiteSpace(card.CardId))
                    .Select(card => card.CardId)
                    .ToList(),
                EquippedItemIds = equippedRunItemIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToList(),
                ActiveMutationIds = activeEndlessMutationIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList(),
                NewAchievementNames = newlyCompletedAchievementNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList()
            };

            try
            {
                RunHistoryStore.Append(runHistoryKeyPrefix, entry);
                runHistoryRecordedThisRun = true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"Local run history was not saved: {exception.Message}");
            }
        }

        private void RecordGameOverRunHistory(bool victory, string message)
        {
            string causeKey = GetRunHistoryGameOverCauseKey(message);
            RecordCompletedRunHistory(
                victory,
                victory ? "victory" : "death",
                causeKey,
                message);
        }

        private void RecordJourneyRunHistory()
        {
            string endingKind = currentJourneyEndingKind switch
            {
                JourneyEndingKind.TrueDebtCleared => "true",
                JourneyEndingKind.EndlessReturn => "endless",
                _ => "return"
            };
            RecordCompletedRunHistory(
                true,
                endingKind,
                GetJourneyEndingTitleKey(),
                string.Empty);
        }

        private string GetRunHistoryGameOverCauseKey(string message)
        {
            const string defaultKorean =
                "동굴이 또 하나의 이름을 삼켰습니다.";
            const string deckExhaustedKorean =
                "덱과 손패가 모두 소진되었습니다. 더 이상 카드를 사용할 수 없어 패배했습니다.";
            if (message == deckExhaustedKorean
                || message == L("gameOver.deckExhausted"))
            {
                return "gameOver.deckExhausted";
            }

            if (message == defaultKorean || message == L("gameOver.default"))
            {
                return "gameOver.default";
            }

            return string.Empty;
        }

        private string GetJourneyEndingTitleKey()
        {
            if (currentJourneyEndingKind == JourneyEndingKind.EndlessReturn)
            {
                return "ending.title.endlessReturn";
            }

            string classSuffix = selectedClass switch
            {
                CharacterClass.Oracle => "oracle",
                CharacterClass.Exile => "exile",
                _ => "gambler"
            };
            return currentJourneyEndingKind == JourneyEndingKind.TrueDebtCleared
                ? $"ending.title.true.{classSuffix}"
                : $"ending.title.return.{classSuffix}";
        }

        private string BuildCurrentRunHistoryEpithetText()
        {
            RunHistoryEntry entry = RunHistoryStore.Read(runHistoryKeyPrefix)
                .FirstOrDefault(candidate => string.Equals(
                    candidate.RunId,
                    activeRunId,
                    StringComparison.Ordinal));
            if (entry == null)
            {
                return string.Empty;
            }

            return string.Join(
                ", ",
                RunHistoryEpithetPolicy.Get(entry).Select(L));
        }

        private void ShowRunHistory()
        {
            HideHowToPlay();
            HideAchievements();
            HideSettingsPanel();
            ClearGameOverOverlay();
            PlayMainMenuMusic();
            phase = GamePhase.MainMenu;
            SetBackground(
                mainMenuBackground != null
                    ? mainMenuBackground
                    : classSelectBackground);
            ClearContent();
            AppleGameServicesRuntime.SetAccessPointVisible(false);
            titleText.text = L("app.title");
            subtitleText.text = L("runHistory.title");
            BindLocalizedText(subtitleText, "runHistory.title");
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(false);
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetAnchors(contentRoot, Vector2.zero, Vector2.one);

            displayedRunHistoryEntries.Clear();
            displayedRunHistoryEntries.AddRange(
                RunHistoryStore.Read(runHistoryKeyPrefix));

            Sprite outerSprite = statusPanelFrameSprite != null
                ? statusPanelFrameSprite
                : panelSprite;
            RectTransform outer = AddPanel(
                contentRoot,
                "운명 기록 외곽 프레임",
                Color.white,
                outerSprite);
            outer.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                outer,
                new Vector2(0.045f, 0.105f),
                new Vector2(0.955f, 0.865f));

            AddRunStatusLabelBox(
                contentRoot,
                "운명 기록 제목 박스",
                L("runHistory.title"),
                new Vector2(0.340f, 0.885f),
                new Vector2(0.660f, 0.985f),
                30);
            AddRunStatusTextButton(
                contentRoot,
                "운명 기록 닫기",
                L("common.close"),
                new Vector2(0.835f, 0.895f),
                new Vector2(0.955f, 0.975f),
                ShowMainMenu,
                18);

            if (displayedRunHistoryEntries.Count == 0)
            {
                Text empty = AddText(
                    outer,
                    "운명 기록 없음",
                    L("runHistory.empty"),
                    24,
                    TextAnchor.MiddleCenter,
                    new Color(0.84f, 0.88f, 0.82f, 1f));
                SetAnchors(
                    empty.rectTransform,
                    new Vector2(0.12f, 0.30f),
                    new Vector2(0.88f, 0.70f));
                return;
            }

            for (int index = 0;
                index < displayedRunHistoryEntries.Count;
                index += 1)
            {
                int column = index / 5;
                int row = index % 5;
                float minX = column == 0 ? 0.045f : 0.515f;
                float maxX = column == 0 ? 0.485f : 0.955f;
                float maxY = 0.945f - row * 0.180f;
                float minY = maxY - 0.145f;
                AddRunHistoryListButton(
                    outer,
                    index,
                    displayedRunHistoryEntries[index],
                    new Vector2(minX, minY),
                    new Vector2(maxX, maxY));
            }
        }

        private void AddRunHistoryListButton(
            RectTransform parent,
            int index,
            RunHistoryEntry entry,
            Vector2 minimum,
            Vector2 maximum)
        {
            RectTransform row = AddPanel(
                parent,
                $"운명 기록 항목 {index}",
                Color.white,
                GetRunStatusWideBoxFrameSprite());
            Image image = row.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.raycastTarget = true;
            SetAnchors(row, minimum, maximum);

            Button button = AddSfxButton(row.gameObject);
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            int capturedIndex = index;
            button.onClick.AddListener(
                () => ShowRunHistoryDetail(capturedIndex));

            Text label = AddText(
                row,
                $"운명 기록 항목 {index} 라벨",
                BuildRunHistoryListLabel(entry),
                16,
                TextAnchor.MiddleLeft,
                new Color(0.88f, 0.95f, 0.87f, 1f));
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 11;
            label.resizeTextMaxSize = 16;
            label.lineSpacing = 0.90f;
            label.raycastTarget = false;
            SetAnchors(
                label.rectTransform,
                new Vector2(0.075f, 0.115f),
                new Vector2(0.925f, 0.885f));
        }

        private string BuildRunHistoryListLabel(RunHistoryEntry entry)
        {
            string epithets = string.Join(
                ", ",
                RunHistoryEpithetPolicy.Get(entry).Take(2).Select(L));
            if (string.IsNullOrWhiteSpace(epithets))
            {
                epithets = L("runHistory.epithet.none");
            }

            return LF(
                "runHistory.list.entry",
                FormatRunHistoryDate(entry.FinishedAtUnixSeconds),
                GetRunHistoryClassName(entry.CharacterClass),
                GetRunHistoryDifficultyName(entry.Difficulty),
                entry.DoorsCleared,
                ResolveRunHistoryEnding(entry),
                epithets);
        }

        private void ShowRunHistoryDetail(int index)
        {
            if (index < 0 || index >= displayedRunHistoryEntries.Count)
            {
                ShowRunHistory();
                return;
            }

            RunHistoryEntry entry = displayedRunHistoryEntries[index];
            ClearContent();
            SetAnchors(contentRoot, Vector2.zero, Vector2.one);
            subtitleText.text = L("runHistory.detail.title");
            BindLocalizedText(subtitleText, "runHistory.detail.title");
            SetSubtitleBoxVisible(true);

            Sprite outerSprite = statusPanelFrameSprite != null
                ? statusPanelFrameSprite
                : panelSprite;
            RectTransform outer = AddPanel(
                contentRoot,
                "운명 기록 상세 외곽 프레임",
                Color.white,
                outerSprite);
            outer.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                outer,
                new Vector2(0.045f, 0.105f),
                new Vector2(0.955f, 0.865f));

            AddRunStatusLabelBox(
                contentRoot,
                "운명 기록 상세 제목 박스",
                L("runHistory.detail.title"),
                new Vector2(0.325f, 0.885f),
                new Vector2(0.675f, 0.985f),
                28);
            AddRunStatusTextButton(
                contentRoot,
                "운명 기록 상세 뒤로",
                L("runHistory.back"),
                new Vector2(0.045f, 0.895f),
                new Vector2(0.180f, 0.975f),
                ShowRunHistory,
                18);

            RectTransform summaryPanel = AddPanel(
                outer,
                "운명 기록 상세 요약",
                Color.white,
                GetRunStatusDetailBoxFrameSprite());
            summaryPanel.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                summaryPanel,
                new Vector2(0.050f, 0.075f),
                new Vector2(0.485f, 0.925f));
            Text summary = AddText(
                summaryPanel,
                "운명 기록 상세 요약 텍스트",
                BuildRunHistoryDetailSummary(entry),
                18,
                TextAnchor.UpperLeft,
                new Color(0.88f, 0.95f, 0.88f, 1f));
            ConfigureRunHistoryDetailText(summary);

            RectTransform loadoutPanel = AddPanel(
                outer,
                "운명 기록 상세 덱과 아이템",
                Color.white,
                GetRunStatusDetailBoxFrameSprite());
            loadoutPanel.GetComponent<Image>().raycastTarget = false;
            SetAnchors(
                loadoutPanel,
                new Vector2(0.515f, 0.075f),
                new Vector2(0.950f, 0.925f));
            Text loadout = AddText(
                loadoutPanel,
                "운명 기록 상세 덱과 아이템 텍스트",
                BuildRunHistoryLoadoutSummary(entry),
                17,
                TextAnchor.UpperLeft,
                new Color(0.88f, 0.93f, 0.84f, 1f));
            ConfigureRunHistoryDetailText(loadout);
        }

        private static void ConfigureRunHistoryDetailText(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 18;
            text.lineSpacing = 0.91f;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            SetAnchors(
                text.rectTransform,
                new Vector2(0.075f, 0.070f),
                new Vector2(0.925f, 0.930f));
        }

        private string BuildRunHistoryDetailSummary(RunHistoryEntry entry)
        {
            string epithets = string.Join(
                ", ",
                RunHistoryEpithetPolicy.Get(entry).Select(L));
            if (string.IsNullOrWhiteSpace(epithets))
            {
                epithets = L("runHistory.epithet.none");
            }

            return string.Join("\n", new[]
            {
                L("runHistory.detail.summaryHeader"),
                LF(
                    "runHistory.detail.dateVersion",
                    FormatRunHistoryDate(entry.FinishedAtUnixSeconds),
                    entry.GameVersion),
                LF(
                    "runHistory.detail.classDifficulty",
                    GetRunHistoryClassName(entry.CharacterClass),
                    GetRunHistoryDifficultyName(entry.Difficulty)),
                LF(
                    "runHistory.detail.contract",
                    GetRunHistoryContractName(entry.StarterContractId)),
                LF(
                    "runHistory.detail.ending",
                    ResolveRunHistoryEnding(entry)),
                LF(
                    "runHistory.detail.progress",
                    entry.DoorsCleared,
                    entry.BattlesDefeated,
                    entry.BossesDefeated),
                LF(
                    "runHistory.detail.resources",
                    entry.FinalHealth,
                    entry.FinalMaxHealth,
                    entry.FinalGold,
                    entry.FinalDebt),
                LF(
                    "runHistory.detail.combatStats",
                    entry.CardsPlayed,
                    entry.DamageDealt,
                    entry.DamageTaken,
                    entry.CardsRemoved),
                LF("runHistory.detail.epithets", epithets)
            });
        }

        private string BuildRunHistoryLoadoutSummary(RunHistoryEntry entry)
        {
            string deckText = BuildRunHistoryDeckText(entry.FinalDeckCardIds);
            string itemText = JoinOrNone(entry.EquippedItemIds
                .Select(GetRunHistoryItemName));
            string mutationText = JoinOrNone(entry.ActiveMutationIds
                .Select(GetRunHistoryMutationName));
            string achievementText = JoinOrNone(entry.NewAchievementNames
                .Select(GameLocalization.TextFromSource));
            return string.Join("\n\n", new[]
            {
                LF("runHistory.detail.deck", deckText),
                LF("runHistory.detail.items", itemText),
                LF("runHistory.detail.mutations", mutationText),
                LF("runHistory.detail.achievements", achievementText)
            });
        }

        private string BuildRunHistoryDeckText(IEnumerable<string> cardIds)
        {
            List<string> lines = (cardIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(id => id, StringComparer.Ordinal)
                .OrderBy(group => GetRunHistoryCardName(group.Key), StringComparer.Ordinal)
                .Select(group => LF(
                    "runHistory.detail.deckLine",
                    group.Count(),
                    GetRunHistoryCardName(group.Key)))
                .Take(24)
                .ToList();
            return lines.Count > 0
                ? string.Join("\n", lines)
                : L("runHistory.none");
        }

        private string GetRunHistoryCardName(string cardId)
        {
            CardData card = cardPool.FirstOrDefault(candidate => candidate != null
                && string.Equals(candidate.CardId, cardId, StringComparison.Ordinal));
            return card != null
                ? GetLocalizedCardName(card)
                : CardLocalization.GetName(cardId, cardId);
        }

        private string GetRunHistoryItemName(string itemId)
        {
            RunItemDefinition item = GetRunItemDefinition(itemId);
            return item != null
                ? GameLocalization.TextFromSource(item.Name)
                : itemId;
        }

        private string GetRunHistoryMutationName(string mutationId)
        {
            if (TryGetEndlessMutationCatalog(
                    out EndlessMutationCatalog catalog)
                && catalog.TryGet(
                    mutationId,
                    out EndlessMutationDefinition mutation))
            {
                return L(mutation.NameKey);
            }

            return mutationId;
        }

        private string GetRunHistoryContractName(string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId))
            {
                return L("runHistory.contract.default");
            }

            if (TryGetStarterContractCatalog(
                    out StarterContractCatalog catalog))
            {
                try
                {
                    return L(catalog.GetContract(contractId).NameKey);
                }
                catch (InvalidOperationException)
                {
                    return contractId;
                }
            }

            return contractId;
        }

        private string GetRunHistoryClassName(string className)
        {
            return Enum.TryParse(
                className,
                false,
                out CharacterClass characterClass)
                ? GetClassName(characterClass)
                : className;
        }

        private string GetRunHistoryDifficultyName(string difficultyName)
        {
            return Enum.TryParse(
                difficultyName,
                false,
                out RunDifficulty difficulty)
                ? GetDifficultyName(difficulty)
                : difficultyName;
        }

        private string ResolveRunHistoryEnding(RunHistoryEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.EndingCauseKey))
            {
                return L(entry.EndingCauseKey);
            }

            if (!string.IsNullOrWhiteSpace(entry.EndingCauseFallback))
            {
                return GameLocalization.TextFromSource(
                    entry.EndingCauseFallback);
            }

            return entry.Victory
                ? L("runHistory.ending.victory")
                : L("runHistory.ending.death");
        }

        private static string FormatRunHistoryDate(long unixSeconds)
        {
            if (unixSeconds <= 0)
            {
                return L("runHistory.date.unknown");
            }

            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm");
            }
            catch (ArgumentOutOfRangeException)
            {
                return L("runHistory.date.unknown");
            }
        }

        private static string JoinOrNone(IEnumerable<string> values)
        {
            List<string> safeValues = (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            return safeValues.Count > 0
                ? string.Join(", ", safeValues)
                : L("runHistory.none");
        }
    }
}
