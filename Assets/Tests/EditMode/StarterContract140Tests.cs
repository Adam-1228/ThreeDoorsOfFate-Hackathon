using System;
using System.Collections;
using System.Collections.Generic;
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
    public sealed class StarterContract140Tests
    {
        private const string CatalogTypeName =
            "ThreeDoorsOfFate.Game.V140.StarterContractCatalog, Assembly-CSharp";
        private const string BuilderTypeName =
            "ThreeDoorsOfFate.Game.V140.StarterDeckBuilder, Assembly-CSharp";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string CharacterClassTypeName =
            "ThreeDoorsOfFate.Cards.CharacterClass, Assembly-CSharp";
        private const string CatalogPath =
            "Assets/Resources/GameData/V140/starter_contracts.json";
        private const string FontPath = "Assets/Fonts/GowunBatang-Regular.ttf";

        private readonly List<UnityEngine.Object> loadedCards = new();
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
            GameLocalization.Initialize(SystemLanguage.English);
        }

        [TearDown]
        public void TearDown()
        {
            loadedCards.Clear();
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

        [TestCase("Gambler")]
        [TestCase("Oracle")]
        [TestCase("Exile")]
        public void EveryContractBuildsTwentyFourLegalCards(string className)
        {
            object catalog = LoadCatalog();
            object builder = Activator.CreateInstance(RequireType(BuilderTypeName), catalog);
            object characterClass = ParseEnum(CharacterClassTypeName, className);
            IEnumerable contracts = (IEnumerable)Invoke(
                catalog,
                "GetContracts",
                characterClass);
            object cardLookup = CreateCardLookup();
            int contractCount = 0;

            foreach (object contract in contracts)
            {
                contractCount += 1;
                string contractId = ReadProperty<string>(contract, "Id");
                IList deck = (IList)Invoke(
                    builder,
                    "Build",
                    characterClass,
                    contractId,
                    cardLookup);

                Assert.That(deck, Has.Count.EqualTo(24), contractId);
                Assert.That(CountCategory(deck, "Attack"), Is.EqualTo(10), contractId);
                Assert.That(CountCategory(deck, "Defense"), Is.EqualTo(8), contractId);
                Assert.That(CountCategory(deck, "Skill"), Is.EqualTo(6), contractId);
                Assert.That(CountClassCards(deck, className), Is.GreaterThanOrEqualTo(4), contractId);
                Assert.That(
                    deck.Cast<object>().All(card =>
                        ReadProperty<object>(card, "Rarity").ToString() == "Common"),
                    Is.True,
                    contractId);
                Assert.That(
                    deck.Cast<object>().All(card =>
                        ReadProperty<object>(card, "Category").ToString() != "Curse"),
                    Is.True,
                    contractId);
            }

            Assert.That(contractCount, Is.EqualTo(3));
        }

        [Test]
        public void MissingReferencedCardFailsInsteadOfReturningAPartialDeck()
        {
            object catalog = LoadCatalog();
            object builder = Activator.CreateInstance(RequireType(BuilderTypeName), catalog);
            object characterClass = ParseEnum(CharacterClassTypeName, "Gambler");
            IDictionary cardLookup = (IDictionary)CreateCardLookup();
            cardLookup.Remove("card_worn_dagger");

            TargetInvocationException error = Assert.Throws<TargetInvocationException>(
                () => Invoke(
                    builder,
                    "Build",
                    characterClass,
                    "gambler.high_roll",
                    cardLookup));

            Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(error.InnerException.Message, Does.Contain("card_worn_dagger"));
        }

        [Test]
        public void CharacterConfirmationShowsThreeContractsAndPersistentSettings()
        {
            Type controllerType = RequireType(ControllerTypeName);
            GameObject host = new("Starter Contract UI Test Host");
            Component controller = null;
            EventSystem originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            try
            {
                controller = host.AddComponent(controllerType);
                Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                Assert.That(font, Is.Not.Null);
                SetField(controller, "uiFontAsset", font);
                SetField(controller, "uiFont", font);
                SetField(controller, "cardPool", CreateTypedCardList());
                if (ReadField<RectTransform>(controller, "canvasRoot") == null)
                {
                    Invoke(controller, "BuildShell");
                }

                Invoke(controller, "ShowClassDetail", ParseEnum(CharacterClassTypeName, "Gambler"));
                Button confirm = FindActiveButton(controller, "캐릭터 확정");
                Assert.That(confirm, Is.Not.Null);
                confirm.onClick.Invoke();
                Canvas.ForceUpdateCanvases();

                RectTransform contentRoot = ReadField<RectTransform>(controller, "contentRoot");
                Button[] contractButtons = contentRoot
                    .GetComponentsInChildren<Button>(true)
                    .Where(button =>
                        button.gameObject.activeInHierarchy
                        && button.name.StartsWith("운명 계약 선택 ", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(contractButtons, Has.Length.EqualTo(3));
                Assert.That(FindActiveButton(controller, "계약 설정"), Is.Not.Null);
                Assert.That(
                    ReadField<Text>(controller, "titleText").text,
                    Is.EqualTo("Choose Your Fate Contract"));

                foreach (Button button in contractButtons)
                {
                    RectTransform rect = button.GetComponent<RectTransform>();
                    AssertInsideUnitAnchors(rect);
                }

                contractButtons[0].onClick.Invoke();
                IList startedDeck = (IList)ReadRawField(controller, "deck");
                Assert.That(startedDeck, Has.Count.EqualTo(24));
                Assert.That(
                    ReadRawField(controller, "selectedStarterContractId"),
                    Is.Not.EqualTo(string.Empty));
                Assert.That(
                    ReadRawField(controller, "phase").ToString(),
                    Is.EqualTo("DoorSelection"));
            }
            finally
            {
                if (controller != null)
                {
                    RectTransform canvasRoot = ReadField<RectTransform>(controller, "canvasRoot");
                    if (canvasRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
                    }
                }

                UnityEngine.Object.DestroyImmediate(host);
                if (originalEventSystem == null)
                {
                    EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                    if (created != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created.gameObject);
                    }
                }
            }
        }

        private object LoadCatalog()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(asset, Is.Not.Null);
            Type catalogType = RequireType(CatalogTypeName);
            MethodInfo load = catalogType.GetMethod(
                "Load",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(load, Is.Not.Null);
            return load.Invoke(null, new object[] { asset });
        }

        private object CreateCardLookup()
        {
            Type cardType = RequireType(CardTypeName);
            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(
                typeof(string),
                cardType);
            IDictionary lookup = (IDictionary)Activator.CreateInstance(dictionaryType);
            foreach (UnityEngine.Object card in LoadAllCards(cardType))
            {
                lookup.Add(ReadProperty<string>(card, "CardId"), card);
            }

            return lookup;
        }

        private object CreateTypedCardList()
        {
            Type cardType = RequireType(CardTypeName);
            Type listType = typeof(List<>).MakeGenericType(cardType);
            IList list = (IList)Activator.CreateInstance(listType);
            foreach (UnityEngine.Object card in LoadAllCards(cardType))
            {
                list.Add(card);
            }

            return list;
        }

        private IEnumerable<UnityEngine.Object> LoadAllCards(Type cardType)
        {
            foreach (string guid in AssetDatabase.FindAssets(
                "t:CardData",
                new[] { "Assets/Data/Cards/MVP" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object card = AssetDatabase.LoadAssetAtPath(path, cardType);
                if (card == null)
                {
                    continue;
                }

                loadedCards.Add(card);
                yield return card;
            }
        }

        private static int CountCategory(IList deck, string category)
        {
            return deck.Cast<object>().Count(card =>
                ReadProperty<object>(card, "Category").ToString() == category);
        }

        private static int CountClassCards(IList deck, string className)
        {
            return deck.Cast<object>().Count(card =>
                ReadProperty<object>(card, "CharacterClass").ToString() == className);
        }

        private static Button FindActiveButton(Component controller, string name)
        {
            RectTransform canvasRoot = ReadField<RectTransform>(controller, "canvasRoot");
            return canvasRoot
                .GetComponentsInChildren<Button>(true)
                .SingleOrDefault(button =>
                    button.gameObject.activeInHierarchy && button.name == name);
        }

        private static void AssertInsideUnitAnchors(RectTransform rect)
        {
            foreach (float value in new[]
            {
                rect.anchorMin.x,
                rect.anchorMin.y,
                rect.anchorMax.x,
                rect.anchorMax.y
            })
            {
                Assert.That(value, Is.InRange(0f, 1f));
            }
        }

        private static object ParseEnum(string typeName, string value)
        {
            return Enum.Parse(RequireType(typeName), value);
        }

        private static object Invoke(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                arguments.Select(argument => argument.GetType()).ToArray(),
                null);
            if (method == null)
            {
                method = instance.GetType().GetMethods(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SingleOrDefault(candidate =>
                        candidate.Name == methodName
                        && candidate.GetParameters().Length == arguments.Length);
            }

            Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
            return method.Invoke(instance, arguments);
        }

        private static T ReadProperty<T>(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Missing property: {propertyName}");
            return (T)property.GetValue(instance);
        }

        private static T ReadField<T>(object instance, string fieldName) where T : class
        {
            FieldInfo field = instance.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            return field.GetValue(instance) as T;
        }

        private static object ReadRawField(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            return field.GetValue(instance);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
            field.SetValue(instance, value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Missing type: {typeName}");
            return type;
        }
    }
}
