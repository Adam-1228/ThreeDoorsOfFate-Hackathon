using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class FramedModalLayoutTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform contentRoot;
        private RectTransform canvasRoot;
        private EventSystem originalEventSystem;

        [SetUp]
        public void SetUp()
        {
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("Framed Modal Layout Test Host");
            controller = controllerHost.AddComponent(controllerType);
            contentRoot = GetField<RectTransform>("contentRoot");
            if (contentRoot == null)
            {
                Invoke("BuildShell");
                contentRoot = GetField<RectTransform>("contentRoot");
            }

            Assert.That(contentRoot, Is.Not.Null, "Controller must build its runtime UI shell.");
            canvasRoot = GetField<RectTransform>("canvasRoot");
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

            if (originalEventSystem == null)
            {
                EventSystem createdEventSystem =
                    UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (createdEventSystem != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdEventSystem.gameObject);
                }
            }
        }

        [Test]
        public void TenDoorClearSummary_StaysBelowCrestAndAboveFirstChoice()
        {
            SetEnumField("currentDifficulty", "Normal");
            SetField("debt", 0);
            SetField("gold", 94);

            Invoke("ShowTenDoorClearChoice");

            RectTransform summary = FindRequired("10문 클리어 설명");
            RectTransform firstChoice = FindRequired("귀환한다 선택지");

            Assert.That(
                summary.anchorMax.y,
                Is.LessThanOrEqualTo(0.7201f),
                "Summary text must stay below the frame's centre crest.");
            Assert.That(
                summary.anchorMin.y - firstChoice.anchorMax.y,
                Is.GreaterThanOrEqualTo(0.0149f),
                "Summary text must retain visible separation from the first choice.");
        }

        [Test]
        public void PostCombatSustainTitle_IsOutsideDecoratedPanel()
        {
            Invoke("ShowPostCombatSustainChoice", null, false);

            AssertExternalTitle(
                "전투 후 정비",
                "전투 후 정비 제목 박스");
        }

        [Test]
        public void EndlessCheckpointTitle_IsOutsideDecoratedPanel()
        {
            Invoke("ShowEndlessCheckpoint");

            AssertExternalTitle(
                "무한 체크포인트",
                "무한 체크포인트 제목 박스");
        }

        private void AssertExternalTitle(string panelName, string titleBoxName)
        {
            RectTransform panel = FindRequired(panelName);
            RectTransform titleBox = FindRequired(titleBoxName);

            Assert.That(panel.anchorMax.y, Is.EqualTo(0.840f).Within(0.0001f));
            Assert.That(titleBox.anchorMin.y, Is.GreaterThanOrEqualTo(0.870f));
            Assert.That(panel.parent, Is.EqualTo(contentRoot));
            Assert.That(
                titleBox.parent,
                Is.EqualTo(contentRoot),
                "The title must be a sibling above the decorated panel.");
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindDescendant(contentRoot, objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private static RectTransform FindDescendant(RectTransform parent, string objectName)
        {
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

        private void Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected controller method '{methodName}'.");
            method.Invoke(controller, arguments);
        }

        private T GetField<T>(string fieldName) where T : class
        {
            FieldInfo field = GetFieldInfo(fieldName);
            return field.GetValue(controller) as T;
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
    }
}
