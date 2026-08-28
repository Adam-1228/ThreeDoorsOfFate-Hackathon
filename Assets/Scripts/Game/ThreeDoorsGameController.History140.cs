using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game.V140;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using ThreeDoorsOfFate.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const string RunHistoryGameVersion = "1.4.0";
        private const int RunHistoryEntriesPerPage = 3;

        private string runHistoryKeyPrefix =
            PlayerPrefsProgressStore.ProductionPrefix;
        private readonly List<RunHistoryEntry> displayedRunHistoryEntries = new();
        private int runHistoryPage;
        private int selectedRunHistoryIndex = -1;
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
            AppleGameServicesRuntime.SetAccessPointVisible(false);
            titleText.text = L("app.title");

            displayedRunHistoryEntries.Clear();
            displayedRunHistoryEntries.AddRange(
                RunHistoryStore.Read(runHistoryKeyPrefix));
            runHistoryPage = 0;
            selectedRunHistoryIndex = displayedRunHistoryEntries.Count > 0
                ? 0
                : -1;
            RenderRunHistoryLayout();
        }

        private void RenderRunHistoryLayout()
        {
            ClearContent();
            subtitleText.text = L("runHistory.title");
            BindLocalizedText(subtitleText, "runHistory.title");
            SetSubtitleBoxVisible(true);
            primaryButton.gameObject.SetActive(false);
            topBar.gameObject.SetActive(false);
            SetLogVisible(false);
            SetAnchors(contentRoot, Vector2.zero, Vector2.one);

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

            RectTransform historySafeRoot = AddPanel(
                outer,
                "운명 기록 안전영역",
                new Color(1f, 1f, 1f, 0f));
            historySafeRoot.GetComponent<Image>().raycastTarget = false;
            historySafeRoot.gameObject.AddComponent<RectMask2D>();
            SetAnchors(
                historySafeRoot,
                new Vector2(0.090f, 0.145f),
                new Vector2(0.910f, 0.800f));

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
                    historySafeRoot,
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

            int pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    displayedRunHistoryEntries.Count
                    / (float)RunHistoryEntriesPerPage));
            runHistoryPage = Mathf.Clamp(runHistoryPage, 0, pageCount - 1);
            int pageStart = runHistoryPage * RunHistoryEntriesPerPage;
            int pageEnd = Mathf.Min(
                displayedRunHistoryEntries.Count,
                pageStart + RunHistoryEntriesPerPage);
            if (selectedRunHistoryIndex < pageStart
                || selectedRunHistoryIndex >= pageEnd)
            {
                selectedRunHistoryIndex = pageStart;
            }

            RectTransform listPanel = AddRunStatusContentBox(
                historySafeRoot,
                "운명 기록 목록 패널",
                new Vector2(0.000f, 0.000f),
                new Vector2(0.390f, 1.000f),
                statusInnerPanelFrameSprite);
            RectTransform listContent = AddPanel(
                listPanel,
                "운명 기록 목록 내용 안전영역",
                new Color(1f, 1f, 1f, 0f));
            listContent.GetComponent<Image>().raycastTarget = false;
            listContent.gameObject.AddComponent<RectMask2D>();
            SetAnchors(
                listContent,
                new Vector2(0.065f, 0.065f),
                new Vector2(0.935f, 0.935f));

            AddRunStatusFlatLabelBox(
                listContent,
                "운명 기록 페이지 제목",
                LF("runHistory.recentPage", runHistoryPage + 1, pageCount),
                new Vector2(0.000f, 0.875f),
                new Vector2(1.000f, 1.000f),
                22);

            const float rowTop = 0.850f;
            const float rowHeight = 0.225f;
            const float rowGap = 0.025f;
            for (int index = pageStart; index < pageEnd; index += 1)
            {
                int visibleRow = index - pageStart;
                float maxY = rowTop
                    - visibleRow * (rowHeight + rowGap);
                float minY = maxY - rowHeight;
                AddRunHistoryListButton(
                    listContent,
                    index,
                    displayedRunHistoryEntries[index],
                    new Vector2(0.015f, minY),
                    new Vector2(0.985f, maxY),
                    index == selectedRunHistoryIndex);
            }

            if (pageCount > 1)
            {
                AddRunStatusTextButton(
                    listContent,
                    "운명 기록 이전 페이지",
                    L("common.previous"),
                    new Vector2(0.015f, 0.000f),
                    new Vector2(0.470f, 0.085f),
                    () => ShowRunHistoryPage(runHistoryPage - 1),
                    14).interactable = runHistoryPage > 0;
                AddRunStatusTextButton(
                    listContent,
                    "운명 기록 다음 페이지",
                    L("common.next"),
                    new Vector2(0.530f, 0.000f),
                    new Vector2(0.985f, 0.085f),
                    () => ShowRunHistoryPage(runHistoryPage + 1),
                    14).interactable = runHistoryPage < pageCount - 1;
            }

            RectTransform summaryPanel = AddRunStatusContentBox(
                historySafeRoot,
                "운명 기록 선택 요약",
                new Vector2(0.415f, 0.000f),
                new Vector2(1.000f, 1.000f),
                statusInnerPanelFrameSprite);
            PopulateRunHistorySelectionSummary(
                summaryPanel,
                displayedRunHistoryEntries[selectedRunHistoryIndex]);
        }

        private void AddRunHistoryListButton(
            RectTransform parent,
            int index,
            RunHistoryEntry entry,
            Vector2 minimum,
            Vector2 maximum,
            bool selected)
        {
            RectTransform row = AddRunStatusContentBox(
                parent,
                $"운명 기록 항목 {index}",
                minimum,
                maximum,
                statusInnerHeaderFrameSprite != null
                    ? statusInnerHeaderFrameSprite
                    : GetRunStatusWideBoxFrameSprite());
            Image image = row.GetComponent<Image>();
            image.raycastTarget = true;
            image.color = selected
                ? new Color(0.055f, 0.115f, 0.120f, 1f)
                : new Color(0.018f, 0.024f, 0.028f, 0.985f);

            Button button = AddSfxButton(row.gameObject);
            button.targetGraphic = image;
            button.colors = CreateButtonColors();
            int capturedIndex = index;
            button.onClick.AddListener(
                () => SelectRunHistoryEntry(capturedIndex));

            Text title = AddText(
                row,
                $"운명 기록 항목 {index} 제목",
                BuildRunHistoryListTitle(entry),
                24,
                TextAnchor.MiddleLeft,
                new Color(0.76f, 1f, 0.96f, 1f));
            title.fontStyle = FontStyle.Bold;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 20;
            title.resizeTextMaxSize = 24;
            title.raycastTarget = false;
            SetAnchors(
                title.rectTransform,
                new Vector2(0.120f, 0.540f),
                new Vector2(0.880f, 0.820f));

            Text metadata = AddText(
                row,
                $"운명 기록 항목 {index} 정보",
                BuildRunHistoryListMetadata(entry),
                20,
                TextAnchor.MiddleLeft,
                new Color(0.88f, 0.93f, 0.84f, 1f));
            metadata.resizeTextForBestFit = true;
            metadata.resizeTextMinSize = 18;
            metadata.resizeTextMaxSize = 20;
            metadata.raycastTarget = false;
            SetAnchors(
                metadata.rectTransform,
                new Vector2(0.120f, 0.180f),
                new Vector2(0.880f, 0.480f));
        }

        private void ShowRunHistoryPage(int page)
        {
            int pageCount = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    displayedRunHistoryEntries.Count
                    / (float)RunHistoryEntriesPerPage));
            runHistoryPage = Mathf.Clamp(page, 0, pageCount - 1);
            selectedRunHistoryIndex = Mathf.Min(
                displayedRunHistoryEntries.Count - 1,
                runHistoryPage * RunHistoryEntriesPerPage);
            RenderRunHistoryLayout();
        }

        private void SelectRunHistoryEntry(int index)
        {
            if (index < 0 || index >= displayedRunHistoryEntries.Count)
            {
                return;
            }

            selectedRunHistoryIndex = index;
            runHistoryPage = index / RunHistoryEntriesPerPage;
            RenderRunHistoryLayout();
        }

        private void ShowSelectedRunHistoryDetail()
        {
            if (selectedRunHistoryIndex < 0
                || selectedRunHistoryIndex >= displayedRunHistoryEntries.Count)
            {
                ShowRunHistory();
                return;
            }

            ShowRunHistoryDetail(selectedRunHistoryIndex);
        }

        private void PopulateRunHistorySelectionSummary(
            RectTransform parent,
            RunHistoryEntry entry)
        {
            RectTransform content = AddPanel(
                parent,
                "운명 기록 선택 요약 안전영역",
                new Color(1f, 1f, 1f, 0f));
            content.GetComponent<Image>().raycastTarget = false;
            content.gameObject.AddComponent<RectMask2D>();
            SetAnchors(
                content,
                new Vector2(0.045f, 0.060f),
                new Vector2(0.955f, 0.940f));

            RectTransform heading = AddRunStatusContentBox(
                content,
                "운명 기록 선택 제목",
                new Vector2(0.000f, 0.870f),
                new Vector2(1.000f, 1.000f),
                statusInnerHeaderFrameSprite);
            AddRunHistoryBoxText(
                heading,
                LF(
                    "runHistory.summary.selectedTitle",
                    GetRunHistoryResultName(entry),
                    GetRunHistoryClassName(entry.CharacterClass)),
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0.100f, 0.180f),
                new Vector2(0.900f, 0.820f));

            string[] statNames =
            {
                L("runHistory.summary.doorsLabel"),
                L("runHistory.summary.battlesLabel"),
                L("runHistory.summary.goldLabel"),
                L("runHistory.summary.debtLabel")
            };
            int[] statValues =
            {
                entry.DoorsCleared,
                entry.BattlesDefeated,
                entry.FinalGold,
                entry.FinalDebt
            };
            Vector2[] statMinimums =
            {
                new(0.000f, 0.715f),
                new(0.508f, 0.715f),
                new(0.000f, 0.560f),
                new(0.508f, 0.560f)
            };
            Vector2[] statMaximums =
            {
                new(0.492f, 0.850f),
                new(1.000f, 0.850f),
                new(0.492f, 0.695f),
                new(1.000f, 0.695f)
            };
            for (int index = 0; index < statNames.Length; index += 1)
            {
                AddRunHistoryStatBox(
                    content,
                    index,
                    statNames[index],
                    statValues[index],
                    statMinimums[index],
                    statMaximums[index]);
            }

            RectTransform cause = AddRunStatusContentBox(
                content,
                "운명 기록 종료 원인",
                new Vector2(0.000f, 0.350f),
                new Vector2(1.000f, 0.540f),
                statusInnerPanelFrameSprite);
            AddRunHistorySectionTitle(
                cause,
                L("runHistory.summary.cause"));
            AddRunHistoryBoxText(
                cause,
                "운명 기록 종료 원인 내용",
                ResolveRunHistoryEnding(entry),
                20,
                TextAnchor.MiddleCenter,
                new Vector2(0.120f, 0.120f),
                new Vector2(0.880f, 0.600f));

            RectTransform deck = AddRunStatusContentBox(
                content,
                "운명 기록 최종 덱",
                new Vector2(0.000f, 0.105f),
                new Vector2(0.492f, 0.330f),
                statusInnerPanelFrameSprite);
            AddRunHistorySectionTitle(
                deck,
                L("runHistory.summary.deck"));
            AddRunHistoryBoxText(
                deck,
                "운명 기록 최종 덱 내용",
                BuildRunHistoryDeckPreview(entry.FinalDeckCardIds),
                20,
                TextAnchor.UpperLeft,
                new Vector2(0.120f, 0.120f),
                new Vector2(0.880f, 0.600f));

            RectTransform loadout = AddRunStatusContentBox(
                content,
                "운명 기록 유물 변칙",
                new Vector2(0.508f, 0.105f),
                new Vector2(1.000f, 0.330f),
                statusInnerPanelFrameSprite);
            AddRunHistorySectionTitle(
                loadout,
                L("runHistory.summary.loadout"));
            AddRunHistoryBoxText(
                loadout,
                "운명 기록 유물 변칙 내용",
                BuildRunHistoryLoadoutPreview(entry),
                20,
                TextAnchor.UpperLeft,
                new Vector2(0.120f, 0.120f),
                new Vector2(0.880f, 0.600f));

            AddRunStatusTextButton(
                content,
                "운명 기록 상세 보기",
                L("runHistory.summary.detail"),
                new Vector2(0.000f, 0.000f),
                new Vector2(1.000f, 0.085f),
                ShowSelectedRunHistoryDetail,
                22);
        }

        private void AddRunHistoryStatBox(
            RectTransform parent,
            int index,
            string label,
            int value,
            Vector2 minimum,
            Vector2 maximum)
        {
            RectTransform stat = AddRunStatusContentBox(
                parent,
                $"운명 기록 통계 {index}",
                minimum,
                maximum,
                statusItemSlotFrameSprite != null
                    ? statusItemSlotFrameSprite
                    : GetRunStatusSlotFrameSprite());
            Text name = AddText(
                stat,
                $"운명 기록 통계 {index} 이름",
                label,
                20,
                TextAnchor.MiddleCenter,
                new Color(0.72f, 1f, 0.96f, 1f));
            name.fontStyle = FontStyle.Bold;
            name.resizeTextForBestFit = false;
            name.raycastTarget = false;
            AddTextGlow(
                name,
                new Color(0f, 0f, 0f, 0.88f),
                new Color(0.08f, 0.62f, 0.58f, 0.34f),
                new Vector2(0.9f, -1.0f));
            SetAnchors(
                name.rectTransform,
                new Vector2(0.140f, 0.200f),
                new Vector2(0.520f, 0.800f));

            Text amount = AddText(
                stat,
                $"운명 기록 통계 {index} 값",
                value.ToString(),
                28,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.92f, 0.76f, 1f));
            amount.fontStyle = FontStyle.Bold;
            amount.resizeTextForBestFit = false;
            amount.raycastTarget = false;
            AddTextGlow(
                amount,
                new Color(0f, 0f, 0f, 0.90f),
                new Color(0.54f, 0.42f, 0.24f, 0.46f),
                new Vector2(0.9f, -1.0f));
            SetAnchors(
                amount.rectTransform,
                new Vector2(0.560f, 0.140f),
                new Vector2(0.860f, 0.860f));
        }

        private void AddRunHistorySectionTitle(
            RectTransform parent,
            string label)
        {
            AddRunHistoryBoxText(
                parent,
                $"{parent.name} 제목",
                label,
                20,
                TextAnchor.MiddleCenter,
                new Vector2(0.120f, 0.620f),
                new Vector2(0.880f, 0.920f),
                true);
        }

        private void AddRunHistoryBoxText(
            RectTransform parent,
            string label,
            int fontSize,
            TextAnchor alignment,
            Vector2 minimum,
            Vector2 maximum,
            bool bold = false)
        {
            AddRunHistoryBoxText(
                parent,
                $"{parent.name} 텍스트",
                label,
                fontSize,
                alignment,
                minimum,
                maximum,
                bold);
        }

        private void AddRunHistoryBoxText(
            RectTransform parent,
            string objectName,
            string label,
            int fontSize,
            TextAnchor alignment,
            Vector2 minimum,
            Vector2 maximum,
            bool bold = false)
        {
            Text text = AddText(
                parent,
                objectName,
                label,
                fontSize,
                alignment,
                new Color(0.88f, 0.95f, 0.87f, 1f));
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, fontSize - 2);
            text.resizeTextMaxSize = fontSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 0.88f;
            text.raycastTarget = false;
            SetAnchors(text.rectTransform, minimum, maximum);
        }

        private string BuildRunHistoryListTitle(RunHistoryEntry entry)
        {
            return LF(
                "runHistory.list.mobileTitle",
                GetRunHistoryResultName(entry),
                FormatRunHistoryDate(entry.FinishedAtUnixSeconds));
        }

        private string BuildRunHistoryListMetadata(RunHistoryEntry entry)
        {
            return LF(
                "runHistory.list.mobileMetadata",
                GetRunHistoryClassName(entry.CharacterClass),
                GetRunHistoryDifficultyName(entry.Difficulty),
                entry.DoorsCleared,
                entry.BattlesDefeated);
        }

        private string GetRunHistoryResultName(RunHistoryEntry entry)
        {
            return L(entry.Victory
                ? "runHistory.result.victory"
                : "runHistory.result.defeat");
        }

        private string BuildRunHistoryDeckPreview(IEnumerable<string> cardIds)
        {
            List<IGrouping<string, string>> groups =
                (cardIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .GroupBy(id => id, StringComparer.Ordinal)
                .OrderBy(
                    group => GetRunHistoryCardName(group.Key),
                    StringComparer.Ordinal)
                .ToList();
            List<string> lines = groups
                .Take(3)
                .Select(group => LF(
                    "runHistory.detail.deckLine",
                    group.Count(),
                    GetRunHistoryCardName(group.Key)))
                .ToList();
            int remaining = groups.Count - lines.Count;
            if (remaining > 0)
            {
                lines.Add(LF("runHistory.summary.moreCards", remaining));
            }

            return lines.Count > 0
                ? string.Join("\n", lines)
                : L("runHistory.none");
        }

        private string BuildRunHistoryLoadoutPreview(RunHistoryEntry entry)
        {
            string items = JoinOrNone(entry.EquippedItemIds
                .Select(GetRunHistoryItemName)
                .Take(3));
            string mutations = JoinOrNone(entry.ActiveMutationIds
                .Select(GetRunHistoryMutationName)
                .Take(2));
            return LF(
                "runHistory.summary.loadoutBody",
                items,
                mutations);
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

            RectTransform detailSafeRoot = AddPanel(
                outer,
                "운명 기록 상세 안전영역",
                new Color(1f, 1f, 1f, 0f));
            detailSafeRoot.GetComponent<Image>().raycastTarget = false;
            detailSafeRoot.gameObject.AddComponent<RectMask2D>();
            SetAnchors(detailSafeRoot, PcUiLayoutPolicy.StatusDetailBody);

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
                RenderRunHistoryLayout,
                18);

            RectTransform summaryPanel = AddRunStatusContentBox(
                detailSafeRoot,
                "운명 기록 상세 요약",
                new Vector2(0.000f, 0.000f),
                new Vector2(0.480f, 1.000f),
                statusInnerPanelFrameSprite);
            summaryPanel.GetComponent<Image>().raycastTarget = false;
            Text summary = AddText(
                summaryPanel,
                "운명 기록 상세 요약 텍스트",
                BuildRunHistoryDetailSummary(entry),
                18,
                TextAnchor.UpperLeft,
                new Color(0.88f, 0.95f, 0.88f, 1f));
            ConfigureRunHistoryDetailText(summary);

            RectTransform loadoutPanel = AddRunStatusContentBox(
                detailSafeRoot,
                "운명 기록 상세 덱과 아이템",
                new Vector2(0.520f, 0.000f),
                new Vector2(1.000f, 1.000f),
                statusInnerPanelFrameSprite);
            loadoutPanel.GetComponent<Image>().raycastTarget = false;
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
                PcUiLayoutPolicy.StatusFramedTextSafe);
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
