using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104RunUiTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "en");
            PlayerPrefs.Save();
            GameLocalization.Initialize(SystemLanguage.English);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);
            controllerHost = new GameObject("Quality 1.0.4 Run UI Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = TryGetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = TryGetField<RectTransform>("root");
            }

            canvasRoot = TryGetField<RectTransform>("canvasRoot");
            SetField("playerHealth", 40);
            SetField("playerMaxHealth", 60);
            SetField("playerBlock", 5);
            SetField("action", 2);
            SetField("gold", 20);
            SetField("debt", 2);
            SetField("roomsCleared", 3);
            GameLocalization.SetLanguage(GameLanguage.English);
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
                EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }

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
        public void TopBar_UsesSeparateNonOverlappingProgressAndResourceGroups()
        {
            Text progress = GetField<Text>("runProgressText");
            Text resources = GetField<Text>("runResourcesText");

            RectTransform progressRect = progress.rectTransform;
            RectTransform resourcesRect = resources.rectTransform;
            Assert.That(resourcesRect.anchorMax.y, Is.LessThanOrEqualTo(progressRect.anchorMin.y));
            Assert.That(progress.resizeTextForBestFit, Is.True);
            Assert.That(resources.resizeTextForBestFit, Is.True);
            Assert.That(progress.resizeTextMinSize, Is.GreaterThanOrEqualTo(12));
            Assert.That(resources.resizeTextMinSize, Is.GreaterThanOrEqualTo(12));
        }

        [Test]
        public void ProgressLogHeader_UsesTheActiveLanguage()
        {
            Text logTitle = FindRequired("기록 제목").GetComponent<Text>();

            Assert.That(logTitle.text, Is.EqualTo("Progress Log"));
            Assert.That(logTitle.resizeTextForBestFit, Is.True);
            Assert.That(logTitle.resizeTextMinSize, Is.GreaterThanOrEqualTo(14));
        }

        [Test]
        public void CombatVitals_RemainVisibleWhileSubtitleIsVisible()
        {
            SetEnumField("phase", "Combat");
            Invoke("SetSubtitleBoxVisible", true);
            Invoke("RefreshTopBar");

            Text vitals = GetField<Text>("playerStatsText");
            Assert.That(vitals.gameObject.activeSelf, Is.True);
            Assert.That(vitals.text, Does.Contain("HP 40/60"));
            Assert.That(vitals.text, Does.Contain("Block 5"));
            Assert.That(vitals.text, Does.Contain("Action 2"));
        }

        [Test]
        public void SubtitleFrame_SharesTheTitleRowAndKeepsItsTextInsideTheFrame()
        {
            RectTransform topBar = GetField<RectTransform>("topBar");
            RectTransform titleFrame = FindRequired("상단 제목 박스");
            RectTransform subtitleFrame = GetField<RectTransform>("subtitleFrame");
            Text subtitle = GetField<Text>("subtitleText");
            subtitle.text = "Choose one of the three doors";

            Invoke("SetSubtitleBoxVisible", true);
            Canvas.ForceUpdateCanvases();

            Assert.That(subtitleFrame.parent, Is.SameAs(topBar));
            Assert.That(subtitleFrame.anchorMin.y, Is.EqualTo(titleFrame.anchorMin.y).Within(0.0001f));
            Assert.That(subtitleFrame.anchorMax.y, Is.EqualTo(titleFrame.anchorMax.y).Within(0.0001f));
            Assert.That(subtitleFrame.anchorMin.x, Is.GreaterThanOrEqualTo(titleFrame.anchorMax.x));
            Assert.That(subtitle.rectTransform.anchorMin.x, Is.InRange(0f, 1f));
            Assert.That(subtitle.rectTransform.anchorMin.y, Is.InRange(0f, 1f));
            Assert.That(subtitle.rectTransform.anchorMax.x, Is.InRange(0f, 1f));
            Assert.That(subtitle.rectTransform.anchorMax.y, Is.InRange(0f, 1f));
            Assert.That(subtitle.gameObject.activeInHierarchy, Is.True);
            Assert.That(subtitle.text, Is.Not.Empty);
        }

        [Test]
        public void Rest_ShowsCurrentStateAndProjectedHeal()
        {
            Invoke("ShowRest");
            Canvas.ForceUpdateCanvases();

            Text context = FindRequired("선택 현재 상태").GetComponent<Text>();
            string healLabel = FindRequired("회복")
                .GetComponentInChildren<Text>(true).text;

            Assert.That(context.text, Does.Contain("HP 40/60"));
            Assert.That(context.text, Does.Contain("Gold 20"));
            Assert.That(context.text, Does.Contain("Debt 2"));
            Assert.That(healLabel, Does.Contain("58/60"));
        }

        [Test]
        public void EventCatalog_ShowsCurrentStateAndBothProjectedOutcomes()
        {
            SetField("pendingRunEventId", "event.forgotten_altar");
            Invoke("ShowEvent");
            Canvas.ForceUpdateCanvases();

            Text context = FindRequired("선택 현재 상태").GetComponent<Text>();
            string restoreLabel = FindRequired("사건 선택 1")
                .GetComponentInChildren<Text>(true).text;
            string endureLabel = FindRequired("사건 선택 2")
                .GetComponentInChildren<Text>(true).text;

            Assert.That(context.text, Does.Contain("HP 40/60"));
            Assert.That(context.text, Does.Contain("Gold 20"));
            Assert.That(context.text, Does.Contain("Debt 2"));
            Assert.That(restoreLabel, Does.Contain("40/60 → 54/60"));
            Assert.That(restoreLabel, Does.Contain("20 → 8"));
            Assert.That(endureLabel, Does.Contain("40/60 → 32/64"));
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendants(root)
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
    }
}
