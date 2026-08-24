using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class DoorChoiceLayoutTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string FontPath = "Assets/Fonts/GowunBatang-Regular.ttf";
        private const string LongHint =
            "예언 선명: 문 너머의 정예 적을 쓰러뜨리면 강화된 카드와 금화, 미발견 유물을 함께 얻을 수 있습니다.";
        private const float TargetScreenWidth = 2796f;
        private const float TargetScreenHeight = 1290f;
        private const float TargetSafeWidth = 2532f;
        private const float TargetSafeHeight = 1164f;

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform contentRoot;
        private RectTransform canvasRoot;
        private RectTransform safeAreaRoot;
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

            controllerHost = new GameObject("Door Choice Layout Test Host");
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
            safeAreaRoot = contentRoot.parent as RectTransform;
            Assert.That(safeAreaRoot, Is.Not.Null, "Door UI must descend from the mobile safe area.");

            Invoke("ShowDoors");
            Canvas.ForceUpdateCanvases();
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
        public void RunStart_EnglishTitleUsesLocalizationCatalog()
        {
            MethodInfo method = controllerType.GetMethod(
                "StartRun",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object gambler = Enum.Parse(
                method.GetParameters()[0].ParameterType,
                "Gambler");

            method.Invoke(controller, new[] { gambler });

            Assert.That(
                GetField<Text>("titleText").text,
                Is.EqualTo("Three Doors of Fate"));
        }

        [Test]
        public void DoorSelection_EnglishSurfaceContainsNoKoreanText()
        {
            string[] koreanText = canvasRoot
                .GetComponentsInChildren<Text>(true)
                .Where(text => text.gameObject.activeInHierarchy)
                .Select(text => text.text ?? string.Empty)
                .Where(text => Regex.IsMatch(text, "[가-힣]"))
                .ToArray();

            Assert.That(
                koreanText,
                Is.Empty,
                $"Active Korean text remained: {string.Join(" | ", koreanText)}");
            Assert.That(GetField<Text>("titleText").text, Is.EqualTo("Three Doors of Fate"));
            Assert.That(
                GetField<Text>("subtitleText").text,
                Is.EqualTo("Choose one of the three doors"));
        }

        [Test]
        public void DoorHints_StayInMaskedSafeAreaAboveLowerFrameTrim()
        {
            foreach (RectTransform card in GetDoorCards())
            {
                RectTransform artViewport = FindDirectRequired(
                    card,
                    "문 이미지 마스크 영역");
                RectTransform hintSafeArea = FindDirectRequired(
                    card,
                    "문 설명 안전영역");

                Image safeAreaBacking = hintSafeArea.GetComponent<Image>();
                Assert.That(safeAreaBacking, Is.Not.Null);
                Assert.That(
                    safeAreaBacking.raycastTarget,
                    Is.False,
                    "The hint safe area must not intercept door-card taps.");
                Assert.That(
                    hintSafeArea.GetComponent<RectMask2D>(),
                    Is.Not.Null,
                    "The frame-safe area must clip text and glow at its boundary.");
                Assert.That(
                    artViewport.anchorMin.y - hintSafeArea.anchorMax.y,
                    Is.GreaterThanOrEqualTo(0.0099f),
                    "Door hint text must retain a visible gap below the art viewport.");
                Assert.That(hintSafeArea.anchorMin.y, Is.GreaterThanOrEqualTo(0.0999f));
                Assert.That(hintSafeArea.anchorMax.y, Is.LessThanOrEqualTo(0.2751f));

                Text hint = FindDirectRequired(hintSafeArea, "힌트").GetComponent<Text>();
                Assert.That(hint, Is.Not.Null);
                Assert.That(hint.alignment, Is.EqualTo(TextAnchor.UpperCenter));
                Assert.That(hint.horizontalOverflow, Is.EqualTo(HorizontalWrapMode.Wrap));
                Assert.That(hint.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
                Assert.That(hint.resizeTextForBestFit, Is.True);
                Assert.That(hint.resizeTextMinSize, Is.EqualTo(9));
                Assert.That(hint.resizeTextMaxSize, Is.EqualTo(12));
                Assert.That(hint.rectTransform.anchorMin.x, Is.GreaterThanOrEqualTo(0f));
                Assert.That(hint.rectTransform.anchorMin.y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(hint.rectTransform.anchorMax.x, Is.LessThanOrEqualTo(1f));
                Assert.That(hint.rectTransform.anchorMax.y, Is.LessThanOrEqualTo(1f));
            }
        }

        [Test]
        public void DoorHints_LongKoreanDescriptionFitsAtIPhoneLandscapeTarget()
        {
            foreach (RectTransform card in GetDoorCards())
            {
                RectTransform hintSafeArea = FindDirectRequired(
                    card,
                    "문 설명 안전영역");
                Text hint = FindDirectRequired(hintSafeArea, "힌트").GetComponent<Text>();
                hint.text = LongHint;

                Vector2 targetExtents = CalculateTargetExtents(
                    hint.rectTransform,
                    safeAreaRoot);
                TextGenerator generator = new();

                Assert.That(
                    generator.Populate(
                        hint.text,
                        hint.GetGenerationSettings(targetExtents)),
                    Is.True);
                Assert.That(
                    generator.lineCount,
                    Is.GreaterThanOrEqualTo(2),
                    "The long fixture must exercise the wrapping path.");
                Assert.That(
                    generator.characterCountVisible,
                    Is.GreaterThanOrEqualTo(LongHint.Length),
                    "Every character in a long hint must render inside the frame-safe rect.");
            }
        }

        private RectTransform[] GetDoorCards()
        {
            RectTransform[] cards = Enumerable.Range(1, 3)
                .Select(index => FindDirectRequired(contentRoot, $"문 {index}"))
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(3));
            return cards;
        }

        private static RectTransform FindDirectRequired(
            RectTransform parent,
            string objectName)
        {
            for (int index = 0; index < parent.childCount; index += 1)
            {
                RectTransform child = parent.GetChild(index) as RectTransform;
                if (child != null && child.name == objectName)
                {
                    return child;
                }
            }

            Assert.Fail($"Expected direct child '{objectName}' under '{parent.name}'.");
            return null;
        }

        private static Vector2 CalculateTargetExtents(
            RectTransform target,
            RectTransform mobileSafeAreaRoot)
        {
            float scaleFactor = Mathf.Sqrt(
                (TargetScreenWidth / 1920f)
                * (TargetScreenHeight / 1080f));
            Vector2 extents = new(
                TargetSafeWidth / scaleFactor,
                TargetSafeHeight / scaleFactor);

            RectTransform current = target;
            while (current != null && current != mobileSafeAreaRoot)
            {
                extents = Vector2.Scale(
                    extents,
                    current.anchorMax - current.anchorMin);
                current = current.parent as RectTransform;
            }

            if (current != mobileSafeAreaRoot)
            {
                throw new InvalidOperationException(
                    "The hint must descend from the mobile safe-area root.");
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
