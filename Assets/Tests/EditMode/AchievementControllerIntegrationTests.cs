using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class AchievementControllerIntegrationTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string Prefix = PlayerPrefsProgressStore.ProductionPrefix;

        private static readonly string[] StringKeys =
        {
            "ThreeDoorsOfFate.DiscoveredItems.Gambler",
            "ThreeDoorsOfFate.DiscoveredItems.Oracle",
            "ThreeDoorsOfFate.DiscoveredItems.Exile",
            "ThreeDoorsOfFate.EquippedItems.Gambler",
            "ThreeDoorsOfFate.EquippedItems.Oracle",
            "ThreeDoorsOfFate.EquippedItems.Exile",
            "ThreeDoorsOfFate.HardRunSave"
        };

        private Type controllerType;
        private Type cardType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private readonly List<UnityEngine.Object> createdObjects = new();
        private readonly Dictionary<string, int> savedIntegers = new();
        private readonly Dictionary<string, string> savedStrings = new();
        private readonly HashSet<string> existingIntegerKeys = new();
        private readonly HashSet<string> existingStringKeys = new();
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(
                GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "ko");
            GameLocalization.Initialize(SystemLanguage.Korean);

            SnapshotAndClearPlayerPrefs();
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");
            Assert.That(cardType, Is.Not.Null, "Runtime card type must compile.");

            controllerHost = new GameObject("Achievement Controller Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = TryGetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = TryGetField<RectTransform>("root");
            }

            Assert.That(root, Is.Not.Null, "Controller must build its runtime UI shell.");
            canvasRoot = TryGetField<RectTransform>("canvasRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            if (controllerHost != null)
            {
                UnityEngine.Object.DestroyImmediate(controllerHost);
            }

            foreach (UnityEngine.Object createdObject in createdObjects)
            {
                if (createdObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObject);
                }
            }

            if (originalEventSystem == null)
            {
                EventSystem createdEventSystem =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdEventSystem.gameObject);
                }
            }

            RestorePlayerPrefs();
            RestoreLanguagePreference();
        }

        private void LegacyAchievementModal_PagesEightReadableCardsFourAtATime()
        {
            Sprite artworkFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GeneratedFrames/ui_status_inner_panel_frame_ai.png");
            Sprite cardFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GeneratedFrames/ui_status_category_card_frame.png");
            Sprite fallbackCardFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GeneratedFrames/ui_inner_panel_frame.png");
            Sprite ornateModalFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GeneratedFrames/ui_status_modal_frame_v2.png");
            Assert.That(artworkFrame, Is.Not.Null);
            Assert.That(cardFrame, Is.Not.Null);
            Assert.That(fallbackCardFrame, Is.Not.Null);
            Assert.That(ornateModalFrame, Is.Not.Null);
            SetField("panelSprite", fallbackCardFrame);
            SetField("statusInnerPanelFrameSprite", artworkFrame);
            SetField("statusCategoryCardFrameSprite", cardFrame);
            SetField("statusPanelFrameSprite", ornateModalFrame);

            Invoke("ShowMainMenu");
            RectTransform achievementButton = FindRequired(root, "업적");
            Assert.That(achievementButton.GetComponent<Button>(), Is.Not.Null);

            achievementButton.GetComponent<Button>().onClick.Invoke();
            Canvas.ForceUpdateCanvases();

            RectTransform overlay = GetField<RectTransform>("achievementOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(GetField<RectTransform>("contentRoot").gameObject.activeSelf, Is.False);

            List<RectTransform> cards = FindDescendants(overlay)
                .Where(candidate => candidate.name.StartsWith("업적 카드 ", StringComparison.Ordinal))
                .ToList();
            Assert.That(cards.Count, Is.EqualTo(4));

            string[] expectedFirstPage =
            {
                "심연으로 가는 문",
                "도박사의 진엔딩",
                "예언가의 진엔딩",
                "추방자의 진엔딩"
            };

            string[] visibleNames = cards
                .Select(card => FindRequired(card, "업적 제목").GetComponent<Text>().text)
                .ToArray();
            Assert.That(visibleNames, Is.EquivalentTo(expectedFirstPage));
            Assert.That(GetField<Text>("achievementPageText").text, Is.EqualTo("1 / 2"));
            Assert.That(GetField<Text>("achievementCompletionText").text, Is.EqualTo("달성 0/8"));
            Assert.That(GetField<Button>("achievementPreviousButton").interactable, Is.False);
            Assert.That(GetField<Button>("achievementNextButton").interactable, Is.True);

            GetField<Button>("achievementNextButton").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            cards = FindDescendants(overlay)
                .Where(candidate => candidate.name.StartsWith("업적 카드 ", StringComparison.Ordinal))
                .ToList();
            Assert.That(cards.Count, Is.EqualTo(4));
            string[] expectedSecondPage =
            {
                "심연의 수집가",
                "운명을 건 판돈",
                "세 문의 예언",
                "끊어진 맹세"
            };

            visibleNames = cards
                .Select(card => FindRequired(card, "업적 제목").GetComponent<Text>().text)
                .ToArray();
            Assert.That(visibleNames, Is.EquivalentTo(expectedSecondPage));
            Assert.That(GetField<Text>("achievementPageText").text, Is.EqualTo("2 / 2"));
            Assert.That(GetField<Button>("achievementPreviousButton").interactable, Is.True);
            Assert.That(GetField<Button>("achievementNextButton").interactable, Is.False);

            foreach (RectTransform card in cards)
            {
                Assert.That(
                    card.GetComponent<Image>().sprite,
                    Is.SameAs(cardFrame),
                    "Achievement cards need a wide frame with a clear center, not the ornamented modal frame.");
                RectTransform artworkSlot = FindRequired(card, "업적 이미지 슬롯");
                RectTransform artwork = FindRequired(card, "업적 이미지");
                RectTransform informationPanel = FindRequired(card, "업적 정보 패널");
                Assert.That(artworkSlot.GetComponent<Image>().sprite, Is.SameAs(artworkFrame));
                Assert.That(artwork.parent, Is.SameAs(artworkSlot));
                Assert.That(informationPanel.GetComponent<Image>().sprite, Is.SameAs(artworkFrame));
                Assert.That(informationPanel.parent, Is.SameAs(card));
                Text title = FindRequired(card, "업적 제목").GetComponent<Text>();
                Text description = FindRequired(card, "업적 설명").GetComponent<Text>();
                Text status = FindRequired(card, "업적 상태").GetComponent<Text>();
                Assert.That(title.transform.parent, Is.SameAs(informationPanel));
                Assert.That(description.transform.parent, Is.SameAs(informationPanel));
                Assert.That(status.transform.parent, Is.SameAs(informationPanel));
                AssertReadableText(title);
                AssertReadableText(description);
                AssertReadableText(status);
                Assert.That(description.resizeTextMinSize, Is.GreaterThanOrEqualTo(11));
                Assert.That(status.text, Does.Contain("100점"));
                Text progress = FindRequired(card, "업적 진행").GetComponent<Text>();
                Assert.That(progress.transform.parent, Is.SameAs(informationPanel));
                Assert.That(progress.text, Does.Match("^[0-9]+/[0-9]+$"));
                Assert.That(
                    progress.rectTransform.anchorMin.y,
                    Is.GreaterThanOrEqualTo(0.120f));
                Assert.That(
                    artworkSlot.anchorMax.x,
                    Is.LessThanOrEqualTo(informationPanel.anchorMin.x),
                    "Artwork and achievement information must occupy separate framed regions.");
            }
        }

        [Test]
        public void MainMenuAchievementModal_UsesTwentyRelicStyleSlotsTenAtATime()
        {
            Sprite slotFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GeneratedFrames/ui_status_section_medium_frame_v2.png");
            Sprite detailFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/GeneratedFrames/ui_status_inner_panel_frame_ai.png");
            Sprite selectionFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/Frames/selection_hover_frame.png");
            Assert.That(slotFrame, Is.Not.Null);
            Assert.That(detailFrame, Is.Not.Null);
            Assert.That(selectionFrame, Is.Not.Null);
            SetField("statusSectionMediumFrameSprite", slotFrame);
            SetField("statusInnerPanelFrameSprite", detailFrame);
            SetField("selectionFrameSprite", selectionFrame);
            AchievementProgress.Complete(Prefix, AchievementProgress.AbyssCollector);

            Invoke("ShowAchievements");
            Canvas.ForceUpdateCanvases();

            RectTransform grid = GetField<RectTransform>("achievementCardsRoot");
            List<RectTransform> slots = Enumerable.Range(0, grid.childCount)
                .Select(index => grid.GetChild(index) as RectTransform)
                .Where(candidate => candidate != null
                    && candidate.name.StartsWith("업적 슬롯 ", StringComparison.Ordinal))
                .ToList();
            Assert.That(slots, Has.Count.EqualTo(10));
            Assert.That(GetField<Text>("achievementPageText").text, Is.EqualTo("1 / 2"));
            Assert.That(GetField<Text>("achievementCompletionText").text, Is.EqualTo("달성 1/20"));
            Assert.That(slots.All(slot => slot.GetComponent<Image>().sprite == slotFrame), Is.True);

            RectTransform completed = slots.Single(slot =>
                FindDescendants(slot).Any(candidate =>
                    candidate.name == "업적 이름"
                    && candidate.GetComponent<Text>().text == "심연의 수집가"));
            Assert.That(completed.GetComponent<Button>().interactable, Is.True);
            completed.GetComponent<Button>().onClick.Invoke();
            Canvas.ForceUpdateCanvases();

            RectTransform detailRoot = GetField<RectTransform>("achievementDetailRoot");
            RectTransform detail = FindRequired(detailRoot, "업적 상세 패널");
            Assert.That(detail.GetComponent<Image>().sprite, Is.SameAs(detailFrame));
            Assert.That(
                FindRequired(detail, "업적 상세 제목").GetComponent<Text>().text,
                Does.Contain("심연의 수집가"));
            Assert.That(
                FindRequired(detail, "업적 상세 상태").GetComponent<Text>().text,
                Does.Contain("100점"));
            Assert.That(
                FindDescendants(completed).Single(candidate => candidate.name == "업적 선택 표시")
                    .GetComponent<Image>().sprite,
                Is.SameAs(selectionFrame));

            GetField<Button>("achievementNextButton").onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            slots = Enumerable.Range(0, grid.childCount)
                .Select(index => grid.GetChild(index) as RectTransform)
                .Where(candidate => candidate != null
                    && candidate.name.StartsWith("업적 슬롯 ", StringComparison.Ordinal))
                .ToList();
            Assert.That(slots, Has.Count.EqualTo(10));
            Assert.That(GetField<Text>("achievementPageText").text, Is.EqualTo("2 / 2"));
        }

        [Test]
        public void RunStatusMainPanel_KeepsOuterPanelsAndEquipmentSlotsInsideTheirFrames()
        {
            Invoke("ShowRunStatusPanel");
            Canvas.ForceUpdateCanvases();

            RectTransform statusWindow = FindRequired(root, "상태 확인 창");
            RectTransform outerFrame = FindRequired(root, "상태 확인 외곽 프레임");
            Assert.That(outerFrame.anchorMin, Is.EqualTo(new Vector2(0.025f, 0.075f)));
            Assert.That(outerFrame.anchorMax, Is.EqualTo(new Vector2(0.975f, 0.875f)));
            Assert.That(
                outerFrame.GetSiblingIndex(),
                Is.LessThan(statusWindow.GetSiblingIndex()),
                "The widened ornamental frame must render behind the unchanged inner layout.");
            Assert.That(statusWindow.anchorMin, Is.EqualTo(new Vector2(0.070f, 0.090f)));
            Assert.That(statusWindow.anchorMax, Is.EqualTo(new Vector2(0.930f, 0.860f)));
            Assert.That(statusWindow.GetComponent<Image>().color.a, Is.EqualTo(0f));
            string[] panelNames =
            {
                "보유 효과 패널",
                GameLocalization.Text("runStatus.section.cardSynergies"),
                GameLocalization.Text("runStatus.section.ownedCards"),
                GameLocalization.Text("runStatus.section.combatAwakenings"),
                GameLocalization.Text("runStatus.section.characterTraits")
            };
            foreach (string panelName in panelNames)
            {
                RectTransform panel = FindRequired(statusWindow, panelName);
                Assert.That(panel.anchorMin.x, Is.GreaterThanOrEqualTo(0.065f), panelName);
                Assert.That(panel.anchorMax.x, Is.LessThanOrEqualTo(0.935f), panelName);
                Assert.That(panel.anchorMin.y, Is.GreaterThanOrEqualTo(0.170f), panelName);
                Assert.That(panel.anchorMax.y, Is.LessThanOrEqualTo(0.820f), panelName);
            }

            RectTransform equipment = FindRequired(statusWindow, "보유 효과 패널");
            RectTransform equipmentHeader = FindRequired(equipment, "장착 아이템 헤더");
            Assert.That(equipmentHeader.anchorMin.x, Is.GreaterThanOrEqualTo(0.070f));
            Assert.That(equipmentHeader.anchorMax.x, Is.LessThanOrEqualTo(0.930f));

            List<RectTransform> slots = FindDescendants(equipment)
                .Where(candidate => candidate.name.StartsWith(
                    "장착 아이템 슬롯 ",
                    StringComparison.Ordinal))
                .ToList();
            Assert.That(slots.Count, Is.EqualTo(3));
            foreach (RectTransform slot in slots)
            {
                Assert.That(slot.anchorMin.x, Is.GreaterThanOrEqualTo(0.070f), slot.name);
                Assert.That(slot.anchorMax.x, Is.LessThanOrEqualTo(0.930f), slot.name);
            }
        }

        [Test]
        public void GeneratedAchievementArtwork_IsImportedAsSingleSprite()
        {
            string[] assetPaths =
            {
                "Assets/Resources/Achievements/achievement_abyss_collector.png",
                "Assets/Resources/Achievements/achievement_gambler_high_roll.png",
                "Assets/Resources/Achievements/achievement_oracle_rift_engine.png",
                "Assets/Resources/Achievements/achievement_exile_last_oath.png",
                "Assets/Resources/Achievements/achievement_gambler_card_reading.png",
                "Assets/Resources/Achievements/achievement_oracle_precise_prediction.png",
                "Assets/Resources/Achievements/achievement_exile_curse_eater.png",
                "Assets/Resources/Achievements/achievement_fate_cleaver_50.png",
                "Assets/Resources/Achievements/achievement_iron_wall_40.png",
                "Assets/Resources/Achievements/achievement_five_cards_turn.png",
                "Assets/Resources/Achievements/achievement_deck_50.png",
                "Assets/Resources/Achievements/achievement_cliffside_victory.png",
                "Assets/Resources/Achievements/achievement_triple_contract.png",
                "Assets/Resources/Achievements/achievement_build_masterpiece.png",
                "Assets/Resources/Achievements/achievement_twentieth_door.png",
                "Assets/Resources/Achievements/achievement_three_survivors.png"
            };

            foreach (string assetPath in assetPaths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null, assetPath);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), assetPath);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), assetPath);
                Assert.That(importer.mipmapEnabled, Is.False, assetPath);
                Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(assetPath), Is.Not.Null, assetPath);
            }
        }

        [Test]
        public void SavedCollections_DoNotCombineCharactersAndBackfillOneCompleteCharacter()
        {
            SetTestRunItemCatalog(30);
            PlayerPrefs.SetString(StringKeys[0], BuildItemSaveJson(Enumerable.Range(1, 10)));
            PlayerPrefs.SetString(StringKeys[1], BuildItemSaveJson(Enumerable.Range(11, 10)));
            PlayerPrefs.SetString(StringKeys[2], BuildItemSaveJson(Enumerable.Range(21, 10)));
            PlayerPrefs.Save();

            Invoke("TryCompleteAbyssCollectorFromSavedCharacters");
            Assert.That(
                AchievementProgress.IsCompleted(Prefix, AchievementProgress.AbyssCollector),
                Is.False,
                "Collections belonging to different characters must never be combined.");

            PlayerPrefs.SetString(StringKeys[0], BuildItemSaveJson(Enumerable.Range(1, 30)));
            PlayerPrefs.Save();
            Invoke("TryCompleteAbyssCollectorFromSavedCharacters");

            Assert.That(
                AchievementProgress.IsCompleted(Prefix, AchievementProgress.AbyssCollector),
                Is.True);
        }

        [Test]
        public void DiscoveringThirtiethItem_CompletesCollectorImmediately()
        {
            SetTestRunItemCatalog(30);
            IList definitions = (IList)InvokeWithResult("GetRunItemDefinitions");
            Assert.That(definitions.Count, Is.EqualTo(30));

            HashSet<string> discovered = GetField<HashSet<string>>("discoveredRunItemIds");
            for (int index = 0; index < definitions.Count - 1; index += 1)
            {
                discovered.Add(GetProperty<string>(definitions[index], "Id"));
            }

            bool changed = (bool)InvokeWithResult(
                "DiscoverRunItemForSelectedClass",
                definitions[definitions.Count - 1]);

            Assert.That(changed, Is.True);
            Assert.That(
                AchievementProgress.IsCompleted(Prefix, AchievementProgress.AbyssCollector),
                Is.True);
        }

        [Test]
        public void CompletingGamblerBuild_UnlocksOnlyItsAchievement()
        {
            AddBuildCards(
                "Gambler",
                "class_gambler_attack_wager_dagger",
                "class_gambler_defense_stake_shield",
                "class_gambler_skill_turn_the_table");

            Invoke("CheckBuildUnlocks");

            AssertCompleted("build.gambler_high_roll", true);
            AssertCompleted("build.oracle_rift_engine", false);
            AssertCompleted("build.exile_last_oath", false);
        }

        [Test]
        public void RestoringCompleteOracleBuild_BackfillsAchievement()
        {
            string[] cardIds =
            {
                "class_oracle_attack_constellation_cut",
                "class_oracle_defense_foreseen_barrier",
                "class_oracle_skill_three_door_omen"
            };
            AddBuildCards("Oracle", cardIds);
            PlayerPrefs.SetString(
                "ThreeDoorsOfFate.HardRunSave",
                "{\"version\":1,\"selectedClass\":2,\"currentDifficulty\":2,"
                + "\"currentJourneyEndingKind\":0,\"endlessModeActive\":false,"
                + "\"playerMaxHealth\":80,\"playerHealth\":80,\"luck\":3,"
                + "\"deckCardIds\":[\"" + string.Join("\",\"", cardIds) + "\"],"
                + "\"equippedItemIds\":[],\"combatLog\":[],\"buildUpgradeLevels\":[]}");
            PlayerPrefs.Save();

            Assert.That((bool)InvokeWithResult("TryLoadHardRunSave"), Is.True);
            AssertCompleted("build.oracle_rift_engine", true);
        }

        [Test]
        public void CombatCardMilestones_UseLiveStateAndTrackOnlyOnce()
        {
            SetEnumField("phase", "Combat");
            SetField("playerBlock", 39);
            HashSet<string> played = GetField<HashSet<string>>("cardsPlayedThisTurn");
            played.UnionWith(new[] { "a", "b", "c", "d" });

            InvokeWithResult("TryCompleteCombatCardAchievements", 49);
            AssertCompleted("combat.fate_cleaver_50", false);
            AssertCompleted("combat.iron_wall_40", false);
            AssertCompleted("combat.five_cards_turn", false);

            SetField("playerBlock", 40);
            played.Add("e");
            InvokeWithResult("TryCompleteCombatCardAchievements", 50);
            AssertCompleted("combat.fate_cleaver_50", true);
            AssertCompleted("combat.iron_wall_40", true);
            AssertCompleted("combat.five_cards_turn", true);

            List<string> tracked =
                GetField<List<string>>("newlyCompletedAchievementNames");
            Assert.That(tracked, Has.Count.EqualTo(3));
            InvokeWithResult("TryCompleteCombatCardAchievements", 99);
            Assert.That(tracked, Has.Count.EqualTo(3));
        }

        [Test]
        public void ExistingAwakeningSignals_CompleteTheirThreeAchievements()
        {
            SetField("gamblerCardReadingAwakened", true);
            SetField("oraclePrecisePredictionAwakened", true);
            SetField("exileCurseEaterAwakened", true);

            Invoke("TryCompleteCombatAwakeningAchievements");

            AssertCompleted("combat.gambler_card_reading", true);
            AssertCompleted("combat.oracle_precise_prediction", true);
            AssertCompleted("combat.exile_curse_eater", true);
        }

        [Test]
        public void DeckAndCliffsideChecks_RespectLiveBoundaries()
        {
            AddDeckCards(49);
            Invoke("TryCompleteDeckFiftyAchievement");
            AssertCompleted("collection.deck_50", false);

            AddDeckCards(1, false);
            Invoke("TryCompleteDeckFiftyAchievement");
            AssertCompleted("collection.deck_50", true);

            SetField("playerMaxHealth", 100);
            SetField("playerHealth", 21);
            Invoke("TryCompleteCliffsideVictoryAchievement");
            AssertCompleted("combat.cliffside_victory", false);

            SetField("playerHealth", 20);
            Invoke("TryCompleteCliffsideVictoryAchievement");
            AssertCompleted("combat.cliffside_victory", true);
        }

        private void SetTestRunItemCatalog(int count)
        {
            TextAsset catalog = new(BuildCatalogJson(count));
            createdObjects.Add(catalog);
            SetField("runModifierCatalog", catalog);
            SetField<object>("cachedRunItemDefinitions", null);
        }

        private void AddBuildCards(string characterClass, params string[] cardIds)
        {
            SetEnumField("selectedClass", characterClass);
            IList cardPool = GetField<IList>("cardPool");
            IList deck = GetField<IList>("deck");
            cardPool.Clear();
            deck.Clear();
            foreach (string cardId in cardIds)
            {
                ScriptableObject card = ScriptableObject.CreateInstance(cardType);
                createdObjects.Add(card);
                SetObjectField(card, "cardId", cardId);
                SetObjectField(card, "displayName", cardId);
                cardPool.Add(card);
                deck.Add(card);
            }
        }

        private void AddDeckCards(int count, bool clear = true)
        {
            IList cardPool = GetField<IList>("cardPool");
            IList deck = GetField<IList>("deck");
            if (clear)
            {
                cardPool.Clear();
                deck.Clear();
            }

            int start = deck.Count;
            for (int index = 0; index < count; index += 1)
            {
                ScriptableObject card = ScriptableObject.CreateInstance(cardType);
                createdObjects.Add(card);
                string cardId = $"achievement_test_card_{start + index:00}";
                SetObjectField(card, "cardId", cardId);
                SetObjectField(card, "displayName", cardId);
                cardPool.Add(card);
                deck.Add(card);
            }
        }

        private static string BuildCatalogJson(int count)
        {
            StringBuilder json = new();
            json.Append("{\"slotLimitPerCharacter\":3,\"modifiers\":[");
            for (int index = 1; index <= count; index += 1)
            {
                if (index > 1)
                {
                    json.Append(',');
                }

                string category = index <= 10
                    ? "Relic"
                    : index <= 20
                        ? "Blessing"
                        : "Curse";
                json.Append("{\"id\":\"item_")
                    .Append(index.ToString("00"))
                    .Append("\",\"category\":\"")
                    .Append(category)
                    .Append("\",\"name\":\"Item ")
                    .Append(index)
                    .Append("\",\"effect\":\"Effect\",\"description\":\"Description\"}");
            }

            json.Append("]}");
            return json.ToString();
        }

        private static string BuildItemSaveJson(IEnumerable<int> itemNumbers)
        {
            return "{\"itemIds\":[\""
                + string.Join("\",\"", itemNumbers.Select(number => $"item_{number:00}"))
                + "\"]}";
        }

        private static void AssertReadableText(Text text)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.resizeTextForBestFit, Is.True);
            Assert.That(text.resizeTextMinSize, Is.GreaterThanOrEqualTo(9));
            Assert.That(text.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
            Assert.That(text.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
            Assert.That(text.rectTransform.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(text.rectTransform.anchorMin.y, Is.GreaterThanOrEqualTo(0f));
            Assert.That(text.rectTransform.anchorMax.x, Is.LessThanOrEqualTo(1f));
            Assert.That(text.rectTransform.anchorMax.y, Is.LessThanOrEqualTo(1f));
        }

        private static void AssertCompleted(string suffix, bool expected)
        {
            string key = AchievementProgress.GetCompletionKey(Prefix, suffix);
            Assert.That(PlayerPrefs.GetInt(key, 0) > 0, Is.EqualTo(expected), key);
        }

        private void SnapshotAndClearPlayerPrefs()
        {
            foreach (string key in AchievementProgress.GetCompletionKeys(Prefix))
            {
                if (PlayerPrefs.HasKey(key))
                {
                    existingIntegerKeys.Add(key);
                    savedIntegers[key] = PlayerPrefs.GetInt(key);
                }

                PlayerPrefs.DeleteKey(key);
            }

            foreach (string key in StringKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    existingStringKeys.Add(key);
                    savedStrings[key] = PlayerPrefs.GetString(key);
                }

                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        private void RestorePlayerPrefs()
        {
            foreach (string key in AchievementProgress.GetCompletionKeys(Prefix))
            {
                PlayerPrefs.DeleteKey(key);
                if (existingIntegerKeys.Contains(key))
                {
                    PlayerPrefs.SetInt(key, savedIntegers[key]);
                }
            }

            foreach (string key in StringKeys)
            {
                PlayerPrefs.DeleteKey(key);
                if (existingStringKeys.Contains(key))
                {
                    PlayerPrefs.SetString(key, savedStrings[key]);
                }
            }

            PlayerPrefs.Save();
        }

        private void RestoreLanguagePreference()
        {
            if (hadPreviousLanguage)
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

        private RectTransform FindRequired(RectTransform parent, string objectName)
        {
            RectTransform found = FindDescendants(parent)
                .FirstOrDefault(candidate => candidate.name == objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private static IEnumerable<RectTransform> FindDescendants(RectTransform parent)
        {
            yield return parent;
            for (int index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index) is not RectTransform child)
                {
                    continue;
                }

                foreach (RectTransform descendant in FindDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private void Invoke(string methodName)
        {
            InvokeWithResult(methodName);
        }

        private object InvokeWithResult(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(controller, arguments);
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return (T)field.GetValue(controller);
        }

        private T TryGetField<T>(string fieldName)
            where T : class
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(controller) as T;
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, value);
        }

        private void SetEnumField(string fieldName, string enumValue)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, Enum.Parse(field.FieldType, enumValue));
        }

        private static void SetObjectField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            return (T)property.GetValue(target);
        }
    }
}
