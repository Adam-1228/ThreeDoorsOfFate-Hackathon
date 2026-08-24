using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104SettingsIconTests
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
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);
            controllerHost = new GameObject("Quality 1.0.4 Settings Icon Test Host");
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

        [TestCase("ko", "설정")]
        [TestCase("en", "Settings")]
        public void MainMenuSettings_UsesLargeNonOverlappingGearAndReadableLabel(
            string languageCode,
            string expectedLabel)
        {
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, languageCode);
            PlayerPrefs.Save();
            GameLocalization.SetLanguage(
                languageCode == "ko" ? GameLanguage.Korean : GameLanguage.English);
            Invoke("ShowMainMenu");
            Canvas.ForceUpdateCanvases();

            Button settings = root
                .GetComponentsInChildren<Button>(true)
                .Single(button => button.name == "설정"
                    && button.gameObject.activeInHierarchy);
            Image icon = settings.GetComponentsInChildren<Image>(true)
                .Single(image => image.name == "설정 톱니바퀴");
            Text[] labels = settings.GetComponentsInChildren<Text>(true)
                .Where(text => text.name == "라벨")
                .ToArray();
            Assert.That(labels, Has.Length.EqualTo(1));
            Text label = labels[0];

            Assert.That(icon.rectTransform.anchorMax.x - icon.rectTransform.anchorMin.x, Is.GreaterThanOrEqualTo(0.30f));
            Assert.That(icon.rectTransform.anchorMax.y - icon.rectTransform.anchorMin.y, Is.GreaterThanOrEqualTo(0.72f));
            Assert.That(icon.rectTransform.anchorMax.x, Is.LessThanOrEqualTo(label.rectTransform.anchorMin.x));
            Assert.That(label.text, Is.EqualTo(expectedLabel));
            Assert.That(label.resizeTextForBestFit, Is.True);
            Assert.That(label.resizeTextMinSize, Is.GreaterThanOrEqualTo(16));
            Assert.That(icon.raycastTarget, Is.False);
            Assert.That(settings.interactable, Is.True);
        }

        private void Invoke(string methodName)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}'.");
            method.Invoke(controller, Array.Empty<object>());
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
    }
}
