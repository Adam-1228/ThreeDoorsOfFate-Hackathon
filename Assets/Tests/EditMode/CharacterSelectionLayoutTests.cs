using System;
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
    public sealed class CharacterSelectionLayoutTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string FontPath = "Assets/Fonts/GowunBatang-Regular.ttf";
        private const float TargetScreenWidth = 2796f;
        private const float TargetScreenHeight = 1290f;
        private const float TargetSafeWidth = 2532f;
        private const float TargetSafeHeight = 1164f;

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
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "en");
            GameLocalization.Initialize(SystemLanguage.English);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("Character Selection Layout Test Host");
            controller = controllerHost.AddComponent(controllerType);
            embeddedFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Assert.That(embeddedFont, Is.Not.Null, "The embedded Korean UI font is required.");
            SetField("uiFontAsset", embeddedFont);
            SetField("uiFont", embeddedFont);
            contentRoot = GetField<RectTransform>("contentRoot");
            if (contentRoot == null)
            {
                Invoke("BuildShell");
                contentRoot = GetField<RectTransform>("contentRoot");
            }

            Assert.That(contentRoot, Is.Not.Null, "Controller must build its runtime UI shell.");
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
                EventSystem createdEventSystem =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdEventSystem.gameObject);
                }
            }

            RestoreLanguagePreference();
        }

        [Test]
        public void MainMenu_QaMobileLayoutFindsSettingsButton()
        {
            Invoke("ShowMainMenu");
            Type captureType = Type.GetType(
                "ThreeDoorsOfFate.Editor.HowToPlaySourceQACapture, Assembly-CSharp-Editor");
            Assert.That(captureType, Is.Not.Null);
            MethodInfo method = captureType.GetMethod(
                "ConfigureMobileMainMenuButtons",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            Assert.That(
                () => method.Invoke(
                    null,
                    new object[] { controllerType, controller }),
                Throws.Nothing);
        }

        [Test]
        public void ClassSelection_EnglishHeaderUsesLocalizationCatalog()
        {
            Invoke("ShowClassSelection");

            Text titleText = GetField<Text>("titleText");
            Text subtitleText = GetField<Text>("subtitleText");

            Assert.That(titleText.text, Is.EqualTo("Three Doors of Fate"));
            Assert.That(
                subtitleText.text,
                Is.EqualTo("Choose who will face the first door — Easy"));
        }

        [Test]
        public void Settings_EnglishLabelsAreLocalizedExceptKoreanLanguageName()
        {
            Invoke("ShowMainMenu");
            Invoke("ShowSettingsPanel");

            string[] labels = canvasRoot
                .GetComponentsInChildren<Text>(true)
                .Where(text => text.gameObject.activeInHierarchy)
                .Select(text => text.text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();

            CollectionAssert.IsSubsetOf(
                new[]
                {
                    "Settings",
                    "Language",
                    "한국어",
                    "English",
                    "Display Mode",
                    "Fullscreen",
                    "Windowed",
                    "Volume",
                    "Close",
                    "Main Menu",
                    "Quit Game"
                },
                labels);

            string unexpectedHangul = string.Join("\n", labels)
                .Replace("한국어", string.Empty);
            Assert.That(
                unexpectedHangul,
                Does.Not.Match("[가-힣]"),
                $"Unexpected Korean text remained in English settings: {unexpectedHangul}");
        }

        [Test]
        public void ClassSelection_EnglishNameLabelsRemainVisibleInsideFrames()
        {
            Invoke("ShowClassSelection");
            Canvas.ForceUpdateCanvases();
            RectTransform safeAreaRoot = contentRoot.parent as RectTransform;
            Assert.That(safeAreaRoot, Is.Not.Null);

            foreach (string expectedName in new[] { "Gambler", "Oracle", "Exile" })
            {
                Text nameText = contentRoot
                    .GetComponentsInChildren<Text>(true)
                    .SingleOrDefault(text =>
                        text.name == "직업명" && text.text == expectedName);

                Assert.That(nameText, Is.Not.Null, $"Expected visible label for {expectedName}.");
                Assert.That(nameText.gameObject.activeInHierarchy, Is.True);
                Assert.That(nameText.font, Is.SameAs(embeddedFont));
                Assert.That(nameText.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
                Assert.That(nameText.resizeTextForBestFit, Is.True);
                Assert.That(nameText.resizeTextMinSize, Is.GreaterThanOrEqualTo(16));
                Assert.That(
                    nameText.rectTransform.anchorMin.y,
                    Is.LessThanOrEqualTo(0.1201f));
                Assert.That(
                    nameText.rectTransform.anchorMax.y,
                    Is.GreaterThanOrEqualTo(0.8799f));
                Assert.That(
                    nameText.verticalOverflow,
                    Is.EqualTo(VerticalWrapMode.Truncate));

                Vector2 targetExtents = CalculateTargetExtents(
                    nameText.rectTransform,
                    safeAreaRoot);
                TextGenerator generator = new();
                Assert.That(
                    generator.Populate(
                        nameText.text,
                        nameText.GetGenerationSettings(targetExtents)),
                    Is.True);
                Assert.That(generator.lineCount, Is.EqualTo(1));
                Assert.That(
                    generator.characterCountVisible,
                    Is.GreaterThanOrEqualTo(nameText.text.Length));
            }
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

        private void Invoke(string methodName)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            method.Invoke(controller, Array.Empty<object>());
        }

        private T GetField<T>(string fieldName) where T : class
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return field.GetValue(controller) as T;
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, value);
        }
    }
}
