using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private const string ContractInfoFramePath =
            "Assets/Art/UI/GeneratedFrames/ui_contract_info_frame_v1.png";

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
        public void ContractInfoFrameIsTransparentNineSliceSprite()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                ContractInfoFramePath);
            Assert.That(
                sprite,
                Is.Not.Null,
                "The generated contract frame must be imported as a Sprite.");

            TextureImporter importer = AssetImporter.GetAtPath(
                ContractInfoFramePath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(
                importer.DoesSourceTextureHaveAlpha(),
                Is.True,
                "The inside and outside of the generated frame need real alpha transparency.");
            Assert.That(importer.spriteBorder.x, Is.GreaterThan(0f));
            Assert.That(importer.spriteBorder.y, Is.GreaterThan(0f));
            Assert.That(importer.spriteBorder.z, Is.GreaterThan(0f));
            Assert.That(importer.spriteBorder.w, Is.GreaterThan(0f));
            Assert.That(
                sprite.texture.width / (float)sprite.texture.height,
                Is.GreaterThanOrEqualTo(2.8f),
                "The contract frame must remain a wide, shallow information box.");

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolutePath = Path.Combine(projectRoot, ContractInfoFramePath);
            Texture2D decoded = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(
                    ImageConversion.LoadImage(decoded, File.ReadAllBytes(absolutePath), false),
                    Is.True);
                Color32[] pixels = decoded.GetPixels32();
                int minVisibleY = decoded.height;
                int maxVisibleY = -1;
                for (int y = 0; y < decoded.height; y += 1)
                {
                    for (int x = 0; x < decoded.width; x += 1)
                    {
                        if (pixels[y * decoded.width + x].a < 128)
                        {
                            continue;
                        }

                        minVisibleY = Mathf.Min(minVisibleY, y);
                        maxVisibleY = Mathf.Max(maxVisibleY, y);
                    }
                }

                Assert.That(maxVisibleY, Is.GreaterThanOrEqualTo(0));
                float visibleHeight = (maxVisibleY - minVisibleY + 1f) / decoded.height;
                Assert.That(
                    visibleHeight,
                    Is.GreaterThanOrEqualTo(0.70f),
                    "The visible border must fill the compact UI box instead of collapsing into transparent vertical padding.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
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
                SetSpriteField(
                    controller,
                    "statusSectionTallFrameSprite",
                    "Assets/Art/UI/GeneratedFrames/ui_status_section_tall_frame_v2.png");
                SetSpriteField(
                    controller,
                    "statusInnerHeaderFrameSprite",
                    "Assets/Art/UI/GeneratedFrames/ui_status_inner_header_frame_ai.png");
                SetSpriteField(
                    controller,
                    "statusItemSlotFrameSprite",
                    "Assets/Art/UI/GeneratedFrames/ui_status_item_slot_frame_ai.png");
                SetSpriteField(
                    controller,
                    "contractInfoFrameSprite",
                    ContractInfoFramePath);
                SetSpriteField(
                    controller,
                    "classConfirmButtonSprite",
                    "Assets/Art/UI/GeneratedFrames/ui_class_confirm_button_frame.png");
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
                Vector2 frameVisibleY = ReadVisibleAlphaYRange(
                    ContractInfoFramePath);

                for (int index = 0; index < contractButtons.Length; index += 1)
                {
                    Button button = contractButtons[index];
                    RectTransform rect = button.GetComponent<RectTransform>();
                    AssertInsideUnitAnchors(rect);

                    RectTransform panel = FindDescendant(
                        contentRoot,
                        $"운명 계약 {index + 1}");
                    RectTransform safeRoot = FindDescendant(
                        panel,
                        "계약 텍스트 안전영역");
                    RectTransform swapBox = FindDescendant(
                        panel,
                        "계약 카드 교체");
                    RectTransform resourceBox = FindDescendant(
                        panel,
                        "계약 자원 변화");
                    RectTransform swapFrame = FindDescendant(
                        swapBox,
                        "생성 투명 프레임");
                    RectTransform resourceFrame = FindDescendant(
                        resourceBox,
                        "생성 투명 프레임");
                    Text description = FindDescendant(
                        panel,
                        "계약 설명").GetComponent<Text>();
                    Text swapText = FindDescendant(
                        swapBox,
                        "계약 카드 교체 텍스트").GetComponent<Text>();
                    Text resourceText = FindDescendant(
                        resourceBox,
                        "계약 자원 변화 텍스트").GetComponent<Text>();
                    Assert.That(panel, Is.Not.Null);
                    Assert.That(safeRoot, Is.Not.Null);
                    Assert.That(safeRoot.GetComponent<RectMask2D>(), Is.Not.Null);
                    Assert.That(
                        safeRoot.anchorMax.x - safeRoot.anchorMin.x,
                        Is.GreaterThanOrEqualTo(0.55f));
                    Assert.That(swapBox, Is.Not.Null);
                    Assert.That(resourceBox, Is.Not.Null);
                    Assert.That(swapFrame, Is.Not.Null);
                    Assert.That(resourceFrame, Is.Not.Null);
                    Sprite expectedFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                        ContractInfoFramePath);
                    Assert.That(
                        swapFrame.GetComponent<Image>().sprite,
                        Is.SameAs(expectedFrame));
                    Assert.That(
                        resourceFrame.GetComponent<Image>().sprite,
                        Is.SameAs(expectedFrame));
                    Assert.That(
                        swapFrame.GetComponent<Image>().type,
                        Is.EqualTo(Image.Type.Simple),
                        "The compact fixed-ratio frame must not collapse its tall nine-slice borders.");
                    Assert.That(
                        resourceFrame.GetComponent<Image>().type,
                        Is.EqualTo(Image.Type.Simple),
                        "The compact fixed-ratio frame must not collapse its tall nine-slice borders.");
                    Assert.That(swapBox.GetComponent<Image>().color.a, Is.Zero);
                    Assert.That(resourceBox.GetComponent<Image>().color.a, Is.Zero);
                    Rect swapRect = GetWorldRect(swapBox);
                    Rect resourceRect = GetWorldRect(resourceBox);
                    Rect safeRect = GetWorldRect(safeRoot);
                    Rect descriptionRect = GetWorldRect(description.rectTransform);
                    Assert.That(
                        swapRect.width,
                        Is.EqualTo(resourceRect.width).Within(0.5f),
                        "Both contract change boxes must use the same width.");
                    Assert.That(
                        swapRect.height,
                        Is.EqualTo(resourceRect.height).Within(0.5f),
                        "Both contract change boxes must use the same height.");
                    float horizontalFrameClearance = safeRect.width * 0.05f;
                    Assert.That(
                        swapRect.xMin - safeRect.xMin,
                        Is.GreaterThanOrEqualTo(horizontalFrameClearance),
                        "The information box must clear the left decorative frame rail.");
                    Assert.That(
                        safeRect.xMax - swapRect.xMax,
                        Is.GreaterThanOrEqualTo(horizontalFrameClearance),
                        "The information box must clear the right decorative frame rail.");
                    Assert.That(
                        resourceRect.yMin - safeRect.yMin,
                        Is.GreaterThanOrEqualTo(safeRect.height * 0.27f),
                        "The lower information box must leave visible space above the card's bottom ornament.");
                    Assert.That(
                        swapBox.anchorMax.y - swapBox.anchorMin.y,
                        Is.EqualTo(0.12f).Within(0.005f),
                        "The information box must keep the approved compact height across resolutions.");
                    Assert.That(
                        swapRect.yMin - resourceRect.yMax,
                        Is.GreaterThanOrEqualTo(safeRect.height * 0.015f),
                        "The two information frames need a visible gap.");
                    Assert.That(
                        descriptionRect.yMin - swapRect.yMax,
                        Is.GreaterThanOrEqualTo(safeRect.height * 0.015f),
                        "The description must not touch the upper information frame.");
                    AssertVisibleFrameFillsBox(swapFrame, swapBox, frameVisibleY);
                    AssertVisibleFrameFillsBox(resourceFrame, resourceBox, frameVisibleY);
                    Assert.That(description.resizeTextMinSize, Is.GreaterThanOrEqualTo(15));
                    Assert.That(swapText.resizeTextMinSize, Is.GreaterThanOrEqualTo(14));
                    Assert.That(resourceText.resizeTextMinSize, Is.GreaterThanOrEqualTo(14));
                    Assert.That(swapText.resizeTextMaxSize, Is.GreaterThanOrEqualTo(15));
                    Assert.That(resourceText.resizeTextMaxSize, Is.GreaterThanOrEqualTo(15));
                    Assert.That(resourceText.text.Count(character => character == '\n'), Is.EqualTo(1));
                    AssertInside(safeRoot, panel);
                    AssertInside(swapBox, safeRoot);
                    AssertInside(resourceBox, safeRoot);
                    AssertInside(
                        FindDescendant(panel, "계약 이름"),
                        safeRoot);
                    AssertInside(
                        FindDescendant(panel, "계약 역할"),
                        safeRoot);
                    AssertInside(
                        FindDescendant(panel, "계약 설명"),
                        safeRoot);
                    AssertInside(
                        FindDescendant(swapBox, "계약 카드 교체 텍스트"),
                        swapBox);
                    AssertInside(
                        FindDescendant(resourceBox, "계약 자원 변화 텍스트"),
                        resourceBox);
                    AssertWorldRectsDoNotOverlap(swapBox, resourceBox);
                    AssertWorldRectsDoNotOverlap(resourceBox, rect);
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

        private static void AssertInside(
            RectTransform inner,
            RectTransform outer)
        {
            Assert.That(inner, Is.Not.Null);
            Assert.That(outer, Is.Not.Null);
            Rect innerRect = GetWorldRect(inner);
            Rect outerRect = GetWorldRect(outer);
            const float tolerance = 0.5f;
            Assert.That(innerRect.xMin, Is.GreaterThanOrEqualTo(outerRect.xMin - tolerance));
            Assert.That(innerRect.xMax, Is.LessThanOrEqualTo(outerRect.xMax + tolerance));
            Assert.That(innerRect.yMin, Is.GreaterThanOrEqualTo(outerRect.yMin - tolerance));
            Assert.That(innerRect.yMax, Is.LessThanOrEqualTo(outerRect.yMax + tolerance));
        }

        private static void AssertWorldRectsDoNotOverlap(
            RectTransform first,
            RectTransform second)
        {
            Rect firstRect = GetWorldRect(first);
            Rect secondRect = GetWorldRect(second);
            Assert.That(
                firstRect.Overlaps(secondRect),
                Is.False,
                $"UI frames overlap: '{first.name}' {firstRect} and "
                + $"'{second.name}' {secondRect}.");
        }

        private static Rect GetWorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Rect.MinMaxRect(
                corners[0].x,
                corners[0].y,
                corners[2].x,
                corners[2].y);
        }

        private static Vector2 ReadVisibleAlphaYRange(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolutePath = Path.Combine(projectRoot, assetPath);
            Texture2D decoded = new(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(
                    ImageConversion.LoadImage(
                        decoded,
                        File.ReadAllBytes(absolutePath),
                        false),
                    Is.True);
                Color32[] pixels = decoded.GetPixels32();
                int minVisibleY = decoded.height;
                int maxVisibleY = -1;
                for (int y = 0; y < decoded.height; y += 1)
                {
                    for (int x = 0; x < decoded.width; x += 1)
                    {
                        if (pixels[y * decoded.width + x].a < 128)
                        {
                            continue;
                        }

                        minVisibleY = Mathf.Min(minVisibleY, y);
                        maxVisibleY = Mathf.Max(maxVisibleY, y);
                    }
                }

                Assert.That(maxVisibleY, Is.GreaterThanOrEqualTo(0));
                return new Vector2(
                    minVisibleY / (float)decoded.height,
                    (maxVisibleY + 1f) / decoded.height);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decoded);
            }
        }

        private static void AssertVisibleFrameFillsBox(
            RectTransform frame,
            RectTransform box,
            Vector2 visibleY)
        {
            Rect frameRect = GetWorldRect(frame);
            Rect boxRect = GetWorldRect(box);
            float visibleMin = frameRect.yMin + visibleY.x * frameRect.height;
            float visibleMax = frameRect.yMin + visibleY.y * frameRect.height;
            const float tolerance = 2f;
            Assert.That(
                visibleMin,
                Is.EqualTo(boxRect.yMin).Within(tolerance),
                "Transparent source padding must not shrink the visible lower border.");
            Assert.That(
                visibleMax,
                Is.EqualTo(boxRect.yMax).Within(tolerance),
                "Transparent source padding must not shrink the visible upper border.");
        }

        private static RectTransform FindDescendant(
            RectTransform parent,
            string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int index = 0; index < parent.childCount; index += 1)
            {
                RectTransform child = parent.GetChild(index) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                if (child.name == objectName)
                {
                    return child;
                }

                RectTransform nested = FindDescendant(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
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

        private static void SetSpriteField(
            object instance,
            string fieldName,
            string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Assert.That(sprite, Is.Not.Null, assetPath);
            SetField(instance, fieldName, sprite);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Missing type: {typeName}");
            return type;
        }
    }
}
