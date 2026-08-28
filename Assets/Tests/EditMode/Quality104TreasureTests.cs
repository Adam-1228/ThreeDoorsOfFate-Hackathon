using System;
using System.Collections;
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
    public sealed class Quality104TreasureTests
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
        private Texture2D fallbackTexture;
        private Sprite fallbackSprite;
        private ScriptableObject card;
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
            cardType = Type.GetType(CardTypeName);
            Assert.That(controllerType, Is.Not.Null);
            Assert.That(cardType, Is.Not.Null);

            controllerHost = new GameObject("Quality 1.0.4 Treasure Test Host");
            controller = controllerHost.AddComponent(controllerType);
            root = GetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = GetField<RectTransform>("root");
            }

            canvasRoot = GetField<RectTransform>("canvasRoot");
            Invoke("ClearContent");
            Invoke("SetDefaultContentRootPlacement");
            GetField<Button>("primaryButton").gameObject.SetActive(false);

            fallbackTexture = new Texture2D(4, 6, TextureFormat.RGBA32, false);
            fallbackTexture.SetPixels(Enumerable.Repeat(Color.red, 24).ToArray());
            fallbackTexture.Apply();
            fallbackSprite = Sprite.Create(
                fallbackTexture,
                new Rect(0f, 0f, 4f, 6f),
                new Vector2(0.5f, 0.5f));

            card = ScriptableObject.CreateInstance(cardType);
            SetObjectField(card, "cardId", "card_absolute_barrier");
            SetObjectField(card, "displayName", "절대 방벽");
            SetObjectField(card, "rulesText", "방어도 22를 얻습니다.");
            SetObjectField(card, "fullCardSprite", fallbackSprite);
            SetField("gold", 10);
            GetField<IList>("deck").Clear();
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

            if (card != null)
            {
                UnityEngine.Object.DestroyImmediate(card);
            }

            if (fallbackSprite != null)
            {
                UnityEngine.Object.DestroyImmediate(fallbackSprite);
            }

            if (fallbackTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(fallbackTexture);
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
        public void EnglishTreasureOffer_ShowsLocalizedCardAndTakeOrSkipChoices()
        {
            Sprite expectedEnglish = CardLocalization.GetFullCardSprite(
                "card_absolute_barrier",
                fallbackSprite);
            Assert.That(expectedEnglish, Is.Not.Null);
            Assert.That(expectedEnglish, Is.Not.SameAs(fallbackSprite));

            Invoke("RenderTreasureOffer", 31, card);
            Canvas.ForceUpdateCanvases();

            Image preview = FindRequired("Treasure Card Preview").GetComponent<Image>();
            Assert.That(preview.sprite, Is.SameAs(expectedEnglish));
            Assert.That(preview.raycastTarget, Is.True);
            Button previewButton = preview.GetComponent<Button>();
            Assert.That(previewButton, Is.Not.Null);
            Assert.That(
                FindRequired("Treasure Card Name").GetComponent<Text>().text,
                Is.EqualTo("Absolute Barrier"));
            Assert.That(
                FindRequired("Treasure Card Rules").GetComponent<Text>().text,
                Is.EqualTo("Gain 22 Block."));
            Assert.That(
                FindRequired("Treasure Reward Gold").GetComponent<Text>().text,
                Does.Contain("31"));

            string[] activeButtonLabels = GetActiveButtonLabels();
            Assert.That(activeButtonLabels, Does.Contain("Take Card"));
            Assert.That(activeButtonLabels, Does.Contain("Skip Card"));
            Assert.That(activeButtonLabels, Does.Not.Contain("Continue"));

            Assert.That(GetField<int>("gold"), Is.EqualTo(10));
            Assert.That(GetField<IList>("deck"), Is.Empty);

            previewButton.onClick.Invoke();
            Assert.That(GetField<IList>("deck"), Is.Empty);
            Image inspection = GetField<Image>("cardPreviewImage");
            Assert.That(inspection.gameObject.activeSelf, Is.True);
            Assert.That(inspection.sprite, Is.SameAs(expectedEnglish));
            GetField<Button>("cardPreviewCancelButton").onClick.Invoke();
            Assert.That(inspection.gameObject.activeSelf, Is.False);

            Assert.That(
                InvokeValue<bool>("TryResolveTreasureCardChoice", card, true),
                Is.True);
            Assert.That(GetField<IList>("deck"), Has.Count.EqualTo(1));
        }

        [Test]
        public void SkippingTreasureCard_DoesNotMutateTheDeck()
        {
            Assert.That(
                InvokeValue<bool>("TryResolveTreasureCardChoice", card, false),
                Is.False);
            Assert.That(GetField<IList>("deck"), Is.Empty);
        }

        [Test]
        public void EnglishTreasureLog_UsesLocalizedCardName()
        {
            string log = InvokeValue<string>(
                "BuildTreasureLog",
                31,
                card,
                true);

            Assert.That(log, Is.EqualTo("Treasure: 31 Gold, Absolute Barrier."));
            Assert.That(log, Does.Not.Match("[가-힣]"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MissingOrDeckFullCard_ShowsGoldOnlyAndContinue(bool deckFull)
        {
            if (deckFull)
            {
                int maximum = InvokeValue<int>("GetMaxDeckSize");
                IList deck = GetField<IList>("deck");
                for (int index = 0; index < maximum; index += 1)
                {
                    deck.Add(card);
                }
            }

            Invoke("RenderTreasureOffer", 29, deckFull ? card : null);
            Canvas.ForceUpdateCanvases();

            Assert.That(FindOptional("Treasure Card Preview"), Is.Null);
            Assert.That(FindOptional("Treasure Card Name"), Is.Null);
            Assert.That(FindOptional("Treasure Card Rules"), Is.Null);
            Assert.That(GetActiveButtonLabels(), Does.Contain("Continue"));
            Assert.That(GetField<int>("gold"), Is.EqualTo(10));
            Assert.That(
                GetField<IList>("deck").Count,
                Is.EqualTo(deckFull ? InvokeValue<int>("GetMaxDeckSize") : 0));
        }

        private string[] GetActiveButtonLabels()
        {
            return root
                .GetComponentsInChildren<Button>(true)
                .Where(button => button.gameObject.activeInHierarchy)
                .Select(button => button.GetComponentInChildren<Text>(true)?.text)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .ToArray();
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindOptional(objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private RectTransform FindOptional(string objectName)
        {
            return root.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(candidate => candidate.name == objectName
                    && candidate.gameObject.activeInHierarchy);
        }

        private void Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            method.Invoke(controller, arguments);
        }

        private T InvokeValue<T>(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            return (T)method.Invoke(controller, arguments);
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            return (T)field.GetValue(controller);
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            field.SetValue(controller, value);
        }

        private static void SetObjectField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected card field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
