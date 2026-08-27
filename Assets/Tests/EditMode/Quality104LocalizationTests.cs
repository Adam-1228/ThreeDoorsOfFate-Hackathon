using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104LocalizationTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private RectTransform root;
        private EventSystem originalEventSystem;
        private Texture2D messageTexture;
        private Sprite messageSprite;
        private bool hadPreviousLanguage;
        private string previousLanguage;
        private string hardRunSaveKey;
        private string hardRunSaveBackupKey;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(
                GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, "en");
            PlayerPrefs.Save();
            GameLocalization.Initialize(SystemLanguage.English);

            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null, "Runtime controller type must compile.");

            controllerHost = new GameObject("Quality 1.0.4 Localization Test Host");
            controller = controllerHost.AddComponent(controllerType);
            string checkpointPrefix =
                $"ThreeDoorsOfFate.Tests.Localization.{Guid.NewGuid():N}.";
            hardRunSaveKey = checkpointPrefix + "HardRunSave";
            hardRunSaveBackupKey = checkpointPrefix + "HardRunSave.BackupV1";
            SetField("hardRunSaveKey", hardRunSaveKey);
            SetField("hardRunSaveBackupKey", hardRunSaveBackupKey);
            root = TryGetField<RectTransform>("root");
            if (root == null)
            {
                Invoke("BuildShell");
                root = TryGetField<RectTransform>("root");
            }

            Assert.That(root, Is.Not.Null, "Controller must build its runtime UI shell.");
            canvasRoot = TryGetField<RectTransform>("canvasRoot");

            messageTexture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            messageTexture.SetPixels(Enumerable.Repeat(Color.red, 64).ToArray());
            messageTexture.Apply();
            messageSprite = Sprite.Create(
                messageTexture,
                new Rect(0f, 0f, 8f, 8f),
                new Vector2(0.5f, 0.5f));
            messageSprite.name = "Korean Baked Game Over Message Fixture";
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

            if (messageSprite != null)
            {
                UnityEngine.Object.DestroyImmediate(messageSprite);
            }

            if (messageTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(messageTexture);
            }

            if (originalEventSystem == null)
            {
                EventSystem created = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created.gameObject);
                }
            }

            if (!string.IsNullOrWhiteSpace(hardRunSaveKey))
            {
                PlayerPrefs.DeleteKey(hardRunSaveKey);
                PlayerPrefs.DeleteKey(hardRunSaveBackupKey);
                PlayerPrefs.DeleteKey(hardRunSaveKey + ".DeletedRunIds");
            }

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
        public void ShopCombinationGuide_EnglishColumnsContainNoHangul()
        {
            GameLocalization.SetLanguage(GameLanguage.English);

            Invoke("ShowShopCombinationGuide");
            Canvas.ForceUpdateCanvases();

            string combined = string.Join(
                "\n",
                GetField<Text>("subtitleText").text,
                FindRequired("카드 조합법 왼쪽 목록").GetComponent<Text>().text,
                FindRequired("카드 조합법 오른쪽 목록").GetComponent<Text>().text);

            Assert.That(combined, Does.Not.Match("[가-힣]"));
            Assert.That(combined, Does.Contain("Shop: Review card synergy effects"));
            Assert.That(combined, Does.Contain("Fate Counter"));
            Assert.That(combined, Does.Contain("Gatekeeper Hunt"));
        }

        [Test]
        public void EnglishGameOver_UsesLiveTextInsteadOfKoreanMessageSprite()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            GetField<List<Sprite>>("gameOverMessageSprites").Add(messageSprite);
            GetField<List<Sprite>>("gameOverBackgroundSprites").Clear();

            Invoke("ShowGameOver", false, "동굴이 또 하나의 이름을 삼켰습니다.");
            Canvas.ForceUpdateCanvases();

            RectTransform bakedMessage = FindOptional("Game Over Message Image");
            RectTransform liveMessage = FindOptional("Game Over Message");
            Assert.That(
                bakedMessage,
                Is.Null,
                "English mode must not display a Korean-baked message sprite.");
            Assert.That(liveMessage, Is.Not.Null);
            Assert.That(liveMessage.GetComponent<Text>().text, Does.Not.Match("[가-힣]"));
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform found = FindOptional(objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
        }

        private RectTransform FindOptional(string objectName)
        {
            return FindDescendants(root)
                .FirstOrDefault(candidate => candidate.name == objectName);
        }

        private static IEnumerable<RectTransform> FindDescendants(RectTransform parent)
        {
            yield return parent;
            for (int index = 0; index < parent.childCount; index += 1)
            {
                if (parent.GetChild(index) is not RectTransform child)
                {
                    continue;
                }

                foreach (RectTransform descendant in FindDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private void Invoke(string methodName, params object[] arguments)
        {
            MethodInfo method = controllerType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == arguments.Length);
            method.Invoke(controller, arguments);
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            return (T)field.GetValue(controller);
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, value);
        }

        private T TryGetField<T>(string fieldName)
            where T : class
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(controller) as T;
        }
    }
}
