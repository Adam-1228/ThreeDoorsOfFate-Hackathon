using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class HowToPlayBuilderBindingTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private static readonly string[] ExpectedResourcePaths =
        {
            "Tutorial/how_to_play_01_class",
            "Tutorial/how_to_play_02_doors",
            "Tutorial/how_to_play_03_combat",
            "Tutorial/how_to_play_04_card_use",
            "Tutorial/how_to_play_05_growth"
        };

        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private EventSystem originalEventSystem;

        [SetUp]
        public void SetUp()
        {
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Type controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("How To Play Builder Test Host");
            controller = controllerHost.AddComponent(controllerType);
            FieldInfo canvasRootField = controllerType.GetField(
                "canvasRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(canvasRootField, Is.Not.Null);
            canvasRoot = canvasRootField.GetValue(controller) as RectTransform;
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
        public void Controller_DoesNotSerializeTutorialImageCache()
        {
            SerializedObject serializedController = new(controller);
            serializedController.Update();
            Assert.That(
                serializedController.FindProperty("howToPlaySprites"),
                Is.Null,
                "Tutorial image caching must not alter the player scene's serialized controller schema.");
        }

        [Test]
        public void RuntimeResources_ContainAllTutorialPagesInOrder()
        {
            foreach (string resourcePath in ExpectedResourcePaths)
            {
                Assert.That(
                    Resources.Load<Sprite>(resourcePath),
                    Is.Not.Null,
                    $"The packaged player must be able to load {resourcePath} without scene bindings.");
            }
        }

        [Test]
        public void TutorialQaCapture_IncludesExactAppStoreLandscapeSizes()
        {
            Type captureType = Type.GetType(
                "ThreeDoorsOfFate.Editor.HowToPlaySourceQACapture, Assembly-CSharp-Editor");
            Assert.That(captureType, Is.Not.Null);

            FieldInfo resolutionsField = captureType.GetField(
                "TutorialQaResolutions",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(resolutionsField, Is.Not.Null);

            Vector2Int[] resolutions = (Vector2Int[])resolutionsField.GetValue(null);
            Assert.That(resolutions, Does.Contain(new Vector2Int(2778, 1284)));
            Assert.That(resolutions, Does.Contain(new Vector2Int(2732, 2048)));
        }
    }
}
