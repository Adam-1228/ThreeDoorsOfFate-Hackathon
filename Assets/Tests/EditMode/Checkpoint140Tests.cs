using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Checkpoint140Tests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardAssetPath =
            "Assets/Data/Cards/MVP/card_fate_strike.asset";

        private Type controllerType;
        private GameObject host;
        private Component controller;
        private UnityEngine.Object card;

        [SetUp]
        public void SetUp()
        {
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);
            host = new GameObject("Checkpoint 1.4 Test Host");
            controller = host.AddComponent(controllerType);
            card = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CardAssetPath);
            Assert.That(card, Is.Not.Null);
        }

        [TearDown]
        public void TearDown()
        {
            RectTransform canvasRoot = ReadField("canvasRoot") as RectTransform;
            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(host);
        }

        [Test]
        public void V1HardSaveMigratesWithoutLosingDeckOrItems()
        {
            const string v1 =
                "{\"version\":1,\"selectedClass\":1,\"currentDifficulty\":2,"
                + "\"currentJourneyEndingKind\":0,\"endlessModeActive\":false,"
                + "\"nextEndlessBossRoom\":0,\"endlessBossesDefeated\":0,"
                + "\"playerMaxHealth\":70,\"playerHealth\":51,\"playerBlock\":0,"
                + "\"action\":3,\"luck\":4,\"gold\":88,\"debt\":2,"
                + "\"roomsCleared\":5,\"combatEncountersCompleted\":3,"
                + "\"consecutiveNonCombatDoors\":1,\"storedLuck\":2,"
                + "\"curseReduction\":0,\"hasStoredLuck\":true,"
                + "\"keepLuckNextTurn\":false,\"doorInsightLevel\":2,"
                + "\"retainBlockNextTurn\":false,"
                + "\"deckCardIds\":[\"card_fate_strike\",\"card_fate_strike\"],"
                + "\"equippedItemIds\":[\"relic_gate_shard\"],"
                + "\"combatLog\":[\"legacy\"],\"buildUpgradeLevels\":[]}";

            MethodInfo migrate = FindMethod("TryMigrateRunCheckpointV1", 2);
            Assert.That(migrate, Is.Not.Null);
            object[] arguments = { v1, null };
            Assert.That((bool)migrate.Invoke(controller, arguments), Is.True);
            object migrated = arguments[1];

            Assert.That(ReadMember<int>(migrated, "version"), Is.EqualTo(2));
            Assert.That(
                ReadMember<List<string>>(migrated, "deckCardIds"),
                Is.EqualTo(new[] { "card_fate_strike", "card_fate_strike" }));
            Assert.That(
                ReadMember<List<string>>(migrated, "equippedItemIds"),
                Is.EqualTo(new[] { "relic_gate_shard" }));
            Assert.That(ReadMember<string>(migrated, "runId"), Is.Not.Empty);
            Assert.That(ReadMember<int>(migrated, "runSeed"), Is.Not.Zero);
            Assert.That(ReadMember<int>(migrated, "randomCursor"), Is.Zero);
        }

        [Test]
        public void V1SaveContinuesToDoorsAndPersistsChoicesWithoutTouchingMeta()
        {
            string preferencePrefix =
                $"ThreeDoorsOfFate.Tests.Checkpoint.{Guid.NewGuid():N}.";
            string saveKey = preferencePrefix + "HardRunSave";
            string backupKey = preferencePrefix + "HardRunSave.BackupV1";
            string achievementKey =
                AchievementProgress.GetCompletionKeys(preferencePrefix).First();
            const string v1 =
                "{\"version\":1,\"selectedClass\":1,\"currentDifficulty\":2,"
                + "\"currentJourneyEndingKind\":0,\"playerMaxHealth\":70,"
                + "\"playerHealth\":51,\"action\":3,\"luck\":4,"
                + "\"roomsCleared\":2,\"combatEncountersCompleted\":1,"
                + "\"deckCardIds\":[\"card_fate_strike\",\"card_fate_strike\"],"
                + "\"equippedItemIds\":[],\"combatLog\":[],"
                + "\"buildUpgradeLevels\":[]}";

            try
            {
                ConfigureNormalRun();
                EnsureShell();
                SetField("hardRunSaveKey", saveKey);
                SetField("hardRunSaveBackupKey", backupKey);
                PlayerPrefs.SetString(saveKey, v1);
                PlayerPrefs.SetInt(achievementKey, 1);
                PlayerPrefs.Save();

                bool loaded = (bool)Invoke("TryLoadHardRunSave");
                Assert.That(
                    loaded,
                    Is.True,
                    $"{ReadField("lastRunRestoreErrorKey")}:"
                    + ReadField("lastRunRestoreErrorDetail"));
                Assert.That(
                    GetField<IList>("deck").Cast<object>().Select(GetCardId),
                    Is.EqualTo(new[] { "card_fate_strike", "card_fate_strike" }));

                Invoke("ShowDoors");
                string persistedJson = PlayerPrefs.GetString(saveKey, string.Empty);
                CheckpointJson persisted =
                    JsonUtility.FromJson<CheckpointJson>(persistedJson);
                Assert.That(persisted, Is.Not.Null);
                Assert.That(persisted.version, Is.EqualTo(2));
                Assert.That(persisted.pendingDoorTypeIds, Has.Count.EqualTo(3));
                Assert.That(persisted.randomCursor, Is.GreaterThan(0));
                int expectedNext = (int)Invoke("RunRange", 0, 1000);

                GetField<IList>("pendingDoorTypes").Clear();
                Invoke("ResetRunRandom", 1);
                Assert.That(
                    (bool)Invoke("TryRestoreRunCheckpointV2", persistedJson),
                    Is.True);
                Assert.That(
                    GetField<IList>("pendingDoorTypes")
                        .Cast<object>()
                        .Select(value => Convert.ToInt32(value)),
                    Is.EqualTo(persisted.pendingDoorTypeIds));
                Assert.That(
                    (int)Invoke("RunRange", 0, 1000),
                    Is.EqualTo(expectedNext));
                Assert.That(PlayerPrefs.GetInt(achievementKey), Is.EqualTo(1));
                Assert.That(PlayerPrefs.HasKey(backupKey), Is.False);
            }
            finally
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.DeleteKey(backupKey);
                PlayerPrefs.DeleteKey(achievementKey);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void V2RoundTripPreservesPendingDoorsAndRandomCursor()
        {
            ConfigureNormalRun();
            Invoke("ResetRunRandom", 140042);
            Invoke("RunRange", 0, 100);
            Invoke("RunRange", 0, 100);
            AddEnumValues(
                "pendingDoorTypes",
                "Battle",
                "Shop",
                "Rest");
            AddStringValues("seenRunEventIds", "event.blood_broker");
            AddStringValues("activeEndlessMutationIds", "mutation.ashen_tithe");
            SetField("runStartedAtUnixSeconds", 1400000L);
            SetField("runHistoryCardsPlayed", 17);
            SetField("runHistoryDamageDealt", 180);
            SetField("runHistoryDamageTaken", 52);
            SetField("runHistoryBossesDefeated", 2);
            SetField("runHistoryZeroGoldShopVisits", 1);
            SetField("runHistoryMaximumSameRerollStreak", 3);
            SetField("runHistoryLowLuckRolls", 6);

            string json = (string)Invoke("CaptureRunCheckpointV2");
            CheckpointJson captured = JsonUtility.FromJson<CheckpointJson>(json);
            Assert.That(captured.version, Is.EqualTo(2));
            Assert.That(captured.randomCursor, Is.EqualTo(2));
            Assert.That(captured.pendingDoorTypeIds, Is.EqualTo(new[] { 0, 2, 5 }));
            Assert.That(captured.runStartedAtUnixSeconds, Is.EqualTo(1400000L));
            Assert.That(captured.runHistoryCardsPlayed, Is.EqualTo(17));
            Assert.That(captured.runHistoryDamageDealt, Is.EqualTo(180));
            Assert.That(captured.runHistoryDamageTaken, Is.EqualTo(52));
            Assert.That(captured.runHistoryBossesDefeated, Is.EqualTo(2));
            Assert.That(captured.runHistoryZeroGoldShopVisits, Is.EqualTo(1));
            Assert.That(captured.runHistoryMaximumSameRerollStreak, Is.EqualTo(3));
            Assert.That(captured.runHistoryLowLuckRolls, Is.EqualTo(6));
            int expectedNext = (int)Invoke("RunRange", 0, 1000);

            Invoke("ResetRunRandom", 9);
            GetField<IList>("deck").Clear();
            GetField<IList>("pendingDoorTypes").Clear();
            SetField("cardsRemovedThisRun", 0);
            SetField("selectedStarterContractId", string.Empty);
            SetField("runStartedAtUnixSeconds", 0L);
            SetField("runHistoryCardsPlayed", 0);
            SetField("runHistoryDamageDealt", 0);
            SetField("runHistoryDamageTaken", 0);
            SetField("runHistoryBossesDefeated", 0);
            SetField("runHistoryZeroGoldShopVisits", 0);
            SetField("runHistoryMaximumSameRerollStreak", 0);
            SetField("runHistoryLowLuckRolls", 0);

            Assert.That(
                (bool)Invoke("TryRestoreRunCheckpointV2", json),
                Is.True);
            Assert.That(
                GetField<IList>("deck").Cast<object>().Select(GetCardId),
                Is.EqualTo(new[] { "card_fate_strike", "card_fate_strike" }));
            Assert.That(
                GetField<IList>("pendingDoorTypes")
                    .Cast<object>()
                    .Select(value => value.ToString()),
                Is.EqualTo(new[] { "Battle", "Shop", "Rest" }));
            Assert.That(SetNames("seenRunEventIds"), Does.Contain("event.blood_broker"));
            Assert.That(SetNames("activeEndlessMutationIds"), Does.Contain("mutation.ashen_tithe"));
            Assert.That(ReadField("selectedStarterContractId"), Is.EqualTo("gambler.high_roll"));
            Assert.That(ReadField("cardsRemovedThisRun"), Is.EqualTo(2));
            Assert.That(ReadField("runStartedAtUnixSeconds"), Is.EqualTo(1400000L));
            Assert.That(ReadField("runHistoryCardsPlayed"), Is.EqualTo(17));
            Assert.That(ReadField("runHistoryDamageDealt"), Is.EqualTo(180));
            Assert.That(ReadField("runHistoryDamageTaken"), Is.EqualTo(52));
            Assert.That(ReadField("runHistoryBossesDefeated"), Is.EqualTo(2));
            Assert.That(ReadField("runHistoryZeroGoldShopVisits"), Is.EqualTo(1));
            Assert.That(ReadField("runHistoryMaximumSameRerollStreak"), Is.EqualTo(3));
            Assert.That(ReadField("runHistoryLowLuckRolls"), Is.EqualTo(6));
            Assert.That((int)Invoke("RunRange", 0, 1000), Is.EqualTo(expectedNext));
        }

        [Test]
        public void UnknownV1CardPreservesOriginalAndBackupInsteadOfMutatingRun()
        {
            const string saveKey = "ThreeDoorsOfFate.HardRunSave";
            const string backupKey = "ThreeDoorsOfFate.HardRunSave.BackupV1";
            const string original =
                "{\"version\":1,\"selectedClass\":1,\"currentDifficulty\":2,"
                + "\"currentJourneyEndingKind\":0,\"playerMaxHealth\":70,"
                + "\"playerHealth\":51,\"luck\":4,"
                + "\"deckCardIds\":[\"missing_v140_card\"],"
                + "\"equippedItemIds\":[],\"combatLog\":[],"
                + "\"buildUpgradeLevels\":[]}";

            try
            {
                ConfigureNormalRun();
                int originalHealth = (int)ReadField("playerHealth");
                PlayerPrefs.SetString(saveKey, original);
                PlayerPrefs.DeleteKey(backupKey);
                PlayerPrefs.Save();

                Assert.That((bool)Invoke("TryLoadHardRunSave"), Is.False);
                Assert.That(PlayerPrefs.GetString(saveKey), Is.EqualTo(original));
                Assert.That(PlayerPrefs.GetString(backupKey), Is.EqualTo(original));
                Assert.That(ReadField("playerHealth"), Is.EqualTo(originalHealth));
            }
            finally
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.DeleteKey(backupKey);
                PlayerPrefs.Save();
            }
        }

        [TestCase("Easy")]
        [TestCase("Normal")]
        [TestCase("Hard")]
        public void EveryDifficultyCanUseRunSaveAtDoorSelection(string difficulty)
        {
            ConfigureNormalRun();
            SetEnumField("currentDifficulty", difficulty);
            SetEnumField("phase", "DoorSelection");

            Assert.That((bool)Invoke("CanUseRunSaveSystem"), Is.True);
            Assert.That((bool)Invoke("CanSaveRunCheckpoint"), Is.True);
        }

        [Test]
        public void GameOverAtZeroHealthDeletesCheckpointAndRecordsRunTombstone()
        {
            string preferencePrefix =
                $"ThreeDoorsOfFate.Tests.Checkpoint.{Guid.NewGuid():N}.";
            string saveKey = preferencePrefix + "HardRunSave";
            string backupKey = preferencePrefix + "HardRunSave.BackupV1";
            string tombstoneKey = saveKey + ".DeletedRunIds";
            const string runId = "defeated-run-140";

            try
            {
                ConfigureNormalRun();
                EnsureShell();
                SetField("hardRunSaveKey", saveKey);
                SetField("hardRunSaveBackupKey", backupKey);
                SetField("activeRunId", runId);
                SetField("playerHealth", 0);
                SetEnumField("phase", "Combat");
                PlayerPrefs.SetString(saveKey, "{\"version\":2,\"runId\":\"defeated-run-140\",\"randomCursor\":9}");
                PlayerPrefs.SetString(backupKey, "legacy-backup");
                PlayerPrefs.Save();

                Invoke(
                    "ShowGameOver",
                    false,
                    "The cave claimed another name.");

                Assert.That(PlayerPrefs.HasKey(saveKey), Is.False);
                Assert.That(PlayerPrefs.HasKey(backupKey), Is.False);
                Assert.That(
                    PlayerPrefs.GetString(tombstoneKey, string.Empty),
                    Does.Contain(runId));
            }
            finally
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.DeleteKey(backupKey);
                PlayerPrefs.DeleteKey(tombstoneKey);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void EndlessMutationCheckpointRoundTripPreservesChoicesAndResumes()
        {
            string preferencePrefix =
                $"ThreeDoorsOfFate.Tests.Checkpoint.{Guid.NewGuid():N}.";
            string saveKey = preferencePrefix + "HardRunSave";
            string backupKey = preferencePrefix + "HardRunSave.BackupV1";

            try
            {
                ConfigureNormalRun();
                EnsureShell();
                SetField("hardRunSaveKey", saveKey);
                SetField("hardRunSaveBackupKey", backupKey);
                SetField("endlessModeActive", true);
                Invoke("ResetRunRandom", 140140);

                Invoke("ShowEndlessMutationSelection");

                string offeredJson = PlayerPrefs.GetString(saveKey, string.Empty);
                Assert.That(
                    offeredJson,
                    Is.Not.Empty,
                    "The mutation offer must be checkpointed before the player chooses.");
                CheckpointJson offered = JsonUtility.FromJson<CheckpointJson>(
                    offeredJson);
                Assert.That(offered, Is.Not.Null);
                Assert.That(offered.savedPhaseName, Is.EqualTo("Reward"));
                Assert.That(offered.pendingEndlessMutationChoiceIds, Has.Count.EqualTo(3));
                Assert.That(offered.pendingEndlessMutationChoiceIds, Is.Unique);
                string[] expectedChoices = offered.pendingEndlessMutationChoiceIds.ToArray();

                FieldInfo pendingChoicesField = GetFieldInfo(
                    "pendingEndlessMutationChoices");
                pendingChoicesField.SetValue(
                    controller,
                    Array.CreateInstance(
                        pendingChoicesField.FieldType.GetGenericArguments()[0],
                        0));
                Assert.That(
                    (bool)Invoke(
                        "TryRestoreRunCheckpointV2",
                        PlayerPrefs.GetString(saveKey)),
                    Is.True);
                Assert.That(
                    ((IEnumerable)ReadField("pendingEndlessMutationChoices"))
                        .Cast<object>()
                        .Select(choice => ReadProperty<string>(choice, "Id")),
                    Is.EqualTo(expectedChoices));

                Invoke("ResumeRestoredRunCheckpoint");
                Assert.That(ReadField("phase").ToString(), Is.EqualTo("Reward"));
                Assert.That(
                    ((IEnumerable)ReadField("pendingEndlessMutationChoices"))
                        .Cast<object>()
                        .Select(choice => ReadProperty<string>(choice, "Id")),
                    Is.EqualTo(expectedChoices));

                Invoke("SelectEndlessMutation", expectedChoices[0]);
                CheckpointJson selected = JsonUtility.FromJson<CheckpointJson>(
                    PlayerPrefs.GetString(saveKey, string.Empty));
                Assert.That(selected.savedPhaseName, Is.EqualTo("DoorSelection"));
                Assert.That(selected.pendingEndlessMutationChoiceIds, Is.Empty);
                Assert.That(selected.pendingEndlessCheckpoint, Is.True);
                Assert.That(selected.activeMutationIds, Does.Contain(expectedChoices[0]));
                Assert.That(
                    (bool)Invoke(
                        "TryRestoreRunCheckpointV2",
                        PlayerPrefs.GetString(saveKey)),
                    Is.True);
                Invoke("ResumeRestoredRunCheckpoint");
                Assert.That(ReadField("phase").ToString(), Is.EqualTo("Reward"));
                Assert.That(ReadField("pendingEndlessCheckpoint"), Is.EqualTo(true));
            }
            finally
            {
                PlayerPrefs.DeleteKey(saveKey);
                PlayerPrefs.DeleteKey(backupKey);
                PlayerPrefs.DeleteKey(saveKey + ".DeletedRunIds");
                PlayerPrefs.Save();
            }
        }

        private void ConfigureNormalRun()
        {
            IList cardPool = GetField<IList>("cardPool");
            cardPool.Clear();
            cardPool.Add(card);
            IList deck = GetField<IList>("deck");
            deck.Clear();
            deck.Add(card);
            deck.Add(card);
            SetEnumField("selectedClass", "Gambler");
            SetEnumField("currentDifficulty", "Normal");
            SetEnumField("currentJourneyEndingKind", "Return");
            SetEnumField("phase", "DoorSelection");
            SetField("playerMaxHealth", 70);
            SetField("playerHealth", 55);
            SetField("luck", 4);
            SetField("gold", 91);
            SetField("debt", 1);
            SetField("roomsCleared", 4);
            SetField("selectedStarterContractId", "gambler.high_roll");
            SetField("cardsRemovedThisRun", 2);
        }

        private void EnsureShell()
        {
            if (ReadField("root") == null)
            {
                Invoke("BuildShell");
            }
        }

        private void AddEnumValues(string fieldName, params string[] values)
        {
            FieldInfo field = GetFieldInfo(fieldName);
            IList list = (IList)field.GetValue(controller);
            Type enumType = field.FieldType.GetGenericArguments()[0];
            foreach (string value in values)
            {
                list.Add(Enum.Parse(enumType, value));
            }
        }

        private void AddStringValues(string fieldName, params string[] values)
        {
            object collection = ReadField(fieldName);
            MethodInfo add = collection.GetType().GetMethod("Add");
            Assert.That(add, Is.Not.Null);
            foreach (string value in values)
            {
                add.Invoke(collection, new object[] { value });
            }
        }

        private string[] SetNames(string fieldName)
        {
            return ((IEnumerable)ReadField(fieldName))
                .Cast<object>()
                .Select(value => value.ToString())
                .ToArray();
        }

        private string GetCardId(object value)
        {
            PropertyInfo property = value.GetType().GetProperty(
                "CardId",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(value) as string;
        }

        private object Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(methodName, arguments.Length);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            return method.Invoke(controller, arguments);
        }

        private MethodInfo FindMethod(string methodName, int argumentCount)
        {
            return controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(method =>
                    method.Name == methodName
                    && method.GetParameters().Length == argumentCount);
        }

        private object ReadField(string fieldName)
        {
            return GetFieldInfo(fieldName).GetValue(controller);
        }

        private T GetField<T>(string fieldName)
        {
            return (T)ReadField(fieldName);
        }

        private FieldInfo GetFieldInfo(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return field;
        }

        private void SetField(string fieldName, object value)
        {
            GetFieldInfo(fieldName).SetValue(controller, value);
        }

        private void SetEnumField(string fieldName, string value)
        {
            FieldInfo field = GetFieldInfo(fieldName);
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private static T ReadMember<T>(object instance, string name)
        {
            FieldInfo field = instance.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected checkpoint field '{name}'.");
            return (T)field.GetValue(instance);
        }

        private static T ReadProperty<T>(object instance, string name)
        {
            PropertyInfo property = instance.GetType().GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected property '{name}'.");
            return (T)property.GetValue(instance);
        }

        [Serializable]
        private sealed class CheckpointJson
        {
            public int version;
            public int randomCursor;
            public int savedPhase;
            public long runStartedAtUnixSeconds;
            public int runHistoryCardsPlayed;
            public int runHistoryDamageDealt;
            public int runHistoryDamageTaken;
            public int runHistoryBossesDefeated;
            public int runHistoryZeroGoldShopVisits;
            public int runHistoryMaximumSameRerollStreak;
            public int runHistoryLowLuckRolls;
            public List<string> activeMutationIds = new();
            public List<string> pendingEndlessMutationChoiceIds = new();
            public bool pendingEndlessCheckpoint;
            public List<int> pendingDoorTypeIds = new();

            public string savedPhaseName => savedPhase switch
            {
                4 => "DoorSelection",
                6 => "Reward",
                _ => savedPhase.ToString()
            };
        }
    }
}
