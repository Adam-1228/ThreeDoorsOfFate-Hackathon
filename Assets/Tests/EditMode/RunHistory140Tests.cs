using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class RunHistory140Tests
    {
        private const string EntryTypeName =
            "ThreeDoorsOfFate.Game.V140.RunHistoryEntry, Assembly-CSharp";
        private const string StoreTypeName =
            "ThreeDoorsOfFate.Game.V140.RunHistoryStore, Assembly-CSharp";
        private const string EpithetTypeName =
            "ThreeDoorsOfFate.Game.V140.RunHistoryEpithetPolicy, Assembly-CSharp";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private string keyPrefix;
        private string sentinelKey;

        [SetUp]
        public void SetUp()
        {
            keyPrefix = $"ThreeDoorsOfFate.Tests.RunHistory.{Guid.NewGuid():N}.";
            sentinelKey = keyPrefix + "UnrelatedProgress";
        }

        [TearDown]
        public void TearDown()
        {
            Type storeType = Type.GetType(StoreTypeName);
            if (storeType != null)
            {
                string storageKey = InvokeStatic(
                    storeType,
                    "GetStorageKey",
                    keyPrefix).ToString();
                PlayerPrefs.DeleteKey(storageKey);
            }

            PlayerPrefs.DeleteKey(sentinelKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void AppendKeepsNewestTenInNewestFirstOrder()
        {
            Type storeType = RequireType(StoreTypeName);
            for (int index = 0; index < 12; index += 1)
            {
                InvokeStatic(
                    storeType,
                    "Append",
                    keyPrefix,
                    CreateEntry(index.ToString(), index));
            }

            object[] entries = ReadEntries(storeType);
            Assert.That(entries, Has.Length.EqualTo(10));
            Assert.That(ReadMember(entries[0], "RunId"), Is.EqualTo("11"));
            Assert.That(ReadMember(entries[^1], "RunId"), Is.EqualTo("2"));
        }

        [Test]
        public void AppendReplacesTheSameRunInsteadOfDuplicatingIt()
        {
            Type storeType = RequireType(StoreTypeName);
            InvokeStatic(storeType, "Append", keyPrefix, CreateEntry("same", 10));
            object replacement = CreateEntry("same", 20);
            SetMember(replacement, "FinalGold", 777);
            InvokeStatic(storeType, "Append", keyPrefix, replacement);

            object[] entries = ReadEntries(storeType);
            Assert.That(entries, Has.Length.EqualTo(1));
            Assert.That(ReadMember(entries[0], "FinishedAtUnixSeconds"), Is.EqualTo(20L));
            Assert.That(ReadMember(entries[0], "FinalGold"), Is.EqualTo(777));
        }

        [Test]
        public void MalformedHistoryReturnsEmptyWithoutChangingOtherProgress()
        {
            Type storeType = RequireType(StoreTypeName);
            string storageKey = InvokeStatic(
                storeType,
                "GetStorageKey",
                keyPrefix).ToString();
            const string corruptJson = "{ definitely-not-history";
            PlayerPrefs.SetString(storageKey, corruptJson);
            PlayerPrefs.SetInt(sentinelKey, 140);
            PlayerPrefs.Save();

            Assert.That(ReadEntries(storeType), Is.Empty);
            Assert.That(PlayerPrefs.GetInt(sentinelKey), Is.EqualTo(140));
            Assert.That(PlayerPrefs.GetString(storageKey), Is.EqualTo(corruptJson));
        }

        [Test]
        public void RoundTripPreservesCountersAndFinalCollections()
        {
            Type storeType = RequireType(StoreTypeName);
            object entry = CreateEntry("round-trip", 140);
            SetMember(entry, "CardsPlayed", 61);
            SetMember(entry, "DamageDealt", 345);
            SetMember(entry, "DamageTaken", 89);
            SetMember(entry, "CardsRemoved", 4);
            SetMember(entry, "FinalDeckCardIds", new List<string> { "card_a", "card_a", "card_b" });
            SetMember(entry, "EquippedItemIds", new List<string> { "relic_a" });
            SetMember(entry, "ActiveMutationIds", new List<string> { "abyss.compound_interest" });
            SetMember(entry, "NewAchievementNames", new List<string> { "Achievement" });

            InvokeStatic(storeType, "Append", keyPrefix, entry);
            object restored = ReadEntries(storeType).Single();

            Assert.That(ReadMember(restored, "CardsPlayed"), Is.EqualTo(61));
            Assert.That(ReadMember(restored, "DamageDealt"), Is.EqualTo(345));
            Assert.That(ReadMember(restored, "DamageTaken"), Is.EqualTo(89));
            Assert.That(ReadMember(restored, "CardsRemoved"), Is.EqualTo(4));
            Assert.That(
                ((IEnumerable)ReadMember(restored, "FinalDeckCardIds"))
                    .Cast<object>()
                    .Select(value => value.ToString()),
                Is.EqualTo(new[] { "card_a", "card_a", "card_b" }));
        }

        [Test]
        public void ComicEpithetsAreLocalKeysWithoutScoresOrRewards()
        {
            object entry = CreateEntry("comic", 140);
            SetMember(entry, "MaximumSameRerollStreak", 3);
            SetMember(entry, "ZeroGoldShopVisits", 1);
            SetMember(entry, "LowLuckRolls", 6);
            SetMember(entry, "FinalDebt", 9);
            SetMember(entry, "CardsPlayed", 60);
            SetMember(entry, "DoorsCleared", 20);

            string[] keys = ((IEnumerable)InvokeStatic(
                    RequireType(EpithetTypeName),
                    "Get",
                    entry))
                .Cast<object>()
                .Select(value => value.ToString())
                .ToArray();

            Assert.That(keys, Does.Contain("runHistory.epithet.sameAgain"));
            Assert.That(keys, Does.Contain("runHistory.epithet.windowShopper"));
            Assert.That(keys, Does.Contain("runHistory.epithet.unlucky"));
            Assert.That(keys, Does.Contain("runHistory.epithet.debtMagnet"));
            Assert.That(keys, Does.Contain("runHistory.epithet.deckWhisperer"));
            Assert.That(keys, Does.Contain("runHistory.epithet.noDoorEnough"));
            Assert.That(
                entry.GetType().GetMember("Points"),
                Is.Empty,
                "Comic epithets must not become scored achievements.");
            Assert.That(entry.GetType().GetMember("Reward"), Is.Empty);
        }

        [Test]
        public void ControllerRecordsCountersCollectionsAndCauseOnlyOnce()
        {
            GameLocalization.Initialize(SystemLanguage.English);
            EventSystem originalEventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            GameObject host = new("Run History 1.4 Controller Test");
            Component controller = null;
            try
            {
                controller = host.AddComponent(RequireType(ControllerTypeName));
                SetControllerField(controller, "runHistoryKeyPrefix", keyPrefix);
                SetControllerField(controller, "activeRunId", "controller-run");
                SetControllerField(controller, "runStartedAtUnixSeconds", 100L);
                SetControllerEnum(controller, "phase", "Combat");
                SetControllerEnum(controller, "selectedClass", "Oracle");
                SetControllerEnum(controller, "currentDifficulty", "Hard");
                SetControllerField(controller, "playerHealth", 0);
                SetControllerField(controller, "playerMaxHealth", 60);
                SetControllerField(controller, "gold", 12);
                SetControllerField(controller, "debt", 9);
                SetControllerField(controller, "roomsCleared", 13);
                SetControllerField(controller, "combatEncountersCompleted", 6);
                SetControllerField(controller, "runHistoryCardsPlayed", 7);
                SetControllerField(controller, "runHistoryDamageDealt", 99);
                SetControllerField(controller, "runHistoryDamageTaken", 44);
                SetControllerField(controller, "runHistoryBossesDefeated", 1);
                SetControllerField(controller, "runHistoryZeroGoldShopVisits", 1);
                SetControllerField(controller, "runHistoryMaximumSameRerollStreak", 3);
                SetControllerField(controller, "runHistoryLowLuckRolls", 6);
                ((IList)ReadControllerField(controller, "equippedRunItemIds"))
                    .Add("relic_fate_coin");
                ((ISet<string>)ReadControllerField(
                    controller,
                    "activeEndlessMutationIds"))
                    .Add("abyss.compound_interest");
                ((IList)ReadControllerField(
                    controller,
                    "newlyCompletedAchievementNames"))
                    .Add("Test Achievement");

                InvokeController(
                    controller,
                    "RecordGameOverRunHistory",
                    false,
                    "동굴이 또 하나의 이름을 삼켰습니다.");
                InvokeController(
                    controller,
                    "RecordGameOverRunHistory",
                    false,
                    "동굴이 또 하나의 이름을 삼켰습니다.");

                object[] entries = ReadEntries(RequireType(StoreTypeName));
                Assert.That(entries, Has.Length.EqualTo(1));
                object entry = entries[0];
                Assert.That(ReadMember(entry, "RunId"), Is.EqualTo("controller-run"));
                Assert.That(ReadMember(entry, "EndingCauseKey"), Is.EqualTo("gameOver.default"));
                Assert.That(ReadMember(entry, "CardsPlayed"), Is.EqualTo(7));
                Assert.That(ReadMember(entry, "DamageDealt"), Is.EqualTo(99));
                Assert.That(ReadMember(entry, "DamageTaken"), Is.EqualTo(44));
                Assert.That(ReadMember(entry, "BossesDefeated"), Is.EqualTo(1));
                Assert.That(
                    ((IEnumerable)ReadMember(entry, "EquippedItemIds"))
                        .Cast<object>()
                        .Select(value => value.ToString()),
                    Does.Contain("relic_fate_coin"));
            }
            finally
            {
                DestroyController(host, controller, originalEventSystem);
                GameLocalization.Initialize(Application.systemLanguage);
            }
        }

        [Test]
        public void HistoryListAndDetailPanelsStayInsideTheirOuterFrame()
        {
            bool hadLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            string previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            EventSystem originalEventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            GameObject host = new("Run History 1.4 Layout Test");
            Component controller = null;
            try
            {
                object layoutEntry = CreateEntry("layout", 140);
                SetMember(
                    layoutEntry,
                    "EquippedItemIds",
                    new List<string> { "relic_fate_coin" });
                InvokeStatic(
                    RequireType(StoreTypeName),
                    "Append",
                    keyPrefix,
                    layoutEntry);
                controller = host.AddComponent(RequireType(ControllerTypeName));
                GameLocalization.SetLanguage(GameLanguage.English);
                SetControllerField(controller, "runHistoryKeyPrefix", keyPrefix);
                TextAsset modifierCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/Data/RunModifiers/run_modifier_catalog.json");
                Assert.That(modifierCatalog, Is.Not.Null);
                SetControllerField(
                    controller,
                    "runModifierCatalog",
                    modifierCatalog);
                Sprite innerPanelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_inner_panel_frame_ai.png");
                Assert.That(innerPanelFrame, Is.Not.Null);
                SetControllerField(
                    controller,
                    "statusInnerPanelFrameSprite",
                    innerPanelFrame);
                Sprite innerHeaderFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_inner_header_frame_ai.png");
                Sprite itemSlotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_item_slot_frame_ai.png");
                Sprite wideFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_section_wide_frame_v2.png");
                Sprite buttonFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_class_confirm_button_frame.png");
                Assert.That(innerHeaderFrame, Is.Not.Null);
                Assert.That(itemSlotFrame, Is.Not.Null);
                Assert.That(wideFrame, Is.Not.Null);
                Assert.That(buttonFrame, Is.Not.Null);
                SetControllerField(
                    controller,
                    "statusInnerHeaderFrameSprite",
                    innerHeaderFrame);
                SetControllerField(
                    controller,
                    "statusItemSlotFrameSprite",
                    itemSlotFrame);
                SetControllerField(
                    controller,
                    "statusSectionWideFrameSprite",
                    wideFrame);
                SetControllerField(
                    controller,
                    "classConfirmButtonSprite",
                    buttonFrame);
                if (ReadControllerField(controller, "root") == null)
                {
                    InvokeController(controller, "BuildShell");
                }

                InvokeController(controller, "ShowRunHistory");
                RectTransform root = (RectTransform)ReadControllerField(
                    controller,
                    "root");
                RectTransform outer = FindDescendant(
                    root,
                    "운명 기록 외곽 프레임");
                RectTransform historySafeRoot = FindDescendant(
                    root,
                    "운명 기록 안전영역");
                RectTransform listPanel = FindDescendant(
                    root,
                    "운명 기록 목록 패널");
                RectTransform listContent = FindDescendant(
                    root,
                    "운명 기록 목록 내용 안전영역");
                RectTransform selectionSummary = FindDescendant(
                    root,
                    "운명 기록 선택 요약");
                RectTransform summaryContent = FindDescendant(
                    root,
                    "운명 기록 선택 요약 안전영역");
                RectTransform cause = FindDescendant(
                    root,
                    "운명 기록 종료 원인");
                RectTransform deckPreview = FindDescendant(
                    root,
                    "운명 기록 최종 덱");
                RectTransform loadoutPreview = FindDescendant(
                    root,
                    "운명 기록 유물 변칙");
                RectTransform row = FindDescendant(root, "운명 기록 항목 0");
                Assert.That(outer, Is.Not.Null);
                Assert.That(historySafeRoot, Is.Not.Null);
                Assert.That(listPanel, Is.Not.Null);
                Assert.That(listContent, Is.Not.Null);
                Assert.That(selectionSummary, Is.Not.Null);
                Assert.That(summaryContent, Is.Not.Null);
                Assert.That(cause, Is.Not.Null);
                Assert.That(deckPreview, Is.Not.Null);
                Assert.That(loadoutPreview, Is.Not.Null);
                Assert.That(row, Is.Not.Null);
                AssertDecorativeFrameSafeRoot(historySafeRoot, outer);
                AssertInside(listPanel, historySafeRoot);
                AssertInside(selectionSummary, historySafeRoot);
                AssertInside(row, listContent);
                AssertInside(cause, summaryContent);
                AssertInside(deckPreview, summaryContent);
                AssertInside(loadoutPreview, summaryContent);
                Assert.That(
                    FindDescendant(listPanel, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    FindDescendant(selectionSummary, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    FindDescendant(cause, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    FindDescendant(deckPreview, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    FindDescendant(loadoutPreview, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    listPanel.anchorMax.x,
                    Is.LessThan(selectionSummary.anchorMin.x));
                Assert.That(
                    deckPreview.anchorMax.x,
                    Is.LessThan(loadoutPreview.anchorMin.x));
                Assert.That(
                    deckPreview.anchorMax.y,
                    Is.LessThan(cause.anchorMin.y));
                Assert.That(
                    loadoutPreview.anchorMax.y,
                    Is.LessThan(cause.anchorMin.y));

                InvokeController(controller, "ShowRunHistoryDetail", 0);
                RectTransform detailOuter = FindDescendant(
                    root,
                    "운명 기록 상세 외곽 프레임");
                RectTransform detailSafeRoot = FindDescendant(
                    root,
                    "운명 기록 상세 안전영역");
                RectTransform summary = FindDescendant(
                    root,
                    "운명 기록 상세 요약");
                RectTransform summaryText = FindDescendant(
                    root,
                    "운명 기록 상세 요약 텍스트");
                RectTransform loadout = FindDescendant(
                    root,
                    "운명 기록 상세 덱과 아이템");
                RectTransform loadoutText = FindDescendant(
                    root,
                    "운명 기록 상세 덱과 아이템 텍스트");
                Assert.That(detailOuter, Is.Not.Null);
                Assert.That(detailSafeRoot, Is.Not.Null);
                Assert.That(summary, Is.Not.Null);
                Assert.That(summaryText, Is.Not.Null);
                Assert.That(loadout, Is.Not.Null);
                Assert.That(loadoutText, Is.Not.Null);
                AssertDecorativeFrameSafeRoot(detailSafeRoot, detailOuter);
                AssertInside(summary, detailSafeRoot);
                AssertInside(loadout, detailSafeRoot);
                AssertFramedTextSafe(summaryText, summary);
                AssertFramedTextSafe(loadoutText, loadout);
                Assert.That(
                    FindDescendant(summary, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    FindDescendant(loadout, "생성 투명 프레임"),
                    Is.Not.Null);
                Assert.That(
                    summary.anchorMax.x,
                    Is.LessThanOrEqualTo(loadout.anchorMin.x));
                Assert.That(
                    loadoutText.GetComponent<Text>().text,
                    Does.Contain("Coin of Fate"));
            }
            finally
            {
                DestroyController(host, controller, originalEventSystem);
                if (hadLanguage)
                {
                    PlayerPrefs.SetString(
                        GameLanguagePolicy.PreferenceKey,
                        previousLanguage);
                }
                else
                {
                    PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
                }

                PlayerPrefs.Save();
                GameLocalization.Initialize(Application.systemLanguage);
            }
        }

        [Test]
        public void HistoryOverviewUsesMobileReadableRowsStatsAndSafeBodyText()
        {
            bool hadLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            string previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            EventSystem originalEventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            GameObject host = new("Run History 1.4 Mobile Readability Test");
            Component controller = null;
            try
            {
                for (int index = 0; index < 4; index += 1)
                {
                    object entry = CreateEntry($"mobile-{index}", 140 + index);
                    SetMember(entry, "DoorsCleared", 5 + index);
                    SetMember(entry, "BattlesDefeated", 1 + index);
                    SetMember(entry, "FinalGold", 39 + index);
                    SetMember(entry, "FinalDebt", index);
                    SetMember(
                        entry,
                        "FinalDeckCardIds",
                        new List<string>
                        {
                            "card_worn_dagger",
                            "card_worn_shield",
                            "card_heavy_blow",
                            "card_evade",
                            "card_reroll"
                        });
                    SetMember(
                        entry,
                        "EquippedItemIds",
                        new List<string> { "relic_fate_coin" });
                    InvokeStatic(
                        RequireType(StoreTypeName),
                        "Append",
                        keyPrefix,
                        entry);
                }

                controller = host.AddComponent(RequireType(ControllerTypeName));
                GameLocalization.SetLanguage(GameLanguage.Korean);
                SetControllerField(controller, "runHistoryKeyPrefix", keyPrefix);
                TextAsset modifierCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/Data/RunModifiers/run_modifier_catalog.json");
                Sprite innerPanelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_inner_panel_frame_ai.png");
                Sprite innerHeaderFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_inner_header_frame_ai.png");
                Sprite itemSlotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_item_slot_frame_ai.png");
                Sprite wideFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_status_section_wide_frame_v2.png");
                Sprite buttonFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Art/UI/GeneratedFrames/ui_class_confirm_button_frame.png");
                Assert.That(modifierCatalog, Is.Not.Null);
                Assert.That(innerPanelFrame, Is.Not.Null);
                Assert.That(innerHeaderFrame, Is.Not.Null);
                Assert.That(itemSlotFrame, Is.Not.Null);
                Assert.That(wideFrame, Is.Not.Null);
                Assert.That(buttonFrame, Is.Not.Null);
                SetControllerField(controller, "runModifierCatalog", modifierCatalog);
                SetControllerField(
                    controller,
                    "statusInnerPanelFrameSprite",
                    innerPanelFrame);
                SetControllerField(
                    controller,
                    "statusInnerHeaderFrameSprite",
                    innerHeaderFrame);
                SetControllerField(
                    controller,
                    "statusItemSlotFrameSprite",
                    itemSlotFrame);
                SetControllerField(
                    controller,
                    "statusSectionWideFrameSprite",
                    wideFrame);
                SetControllerField(
                    controller,
                    "classConfirmButtonSprite",
                    buttonFrame);
                if (ReadControllerField(controller, "root") == null)
                {
                    InvokeController(controller, "BuildShell");
                }

                InvokeController(controller, "ShowRunHistory");
                RectTransform root = (RectTransform)ReadControllerField(
                    controller,
                    "root");

                RectTransform firstRow = FindDescendant(
                    root,
                    "운명 기록 항목 0");
                RectTransform secondRow = FindDescendant(
                    root,
                    "운명 기록 항목 1");
                RectTransform thirdRow = FindDescendant(
                    root,
                    "운명 기록 항목 2");
                Assert.That(firstRow, Is.Not.Null);
                Assert.That(secondRow, Is.Not.Null);
                Assert.That(thirdRow, Is.Not.Null);
                Assert.That(
                    FindDescendant(root, "운명 기록 항목 3"),
                    Is.Null,
                    "A phone-sized page must show at most three readable rows.");

                RectTransform firstRowFrame = FindDescendant(
                    firstRow,
                    "생성 투명 프레임");
                Assert.That(firstRowFrame, Is.Not.Null);
                Assert.That(
                    firstRowFrame.GetComponent<Image>().sprite,
                    Is.SameAs(innerHeaderFrame),
                    "Shallow history rows must use the matching shallow header frame.");

                Text rowTitle = FindDescendant(
                    firstRow,
                    "운명 기록 항목 0 제목")?.GetComponent<Text>();
                Text rowMetadata = FindDescendant(
                    firstRow,
                    "운명 기록 항목 0 정보")?.GetComponent<Text>();
                AssertMobileReadableText(rowTitle, 24, 20);
                AssertMobileReadableText(rowMetadata, 20, 18);
                AssertFramedTextSafe(rowTitle.rectTransform, firstRow);
                AssertFramedTextSafe(rowMetadata.rectTransform, firstRow);
                Assert.That(
                    rowMetadata.rectTransform.anchorMax.y,
                    Is.LessThanOrEqualTo(rowTitle.rectTransform.anchorMin.y));

                RectTransform[] stats = new RectTransform[4];
                for (int index = 0; index < stats.Length; index += 1)
                {
                    stats[index] = FindDescendant(
                        root,
                        $"운명 기록 통계 {index}");
                    Assert.That(stats[index], Is.Not.Null);
                    Text statName = FindDescendant(
                        stats[index],
                        $"운명 기록 통계 {index} 이름")?.GetComponent<Text>();
                    Text statValue = FindDescendant(
                        stats[index],
                        $"운명 기록 통계 {index} 값")?.GetComponent<Text>();
                    AssertMobileReadableText(statName, 20, 20);
                    AssertMobileReadableText(statValue, 28, 28);
                    AssertFramedTextSafe(statName.rectTransform, stats[index]);
                    AssertFramedTextSafe(statValue.rectTransform, stats[index]);
                }

                Assert.That(stats[0].anchorMax.x, Is.LessThan(stats[1].anchorMin.x));
                Assert.That(stats[2].anchorMax.x, Is.LessThan(stats[3].anchorMin.x));
                Assert.That(stats[2].anchorMax.y, Is.LessThan(stats[0].anchorMin.y));
                Assert.That(stats[3].anchorMax.y, Is.LessThan(stats[1].anchorMin.y));

                Text causeBody = FindDescendant(
                    root,
                    "운명 기록 종료 원인 내용")?.GetComponent<Text>();
                Text causeTitle = FindDescendant(
                    root,
                    "운명 기록 종료 원인 제목")?.GetComponent<Text>();
                Text deckBody = FindDescendant(
                    root,
                    "운명 기록 최종 덱 내용")?.GetComponent<Text>();
                Text loadoutBody = FindDescendant(
                    root,
                    "운명 기록 유물 변칙 내용")?.GetComponent<Text>();
                RectTransform causeBox = FindDescendant(
                    root,
                    "운명 기록 종료 원인");
                AssertMobileReadableText(causeTitle, 20, 18);
                AssertMobileReadableText(causeBody, 20, 18);
                AssertMobileReadableText(deckBody, 20, 18);
                AssertMobileReadableText(loadoutBody, 20, 18);
                AssertFramedTextSafe(
                    causeBody.rectTransform,
                    causeBox);
                AssertFramedTextSafe(
                    deckBody.rectTransform,
                    FindDescendant(root, "운명 기록 최종 덱"));
                AssertFramedTextSafe(
                    loadoutBody.rectTransform,
                    FindDescendant(root, "운명 기록 유물 변칙"));
                Canvas.ForceUpdateCanvases();
                Assert.That(
                    causeTitle.rectTransform.rect.height,
                    Is.GreaterThanOrEqualTo(causeTitle.resizeTextMinSize),
                    "The ending-cause title needs enough vertical room to render instead of truncating away.");
                Assert.That(
                    causeBox.anchorMax.y - causeBox.anchorMin.y,
                    Is.GreaterThanOrEqualTo(0.18f),
                    "The ending-cause box needs enough compact-viewport height for both its title and body.");
                Assert.That(
                    causeTitle.rectTransform.anchorMax.y
                    - causeTitle.rectTransform.anchorMin.y,
                    Is.GreaterThanOrEqualTo(0.30f),
                    "The ending-cause title band must remain visible in the compact macOS/mobile preview.");
                Assert.That(
                    causeBody.rectTransform.anchorMax.y,
                    Is.LessThanOrEqualTo(
                        causeTitle.rectTransform.anchorMin.y - 0.02f),
                    "The ending-cause body must not collide with its title band.");
                Assert.That(deckBody.text.Split('\n'), Has.Length.LessThanOrEqualTo(4));
                Assert.That(deckBody.text, Does.Contain("외 2종"));
            }
            finally
            {
                DestroyController(host, controller, originalEventSystem);
                if (hadLanguage)
                {
                    PlayerPrefs.SetString(
                        GameLanguagePolicy.PreferenceKey,
                        previousLanguage);
                }
                else
                {
                    PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
                }

                PlayerPrefs.Save();
                GameLocalization.Initialize(Application.systemLanguage);
            }
        }

        private object[] ReadEntries(Type storeType)
        {
            return ((IEnumerable)InvokeStatic(storeType, "Read", keyPrefix))
                .Cast<object>()
                .ToArray();
        }

        private static object CreateEntry(string runId, long finishedAt)
        {
            object entry = Activator.CreateInstance(RequireType(EntryTypeName));
            SetMember(entry, "RunId", runId);
            SetMember(entry, "GameVersion", "1.4.0");
            SetMember(entry, "FinishedAtUnixSeconds", finishedAt);
            SetMember(entry, "CharacterClass", "Gambler");
            SetMember(entry, "Difficulty", "Hard");
            SetMember(entry, "EndingKind", "death");
            SetMember(entry, "EndingCauseKey", "gameOver.default");
            SetMember(entry, "FinalMaxHealth", 70);
            return entry;
        }

        private static object InvokeStatic(
            Type type,
            string methodName,
            params object[] values)
        {
            MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == values.Length);
            return method.Invoke(null, values);
        }

        private static object InvokeController(
            object controller,
            string methodName,
            params object[] values)
        {
            MethodInfo method = controller.GetType()
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == values.Length);
            return method.Invoke(controller, values);
        }

        private static object ReadControllerField(
            object controller,
            string fieldName)
        {
            FieldInfo field = controller.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            return field.GetValue(controller);
        }

        private static void SetControllerField(
            object controller,
            string fieldName,
            object value)
        {
            FieldInfo field = controller.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(controller, value);
        }

        private static void SetControllerEnum(
            object controller,
            string fieldName,
            string value)
        {
            FieldInfo field = controller.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private static void DestroyController(
            GameObject host,
            Component controller,
            EventSystem originalEventSystem)
        {
            RectTransform canvasRoot = controller == null
                ? null
                : ReadControllerField(controller, "canvasRoot") as RectTransform;
            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(host);
            if (originalEventSystem == null)
            {
                EventSystem created =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }
        }

        private static RectTransform FindDescendant(
            RectTransform parent,
            string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index += 1)
            {
                RectTransform child = parent.GetChild(index) as RectTransform;
                RectTransform found = FindDescendant(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void AssertInside(
            RectTransform child,
            RectTransform parent)
        {
            Assert.That(child.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(child.anchorMin.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(child.anchorMax.x, Is.LessThanOrEqualTo(1f));
            Assert.That(child.anchorMax.y, Is.LessThanOrEqualTo(1f));
            Assert.That(child.parent, Is.SameAs(parent));
        }

        private static void AssertDecorativeFrameSafeRoot(
            RectTransform child,
            RectTransform parent)
        {
            AssertInside(child, parent);
            Assert.That(child.anchorMin.x, Is.GreaterThanOrEqualTo(0.08f));
            Assert.That(child.anchorMin.y, Is.GreaterThanOrEqualTo(0.14f));
            Assert.That(child.anchorMax.x, Is.LessThanOrEqualTo(0.92f));
            Assert.That(child.anchorMax.y, Is.LessThanOrEqualTo(0.80f));
        }

        private static void AssertFramedTextSafe(
            RectTransform text,
            RectTransform frame)
        {
            AssertInside(text, frame);
            Assert.That(text.anchorMin.x, Is.GreaterThanOrEqualTo(0.10f));
            Assert.That(text.anchorMin.y, Is.GreaterThanOrEqualTo(0.10f));
            Assert.That(text.anchorMax.x, Is.LessThanOrEqualTo(0.90f));
            Assert.That(text.anchorMax.y, Is.LessThanOrEqualTo(0.90f));
        }

        private static void AssertMobileReadableText(
            Text text,
            int minimumFontSize,
            int minimumBestFitSize)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.fontSize, Is.GreaterThanOrEqualTo(minimumFontSize));
            if (text.resizeTextForBestFit)
            {
                Assert.That(
                    text.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(minimumBestFitSize));
            }
        }

        private static object ReadMember(object instance, string memberName)
        {
            FieldInfo field = instance.GetType().GetField(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            PropertyInfo property = instance.GetType().GetProperty(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, memberName);
            return property.GetValue(instance);
        }

        private static void SetMember(object instance, string memberName, object value)
        {
            FieldInfo field = instance.GetType().GetField(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(instance, value);
                return;
            }

            PropertyInfo property = instance.GetType().GetProperty(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, memberName);
            property.SetValue(instance, value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, typeName);
            return type;
        }
    }
}
