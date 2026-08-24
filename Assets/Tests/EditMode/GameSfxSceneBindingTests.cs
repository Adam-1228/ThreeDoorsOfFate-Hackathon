using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class GameSfxSceneBindingTests
    {
        private const string PlayableScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";

        private static readonly string[] ListProperties =
        {
            "cardDrawClips",
            "cardPlayClips"
        };

        private static readonly string[] SingleClipProperties =
        {
            "uiDeniedClip",
            "cardDiscardClip",
            "doorOpenClip",
            "turnCommitClip",
            "diceRollClip",
            "playerHitClip",
            "healClip",
            "combatStartClip",
            "enemyDefeatClip",
            "treasureOpenClip",
            "eventChoiceClip",
            "restClip",
            "curseAcceptClip",
            "defeatClip",
            "victoryClip",
            "endingClip",
            "rewardRevealClip",
            "rewardClaimClip",
            "goldGainClip",
            "purchaseClip",
            "upgradeClip",
            "itemEquipClip",
            "saveSuccessClip",
            "saveFailureClip",
            "loadSuccessClip",
            "loadFailureClip"
        };

        private static readonly string[] SilentUiClipProperties =
        {
            "uiBackClip",
            "panelOpenClip",
            "panelCloseClip",
            "runStartClip"
        };

        private static readonly IReadOnlyDictionary<string, string> MusicHashes =
            new Dictionary<string, string>
            {
                ["Assets/Audio/Music/Bronze_Gates_Closing.mp3"] =
                    "03D71683441E45B400FF80CCCB490DF6C69840EBA1FD580DBDD52A680F8F3C3A",
                ["Assets/Audio/Music/Payment_in_Iron.mp3"] =
                    "DB3282B5DEE6B7A2410CE3814906FB5FE5025DD284AB33464E517913424E134F",
                ["Assets/Audio/Music/The_Gatekeeper_s_Toll.mp3"] =
                    "4BA67DF185595A8579B1315DFC3C97C1EDD84FF369A3DD0D900D6BFC8E3816B4",
                ["Assets/Audio/Music/The_Iron_Seal.mp3"] =
                    "3BB120ADB34C3C6DBE720EA3ACD91AA5962877815817A3A39FE644CEB4A3F696",
                ["Assets/Audio/Music/The_Merchant_s_Toll.mp3"] =
                    "9F9BF8A679F02678E2ECA2CA2088BAAAC7B3AFD6B4C0BC04A2C19B15D1699B1B"
            };

        [Test]
        public void ThreeDoorsPlayable_AllGameSfxFieldsResolve()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayableScenePath, OpenSceneMode.Single);
            Component controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .FirstOrDefault(component => component != null && component.GetType().Name == "ThreeDoorsGameController");

            Assert.That(controller, Is.Not.Null, "Playable scene controller is missing.");
            SerializedObject serializedController = new(controller);

            foreach (string propertyName in ListProperties)
            {
                SerializedProperty property = serializedController.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, propertyName);
                Assert.That(property.isArray, Is.True, propertyName);
                Assert.That(property.arraySize, Is.GreaterThan(0), propertyName);
                for (int i = 0; i < property.arraySize; i += 1)
                {
                    Assert.That(
                        property.GetArrayElementAtIndex(i).objectReferenceValue,
                        Is.Not.Null,
                        $"{propertyName}[{i}]");
                }
            }

            foreach (string propertyName in SingleClipProperties)
            {
                SerializedProperty property = serializedController.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, propertyName);
                Assert.That(property.objectReferenceValue, Is.Not.Null, propertyName);
            }
        }

        [Test]
        public void ThreeDoorsPlayable_GenericUiAcceptClipsRemainEmpty()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayableScenePath, OpenSceneMode.Single);
            Component controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .FirstOrDefault(component => component != null && component.GetType().Name == "ThreeDoorsGameController");

            Assert.That(controller, Is.Not.Null, "Playable scene controller is missing.");
            SerializedProperty uiAcceptClips = new SerializedObject(controller).FindProperty("uiAcceptClips");
            Assert.That(uiAcceptClips, Is.Not.Null);
            Assert.That(uiAcceptClips.arraySize, Is.Zero, "Generic click clips must not be serialized into release scenes.");
        }

        [Test]
        public void ThreeDoorsPlayable_MenuNavigationClipsRemainUnassigned()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayableScenePath, OpenSceneMode.Single);
            Component controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .FirstOrDefault(component => component != null && component.GetType().Name == "ThreeDoorsGameController");

            Assert.That(controller, Is.Not.Null, "Playable scene controller is missing.");
            SerializedObject serializedController = new(controller);
            foreach (string propertyName in SilentUiClipProperties)
            {
                SerializedProperty property = serializedController.FindProperty(propertyName);
                Assert.That(property, Is.Not.Null, propertyName);
                Assert.That(property.objectReferenceValue, Is.Null, $"{propertyName} must stay silent in release scenes.");
            }
        }

        [TestCaseSource(nameof(MusicHashCases))]
        public void BackgroundMusic_MatchesUnchangedBaseline(string assetPath, string expectedHash)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(File.ReadAllBytes(assetPath));
            string actualHash = BitConverter.ToString(hash).Replace("-", string.Empty);

            Assert.That(actualHash, Is.EqualTo(expectedHash), assetPath);
        }

        private static IEnumerable<object[]> MusicHashCases()
        {
            return MusicHashes.Select(pair => new object[] { pair.Key, pair.Value });
        }
    }
}
