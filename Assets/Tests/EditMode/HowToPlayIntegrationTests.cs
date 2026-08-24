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
    public sealed class HowToPlayIntegrationTests
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
        private readonly List<Texture2D> textures = new();
        private readonly List<Sprite> sprites = new();

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(
                GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "ko");
            PlayerPrefs.Save();
            GameLocalization.Initialize(SystemLanguage.Korean);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("How To Play Integration Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = TryGetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = TryGetField<RectTransform>("root");
            }

            Assert.That(root, Is.Not.Null, "Controller must build its runtime UI shell.");
            canvasRoot = TryGetField<RectTransform>("canvasRoot");

            textures.Clear();
            sprites.Clear();
            for (int index = 0; index < 5; index += 1)
            {
                Texture2D texture = CreateTexture(
                    Color.HSVToRGB(index / 5f, 0.55f, 0.82f));
                textures.Add(texture);
                sprites.Add(CreateSprite(texture, $"How To Play Test {index + 1}"));
            }

            FieldInfo spriteField = controllerType.GetField(
                "howToPlaySprites",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (spriteField != null)
            {
                spriteField.SetValue(controller, new List<Sprite>(sprites));
            }

            Invoke("ShowMainMenu");
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

            foreach (Sprite sprite in sprites)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.DestroyImmediate(sprite);
                }
            }

            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
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

        [Test]
        public void MainMenu_OffersHowToPlayAndNonOverlappingAchievementReadyButtons()
        {
            RectTransform guide = FindRequired("플레이 방법");
            Assert.That(guide.GetComponent<Button>(), Is.Not.Null);

            Rect[] desktopRects = Enumerable.Range(0, 5)
                .Select(index => InvokeStaticRect(
                    "GetMainMenuButtonRect",
                    index,
                    5))
                .ToArray();
            AssertNonOverlappingInsideScreen(desktopRects);

            Rect[] mobileRects = Enumerable.Range(0, 4)
                .Select(index => InvokeStaticRect(
                    "GetMainMenuButtonRect",
                    index,
                    4))
                .ToArray();
            AssertNonOverlappingInsideScreen(mobileRects);

            Rect expectedGuide = desktopRects[1];
            Assert.That(
                guide.anchorMin,
                Is.EqualTo(new Vector2(expectedGuide.xMin, expectedGuide.yMin)));
            Assert.That(
                guide.anchorMax,
                Is.EqualTo(new Vector2(expectedGuide.xMax, expectedGuide.yMax)));
        }

        [Test]
        public void GuideNavigation_ClampsPagesAndCompletesFromPageFive()
        {
            OpenGuide();
            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(0));
            Assert.That(
                GetField<Text>("howToPlayProgressText").text,
                Is.EqualTo("1 / 5"));
            Assert.That(
                GetField<Button>("howToPlayPreviousButton").interactable,
                Is.False);

            for (int index = 0; index < 4; index += 1)
            {
                GetField<Button>("howToPlayNextButton").onClick.Invoke();
            }

            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(4));
            Assert.That(
                GetField<Text>("howToPlayProgressText").text,
                Is.EqualTo("5 / 5"));
            Assert.That(
                GetButtonLabel(GetField<Button>("howToPlayNextButton")),
                Is.EqualTo("완료"));

            GetField<Button>("howToPlayNextButton").onClick.Invoke();
            Assert.That(
                GetField<RectTransform>("howToPlayOverlay"),
                Is.Null);
        }

        [Test]
        public void CloseAndReopen_ResetsToFirstPageWithoutChangingGameState()
        {
            object phaseBefore = GetField<object>("phase");
            OpenGuide();
            GetField<Button>("howToPlayNextButton").onClick.Invoke();
            GetField<Button>("howToPlayCloseButton").onClick.Invoke();

            OpenGuide();

            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(0));
            Assert.That(GetField<object>("phase"), Is.EqualTo(phaseBefore));
        }

        [Test]
        public void Guide_HidesUnderlyingMainMenuContentAndRestoresItOnClose()
        {
            RectTransform mainMenuContent = GetField<RectTransform>("contentRoot");
            Assert.That(mainMenuContent.gameObject.activeSelf, Is.True);

            OpenGuide();

            Assert.That(
                mainMenuContent.gameObject.activeSelf,
                Is.False,
                "The translucent guide must not leave duplicate main-menu controls visible underneath it.");
            Assert.That(GetField<RectTransform>("howToPlayOverlay"), Is.Not.Null);

            GetField<Button>("howToPlayCloseButton").onClick.Invoke();

            Assert.That(mainMenuContent.gameObject.activeSelf, Is.True);
            Assert.That(GetField<RectTransform>("howToPlayOverlay"), Is.Null);
        }

        [Test]
        public void MissingImage_ShowsFallbackWhileNavigationRemainsAvailable()
        {
            GetField<List<Sprite>>("howToPlaySprites")[2] = null;
            OpenGuide();
            GetField<Button>("howToPlayNextButton").onClick.Invoke();
            GetField<Button>("howToPlayNextButton").onClick.Invoke();

            Assert.That(
                GetField<Image>("howToPlayImage").gameObject.activeSelf,
                Is.False);
            Assert.That(
                GetField<Text>("howToPlayMissingImageText").gameObject.activeSelf,
                Is.True);
            Assert.That(
                GetField<Button>("howToPlayNextButton").interactable,
                Is.True);
        }

        [Test]
        public void GuideImage_UsesSafeLandscapeFrameAndReadableCaption()
        {
            OpenGuide();

            Image image = GetField<Image>("howToPlayImage");
            Assert.That(image.preserveAspect, Is.True);
            Assert.That(image.raycastTarget, Is.False);
            Assert.That(image.rectTransform.anchorMin, Is.EqualTo(new Vector2(0.075f, 0.265f)));
            Assert.That(image.rectTransform.anchorMax, Is.EqualTo(new Vector2(0.925f, 0.855f)));

            Text caption = GetField<Text>("howToPlayCaptionText");
            Assert.That(caption.alignment, Is.EqualTo(TextAnchor.MiddleCenter));
            Assert.That(caption.resizeTextForBestFit, Is.True);
            Assert.That(caption.resizeTextMinSize, Is.GreaterThanOrEqualTo(15));
            Assert.That(caption.verticalOverflow, Is.EqualTo(VerticalWrapMode.Truncate));
        }

        [Test]
        public void EnglishGuide_HidesKoreanScreenshotAndBuildsLocalizedRuntimeVisual()
        {
            GameLocalization.SetLanguage(GameLanguage.English);

            OpenGuide();

            Assert.That(
                GetField<Image>("howToPlayImage").gameObject.activeSelf,
                Is.False);
            Assert.That(
                GetField<Text>("howToPlayMissingImageText").gameObject.activeSelf,
                Is.False);
            RectTransform englishVisual =
                GetField<RectTransform>("howToPlayEnglishVisualRoot");
            Assert.That(englishVisual.gameObject.activeSelf, Is.True);
            Assert.That(englishVisual.childCount, Is.EqualTo(3));
            Assert.That(
                GetField<Text>("howToPlayTitleText").text,
                Is.EqualTo("Class & Difficulty"));
            Assert.That(
                GetButtonLabel(GetField<Button>("howToPlayNextButton")),
                Is.EqualTo("Next"));
        }

        [Test]
        public void EnglishGuide_StepLabelsStayClearOfDecorativeBottomFrame()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            SetField("panelSprite", sprites[0]);
            SetField("statusSectionMediumFrameSprite", sprites[1]);

            OpenGuide();

            RectTransform englishVisual =
                GetField<RectTransform>("howToPlayEnglishVisualRoot");
            Sprite safeStepFrame = GetField<Sprite>("panelSprite");
            for (int index = 0; index < englishVisual.childCount; index += 1)
            {
                RectTransform step = (RectTransform)englishVisual.GetChild(index);
                Assert.That(
                    step.GetComponent<Image>().sprite,
                    Is.SameAs(safeStepFrame),
                    "Tall tutorial steps must not use the ornate status frame whose internal bars cross the label.");
                RectTransform label = FindDescendant(step, "영문 플레이 방법 안내");
                Assert.That(label, Is.Not.Null);
                Assert.That(
                    label.anchorMin.y,
                    Is.GreaterThanOrEqualTo(0.18f),
                    "Step text needs enough bottom inset to clear the ornate frame border.");
            }
        }

        private void OpenGuide()
        {
            FindRequired("플레이 방법").GetComponent<Button>().onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }

        private static void AssertNonOverlappingInsideScreen(Rect[] rects)
        {
            Assert.That(rects[0].xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(rects[^1].xMax, Is.LessThanOrEqualTo(1f));
            for (int index = 1; index < rects.Length; index += 1)
            {
                Assert.That(rects[index].xMin, Is.GreaterThan(rects[index - 1].xMax));
            }
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(color, 16).ToArray());
            texture.Apply();
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture, string name)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = name;
            return sprite;
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendant(root, objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private static RectTransform FindDescendant(
            RectTransform parent,
            string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index) is not RectTransform child)
                {
                    continue;
                }

                RectTransform found = FindDescendant(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void Invoke(string methodName)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            method.Invoke(controller, Array.Empty<object>());
        }

        private Rect InvokeStaticRect(string methodName, int index, int count)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected static controller method '{methodName}'.");
            return (Rect)method.Invoke(null, new object[] { index, count });
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return (T)field.GetValue(controller);
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, value);
        }

        private T TryGetField<T>(string fieldName)
            where T : class
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(controller) as T;
        }

        private static string GetButtonLabel(Button button)
        {
            Text label = button.GetComponentInChildren<Text>(true);
            Assert.That(label, Is.Not.Null);
            return label.text;
        }
    }
}
