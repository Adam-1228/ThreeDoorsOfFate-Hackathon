using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class V140IntegrationTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CardTypeName =
            "ThreeDoorsOfFate.Cards.CardData, Assembly-CSharp";
        private const string CharacterClassTypeName =
            "ThreeDoorsOfFate.Cards.CharacterClass, Assembly-CSharp";
        private const string StoreTypeName =
            "ThreeDoorsOfFate.Game.V140.RunHistoryStore, Assembly-CSharp";
        private const string LanguagePreferenceKey =
            "ThreeDoorsOfFate.Language";
        private const string HardRunSaveKey =
            "ThreeDoorsOfFate.HardRunSave";
        private const string HardRunSaveBackupKey =
            "ThreeDoorsOfFate.HardRunSave.BackupV1";
        private const string HardRunSaveTombstoneKey =
            "ThreeDoorsOfFate.HardRunSave.DeletedRunIds";

        private Type controllerType;
        private Type cardType;
        private GameObject host;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private string runHistoryKeyPrefix;
        private bool hadLanguage;
        private string previousLanguage;
        private bool hadHardSave;
        private string previousHardSave;
        private bool hadHardSaveBackup;
        private string previousHardSaveBackup;
        private bool hadHardSaveTombstone;
        private string previousHardSaveTombstone;

        [SetUp]
        public void SetUp()
        {
            hadLanguage = PlayerPrefs.HasKey(LanguagePreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                LanguagePreferenceKey,
                string.Empty);
            hadHardSave = PlayerPrefs.HasKey(HardRunSaveKey);
            previousHardSave = PlayerPrefs.GetString(HardRunSaveKey, string.Empty);
            hadHardSaveBackup = PlayerPrefs.HasKey(HardRunSaveBackupKey);
            previousHardSaveBackup = PlayerPrefs.GetString(
                HardRunSaveBackupKey,
                string.Empty);
            hadHardSaveTombstone = PlayerPrefs.HasKey(HardRunSaveTombstoneKey);
            previousHardSaveTombstone = PlayerPrefs.GetString(
                HardRunSaveTombstoneKey,
                string.Empty);
            PlayerPrefs.SetString(LanguagePreferenceKey, "en");
            PlayerPrefs.Save();
            GameLocalization.Initialize(SystemLanguage.English);

            originalEventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = RequireType(ControllerTypeName);
            cardType = RequireType(CardTypeName);
            host = new GameObject("V140 Integration Test Host");
            controller = host.AddComponent(controllerType);

            Font font = AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Fonts/GowunBatang-Regular.ttf");
            Assert.That(font, Is.Not.Null);
            SetField("uiFontAsset", font);
            SetField("uiFont", font);
            SetField("cardPool", CreateTypedCardList());
            TextAsset modifierCatalog = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/Data/RunModifiers/run_modifier_catalog.json");
            Assert.That(modifierCatalog, Is.Not.Null);
            SetField("runModifierCatalog", modifierCatalog);

            if (GetField<RectTransform>("canvasRoot") == null)
            {
                Invoke("BuildShell");
            }

            canvasRoot = GetField<RectTransform>("canvasRoot");
            root = GetField<RectTransform>("root");
            Assert.That(canvasRoot, Is.Not.Null);
            Assert.That(root, Is.Not.Null);
            runHistoryKeyPrefix =
                $"ThreeDoorsOfFate.Tests.V140Integration.{Guid.NewGuid():N}.";
            SetField("runHistoryKeyPrefix", runHistoryKeyPrefix);
        }

        [TearDown]
        public void TearDown()
        {
            Type storeType = Type.GetType(StoreTypeName);
            if (storeType != null && !string.IsNullOrWhiteSpace(runHistoryKeyPrefix))
            {
                string storageKey = InvokeStatic(
                    storeType,
                    "GetStorageKey",
                    runHistoryKeyPrefix).ToString();
                PlayerPrefs.DeleteKey(storageKey);
            }

            RestoreStringPreference(
                HardRunSaveKey,
                hadHardSave,
                previousHardSave);
            RestoreStringPreference(
                HardRunSaveBackupKey,
                hadHardSaveBackup,
                previousHardSaveBackup);
            RestoreStringPreference(
                HardRunSaveTombstoneKey,
                hadHardSaveTombstone,
                previousHardSaveTombstone);
            RestoreStringPreference(
                LanguagePreferenceKey,
                hadLanguage,
                previousLanguage);
            PlayerPrefs.Save();

            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            if (host != null)
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            if (originalEventSystem == null)
            {
                EventSystem created =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }

            GameLocalization.Initialize(Application.systemLanguage);
        }

        [Test]
        public void MainMenuAndContractSelectionExposeSafePersistentRoutes()
        {
            Invoke("ShowMainMenu");
            string[] expectedButtons =
            {
                "게임시작",
                "플레이 방법",
                "운명 기록",
                "업적",
                "설정",
                "게임종료"
            };
            foreach (string buttonName in expectedButtons)
            {
                RectTransform button = FindRequired(buttonName);
                Assert.That(button.GetComponent<Button>(), Is.Not.Null);
                AssertInsideUnitAnchors(button);
            }

            object gambler = ParseEnum(CharacterClassTypeName, "Gambler");
            Invoke("ShowStarterContractSelection", gambler);
            Assert.That(GetRawField("phase").ToString(), Is.EqualTo("ContractSelection"));
            Assert.That(FindRequired("계약 설정").GetComponent<Button>(), Is.Not.Null);
            for (int index = 1; index <= 3; index += 1)
            {
                RectTransform panel = FindRequired($"운명 계약 {index}");
                Assert.That(panel.parent, Is.SameAs(GetField<RectTransform>("contentRoot")));
                AssertInsideUnitAnchors(panel);
                AssertStarterContractTextSafe(panel);
            }
        }

        [Test]
        public void CoreV140SurfacesReuseInspectionAndProtectGameOverMessage()
        {
            object gambler = ParseEnum(CharacterClassTypeName, "Gambler");
            Invoke("StartRun", gambler, "gambler.high_roll");
            Assert.That(GetRawField("phase").ToString(), Is.EqualTo("DoorSelection"));
            foreach (string methodName in new[]
            {
                "BuildCharacterTraitSummaryText",
                "BuildCharacterTraitText",
                "BuildCombatAwakeningSummaryText"
            })
            {
                string[] lines = Invoke(methodName)
                    .ToString()
                    .Split('\n');
                string identityLine = methodName == "BuildCombatAwakeningSummaryText"
                    ? lines[lines.Length - 1]
                    : lines[0];
                AssertNoHangul(identityLine, methodName);
            }
            RectTransform[] doors = ActiveDescendants(root)
                .Where(rect => IsNumberedDoorRoot(rect.name))
                .ToArray();
            Assert.That(doors, Has.Length.GreaterThanOrEqualTo(3));
            foreach (RectTransform door in doors)
            {
                AssertInsideUnitAnchors(door);
            }

            SetField("pendingRunEventId", "event.forgotten_altar");
            Invoke("ShowEvent");
            RectTransform eventPanel = FindRequired(
                "운명 사건 event.forgotten_altar");
            AssertInsideUnitAnchors(eventPanel);

            ScriptableObject inspectionCard = FindCardWithFullSprite();
            SetField("currentShopOffersReady", true);
            IList shopCards = GetField<IList>("currentShopCards");
            shopCards.Clear();
            shopCards.Add(inspectionCard);
            GetField<ISet<int>>("purchasedShopCardSlots").Clear();
            SetField("currentShopRunItemId", string.Empty);
            SetField("currentShopRunItemPurchased", false);
            SetField("gold", 999);
            Invoke("ShowShop");
            Assert.That(GetRawField("phase").ToString(), Is.EqualTo("Shop"));
            AssertInsideUnitAnchors(FindRequired("상품 0"));

            UnityAction noOp = () => { };
            Invoke("ShowCardInspection", inspectionCard, "Inspect", noOp);
            Image shopPreview = GetField<Image>("cardPreviewImage");
            Assert.That(shopPreview.gameObject.activeInHierarchy, Is.True);
            Assert.That(shopPreview.transform.parent, Is.SameAs(root));
            Assert.That(
                ActiveDescendants(root).Count(rect => rect.name == "카드 확대 프리뷰"),
                Is.EqualTo(1));
            Invoke("HideCardInspection");

            object enemy = Invoke("CreateEnemy", false, false);
            Invoke("StartCombat", enemy);
            Assert.That(GetRawField("phase").ToString(), Is.EqualTo("Combat"));
            Assert.That(FindRequired("적 정보"), Is.Not.Null);
            string selectedIntentLog = GetField<IList>("combatLog")
                .Cast<string>()
                .Last(entry => entry.Contains("action pool"));
            AssertNoHangul(selectedIntentLog, "catalog enemy intent log");
            Type inspectionModeType = controllerType.GetNestedType(
                "CardInspectionMode",
                BindingFlags.NonPublic);
            Assert.That(inspectionModeType, Is.Not.Null);
            object combatUse = Enum.Parse(inspectionModeType, "CombatUse");
            Invoke(
                "ShowCardInspection",
                inspectionCard,
                combatUse,
                "Use",
                noOp);
            Image combatPreview = GetField<Image>("cardPreviewImage");
            Assert.That(combatPreview, Is.SameAs(shopPreview));
            Assert.That(
                ActiveDescendants(root).Count(rect => rect.name == "카드 확대 프리뷰"),
                Is.EqualTo(1));
            Invoke("HideCardInspection");

            Invoke("ShowAchievements");
            RectTransform achievementModal = FindRequired("업적 모달");
            AssertInsideUnitAnchors(achievementModal);
            Invoke("HideAchievements");

            SetField("debt", 9);
            SetField("runHistoryMaximumSameRerollStreak", 3);
            Invoke(
                "ShowGameOver",
                false,
                "동굴이 또 하나의 이름을 삼켰습니다.");
            RectTransform message = FindRequired("Game Over Message");
            RectTransform summaryPanel = FindRequired(
                "Game Over Run Summary Panel");
            Text summary = FindRequired("Game Over Run Summary")
                .GetComponent<Text>();
            Assert.That(
                summaryPanel.anchorMax.y,
                Is.LessThanOrEqualTo(message.anchorMin.y - 0.04f));
            Assert.That(summary.text, Does.Contain("Record Epithets"));
            Assert.That(
                summary.text,
                Does.Contain("What Changes If You Roll Again?"));

            Invoke("ShowRunHistory");
            RectTransform historyOuter = FindRequired("운명 기록 외곽 프레임");
            RectTransform historySafeRoot = FindRequired(
                "운명 기록 목록 안전영역");
            RectTransform historyRow = FindRequired("운명 기록 항목 0");
            AssertInsideUnitAnchors(historyOuter);
            AssertInsideUnitAnchors(historySafeRoot);
            AssertInsideUnitAnchors(historyRow);
            Assert.That(historySafeRoot.parent, Is.SameAs(historyOuter));
            Assert.That(historyRow.parent, Is.SameAs(historySafeRoot));
        }

        private object CreateTypedCardList()
        {
            Type listType = typeof(List<>).MakeGenericType(cardType);
            IList cards = (IList)Activator.CreateInstance(listType);
            foreach (string guid in AssetDatabase.FindAssets(
                "t:CardData",
                new[] { "Assets/Data/Cards/MVP" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object card = AssetDatabase.LoadAssetAtPath(
                    path,
                    cardType);
                if (card != null)
                {
                    cards.Add(card);
                }
            }

            Assert.That(cards.Count, Is.GreaterThan(0));
            return cards;
        }

        private ScriptableObject FindCardWithFullSprite()
        {
            foreach (object card in GetField<IList>("cardPool"))
            {
                PropertyInfo spriteProperty = card.GetType().GetProperty(
                    "FullCardSprite",
                    BindingFlags.Public | BindingFlags.Instance);
                if (spriteProperty?.GetValue(card) is Sprite)
                {
                    return card as ScriptableObject;
                }
            }

            Assert.Fail("At least one real card with a full-card sprite is required.");
            return null;
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = ActiveDescendants(root)
                .FirstOrDefault(rect => rect.name == objectName);
            Assert.That(
                found,
                Is.Not.Null,
                $"Expected active runtime UI object '{objectName}'.");
            return found;
        }

        private static IEnumerable<RectTransform> ActiveDescendants(
            RectTransform parent)
        {
            if (parent == null)
            {
                yield break;
            }

            if (parent.gameObject.activeInHierarchy)
            {
                yield return parent;
            }

            for (int index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index) is not RectTransform child)
                {
                    continue;
                }

                foreach (RectTransform descendant in ActiveDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private static void AssertInsideUnitAnchors(RectTransform rect)
        {
            Assert.That(rect, Is.Not.Null);
            Assert.That(rect.anchorMin.x, Is.InRange(0f, 1f), rect.name);
            Assert.That(rect.anchorMin.y, Is.InRange(0f, 1f), rect.name);
            Assert.That(rect.anchorMax.x, Is.InRange(0f, 1f), rect.name);
            Assert.That(rect.anchorMax.y, Is.InRange(0f, 1f), rect.name);
            Assert.That(rect.anchorMin.x, Is.LessThanOrEqualTo(rect.anchorMax.x));
            Assert.That(rect.anchorMin.y, Is.LessThanOrEqualTo(rect.anchorMax.y));
        }

        private static void AssertStarterContractTextSafe(RectTransform panel)
        {
            RectTransform name = panel.Find("계약 이름") as RectTransform;
            RectTransform role = panel.Find("계약 역할") as RectTransform;
            RectTransform description = panel.Find("계약 설명") as RectTransform;
            RectTransform changes = panel.Find("계약 변경점") as RectTransform;
            foreach (RectTransform text in new[] { name, role, description, changes })
            {
                Assert.That(text, Is.Not.Null, panel.name);
                Assert.That(text.anchorMin.x, Is.GreaterThanOrEqualTo(0.17f), text.name);
                Assert.That(text.anchorMax.x, Is.LessThanOrEqualTo(0.83f), text.name);
            }

            Assert.That(name.anchorMin.y, Is.GreaterThanOrEqualTo(0.70f));
            Assert.That(name.anchorMax.y, Is.LessThanOrEqualTo(0.82f));
            Assert.That(role.anchorMax.y, Is.LessThanOrEqualTo(name.anchorMin.y));
            Assert.That(description.anchorMax.y, Is.LessThanOrEqualTo(role.anchorMin.y));
            Assert.That(changes.anchorMax.y, Is.LessThanOrEqualTo(description.anchorMin.y));
        }

        private static void AssertNoHangul(string value, string label)
        {
            Assert.That(
                value.Any(character => character >= '\uAC00' && character <= '\uD7A3'),
                Is.False,
                $"{label}: {value}");
        }

        private static bool IsNumberedDoorRoot(string objectName)
        {
            return objectName != null
                && objectName.StartsWith("문 ", StringComparison.Ordinal)
                && int.TryParse(objectName.Substring(2), out _);
        }

        private object Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(candidate =>
                    candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length
                    && ParametersAccept(candidate.GetParameters(), arguments));
            Assert.That(method, Is.Not.Null, methodName);
            return method.Invoke(controller, arguments);
        }

        private static bool ParametersAccept(
            IReadOnlyList<ParameterInfo> parameters,
            IReadOnlyList<object> arguments)
        {
            for (int index = 0; index < parameters.Count; index += 1)
            {
                if (arguments[index] == null)
                {
                    continue;
                }

                if (!parameters[index].ParameterType.IsInstanceOfType(arguments[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private T GetField<T>(string fieldName)
        {
            return (T)GetRawField(fieldName);
        }

        private object GetRawField(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return field.GetValue(controller);
        }

        private void SetField(string fieldName, object value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(controller, value);
        }

        private static object InvokeStatic(
            Type type,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            return method.Invoke(null, arguments);
        }

        private static object ParseEnum(string typeName, string value)
        {
            return Enum.Parse(RequireType(typeName), value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, typeName);
            return type;
        }

        private static void RestoreStringPreference(
            string key,
            bool existed,
            string value)
        {
            if (existed)
            {
                PlayerPrefs.SetString(key, value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
