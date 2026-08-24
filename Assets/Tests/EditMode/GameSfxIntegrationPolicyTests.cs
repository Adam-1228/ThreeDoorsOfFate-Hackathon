using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class GameSfxIntegrationPolicyTests
    {
        private const string PlayableScenePath = "Assets/Scenes/ThreeDoorsPlayable.unity";
        private const string CueTypeName =
            "ThreeDoorsOfFate.Audio.GameSfxCue, ThreeDoorsOfFate.Audio";

        [TestCase(false)]
        [TestCase(true)]
        public void ButtonFactory_RemainsSilentWhenActionRemovesTarget(
            bool destroyTarget)
        {
            Component controller = LoadController();
            MethodInfo addSfxButton = controller.GetType().GetMethod(
                "AddSfxButton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo lastImportantFrame = controller.GetType().GetField(
                "lastImportantUiSfxFrame",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Type cueType = Type.GetType(CueTypeName);

            Assert.That(addSfxButton, Is.Not.Null);
            Assert.That(lastImportantFrame, Is.Not.Null);
            Assert.That(cueType, Is.Not.Null);

            GameObject target = new("Silent button policy test", typeof(RectTransform));
            try
            {
                lastImportantFrame.SetValue(controller, -1);
                object importantConfirm = Enum.Parse(cueType, "ImportantConfirm");
                Button button = addSfxButton.Invoke(
                    controller,
                    new[] { target, importantConfirm }) as Button;

                Assert.That(button, Is.Not.Null);
                bool hasFeedback = target.GetComponents<Component>()
                    .Any(component => component.GetType().Name == "GameSfxButtonFeedback");
                Assert.That(hasFeedback, Is.False);
                Assert.That(target.GetComponent<AudioSource>(), Is.Null);
                if (destroyTarget)
                {
                    button.onClick.AddListener(
                        () => UnityEngine.Object.DestroyImmediate(target));
                }
                else
                {
                    button.onClick.AddListener(() => target.SetActive(false));
                }

                button.onClick.Invoke();

                Assert.That(
                    (int)lastImportantFrame.GetValue(controller),
                    Is.EqualTo(-1),
                    "UI actions must not route any sound effect.");
                Assert.That(target == null, Is.EqualTo(destroyTarget));
                if (!destroyTarget)
                {
                    Assert.That(target.activeSelf, Is.False);
                }
            }
            finally
            {
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }

        [Test]
        public void NoneCue_DoesNotCreateAudioSources()
        {
            Component controller = LoadController();
            MethodInfo playGameSfx = controller.GetType().GetMethod(
                "PlayGameSfx",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Type cueType = Type.GetType(CueTypeName);

            Assert.That(playGameSfx, Is.Not.Null);
            Assert.That(cueType, Is.Not.Null);

            int before = controller.GetComponents<AudioSource>().Length;
            object none = Enum.Parse(cueType, "None");
            playGameSfx.Invoke(controller, new[] { none });
            int after = controller.GetComponents<AudioSource>().Length;

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void GeneralButtonFeedback_DoesNotCreatePlaybackSource()
        {
            Component controller = LoadController();
            Type controllerType = controller.GetType();
            FieldInfo sourceField = controllerType.GetField(
                "selectedUiSfxSource",
                BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo clipField = controllerType.GetField(
                "selectedGeneralUiSfxClip",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo addSfxButton = controllerType.GetMethod(
                "AddSfxButton",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Type cueType = Type.GetType(CueTypeName);

            Assert.That(sourceField, Is.Not.Null);
            Assert.That(clipField, Is.Not.Null);
            Assert.That(addSfxButton, Is.Not.Null);
            Assert.That(cueType, Is.Not.Null);

            AudioClip clip = AudioClip.Create(
                "Immediate G3 test",
                256,
                1,
                48000,
                false);
            GameObject target = new(
                "Immediate general button",
                typeof(RectTransform));

            try
            {
                AudioSource existing = sourceField.GetValue(controller) as AudioSource;
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                }

                sourceField.SetValue(controller, null);
                clipField.SetValue(controller, clip);
                object general = Enum.Parse(cueType, "UiAccept");
                Button button = addSfxButton.Invoke(
                    controller,
                    new[] { target, general }) as Button;

                Assert.That(button, Is.Not.Null);
                button.onClick.Invoke();

                Assert.That(
                    sourceField.GetValue(controller),
                    Is.Null,
                    "General UI clicks must remain silent while background music stays enabled.");
            }
            finally
            {
                AudioSource created = sourceField.GetValue(controller) as AudioSource;
                if (created != null)
                {
                    UnityEngine.Object.DestroyImmediate(created);
                    sourceField.SetValue(controller, null);
                }

                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void EffectSources_AreMutedWhileMusicSourceKeepsConfiguredVolume()
        {
            Component controller = LoadController();
            Type controllerType = controller.GetType();
            MethodInfo ensureAudioSources = controllerType.GetMethod(
                "EnsureAudioSources",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(ensureAudioSources, Is.Not.Null);

            ensureAudioSources.Invoke(controller, null);

            AudioSource music = GetAudioSource(controller, "musicSource");
            Assert.That(music, Is.Not.Null);
            Assert.That(music.loop, Is.True);
            Assert.That(music.volume, Is.GreaterThan(0f));

            string[] effectSourceFields =
            {
                "impactSfxSource",
                "detailSfxSource",
                "selectedCombatSfxSource",
                "selectedUiSfxSource"
            };
            foreach (string fieldName in effectSourceFields)
            {
                AudioSource source = GetAudioSource(controller, fieldName);
                Assert.That(source, Is.Not.Null, fieldName);
                Assert.That(source.volume, Is.EqualTo(0f), fieldName);
            }
        }

        private static AudioSource GetAudioSource(Component controller, string fieldName)
        {
            FieldInfo field = controller.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return field.GetValue(controller) as AudioSource;
        }

        private static Component LoadController()
        {
            Scene scene = EditorSceneManager.OpenScene(PlayableScenePath, OpenSceneMode.Single);
            Component controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .FirstOrDefault(component =>
                    component != null && component.GetType().Name == "ThreeDoorsGameController");

            Assert.That(controller, Is.Not.Null, "Playable scene controller is missing.");
            return controller;
        }
    }
}
