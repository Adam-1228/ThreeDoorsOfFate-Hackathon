using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using ThreeDoorsOfFate.Platform;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104HandFlowTutorialTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private EventSystem originalEventSystem;
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
            Assert.That(controllerType, Is.Not.Null);
            controllerHost = new GameObject("Quality 1.0.4 Hand Flow Test Host");
            controller = controllerHost.AddComponent(controllerType);
            if (TryGetField<RectTransform>("root") == null)
            {
                Invoke("BuildShell");
            }

            canvasRoot = TryGetField<RectTransform>("canvasRoot");
            SetField("howToPlaySprites", new List<Sprite>
            {
                null, null, null, null, null
            });

            GetField<IList>("deck").Add(null);
            GetField<IList>("hand").Add(null);
            Invoke("ShowMainMenu");
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

        [TestCase("en", "Unused cards stay", "only empty slots", "do not automatically return")]
        [TestCase("ko", "사용하지 않은 카드는 손에 남", "빈 자리만 보충", "자동 복귀하지 않")]
        public void PageFour_UsesRuntimePracticeAndStatesExactRule(
            string languageCode,
            string unusedRule,
            string refillRule,
            string discardRule)
        {
            OpenPractice(languageCode);

            Assert.That(GetField<Image>("howToPlayImage").gameObject.activeSelf, Is.False);
            Assert.That(
                GetField<RectTransform>("howToPlayEnglishVisualRoot").gameObject.activeSelf,
                Is.True);
            Assert.That(FindRequired("손패 순환 연습"), Is.Not.Null);
            string caption = GetField<Text>("howToPlayCaptionText").text;
            Assert.That(caption, Does.Contain(unusedRule).IgnoreCase);
            Assert.That(caption, Does.Contain(refillRule).IgnoreCase);
            Assert.That(caption, Does.Contain(discardRule).IgnoreCase);
        }

        [Test]
        public void EndTurn_KeepsUnusedCardsAndDrawPileWithoutChangingRealState()
        {
            OpenPractice("en");
            IList deck = GetField<IList>("deck");
            IList hand = GetField<IList>("hand");
            object[] deckBefore = deck.Cast<object>().ToArray();
            object[] handBefore = hand.Cast<object>().ToArray();
            string progressBefore = CaptureProgress();

            int handCountBefore = GetField<int>("handFlowPracticeHandCount");
            int drawCountBefore = GetField<int>("handFlowPracticeDrawCount");
            GetField<Button>("handFlowPracticeEndTurnButton").onClick.Invoke();

            Assert.That(GetField<int>("handFlowPracticeHandCount"), Is.EqualTo(handCountBefore));
            Assert.That(GetField<int>("handFlowPracticeDrawCount"), Is.EqualTo(drawCountBefore));
            Assert.That(deck.Cast<object>(), Is.EqualTo(deckBefore));
            Assert.That(hand.Cast<object>(), Is.EqualTo(handBefore));
            Assert.That(CaptureProgress(), Is.EqualTo(progressBefore));
        }

        [Test]
        public void SelectAndUse_RefillsOneSlotCompletesAndResetsOnLanguageChange()
        {
            OpenPractice("en");
            string progressBefore = CaptureProgress();
            GetField<Button>("handFlowPracticeEndTurnButton").onClick.Invoke();
            List<Button> cards = GetField<List<Button>>("handFlowPracticeCardButtons");
            cards[1].onClick.Invoke();
            Assert.That(GetField<int>("handFlowPracticeSelectedIndex"), Is.EqualTo(1));
            Assert.That(GetField<Button>("handFlowPracticeUseButton").interactable, Is.True);

            GetField<Button>("handFlowPracticeUseButton").onClick.Invoke();
            Assert.That(GetField<int>("handFlowPracticeHandCount"), Is.EqualTo(3));
            Assert.That(GetField<int>("handFlowPracticeDrawCount"), Is.EqualTo(1));
            Assert.That(
                GetField<Text>("handFlowPracticeStatusText").text,
                Does.Contain("Practice complete"));
            Assert.That(CaptureProgress(), Is.EqualTo(progressBefore));

            Invoke("SetGameLanguage", GameLanguage.Korean);
            Assert.That(GetField<int>("handFlowPracticeHandCount"), Is.EqualTo(3));
            Assert.That(GetField<int>("handFlowPracticeDrawCount"), Is.EqualTo(2));
            Assert.That(GetField<int>("handFlowPracticeSelectedIndex"), Is.EqualTo(-1));
            Assert.That(
                GetField<Text>("handFlowPracticeStatusText").text,
                Does.Not.Contain("Practice complete"));
        }

        private void OpenPractice(string languageCode)
        {
            GameLocalization.SetLanguage(
                languageCode == "ko" ? GameLanguage.Korean : GameLanguage.English);
            Invoke("ShowHowToPlay");
            Invoke("ShowHowToPlayPage", 3);
            Canvas.ForceUpdateCanvases();
        }

        private static string CaptureProgress()
        {
            return PlayerPrefsProgressStore.CaptureJson(
                PlayerPrefsProgressStore.ProductionPrefix,
                "quality104-hand-flow",
                0,
                0);
        }

        private RectTransform FindRequired(string objectName)
        {
            RectTransform root = GetField<RectTransform>("root");
            RectTransform found = FindDescendants(root)
                .FirstOrDefault(candidate => candidate.name == objectName);
            Assert.That(found, Is.Not.Null, $"Expected runtime UI object '{objectName}'.");
            return found;
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

        private T TryGetField<T>(string fieldName)
            where T : class
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(controller) as T;
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected controller field '{fieldName}'.");
            field.SetValue(controller, value);
        }
    }
}
