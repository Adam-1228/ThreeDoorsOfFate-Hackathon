using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class DeckRemoval140Tests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";

        private Type controllerType;
        private Type cardType;
        private GameObject host;
        private Component controller;
        private ScriptableObject attackCard;
        private ScriptableObject defenseCard;
        private ScriptableObject outsiderCard;
        private RectTransform canvasRoot;
        private Texture2D cardTexture;
        private Sprite cardSprite;
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "ko");
            GameLocalization.Initialize(SystemLanguage.Korean);

            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(cardType, Is.Not.Null);

            host = new GameObject("Deck Removal 1.4 Test Host");
            controller = host.AddComponent(controllerType);
            cardTexture = new Texture2D(2, 3, TextureFormat.RGBA32, false);
            cardTexture.SetPixels(new[]
            {
                Color.cyan, Color.cyan,
                Color.cyan, Color.cyan,
                Color.cyan, Color.cyan
            });
            cardTexture.Apply();
            cardSprite = Sprite.Create(
                cardTexture,
                new Rect(0f, 0f, 2f, 3f),
                new Vector2(0.5f, 0.5f),
                100f);
            attackCard = CreateCard("removal_attack", "Removal Attack", "Attack");
            defenseCard = CreateCard("removal_defense", "Removal Defense", "Defense");
            outsiderCard = CreateCard("removal_outsider", "Removal Outsider", "Skill");
        }

        [TearDown]
        public void TearDown()
        {
            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(attackCard);
            UnityEngine.Object.DestroyImmediate(defenseCard);
            UnityEngine.Object.DestroyImmediate(outsiderCard);
            UnityEngine.Object.DestroyImmediate(cardSprite);
            UnityEngine.Object.DestroyImmediate(cardTexture);
            UnityEngine.Object.DestroyImmediate(host);
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
        public void RemovalPreservesTwelveCardsAndFourAttacks()
        {
            SetDeck(total: 12, attacks: 5);
            Assert.That(CanRemove(defenseCard), Is.False);

            SetDeck(total: 20, attacks: 4);
            Assert.That(CanRemove(attackCard), Is.False);
            Assert.That(CanRemove(defenseCard), Is.True);
        }

        [Test]
        public void SuccessfulRemovalAdvancesTheRunPriceOnce()
        {
            SetDeck(total: 20, attacks: 5);
            Assert.That(InvokeValue<int>("GetDeckRemovalPrice"), Is.EqualTo(45));

            Assert.That(
                InvokeValue<bool>("TryRemoveDeckCard", defenseCard, "Test"),
                Is.True);
            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(19));
            Assert.That(GetField<int>("cardsRemovedThisRun"), Is.EqualTo(1));
            Assert.That(InvokeValue<int>("GetDeckRemovalPrice"), Is.EqualTo(70));
        }

        [Test]
        public void RemovalRejectsCardsThatAreNotInTheDeck()
        {
            SetDeck(total: 20, attacks: 5);

            Assert.That(CanRemove(outsiderCard), Is.False);
            Assert.That(
                InvokeValue<bool>("TryRemoveDeckCard", outsiderCard, "Test"),
                Is.False);
            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(20));
            Assert.That(GetField<int>("cardsRemovedThisRun"), Is.Zero);
        }

        [Test]
        public void RestRemovalOpensGalleryAndRequiresInspectionConfirmation()
        {
            PrepareUi();
            SetDeck(total: 20, attacks: 5);
            Invoke("ShowRest");
            Canvas.ForceUpdateCanvases();

            Button removal = FindRequired("휴식 카드 제거").GetComponent<Button>();
            Assert.That(removal, Is.Not.Null);
            Assert.That(removal.interactable, Is.True);
            removal.onClick.Invoke();

            Button cardChoice = FindRequired("제거 카드 0").GetComponent<Button>();
            Assert.That(cardChoice, Is.Not.Null);
            cardChoice.onClick.Invoke();
            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(20));
            Assert.That(
                FindRequired("카드 검사 확정").gameObject.activeInHierarchy,
                Is.True);
            GetField<Button>("cardPreviewCancelButton").onClick.Invoke();
            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(20));
        }

        [Test]
        public void ShopRemovalChargesOnceAndLocksForTheVisit()
        {
            PrepareUi();
            SetDeck(total: 20, attacks: 5);
            SetField("gold", 200);
            SetField("currentShopOffersReady", true);
            GetField<IList>("currentShopCards").Clear();
            SetField("currentShopRunItemId", string.Empty);
            Invoke("ShowShop");
            Canvas.ForceUpdateCanvases();

            Button service = FindRequired("상점 카드 제거").GetComponent<Button>();
            Assert.That(service, Is.Not.Null);
            Assert.That(service.interactable, Is.True);
            Assert.That(service.GetComponentInChildren<Text>().text, Does.Contain("45"));
            service.onClick.Invoke();

            FindRequired("제거 카드 0").GetComponent<Button>().onClick.Invoke();
            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(20));
            Assert.That(GetField<int>("gold"), Is.EqualTo(200));
            FindRequired("카드 검사 확정").GetComponent<Button>().onClick.Invoke();

            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(19));
            Assert.That(GetField<int>("gold"), Is.EqualTo(155));
            Assert.That(GetField<int>("cardsRemovedThisRun"), Is.EqualTo(1));
            Assert.That(GetField<bool>("currentShopRemovalUsed"), Is.True);
            Button usedService = FindRequired("상점 카드 제거").GetComponent<Button>();
            Assert.That(usedService.interactable, Is.False);
        }

        private bool CanRemove(ScriptableObject card)
        {
            return InvokeValue<bool>("CanRemoveDeckCard", card);
        }

        private void SetDeck(int total, int attacks)
        {
            Assert.That(attacks, Is.InRange(0, total));
            IList deck = GetField<IList>("deck");
            deck.Clear();
            for (int index = 0; index < attacks; index += 1)
            {
                deck.Add(attackCard);
            }

            for (int index = attacks; index < total; index += 1)
            {
                deck.Add(defenseCard);
            }

            SetField("cardsRemovedThisRun", 0);
        }

        private void PrepareUi()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Fonts/GowunBatang-Regular.ttf");
            Assert.That(font, Is.Not.Null);
            SetField("uiFontAsset", font);
            SetField("uiFont", font);
            SetEnumField("selectedClass", "Gambler");
            Invoke("BuildShell");
            canvasRoot = GetField<RectTransform>("canvasRoot");
            Assert.That(canvasRoot, Is.Not.Null);
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform root = GetField<RectTransform>("root");
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
            MethodInfo method = FindMethod(methodName, 0);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            method.Invoke(controller, Array.Empty<object>());
        }

        private void SetEnumField(string fieldName, string value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private ScriptableObject CreateCard(
            string cardId,
            string displayName,
            string category)
        {
            ScriptableObject card = ScriptableObject.CreateInstance(cardType);
            SetObjectField(card, "cardId", cardId);
            SetObjectField(card, "displayName", displayName);
            SetObjectField(card, "englishName", displayName);
            SetObjectField(card, "rulesText", "Deck removal test card.");
            SetObjectEnumField(card, "category", category);
            SetObjectField(card, "fullCardSprite", cardSprite);
            return card;
        }

        private T InvokeValue<T>(string methodName, params object[] arguments)
        {
            MethodInfo method = FindMethod(methodName, arguments.Length);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            return (T)method.Invoke(controller, arguments);
        }

        private MethodInfo FindMethod(string methodName, int argumentCount)
        {
            foreach (MethodInfo method in controllerType.GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (method.Name == methodName
                    && method.GetParameters().Length == argumentCount)
                {
                    return method;
                }
            }

            return null;
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
    }
}
