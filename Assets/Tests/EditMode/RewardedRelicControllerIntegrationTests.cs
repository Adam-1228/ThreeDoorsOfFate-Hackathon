using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Ads;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class RewardedRelicControllerIntegrationTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CatalogPath =
            "Assets/Data/RunModifiers/run_modifier_catalog.json";
        private const string FontPath = "Assets/Fonts/GowunBatang-Regular.ttf";
        private const float TargetScreenWidth = 2796f;
        private const float TargetScreenHeight = 1290f;
        private const float TargetSafeWidth = 2532f;
        private const float TargetSafeHeight = 1164f;
        private const string CharacterId = "Gambler";
        private const string DiscoveredKey =
            "ThreeDoorsOfFate.DiscoveredItems.Gambler";
        private const string EquippedKey =
            "ThreeDoorsOfFate.EquippedItems.Gambler";
        private const string DailyPrefix =
            "ThreeDoorsOfFate.Ads.RewardedRelic.Gambler.";

        private readonly Dictionary<string, string> savedStringValues = new();
        private readonly Dictionary<string, int> savedIntValues = new();
        private readonly HashSet<string> missingKeys = new();

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform contentRoot;
        private RectTransform canvasRoot;
        private EventSystem originalEventSystem;
        private Font embeddedFont;
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

            PreserveStringPreference(DiscoveredKey);
            PreserveStringPreference(EquippedKey);
            PreserveStringPreference(DailyPrefix + "Date");
            PreserveIntPreference(DailyPrefix + "Count");
            PreserveStringPreference(DailyPrefix + "GreatestObservedUtc");
            DeleteTestPreferences();

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("Rewarded Relic Controller Test Host");
            controller = controllerHost.AddComponent(controllerType);
            embeddedFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(embeddedFont, Is.Not.Null, "The embedded Korean UI font is required.");
            SetField("uiFontAsset", embeddedFont);
            SetField("uiFont", embeddedFont);
            contentRoot = GetField<RectTransform>("contentRoot");
            if (contentRoot == null)
            {
                FindMethod("BuildShell").Invoke(controller, Array.Empty<object>());
                contentRoot = GetField<RectTransform>("contentRoot");
            }

            Assert.That(contentRoot, Is.Not.Null, "Controller must build its runtime UI shell.");
            canvasRoot = GetField<RectTransform>("canvasRoot");

            TextAsset catalog = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "The complete run-item catalog is required.");
            SetField("runModifierCatalog", catalog);
            SetField("cachedRunItemDefinitions", null);
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

            if (originalEventSystem == null)
            {
                EventSystem createdEventSystem =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdEventSystem.gameObject);
                }
            }

            RestoreTestPreferences();
            RestoreLanguagePreference();
        }

        [Test]
        public void ClassDetail_EnglishRewardStatusUsesLocalizationCatalog()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            InvokeWithEnum("ShowClassDetail", CharacterId);

            RectTransform reward = FindRequired("보상형 유물 광고");
            Text rewardStatus = reward
                .GetComponentsInChildren<Text>(true)
                .Single(text => text.name == "보상형 유물 광고 라벨");

            Assert.That(
                rewardStatus.text,
                Is.EqualTo("Ad loading · 3 remaining today"));
        }

        [Test]
        public void ClassDetail_RendersRewardActionBetweenNavigationButtonsWithImportantFeedback()
        {
            Assert.That(
                MobileAdsService.IsRewardedAdReady,
                Is.False,
                "EditMode must exercise the deterministic ad-preparation state.");
            InvokeWithEnum("ShowClassDetail", CharacterId);

            RectTransform back = FindRequired("뒤로");
            RectTransform reward = FindRequired("보상형 유물 광고");
            RectTransform confirm = FindRequired("캐릭터 확정");

            Assert.That(back.anchorMax.x, Is.LessThan(reward.anchorMin.x));
            Assert.That(reward.anchorMax.x, Is.LessThan(confirm.anchorMin.x));
            Assert.That(reward.GetComponentInChildren<Button>(true), Is.Not.Null);
            Assert.That(
                reward.GetComponentsInChildren<Component>(true)
                    .Any(component => component.GetType().Name
                        == "GameSfxButtonFeedback"),
                Is.False,
                "Important feedback must be the button's first listener, not a later event component.");
            Text rewardStatus = FindText(reward, "오늘 3회 남음");
            Assert.That(rewardStatus, Is.Not.Null);
            Assert.That(
                rewardStatus.text,
                Is.EqualTo("광고 준비 중 · 오늘 3회 남음"));
            Assert.That(rewardStatus.font, Is.SameAs(embeddedFont));
            Assert.That(
                rewardStatus.rectTransform.anchorMin.y,
                Is.GreaterThanOrEqualTo(0.25f));
            Assert.That(
                rewardStatus.rectTransform.anchorMax.y,
                Is.LessThanOrEqualTo(0.75f));
            Assert.That(
                reward.GetComponentsInChildren<Text>(true)
                    .Count(text => text.text.Contains("오늘")),
                Is.EqualTo(1),
                "The reward action must render one status line inside its frame.");

            Canvas.ForceUpdateCanvases();
            RectTransform safeAreaRoot = contentRoot.parent as RectTransform;
            Assert.That(safeAreaRoot, Is.Not.Null);
            Vector2 targetExtents = CalculateTargetExtents(
                rewardStatus.rectTransform,
                safeAreaRoot);
            TextGenerator generator = new();
            Assert.That(
                generator.Populate(
                    rewardStatus.text,
                    rewardStatus.GetGenerationSettings(targetExtents)),
                Is.True);
            Assert.That(generator.lineCount, Is.EqualTo(1));
            Assert.That(
                generator.characterCountVisible,
                Is.GreaterThanOrEqualTo(rewardStatus.text.Length));
        }

        [TestCase("Easy", 10)]
        [TestCase("Normal", 20)]
        [TestCase("Hard", 30)]
        public void RewardCandidates_UseTheSelectedCumulativeDifficultyPool(
            string difficultyName,
            int expectedCount)
        {
            MethodInfo method = FindMethod("GetRewardedRelicCandidates");
            object character = ParseEnum(method.GetParameters()[0].ParameterType, CharacterId);
            object difficulty = ParseEnum(
                method.GetParameters()[1].ParameterType,
                difficultyName);

            object result = method.Invoke(controller, new[] { character, difficulty });
            Assert.That(result, Is.InstanceOf<IEnumerable>());
            Assert.That(((IEnumerable)result).Cast<object>().Count(), Is.EqualTo(expectedCount));
        }

        [Test]
        public void RewardResult_ShowsCommittedItemAndReturnsToSameCharacter()
        {
            object item = CreateRunItemDefinition(
                "relic-test",
                "테스트 유물",
                "Relic",
                "테스트 효과",
                "테스트 설명",
                string.Empty);

            MethodInfo resultMethod = FindMethod("ShowRewardedRelicResult");
            object character = ParseEnum(
                resultMethod.GetParameters()[0].ParameterType,
                CharacterId);
            resultMethod.Invoke(controller, new[] { character, item });

            Assert.That(FindRequired("보상형 유물 결과"), Is.Not.Null);
            Assert.That(FindText(contentRoot, "테스트 유물"), Is.Not.Null);

            RectTransform close = FindRequired("보상 결과 닫기");
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            try
            {
                LogAssert.ignoreFailingMessages = true;
                close.GetComponent<Button>().onClick.Invoke();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }

            Assert.That(FindRequired("보상형 유물 광고"), Is.Not.Null);
        }

        [Test]
        public void RewardCommit_DiscoversOneItemConsumesOneUseAndDoesNotEquipIt()
        {
            MethodInfo candidatesMethod = FindMethod("GetRewardedRelicCandidates");
            object character = ParseEnum(
                candidatesMethod.GetParameters()[0].ParameterType,
                CharacterId);
            object difficulty = ParseEnum(
                candidatesMethod.GetParameters()[1].ParameterType,
                "Easy");
            IList candidates = (IList)candidatesMethod.Invoke(
                controller,
                new[] { character, difficulty });
            Assert.That(candidates.Count, Is.EqualTo(10));

            object firstItem = candidates[0];
            string firstItemId = (string)firstItem.GetType()
                .GetProperty("Id", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(firstItem);
            MethodInfo commitMethod = FindMethod("CommitRewardedRelic");
            bool committed = (bool)commitMethod.Invoke(
                controller,
                new[] { character, difficulty, firstItemId });

            Assert.That(committed, Is.True);
            Assert.That(
                PlayerPrefs.GetString(DiscoveredKey, string.Empty),
                Does.Contain(firstItemId));
            Assert.That(PlayerPrefs.GetInt(DailyPrefix + "Count", 0), Is.EqualTo(1));
            Assert.That(PlayerPrefs.HasKey(EquippedKey), Is.False);

            IList remainingCandidates = (IList)candidatesMethod.Invoke(
                controller,
                new[] { character, difficulty });
            Assert.That(remainingCandidates.Count, Is.EqualTo(9));
        }

        private object CreateRunItemDefinition(
            string id,
            string name,
            string category,
            string effect,
            string description,
            string iconName)
        {
            Type itemType = controllerType.GetNestedType(
                "RunItemDefinition",
                BindingFlags.NonPublic);
            Type categoryType = controllerType.GetNestedType(
                "RunItemType",
                BindingFlags.NonPublic);
            Assert.That(itemType, Is.Not.Null);
            Assert.That(categoryType, Is.Not.Null);

            ConstructorInfo constructor = itemType.GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single();
            return constructor.Invoke(new[]
            {
                id,
                name,
                ParseEnum(categoryType, category),
                effect,
                description,
                iconName
            });
        }

        private void InvokeWithEnum(string methodName, string value)
        {
            MethodInfo method = FindMethod(methodName);
            object argument = ParseEnum(method.GetParameters()[0].ParameterType, value);
            method.Invoke(controller, new[] { argument });
        }

        private MethodInfo FindMethod(string methodName)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            return method;
        }

        private static object ParseEnum(Type enumType, string value)
        {
            return Enum.Parse(enumType, value);
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendant(contentRoot, objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private static RectTransform FindDescendant(
            RectTransform parent,
            string objectName)
        {
            for (int index = 0; index < parent.childCount; index += 1)
            {
                RectTransform child = parent.GetChild(index) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                if (child.name == objectName)
                {
                    return child;
                }

                RectTransform nested = FindDescendant(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static Text FindText(RectTransform parent, string fragment)
        {
            return parent.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.text.Contains(fragment));
        }

        private static Vector2 CalculateTargetExtents(
            RectTransform target,
            RectTransform safeAreaRoot)
        {
            float scaleFactor = Mathf.Sqrt(
                (TargetScreenWidth / 1920f)
                * (TargetScreenHeight / 1080f));
            Vector2 extents = new(
                TargetSafeWidth / scaleFactor,
                TargetSafeHeight / scaleFactor);

            RectTransform current = target;
            while (current != null && current != safeAreaRoot)
            {
                extents = Vector2.Scale(
                    extents,
                    current.anchorMax - current.anchorMin);
                current = current.parent as RectTransform;
            }

            if (current != safeAreaRoot)
            {
                throw new InvalidOperationException(
                    "The text must descend from the mobile safe-area root.");
            }

            return extents;
        }

        private T GetField<T>(string fieldName) where T : class
        {
            return GetFieldInfo(fieldName).GetValue(controller) as T;
        }

        private void SetField(string fieldName, object value)
        {
            GetFieldInfo(fieldName).SetValue(controller, value);
        }

        private FieldInfo GetFieldInfo(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return field;
        }

        private void PreserveStringPreference(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                savedStringValues[key] = PlayerPrefs.GetString(key, string.Empty);
                return;
            }

            missingKeys.Add(key);
        }

        private void PreserveIntPreference(string key)
        {
            if (PlayerPrefs.HasKey(key))
            {
                savedIntValues[key] = PlayerPrefs.GetInt(key, 0);
                return;
            }

            missingKeys.Add(key);
        }

        private static void DeleteTestPreferences()
        {
            PlayerPrefs.DeleteKey(DiscoveredKey);
            PlayerPrefs.DeleteKey(EquippedKey);
            PlayerPrefs.DeleteKey(DailyPrefix + "Date");
            PlayerPrefs.DeleteKey(DailyPrefix + "Count");
            PlayerPrefs.DeleteKey(DailyPrefix + "GreatestObservedUtc");
            PlayerPrefs.Save();
        }

        private void RestoreTestPreferences()
        {
            DeleteTestPreferences();
            foreach (KeyValuePair<string, string> pair in savedStringValues)
            {
                PlayerPrefs.SetString(pair.Key, pair.Value);
            }

            foreach (KeyValuePair<string, int> pair in savedIntValues)
            {
                PlayerPrefs.SetInt(pair.Key, pair.Value);
            }

            foreach (string key in missingKeys)
            {
                PlayerPrefs.DeleteKey(key);
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
    }
}
