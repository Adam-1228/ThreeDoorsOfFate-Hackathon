using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class HowToPlayPointerTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private readonly List<Texture2D> textures = new();
        private readonly List<Sprite> sprites = new();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("How To Play Pointer Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = GetField<RectTransform>("root");
            canvasRoot = GetField<RectTransform>("canvasRoot");
            Assert.That(root, Is.Not.Null, "Awake must build the runtime UI shell.");
            Assert.That(canvasRoot, Is.Not.Null, "Awake must build the runtime canvas.");

            for (int index = 0; index < 5; index += 1)
            {
                Texture2D texture = CreateTexture(
                    Color.HSVToRGB(index / 5f, 0.58f, 0.85f));
                textures.Add(texture);
                sprites.Add(CreateSprite(texture, $"Pointer Tutorial {index + 1}"));
            }

            SetField("howToPlaySprites", new List<Sprite>(sprites));
            Invoke("ShowMainMenu");

            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (canvasRoot != null)
            {
                UnityEngine.Object.Destroy(canvasRoot.gameObject);
            }

            if (controllerHost != null)
            {
                UnityEngine.Object.Destroy(controllerHost);
            }

            foreach (Sprite sprite in sprites)
            {
                if (sprite != null)
                {
                    UnityEngine.Object.Destroy(sprite);
                }
            }

            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            if (originalEventSystem == null)
            {
                EventSystem createdEventSystem =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    UnityEngine.Object.Destroy(createdEventSystem.gameObject);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerRaycasts_OpenNavigateBlockBackgroundAndCloseGuide()
        {
            RectTransform guideButton = FindRequired("플레이 방법");
            Assert.That(
                RaycastAndClick(guideButton),
                Is.SameAs(guideButton.gameObject),
                "The visible guide button must receive a real raycast click.");

            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform overlay = GetField<RectTransform>("howToPlayOverlay");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(0));

            Image guideImage = GetField<Image>("howToPlayImage");
            GameObject imageAreaHandler = RaycastAndClick(guideImage.rectTransform);
            Assert.That(
                imageAreaHandler,
                Is.Not.SameAs(guideButton.gameObject),
                "The modal blocker must prevent the hidden main-menu button from receiving image-area taps.");
            Assert.That(GetField<RectTransform>("howToPlayOverlay"), Is.SameAs(overlay));
            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(0));

            Vector3 outsideModalWorld = root.TransformPoint(new Vector3(
                Mathf.Lerp(root.rect.xMin, root.rect.xMax, 0.015f),
                Mathf.Lerp(root.rect.yMin, root.rect.yMax, 0.50f),
                0f));
            GameObject outsideHandler = RaycastAndClick(
                RectTransformUtility.WorldToScreenPoint(null, outsideModalWorld));
            Assert.That(outsideHandler, Is.SameAs(overlay.gameObject));
            Assert.That(GetField<RectTransform>("howToPlayOverlay"), Is.SameAs(overlay));
            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(0));

            Button nextButton = GetField<Button>("howToPlayNextButton");
            Assert.That(
                RaycastAndClick(nextButton.GetComponent<RectTransform>()),
                Is.SameAs(nextButton.gameObject));
            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(1));
            Assert.That(GetField<Text>("howToPlayProgressText").text, Is.EqualTo("2 / 5"));

            Button closeButton = GetField<Button>("howToPlayCloseButton");
            Assert.That(
                RaycastAndClick(closeButton.GetComponent<RectTransform>()),
                Is.SameAs(closeButton.gameObject));
            Assert.That(GetField<RectTransform>("howToPlayOverlay"), Is.Null);
            Assert.That(GetField<object>("phase").ToString(), Is.EqualTo("MainMenu"));
            Assert.That(FindRequired("게임시작").gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator PointerRaycasts_CompleteTheHandFlowPractice()
        {
            Assert.That(
                RaycastAndClick(FindRequired("플레이 방법")),
                Is.Not.Null);
            yield return null;

            Button nextButton = GetField<Button>("howToPlayNextButton");
            for (int page = 0; page < 3; page += 1)
            {
                Assert.That(
                    RaycastAndClick(nextButton.GetComponent<RectTransform>()),
                    Is.SameAs(nextButton.gameObject));
                yield return null;
            }

            Assert.That(GetField<int>("howToPlayPageIndex"), Is.EqualTo(3));
            Button endTurn = GetField<Button>("handFlowPracticeEndTurnButton");
            Assert.That(
                RaycastAndClick(endTurn.GetComponent<RectTransform>()),
                Is.SameAs(endTurn.gameObject));

            List<Button> practiceCards =
                GetField<List<Button>>("handFlowPracticeCardButtons");
            Assert.That(
                RaycastAndClick(practiceCards[0].GetComponent<RectTransform>()),
                Is.SameAs(practiceCards[0].gameObject));

            Button use = GetField<Button>("handFlowPracticeUseButton");
            Assert.That(
                RaycastAndClick(use.GetComponent<RectTransform>()),
                Is.SameAs(use.gameObject));
            Assert.That(GetField<int>("handFlowPracticeDrawCount"), Is.EqualTo(1));
            Assert.That(GetField<int>("handFlowPracticeStep"), Is.EqualTo(3));
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new(4, 4, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[16];
            for (int index = 0; index < pixels.Length; index += 1)
            {
                pixels[index] = color;
            }

            texture.SetPixels(pixels);
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

        private GameObject RaycastAndClick(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            return RaycastAndClick(
                RectTransformUtility.WorldToScreenPoint(null, worldCenter));
        }

        private GameObject RaycastAndClick(Vector2 screenPosition)
        {
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null, "PlayMode must have the runtime EventSystem.");

            GraphicRaycaster raycaster = canvasRoot.GetComponent<GraphicRaycaster>();
            Assert.That(raycaster, Is.Not.Null, "Runtime canvas must own a GraphicRaycaster.");

            PointerEventData pointer = new(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left
            };
            List<RaycastResult> results = new();
            raycaster.Raycast(pointer, results);
            Assert.That(results, Is.Not.Empty, $"Expected a hit at {screenPosition}.");

            GameObject hit = results[0].gameObject;
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
            GameObject handler = ExecuteEvents.ExecuteHierarchy(
                hit,
                pointer,
                ExecuteEvents.pointerClickHandler);
            Assert.That(handler, Is.Not.Null, $"Expected a handler below '{hit.name}'.");
            return handler;
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

        private T GetField<T>(string fieldName)
        {
            return (T)GetFieldInfo(fieldName).GetValue(controller);
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
    }
}
