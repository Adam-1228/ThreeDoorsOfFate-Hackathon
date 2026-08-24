using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class CombatCardPreviewPointerTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string LocalizationTypeName =
            "ThreeDoorsOfFate.Localization.GameLocalization, ThreeDoorsOfFate.Localization";
        private const string LanguagePreferenceKey =
            "ThreeDoorsOfFate.Language";

        private Type controllerType;
        private Type cardType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private IList hand;
        private ScriptableObject firstCard;
        private ScriptableObject secondCard;
        private Texture2D firstTexture;
        private Texture2D secondTexture;
        private Sprite firstSprite;
        private Sprite secondSprite;
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(
                LanguagePreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                LanguagePreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(LanguagePreferenceKey, "ko");
            PlayerPrefs.Save();

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");
            Assert.That(cardType, Is.Not.Null, "Runtime card type must compile.");
            InitializeGameLocalization(SystemLanguage.Korean);

            controllerHost = new GameObject("Combat Card Pointer Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = GetField<RectTransform>("root");
            canvasRoot = GetField<RectTransform>("canvasRoot");
            Assert.That(root, Is.Not.Null, "Awake must build the runtime UI shell in PlayMode.");

            firstTexture = CreateTexture(new Color(0.15f, 0.75f, 0.82f, 1f));
            secondTexture = CreateTexture(new Color(0.82f, 0.30f, 0.22f, 1f));
            firstSprite = CreateSprite(firstTexture, "Pointer Card A");
            secondSprite = CreateSprite(secondTexture, "Pointer Card B");
            firstCard = CreateCard("pointer_a", "Pointer A", firstSprite);
            secondCard = CreateCard("pointer_b", "Pointer B", secondSprite);

            hand = GetField<IList>("hand");
            hand.Clear();
            hand.Add(firstCard);
            hand.Add(secondCard);

            SetEnumField("phase", "Combat");
            SetField("action", 3);
            SetField("enemy", CreateEnemy());
            Invoke("RenderCombat");

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

            if (firstCard != null)
            {
                UnityEngine.Object.Destroy(firstCard);
            }

            if (secondCard != null)
            {
                UnityEngine.Object.Destroy(secondCard);
            }

            if (firstSprite != null)
            {
                UnityEngine.Object.Destroy(firstSprite);
            }

            if (secondSprite != null)
            {
                UnityEngine.Object.Destroy(secondSprite);
            }

            if (firstTexture != null)
            {
                UnityEngine.Object.Destroy(firstTexture);
            }

            if (secondTexture != null)
            {
                UnityEngine.Object.Destroy(secondTexture);
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

            RestoreLanguagePreference();

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerRaycasts_PreservePreviewSwitchCancelAndExplicitUse()
        {
            RectTransform firstButton = FindRequired("카드 0");
            Assert.That(RaycastAndClick(firstButton), Is.SameAs(firstButton.gameObject));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(0));
            Assert.That(hand.Count, Is.EqualTo(2));

            yield return null;
            RectTransform secondButton = FindRequired("카드 1");
            Assert.That(RaycastAndClick(secondButton), Is.SameAs(secondButton.gameObject));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(1));
            Assert.That(GetField<Image>("cardPreviewImage").sprite, Is.SameAs(secondSprite));
            Assert.That(hand.Count, Is.EqualTo(2));

            yield return null;
            Image preview = GetField<Image>("cardPreviewImage");
            Assert.That(RaycastAndClick(preview.rectTransform), Is.SameAs(preview.gameObject));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));
            Assert.That(hand.Count, Is.EqualTo(2));

            firstButton = FindRequired("카드 0");
            Assert.That(RaycastAndClick(firstButton), Is.SameAs(firstButton.gameObject));
            yield return null;

            Vector3 emptyWorldPoint = root.TransformPoint(new Vector3(
                Mathf.Lerp(root.rect.xMin, root.rect.xMax, 0.98f),
                Mathf.Lerp(root.rect.yMin, root.rect.yMax, 0.50f),
                0f));
            Vector2 emptyScreenPoint = RectTransformUtility.WorldToScreenPoint(
                null,
                emptyWorldPoint);
            Assert.That(RaycastAndClick(emptyScreenPoint), Is.SameAs(root.gameObject));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));
            Assert.That(hand.Count, Is.EqualTo(2));

            secondButton = FindRequired("카드 1");
            Assert.That(RaycastAndClick(secondButton), Is.SameAs(secondButton.gameObject));
            yield return null;

            RectTransform useButton = FindRequired("카드 사용");
            Assert.That(RaycastAndClick(useButton), Is.SameAs(useButton.gameObject));
            Assert.That(hand.Count, Is.EqualTo(1));
            Assert.That(GetCardId(hand[0]), Is.EqualTo("pointer_a"));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));
        }

        private ScriptableObject CreateCard(string cardId, string displayName, Sprite sprite)
        {
            ScriptableObject card = ScriptableObject.CreateInstance(cardType);
            SetObjectField(card, "cardId", cardId);
            SetObjectField(card, "displayName", displayName);
            SetObjectField(card, "englishName", displayName);
            SetObjectField(card, "rulesText", "No effect pointer test card.");
            SetObjectField(card, "cost", 0);
            SetObjectEnumField(card, "category", "Attack");
            SetObjectField(card, "fullCardSprite", sprite);
            return card;
        }

        private object CreateEnemy()
        {
            Type enemyType = controllerType.GetNestedType(
                "EnemyState",
                BindingFlags.NonPublic);
            Assert.That(enemyType, Is.Not.Null);
            ConstructorInfo constructor = enemyType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single();
            return constructor.Invoke(new object[]
            {
                "pointer_enemy",
                "Pointer Enemy",
                30,
                4,
                0,
                false,
                false,
                0
            });
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new(4, 6, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(color, 24).ToArray());
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
            return RaycastAndClick(RectTransformUtility.WorldToScreenPoint(null, worldCenter));
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

        private string GetCardId(object card)
        {
            PropertyInfo property = cardType.GetProperty(
                "CardId",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(card) as string;
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendant(root, objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private static RectTransform FindDescendant(RectTransform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (int index = 0; index < parent.childCount; index += 1)
            {
                RectTransform child = parent.GetChild(index) as RectTransform;
                if (child == null)
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

        private void SetEnumField(string fieldName, string value)
        {
            FieldInfo field = GetFieldInfo(fieldName);
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private FieldInfo GetFieldInfo(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return field;
        }

        private static void SetObjectField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected card field '{fieldName}'.");
            field.SetValue(target, value);
        }

        private static void SetObjectEnumField(
            object target,
            string fieldName,
            string value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected card field '{fieldName}'.");
            field.SetValue(target, Enum.Parse(field.FieldType, value));
        }

        private void RestoreLanguagePreference()
        {
            if (hadPreviousLanguage)
            {
                PlayerPrefs.SetString(
                    LanguagePreferenceKey,
                    previousLanguage);
            }
            else
            {
                PlayerPrefs.DeleteKey(LanguagePreferenceKey);
            }

            PlayerPrefs.Save();
            InitializeGameLocalization(Application.systemLanguage);
        }

        private static void InitializeGameLocalization(
            SystemLanguage systemLanguage)
        {
            Type localizationType = Type.GetType(LocalizationTypeName);
            Assert.That(localizationType, Is.Not.Null);
            MethodInfo initialize = localizationType.GetMethod(
                "Initialize",
                BindingFlags.Static | BindingFlags.Public);
            Assert.That(initialize, Is.Not.Null);
            initialize.Invoke(null, new object[] { systemLanguage });
        }
    }
}
