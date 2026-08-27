using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104GameOverTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string DifficultyUnlockKey =
            "ThreeDoorsOfFate.DifficultyUnlocked";

        private Type controllerType;
        private Type cardType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private readonly List<ScriptableObject> cards = new();
        private bool hadPreviousLanguage;
        private string previousLanguage;
        private bool hadDifficultyUnlock;
        private int previousDifficultyUnlock;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            hadDifficultyUnlock = PlayerPrefs.HasKey(DifficultyUnlockKey);
            previousDifficultyUnlock = PlayerPrefs.GetInt(DifficultyUnlockKey, 0);
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "en");
            PlayerPrefs.SetInt(DifficultyUnlockKey, 2);
            PlayerPrefs.Save();
            GameLocalization.Initialize(SystemLanguage.English);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(cardType, Is.Not.Null);

            controllerHost = new GameObject("Quality 1.0.4 Game Over Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = TryGetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = TryGetField<RectTransform>("root");
            }

            canvasRoot = TryGetField<RectTransform>("canvasRoot");
            SetField("hiddenGameOverChance", 0f);
            PopulateOracleStarterPool();
            SeedKnownRunState();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ScriptableObject card in cards)
            {
                if (card != null)
                {
                    UnityEngine.Object.DestroyImmediate(card);
                }
            }

            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            if (controllerHost != null)
            {
                UnityEngine.Object.DestroyImmediate(controllerHost);
            }

            if (originalEventSystem == null)
            {
                EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }

            RestorePreference(
                GameLanguagePolicy.PreferenceKey,
                hadPreviousLanguage,
                previousLanguage);
            if (hadDifficultyUnlock)
            {
                PlayerPrefs.SetInt(DifficultyUnlockKey, previousDifficultyUnlock);
            }
            else
            {
                PlayerPrefs.DeleteKey(DifficultyUnlockKey);
            }

            PlayerPrefs.Save();
            GameLocalization.Initialize(Application.systemLanguage);
        }

        [TestCase("en", "Oracle", "Hard", "Doors 7", "Combat Wins 4")]
        [TestCase("ko", "점술가", "어려움", "도달한 문 7", "전투 승리 4")]
        public void DefeatSummary_ContainsKnownRunStateInBothLanguages(
            string languageCode,
            string className,
            string difficultyName,
            string doors,
            string combats)
        {
            SetLanguage(languageCode);
            Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");
            Canvas.ForceUpdateCanvases();

            Text summary = FindRequired("Game Over Run Summary").GetComponent<Text>();
            Assert.That(summary.text, Does.Contain(className));
            Assert.That(summary.text, Does.Contain(difficultyName));
            Assert.That(summary.text, Does.Contain(doors));
            Assert.That(summary.text, Does.Contain(combats));
            Assert.That(summary.text, Does.Contain("0/60"));
            Assert.That(summary.text, Does.Contain("Test Enemy"));
            Assert.That(summary.text, Does.Contain("9/30"));
            Assert.That(summary.text, Does.Contain("123"));
            Assert.That(summary.text, Does.Contain("5"));
            Assert.That(summary.text, Does.Contain("2/"));
        }

        [Test]
        public void NormalGameOver_SummaryClearsMessageAndDoesNotRepeatCause()
        {
            Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");
            Canvas.ForceUpdateCanvases();

            RectTransform message = FindRequired("Game Over Message");
            RectTransform summaryPanel = FindRequired("Game Over Run Summary Panel");
            Text summary = FindRequired("Game Over Run Summary").GetComponent<Text>();

            Assert.That(
                summaryPanel.anchorMax.y,
                Is.LessThanOrEqualTo(message.anchorMin.y - 0.04f),
                "The summary frame must leave a visible gap below the main defeat message.");
            Assert.That(summary.text, Does.Not.Contain("The cave claimed another name."));
        }

        [Test]
        public void DefeatActions_ExposeExactlyThreeRoutesAndNavigate()
        {
            Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");
            AssertGameOverActionCount();
            FindRequired("Retry Same Run Button").GetComponent<Button>().onClick.Invoke();
            Assert.That(GetField<object>("phase").ToString(), Is.EqualTo("DoorSelection"));
            Assert.That(GetField<object>("selectedClass").ToString(), Is.EqualTo("Oracle"));
            Assert.That(GetField<object>("currentDifficulty").ToString(), Is.EqualTo("Hard"));

            SeedKnownRunState();
            Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");
            AssertGameOverActionCount();
            FindRequired("Choose Class Button").GetComponent<Button>().onClick.Invoke();
            Assert.That(GetField<object>("phase").ToString(), Is.EqualTo("ClassSelection"));

            SeedKnownRunState();
            Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");
            AssertGameOverActionCount();
            FindRequired("Main Menu Button").GetComponent<Button>().onClick.Invoke();
            Assert.That(GetField<object>("phase").ToString(), Is.EqualTo("MainMenu"));
        }

        [Test]
        public void HiddenGameOver_RetainsSummaryAndAllThreeActions()
        {
            Texture2D texture = new(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(Color.black, 16).ToArray());
            texture.Apply();
            Sprite hidden = Sprite.Create(
                texture,
                new Rect(0f, 0f, 4f, 4f),
                new Vector2(0.5f, 0.5f));
            UnityEngine.Random.State randomState = UnityEngine.Random.state;
            try
            {
                SetField("oracleHiddenGameOverSprite", hidden);
                SetField("hiddenGameOverChance", 1f);
                UnityEngine.Random.InitState(104);
                Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");

                Assert.That(FindRequired("Hidden Game Over Overlay"), Is.Not.Null);
                Text summary = FindRequired("Game Over Run Summary").GetComponent<Text>();
                Assert.That(summary.text, Does.Contain("The cave claimed another name."));
                AssertGameOverActionCount();
            }
            finally
            {
                UnityEngine.Random.state = randomState;
                UnityEngine.Object.DestroyImmediate(hidden);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void NewlyCompletedAchievement_IsTrackedOnceAndClearedByNewRun()
        {
            AchievementDefinition definition = AchievementProgress.NewDefinitions[0];
            string completionKey = AchievementProgress.GetCompletionKey(
                PlayerPrefsProgressStore.ProductionPrefix,
                definition.StorageSuffix);
            bool hadCompletion = PlayerPrefs.HasKey(completionKey);
            int previousCompletion = PlayerPrefs.GetInt(completionKey, 0);
            PlayerPrefs.DeleteKey(completionKey);
            try
            {
                Invoke("CompleteAchievementAndTrack", definition);
                Invoke("CompleteAchievementAndTrack", definition);

                List<string> completed = GetField<List<string>>(
                    "newlyCompletedAchievementNames");
                Assert.That(completed, Has.Count.EqualTo(1));
                Assert.That(completed[0], Is.EqualTo(definition.DisplayName));

                Invoke("StartRun", GetField<object>("selectedClass"));
                Assert.That(completed, Is.Empty);
            }
            finally
            {
                if (hadCompletion)
                {
                    PlayerPrefs.SetInt(completionKey, previousCompletion);
                }
                else
                {
                    PlayerPrefs.DeleteKey(completionKey);
                }

                PlayerPrefs.Save();
            }
        }

        private void SeedKnownRunState()
        {
            SetEnumField("selectedClass", "Oracle");
            SetEnumField("currentDifficulty", "Hard");
            SetField("roomsCleared", 7);
            SetField("combatEncountersCompleted", 4);
            SetField("playerHealth", 0);
            SetField("playerMaxHealth", 60);
            SetField("gold", 123);
            SetField("debt", 5);

            IList deck = GetField<IList>("deck");
            deck.Clear();
            AddTestCard(deck, "summary_card_a");
            AddTestCard(deck, "summary_card_b");

            List<string> items = GetField<List<string>>("equippedRunItemIds");
            items.Clear();
            items.Add("test_item_a");
            items.Add("test_item_b");

            object enemy = CreateEnemy();
            enemy.GetType().GetProperty("Health")?.SetValue(enemy, 9);
            SetField("enemy", enemy);
        }

        private void PopulateOracleStarterPool()
        {
            IList pool = GetField<IList>("cardPool");
            pool.Clear();
            foreach (string cardId in new[]
            {
                "card_worn_dagger",
                "card_fate_strike",
                "card_throwing_dagger",
                "class_oracle_attack_constellation_cut",
                "card_worn_shield",
                "card_protection_charm",
                "card_evade",
                "class_oracle_defense_foreseen_barrier",
                "card_read_the_rift",
                "card_fix_fate",
                "card_reroll",
                "class_oracle_skill_three_door_omen",
                "card_counter_ready"
            })
            {
                UnityEngine.Object card = AssetDatabase.LoadAssetAtPath(
                    $"Assets/Data/Cards/MVP/{cardId}.asset",
                    cardType);
                Assert.That(card, Is.Not.Null, cardId);
                pool.Add(card);
            }
        }

        private void AddTestCard(IList deck, string cardId)
        {
            ScriptableObject card = ScriptableObject.CreateInstance(cardType);
            FieldInfo idField = cardType.GetField(
                "cardId",
                BindingFlags.Instance | BindingFlags.NonPublic);
            idField?.SetValue(card, cardId);
            cards.Add(card);
            deck.Add(card);
        }

        private object CreateEnemy()
        {
            Type enemyType = controllerType.GetNestedType(
                "EnemyState",
                BindingFlags.NonPublic);
            ConstructorInfo constructor = enemyType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single();
            return constructor.Invoke(new object[]
            {
                "test_enemy",
                "Test Enemy",
                30,
                4,
                0,
                false,
                false,
                0
            });
        }

        private void SetLanguage(string languageCode)
        {
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, languageCode);
            PlayerPrefs.Save();
            GameLocalization.SetLanguage(
                languageCode == "ko" ? GameLanguage.Korean : GameLanguage.English);
        }

        private void AssertGameOverActionCount()
        {
            RectTransform overlay = GetField<RectTransform>("gameOverOverlay");
            string[] buttonNames = FindDescendants(overlay)
                .Where(candidate => candidate.gameObject.activeInHierarchy
                    && candidate.GetComponent<Button>() != null)
                .Select(candidate => candidate.name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.That(buttonNames, Is.EquivalentTo(new[]
            {
                "Retry Same Run Button",
                "Choose Class Button",
                "Main Menu Button"
            }));
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendants(root)
                .FirstOrDefault(candidate => candidate.name == objectName
                    && candidate.gameObject.activeInHierarchy);
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

        private void Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            method.Invoke(controller, arguments);
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

        private static void RestorePreference(
            string key,
            bool existed,
            string value)
        {
            if (existed)
            {
                PlayerPrefs.SetString(key, value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
