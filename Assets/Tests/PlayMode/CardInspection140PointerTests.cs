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
    public sealed class CardInspection140PointerTests
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
        private ScriptableObject shopCard;
        private Texture2D cardTexture;
        private Sprite cardSprite;
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(LanguagePreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                LanguagePreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(LanguagePreferenceKey, "ko");
            PlayerPrefs.Save();
            InitializeGameLocalization(SystemLanguage.Korean);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");
            Assert.That(cardType, Is.Not.Null, "Runtime card type must compile.");

            controllerHost = new GameObject("Card Inspection 1.4 Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = GetField<RectTransform>("root");
            canvasRoot = GetField<RectTransform>("canvasRoot");
            Assert.That(root, Is.Not.Null, "Awake must build the runtime UI shell.");

            cardTexture = new Texture2D(4, 6, TextureFormat.RGBA32, false);
            cardTexture.SetPixels(Enumerable.Repeat(
                new Color(0.12f, 0.74f, 0.68f, 1f),
                24).ToArray());
            cardTexture.Apply();
            cardSprite = Sprite.Create(
                cardTexture,
                new Rect(0f, 0f, cardTexture.width, cardTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            shopCard = ScriptableObject.CreateInstance(cardType);
            SetObjectField(shopCard, "cardId", "inspection_shop_card");
            SetObjectField(shopCard, "displayName", "Inspection Shop Card");
            SetObjectField(shopCard, "englishName", "Inspection Shop Card");
            SetObjectField(shopCard, "rulesText", "A test card that must be inspected first.");
            SetObjectField(shopCard, "cost", 1);
            SetObjectEnumField(shopCard, "category", "Attack");
            SetObjectField(shopCard, "fullCardSprite", cardSprite);

            IList offers = GetField<IList>("currentShopCards");
            offers.Clear();
            offers.Add(shopCard);
            SetField("currentShopOffersReady", true);
            SetField("currentShopRunItemId", string.Empty);
            SetField("currentShopRunItemPurchased", false);
            SetField("gold", 200);
            SetField("debt", 0);
            GetField<IList>("deck").Clear();

            Invoke("ShowShop");
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

            if (shopCard != null)
            {
                UnityEngine.Object.Destroy(shopCard);
            }

            if (cardSprite != null)
            {
                UnityEngine.Object.Destroy(cardSprite);
            }

            if (cardTexture != null)
            {
                UnityEngine.Object.Destroy(cardTexture);
            }

            if (originalEventSystem == null)
            {
                EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.Destroy(created.gameObject);
                }
            }

            RestoreLanguagePreference();

            yield return null;
        }

        [UnityTest]
        public IEnumerator ShopCardRequiresInspectionThenBuy()
        {
            int goldBefore = GetField<int>("gold");
            IList deck = GetField<IList>("deck");
            RectTransform product = FindRequired("상품 0");
            RectTransform cardBody = FindDescendant(product, "카드 0");
            Assert.That(cardBody, Is.Not.Null);

            Assert.That(RaycastAndClick(cardBody), Is.SameAs(cardBody.gameObject));
            Assert.That(
                GetField<int>("gold"),
                Is.EqualTo(goldBefore),
                "Opening card inspection must not purchase the card.");
            Assert.That(deck, Has.Count.Zero);

            yield return null;
            RectTransform confirm = FindRequired("카드 검사 확정");
            Assert.That(confirm.gameObject.activeInHierarchy, Is.True);
            Assert.That(RaycastAndClick(confirm), Is.SameAs(confirm.gameObject));
            Assert.That(GetField<int>("gold"), Is.LessThan(goldBefore));
            Assert.That(deck, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InspectionSurvivesHoverExitAndBackdropCancelsWithoutBuying()
        {
            int goldBefore = GetField<int>("gold");
            IList deck = GetField<IList>("deck");
            RectTransform cardBody = FindDescendant(
                FindRequired("상품 0"),
                "카드 0");
            Assert.That(cardBody, Is.Not.Null);
            RaycastAndClick(cardBody);
            yield return null;

            SendPointerExit(cardBody);
            yield return null;

            Image preview = GetField<Image>("cardPreviewImage");
            Image backdrop = GetField<Image>("cardInspectionBackdrop");
            Assert.That(preview.gameObject.activeInHierarchy, Is.True);
            Assert.That(preview.color, Is.EqualTo(Color.white));
            Assert.That(preview.sprite, Is.SameAs(cardSprite));
            Assert.That(backdrop.gameObject.activeInHierarchy, Is.True);
            Assert.That(backdrop.color.a, Is.GreaterThanOrEqualTo(0.85f));
            Assert.That(preview.transform.parent, Is.SameAs(root));
            Assert.That(
                preview.transform.GetSiblingIndex(),
                Is.GreaterThan(GetField<RectTransform>("contentRoot").GetSiblingIndex()));
            Assert.That(
                preview.transform.GetSiblingIndex(),
                Is.GreaterThan(GetField<RectTransform>("topBar").GetSiblingIndex()));
            Assert.That(
                preview.transform.GetSiblingIndex(),
                Is.GreaterThan(GetField<RectTransform>("logRoot").GetSiblingIndex()));
            Assert.That(
                GetField<Button>("cardPreviewUseButton").transform.GetSiblingIndex(),
                Is.GreaterThan(preview.transform.GetSiblingIndex()));

            GetField<Button>("cardPreviewCancelButton").onClick.Invoke();
            Assert.That(preview.gameObject.activeSelf, Is.False);
            Assert.That(GetField<int>("gold"), Is.EqualTo(goldBefore));
            Assert.That(deck, Has.Count.Zero);
        }

        private GameObject RaycastAndClick(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
                null,
                worldCenter);
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null);
            GraphicRaycaster raycaster = canvasRoot.GetComponent<GraphicRaycaster>();
            Assert.That(raycaster, Is.Not.Null);

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

        private static void SendPointerExit(RectTransform target)
        {
            EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null);
            PointerEventData pointer = new(eventSystem);
            ExecuteEvents.ExecuteHierarchy(
                target.gameObject,
                pointer,
                ExecuteEvents.pointerExitHandler);
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

        private FieldInfo GetFieldInfo(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return field;
        }

        private static void SetObjectField(
            object target,
            string fieldName,
            object value)
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

        private static void InitializeGameLocalization(SystemLanguage language)
        {
            Type localizationType = Type.GetType(LocalizationTypeName);
            Assert.That(localizationType, Is.Not.Null);
            MethodInfo initialize = localizationType.GetMethod(
                "Initialize",
                BindingFlags.Static | BindingFlags.Public);
            Assert.That(initialize, Is.Not.Null);
            initialize.Invoke(null, new object[] { language });
        }
    }
}
