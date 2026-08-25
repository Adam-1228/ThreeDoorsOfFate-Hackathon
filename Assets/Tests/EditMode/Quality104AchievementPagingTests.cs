using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104AchievementPagingTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string Prefix = PlayerPrefsProgressStore.ProductionPrefix;

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
        private readonly HashSet<string> existingIntegers = new();
        private readonly HashSet<string> existingStrings = new();
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            SnapshotAndClearProgress();
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "ko");
            PlayerPrefs.Save();
            GameLocalization.Initialize(SystemLanguage.Korean);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(cardType, Is.Not.Null);
            controllerHost = new GameObject("Quality 1.0.4 Achievement Paging Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = TryGetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = TryGetField<RectTransform>("root");
            }

            canvasRoot = GetField<RectTransform>("canvasRoot");
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
                EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }

            RestoreProgress();
            if (hadPreviousLanguage)
            {
                PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, previousLanguage);
            }
            else
            {
                PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
            }

            PlayerPrefs.Save();
            GameLocalization.Initialize(Application.systemLanguage);
        }

        [Test]
        public void LockedGallery_UsesTenSlotsPerPageAndHidesIdentity()
        {
            Invoke("ShowAchievements");
            Invoke("ShowAchievementPage", 0);

            List<RectTransform> firstPage = GetVisibleSlots();
            Assert.That(firstPage, Has.Count.EqualTo(10));
            Assert.That(GetField<Text>("achievementCompletionText").text, Is.EqualTo("달성 0/20"));
            foreach (RectTransform slot in firstPage)
            {
                Assert.That(slot.GetComponent<Button>().interactable, Is.False, slot.name);
                Assert.That(
                    FindDescendants(slot).Any(candidate =>
                        candidate.name == "업적 미발견"
                        && candidate.GetComponent<Text>().text == "미발견"),
                    Is.True,
                    slot.name);
                Assert.That(
                    FindDescendants(slot).Any(candidate => candidate.name == "업적 이미지"),
                    Is.False,
                    slot.name);
                Assert.That(
                    FindDescendants(slot).Any(candidate => candidate.name == "업적 이름"),
                    Is.False,
                    slot.name);
            }

            Invoke("ShowAchievementPage", 1);
            Assert.That(GetVisibleSlots(), Has.Count.EqualTo(10));
        }

        [Test]
        public void CompletedSlot_SelectsOneSharedDetailPanel()
        {
            AchievementProgress.Complete(
                Prefix,
                AchievementProgress.AbyssCollector);

            Invoke("ShowAchievements");
            Invoke("ShowAchievementPage", 0);

            RectTransform collector = FindSlotByTitle("심연의 수집가");
            Button button = collector.GetComponent<Button>();
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            Canvas.ForceUpdateCanvases();

            RectTransform detailRoot = GetField<RectTransform>("achievementDetailRoot");
            RectTransform detail = FindRequired(detailRoot, "업적 상세 패널");
            Assert.That(
                FindRequired(detail, "업적 상세 제목").GetComponent<Text>().text,
                Does.Contain("심연의 수집가"));
            Assert.That(
                FindRequired(detail, "업적 상세 설명").GetComponent<Text>().text,
                Is.EqualTo(AchievementProgress.AbyssCollector.EarnedDescription));
            Assert.That(
                FindRequired(detail, "업적 상세 상태").GetComponent<Text>().text,
                Does.Contain("100점"));
            Assert.That(
                FindDescendants(collector).Any(candidate => candidate.name == "업적 선택 표시"),
                Is.True);
        }

        [Test]
        public void Paging_ClampsEndpointsAndLocalizesEnglishSummaries()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            Invoke("ShowAchievements");
            Invoke("ShowAchievementPage", -99);
            Assert.That(GetField<int>("achievementPageIndex"), Is.EqualTo(0));
            Assert.That(GetField<Button>("achievementPreviousButton").interactable, Is.False);
            Assert.That(GetField<Text>("achievementPageText").text, Is.EqualTo("1 / 2"));
            Assert.That(
                GetField<Text>("achievementCompletionText").text,
                Is.EqualTo("0/20 Completed"));

            Invoke("ShowAchievementPage", 99);
            Assert.That(GetField<int>("achievementPageIndex"), Is.EqualTo(1));
            Assert.That(GetField<Button>("achievementNextButton").interactable, Is.False);
            Assert.That(GetField<Text>("achievementPageText").text, Is.EqualTo("2 / 2"));
            Assert.That(
                Regex.IsMatch(GetField<Text>("achievementCompletionText").text, "[가-힣]"),
                Is.False);
            Assert.That(GetVisibleSlots(), Has.Count.EqualTo(10));
            Assert.That(
                GetVisibleSlots().SelectMany(FindDescendants)
                    .Where(candidate => candidate.name == "업적 미발견")
                    .Select(candidate => candidate.GetComponent<Text>().text),
                Has.All.EqualTo("Undiscovered"));
        }

        private void SetTestRunItemCatalog(int count)
        {
            TextAsset catalog = new(BuildCatalogJson(count));
            createdObjects.Add(catalog);
            SetField("runModifierCatalog", catalog);
            SetField<object>("cachedRunItemDefinitions", null);
        }

        private void AddCard(string cardId)
        {
            ScriptableObject card = ScriptableObject.CreateInstance(cardType);
            createdObjects.Add(card);
            SetObjectField(card, "cardId", cardId);
            SetObjectField(card, "displayName", cardId);
            GetField<IList>("deck").Add(card);
        }

        private RectTransform FindSlotByTitle(string expectedTitle)
        {
            RectTransform slot = GetVisibleSlots().FirstOrDefault(candidate =>
                FindDescendants(candidate)
                    .FirstOrDefault(descendant => descendant.name == "업적 이름")
                    ?.GetComponent<Text>().text == expectedTitle);
            Assert.That(slot, Is.Not.Null, $"Expected achievement '{expectedTitle}'.");
            return slot;
        }

        private List<RectTransform> GetVisibleSlots()
        {
            RectTransform cardsRoot = GetField<RectTransform>("achievementCardsRoot");
            return Enumerable.Range(0, cardsRoot.childCount)
                .Select(index => cardsRoot.GetChild(index) as RectTransform)
                .Where(candidate => candidate != null)
                .Where(candidate => candidate.name.StartsWith(
                    "업적 슬롯 ",
                    StringComparison.Ordinal))
                .ToList();
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

        private void SnapshotAndClearProgress()
        {
            foreach (string key in PlayerPrefsProgressStore.GetIntegerKeys(Prefix))
            {
                if (PlayerPrefs.HasKey(key))
                {
                    existingIntegers.Add(key);
                    savedIntegers[key] = PlayerPrefs.GetInt(key);
                }

                PlayerPrefs.DeleteKey(key);
            }

            foreach (string key in PlayerPrefsProgressStore.GetStringKeys(Prefix))
            {
                if (PlayerPrefs.HasKey(key))
                {
                    existingStrings.Add(key);
                    savedStrings[key] = PlayerPrefs.GetString(key, string.Empty);
                }

                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        private void RestoreProgress()
        {
            foreach (string key in PlayerPrefsProgressStore.GetIntegerKeys(Prefix))
            {
                PlayerPrefs.DeleteKey(key);
                if (existingIntegers.Contains(key))
                {
                    PlayerPrefs.SetInt(key, savedIntegers[key]);
                }
            }

            foreach (string key in PlayerPrefsProgressStore.GetStringKeys(Prefix))
            {
                PlayerPrefs.DeleteKey(key);
                if (existingStrings.Contains(key))
                {
                    PlayerPrefs.SetString(key, savedStrings[key]);
                }
            }
        }

        private static RectTransform FindRequired(
            RectTransform parent,
            string objectName)
        {
            RectTransform found = FindDescendants(parent)
                .FirstOrDefault(candidate => candidate.name == objectName);
            Assert.That(found, Is.Not.Null, $"Expected '{objectName}'.");
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
            Invoke<object>(methodName, arguments);
        }

        private T Invoke<T>(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            object result = method.Invoke(controller, arguments);
            return result == null ? default : (T)result;
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
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
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            field.SetValue(controller, value);
        }

        private void SetEnumField(string fieldName, string value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private static void SetObjectField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected card field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
