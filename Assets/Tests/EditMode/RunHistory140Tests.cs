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
            GameLocalization.Initialize(SystemLanguage.English);
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
                SetControllerField(controller, "runHistoryKeyPrefix", keyPrefix);
                TextAsset modifierCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    "Assets/Data/RunModifiers/run_modifier_catalog.json");
                Assert.That(modifierCatalog, Is.Not.Null);
                SetControllerField(
                    controller,
                    "runModifierCatalog",
                    modifierCatalog);
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
                RectTransform row = FindDescendant(root, "운명 기록 항목 0");
                Assert.That(outer, Is.Not.Null);
                Assert.That(row, Is.Not.Null);
                AssertInside(row, outer);

                InvokeController(controller, "ShowRunHistoryDetail", 0);
                RectTransform detailOuter = FindDescendant(
                    root,
                    "운명 기록 상세 외곽 프레임");
                RectTransform summary = FindDescendant(
                    root,
                    "운명 기록 상세 요약");
                RectTransform loadout = FindDescendant(
                    root,
                    "운명 기록 상세 덱과 아이템");
                RectTransform loadoutText = FindDescendant(
                    root,
                    "운명 기록 상세 덱과 아이템 텍스트");
                Assert.That(detailOuter, Is.Not.Null);
                Assert.That(summary, Is.Not.Null);
                Assert.That(loadout, Is.Not.Null);
                Assert.That(loadoutText, Is.Not.Null);
                AssertInside(summary, detailOuter);
                AssertInside(loadout, detailOuter);
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
