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
    public sealed class ContractAndHistory140PointerTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string EntryTypeName =
            "ThreeDoorsOfFate.Game.V140.RunHistoryEntry, Assembly-CSharp";
        private const string StoreTypeName =
            "ThreeDoorsOfFate.Game.V140.RunHistoryStore, Assembly-CSharp";
        private const string LanguagePreferenceKey =
            "ThreeDoorsOfFate.Language";

        private Type controllerType;
        private Type storeType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private string keyPrefix;
        private bool hadPreviousLanguage;
        private string previousLanguage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(LanguagePreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                LanguagePreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(LanguagePreferenceKey, "en");
            PlayerPrefs.Save();

            originalEventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = RequireType(ControllerTypeName);
            storeType = RequireType(StoreTypeName);
            keyPrefix =
                $"ThreeDoorsOfFate.Tests.HistoryPointer.{Guid.NewGuid():N}.";
            AppendHistoryEntry();

            controllerHost = new GameObject("Contract And History Pointer Host");
            controller = controllerHost.AddComponent(controllerType);
            SetField("runHistoryKeyPrefix", keyPrefix);
            Invoke("ShowMainMenu");
            root = GetField<RectTransform>("root");
            canvasRoot = GetField<RectTransform>("canvasRoot");

            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            string storageKey = InvokeStatic(
                storeType,
                "GetStorageKey",
                keyPrefix).ToString();
            PlayerPrefs.DeleteKey(storageKey);
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
            if (canvasRoot != null)
            {
                UnityEngine.Object.Destroy(canvasRoot.gameObject);
            }

            if (controllerHost != null)
            {
                UnityEngine.Object.Destroy(controllerHost);
            }

            if (originalEventSystem == null)
            {
                EventSystem created =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.Destroy(created.gameObject);
                }
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PointerOpensHistoryDetailReturnsAndClosesToMenu()
        {
            RectTransform historyButton = FindRequired("운명 기록");
            Assert.That(
                RaycastAndClick(historyButton),
                Is.SameAs(historyButton.gameObject));
            yield return null;
            Canvas.ForceUpdateCanvases();

            RectTransform row = FindRequired("운명 기록 항목 0");
            Assert.That(RaycastAndClick(row), Is.SameAs(row.gameObject));
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(
                FindRequired("운명 기록 상세 외곽 프레임"),
                Is.Not.Null);

            RectTransform back = FindRequired("운명 기록 상세 뒤로");
            Assert.That(RaycastAndClick(back), Is.SameAs(back.gameObject));
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(FindRequired("운명 기록 항목 0"), Is.Not.Null);

            RectTransform close = FindRequired("운명 기록 닫기");
            Assert.That(RaycastAndClick(close), Is.SameAs(close.gameObject));
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(GetField<object>("phase").ToString(), Is.EqualTo("MainMenu"));
            Assert.That(FindRequired("게임시작"), Is.Not.Null);
        }

        private void AppendHistoryEntry()
        {
            object entry = Activator.CreateInstance(RequireType(EntryTypeName));
            SetMember(entry, "RunId", "pointer-history");
            SetMember(entry, "GameVersion", "1.4.0");
            SetMember(entry, "FinishedAtUnixSeconds", 140L);
            SetMember(entry, "CharacterClass", "Oracle");
            SetMember(entry, "Difficulty", "Hard");
            SetMember(entry, "EndingKind", "return");
            SetMember(entry, "EndingCauseKey", "ending.title.return.oracle");
            SetMember(entry, "DoorsCleared", 10);
            SetMember(entry, "FinalHealth", 30);
            SetMember(entry, "FinalMaxHealth", 58);
            SetMember(entry, "FinalGold", 42);
            SetMember(entry, "FinalDeckCardIds", new List<string>
            {
                "card_fate_strike",
                "card_fate_strike"
            });
            InvokeStatic(storeType, "Append", keyPrefix, entry);
        }

        private GameObject RaycastAndClick(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
                null,
                worldCenter);
            EventSystem eventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null);
            GraphicRaycaster raycaster =
                canvasRoot.GetComponent<GraphicRaycaster>();
            Assert.That(raycaster, Is.Not.Null);

            PointerEventData pointer = new(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left
            };
            List<RaycastResult> results = new();
            raycaster.Raycast(pointer, results);
            Assert.That(results, Is.Not.Empty);
            GameObject hit = results[0].gameObject;
            ExecuteEvents.ExecuteHierarchy(
                hit,
                pointer,
                ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(
                hit,
                pointer,
                ExecuteEvents.pointerUpHandler);
            return ExecuteEvents.ExecuteHierarchy(
                hit,
                pointer,
                ExecuteEvents.pointerClickHandler);
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendant(root, objectName);
            Assert.That(
                found,
                Is.Not.Null,
                $"Expected runtime UI object '{objectName}'.");
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
            Assert.That(method, Is.Not.Null, methodName);
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
            Assert.That(field, Is.Not.Null, fieldName);
            return field;
        }

        private static object InvokeStatic(
            Type type,
            string methodName,
            params object[] values)
        {
            MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == values.Length);
            return method.Invoke(null, values);
        }

        private static void SetMember(
            object instance,
            string memberName,
            object value)
        {
            FieldInfo field = instance.GetType().GetField(
                memberName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, memberName);
            field.SetValue(instance, value);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, typeName);
            return type;
        }
    }
}
