using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class CombatCardPreviewIntegrationTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";

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

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");
            Assert.That(cardType, Is.Not.Null, "Runtime card type must compile.");

            controllerHost = new GameObject("Combat Card Preview Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = GetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = GetField<RectTransform>("root");
            }

            Assert.That(root, Is.Not.Null, "Controller must build its runtime UI shell.");
            canvasRoot = GetField<RectTransform>("canvasRoot");

            firstTexture = CreateTexture(new Color(0.15f, 0.75f, 0.82f, 1f));
            secondTexture = CreateTexture(new Color(0.82f, 0.30f, 0.22f, 1f));
            firstSprite = CreateSprite(firstTexture, "Preview Card A");
            secondSprite = CreateSprite(secondTexture, "Preview Card B");
            firstCard = CreateCard("preview_a", "Preview A", firstSprite);
            secondCard = CreateCard("preview_b", "Preview B", secondSprite);

            hand = GetField<IList>("hand");
            hand.Clear();
            hand.Add(firstCard);
            hand.Add(secondCard);

            SetEnumField("phase", "Combat");
            SetField("action", 3);
            SetField("enemy", CreateEnemy());
            Invoke("RenderCombat");
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

            if (firstCard != null)
            {
                UnityEngine.Object.DestroyImmediate(firstCard);
            }

            if (secondCard != null)
            {
                UnityEngine.Object.DestroyImmediate(secondCard);
            }

            if (firstSprite != null)
            {
                UnityEngine.Object.DestroyImmediate(firstSprite);
            }

            if (secondSprite != null)
            {
                UnityEngine.Object.DestroyImmediate(secondSprite);
            }

            if (firstTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(firstTexture);
            }

            if (secondTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(secondTexture);
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
        public void Combat_EnglishSurfaceContainsNoKoreanText()
        {
            ExpectUniqueMissingEnglishCardRecords("surface");
            GameLocalization.SetLanguage(GameLanguage.English);
            Invoke("RenderCombat");
            Canvas.ForceUpdateCanvases();

            string[] koreanText = root
                .GetComponentsInChildren<Text>(true)
                .Where(text => text.gameObject.activeInHierarchy)
                .Select(text => text.text ?? string.Empty)
                .Where(text => Regex.IsMatch(text, "[가-힣]"))
                .ToArray();

            Assert.That(
                koreanText,
                Is.Empty,
                $"Active Korean text remained: {string.Join(" | ", koreanText)}");
        }

        [Test]
        public void CompletedCardSprite_RendersAsOneIntegratedCardImage()
        {
            RectTransform card = FindRequired("카드 0");
            Image cardImage = card.GetComponent<Image>();

            Assert.That(cardImage, Is.Not.Null);
            Assert.That(cardImage.sprite, Is.SameAs(firstSprite));
            Assert.That(cardImage.preserveAspect, Is.True);
            Assert.That(FindDescendant(card, "일러스트"), Is.Null);
            Assert.That(FindDescendant(card, "카드명"), Is.Null);
            Assert.That(FindDescendant(card, "효과"), Is.Null);
        }

        [Test]
        public void Combat_EnglishEnemyIntentAndActiveSynergyContainNoKoreanText()
        {
            object localizedEnemy = CreateEnemy();
            SetObjectProperty(
                localizedEnemy,
                "CandidateLabel",
                "그림자 일격, 깊은 할퀴기");
            SetObjectProperty(localizedEnemy, "IntentCardName", "그림자 일격");
            SetObjectProperty(localizedEnemy, "IntentLabel", "공격 7");
            SetField("enemy", localizedEnemy);

            IList deck = GetField<IList>("deck");
            deck.Clear();
            foreach (string assetPath in new[]
            {
                "Assets/Data/Cards/MVP/card_fate_strike.asset",
                "Assets/Data/Cards/MVP/card_guard_stance.asset",
                "Assets/Data/Cards/MVP/card_reroll.asset"
            })
            {
                ScriptableObject card = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    assetPath);
                Assert.That(card, Is.Not.Null, $"Missing synergy fixture: {assetPath}");
                deck.Add(card);
            }

            ExpectUniqueMissingEnglishCardRecords("nested");
            GameLocalization.SetLanguage(GameLanguage.English);
            Invoke("RenderCombat");
            Canvas.ForceUpdateCanvases();

            MethodInfo activeHudMethod = controllerType.GetMethod(
                "BuildActiveCombinationHudText",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(activeHudMethod, Is.Not.Null);
            string activeHud = activeHudMethod.Invoke(
                controller,
                Array.Empty<object>()) as string;
            Assert.That(activeHud, Does.Contain("Fate Counter"));
            Assert.That(activeHud, Does.Not.Match("[가-힣]"));

            string[] koreanText = root
                .GetComponentsInChildren<Text>(true)
                .Where(text => text.gameObject.activeInHierarchy)
                .Select(text => text.text ?? string.Empty)
                .Where(text => Regex.IsMatch(text, "[가-힣]"))
                .ToArray();

            Assert.That(
                koreanText,
                Is.Empty,
                $"Active Korean text remained: {string.Join(" | ", koreanText)}");
        }

        [Test]
        public void QaCapture_SelectsFirstPlayableCardUsingCardData()
        {
            Type captureType = Type.GetType(
                "ThreeDoorsOfFate.Editor.HowToPlaySourceQACapture, Assembly-CSharp-Editor");
            Assert.That(captureType, Is.Not.Null);

            MethodInfo captureMethod = captureType.GetMethod(
                "ShowFirstPlayableCardPreview",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(captureMethod, Is.Not.Null);

            Assert.That(
                () => captureMethod.Invoke(
                    null,
                    new object[] { controllerType, controller }),
                Throws.Nothing);
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(0));
            Assert.That(GetField<Image>("cardPreviewImage").sprite, Is.SameAs(firstSprite));
        }

        [Test]
        public void FirstCardTap_OpensLargePreviewWithoutUsingCard()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();

            Assert.That(hand.Count, Is.EqualTo(2), "Inspecting must not consume a card.");
            Assert.That(GetField<int>("action"), Is.EqualTo(3));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(0));

            Image preview = GetField<Image>("cardPreviewImage");
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.gameObject.activeSelf, Is.True);
            Assert.That(preview.sprite, Is.SameAs(firstSprite));
            Assert.That(
                preview.rectTransform.anchorMin,
                Is.EqualTo(new Vector2(0.390f, 0.300f)));
            Assert.That(
                preview.rectTransform.anchorMax,
                Is.EqualTo(new Vector2(0.610f, 0.850f)));
            Assert.That(FindRequired("카드 사용").gameObject.activeSelf, Is.True);
        }

        [Test]
        public void HoverPreview_DoesNotInterceptTheHoveredCardsPointer()
        {
            RectTransform hoveredCard = FindRequired("카드 0");
            MethodInfo showPreview = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == "ShowCardPreview"
                    && candidate.GetParameters().Length == 2);

            showPreview.Invoke(controller, new object[] { firstCard, hoveredCard });

            Image preview = GetField<Image>("cardPreviewImage");
            Assert.That(preview.gameObject.activeSelf, Is.True);
            Assert.That(
                preview.raycastTarget,
                Is.False,
                "A hover preview must not steal the pointer and trigger an enter/exit flicker loop.");
            Button previewButton = preview.GetComponent<Button>();
            Assert.That(previewButton.interactable, Is.False);
            Assert.That(
                previewButton.transition,
                Is.EqualTo(Selectable.Transition.None),
                "Disabling hover input must not apply a faded disabled tint to the card image.");
            Assert.That(preview.color, Is.EqualTo(Color.white));
        }

        [Test]
        public void CombatSelectionPreview_RendersAboveEveryPrimaryUiLayer()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();

            Image preview = GetField<Image>("cardPreviewImage");
            RectTransform content = GetField<RectTransform>("contentRoot");
            RectTransform top = GetField<RectTransform>("topBar");
            RectTransform log = GetField<RectTransform>("logRoot");

            Assert.That(preview.transform.parent, Is.SameAs(root));
            Assert.That(preview.raycastTarget, Is.True);
            Assert.That(preview.GetComponent<Button>().interactable, Is.True);
            Assert.That(
                preview.transform.GetSiblingIndex(),
                Is.GreaterThan(content.GetSiblingIndex()));
            Assert.That(
                preview.transform.GetSiblingIndex(),
                Is.GreaterThan(top.GetSiblingIndex()));
            Assert.That(
                preview.transform.GetSiblingIndex(),
                Is.GreaterThan(log.GetSiblingIndex()));
            Assert.That(
                GetField<Button>("cardPreviewUseButton").transform.GetSiblingIndex(),
                Is.GreaterThan(preview.transform.GetSiblingIndex()));
        }

        [Test]
        public void AnotherCardTap_ReplacesActivePreviewWithoutUsingEitherCard()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();
            FindRequired("카드 1").GetComponent<Button>().onClick.Invoke();

            Assert.That(hand.Count, Is.EqualTo(2));
            Assert.That(GetField<int>("action"), Is.EqualTo(3));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(1));
            Assert.That(GetField<Image>("cardPreviewImage").sprite, Is.SameAs(secondSprite));
        }

        [Test]
        public void EmptySpace_CancelsPreviewWithoutChangingCombatState()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();
            GetField<Button>("cardPreviewCancelButton").onClick.Invoke();

            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));
            Assert.That(GetField<Image>("cardPreviewImage").gameObject.activeSelf, Is.False);
            Assert.That(hand.Count, Is.EqualTo(2));
            Assert.That(GetField<int>("action"), Is.EqualTo(3));
        }

        [Test]
        public void PreviewTap_CancelsWithoutPassingThroughToCardUse()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();
            Image preview = GetField<Image>("cardPreviewImage");
            Button previewButton = preview.GetComponent<Button>();

            Assert.That(previewButton, Is.Not.Null);
            previewButton.onClick.Invoke();

            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));
            Assert.That(preview.gameObject.activeSelf, Is.False);
            Assert.That(hand.Count, Is.EqualTo(2));
            Assert.That(GetField<int>("action"), Is.EqualTo(3));
        }

        [Test]
        public void UseButton_CommitsOnlyTheActivePreviewOnce()
        {
            FindRequired("카드 1").GetComponent<Button>().onClick.Invoke();
            FindRequired("카드 사용").GetComponent<Button>().onClick.Invoke();

            Assert.That(hand.Count, Is.EqualTo(1));
            Assert.That(GetCardId(hand[0]), Is.EqualTo("preview_a"));
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));
        }

        [Test]
        public void RenderCombat_ClearsActivePreviewSelection()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();

            Invoke("RenderCombat");
            Canvas.ForceUpdateCanvases();

            AssertPreviewIsCleared();
            Assert.That(hand.Count, Is.EqualTo(2));
            Assert.That(GetField<int>("action"), Is.EqualTo(3));
        }

        [Test]
        public void PhaseTransition_ClearsActivePreviewSelection()
        {
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();

            Invoke("ShowMainMenu");
            Canvas.ForceUpdateCanvases();

            AssertPreviewIsCleared();
            Assert.That(hand.Count, Is.EqualTo(2));
        }

        [Test]
        public void CardBecomingUnplayable_CannotBeCommitted()
        {
            SetObjectField(firstCard, "cost", 1);
            Invoke("RenderCombat");
            Canvas.ForceUpdateCanvases();
            FindRequired("카드 0").GetComponent<Button>().onClick.Invoke();

            SetField("action", 0);
            FindRequired("카드 사용").GetComponent<Button>().onClick.Invoke();

            AssertPreviewIsCleared();
            Assert.That(hand.Count, Is.EqualTo(2), "Invalidated cards must not be consumed.");
            Assert.That(GetField<int>("action"), Is.EqualTo(0));
        }

        private ScriptableObject CreateCard(string cardId, string displayName, Sprite sprite)
        {
            ScriptableObject card = ScriptableObject.CreateInstance(cardType);
            SetObjectField(card, "cardId", cardId);
            SetObjectField(card, "displayName", displayName);
            SetObjectField(card, "englishName", displayName);
            SetObjectField(card, "rulesText", "No effect test card.");
            SetObjectField(card, "cost", 0);
            SetObjectEnumField(card, "category", "Attack");
            SetObjectField(card, "fullCardSprite", sprite);
            return card;
        }

        private void ExpectUniqueMissingEnglishCardRecords(string suffix)
        {
            Invoke("ClearContent");
            string firstCardId = $"preview_a_{suffix}";
            string secondCardId = $"preview_b_{suffix}";
            SetObjectField(firstCard, "cardId", firstCardId);
            SetObjectField(secondCard, "cardId", secondCardId);
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
                "test_enemy",
                "Test Enemy",
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
            Color[] pixels = Enumerable.Repeat(color, 24).ToArray();
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

        private string GetCardId(object card)
        {
            PropertyInfo property = cardType.GetProperty(
                "CardId",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null);
            return property.GetValue(card) as string;
        }

        private void AssertPreviewIsCleared()
        {
            Assert.That(GetField<int>("selectedCombatCardIndex"), Is.EqualTo(-1));

            Image preview = GetField<Image>("cardPreviewImage");
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.gameObject.activeSelf, Is.False);

            Button useButton = GetField<Button>("cardPreviewUseButton");
            Assert.That(useButton, Is.Not.Null);
            Assert.That(useButton.gameObject.activeSelf, Is.False);

            Button cancelButton = GetField<Button>("cardPreviewCancelButton");
            Assert.That(cancelButton, Is.Not.Null);
            Assert.That(cancelButton.interactable, Is.False);
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
            FieldInfo field = GetFieldInfo(fieldName);
            return (T)field.GetValue(controller);
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

        private static void SetObjectProperty(
            object target,
            string propertyName,
            object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            property.SetValue(target, value);
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
