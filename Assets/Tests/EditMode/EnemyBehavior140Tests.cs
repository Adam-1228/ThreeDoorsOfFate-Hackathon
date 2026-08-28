using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class EnemyBehavior140Tests
    {
        private const string CatalogTypeName =
            "ThreeDoorsOfFate.Game.V140.EnemyBehaviorCatalog, Assembly-CSharp";
        private const string DirectorTypeName =
            "ThreeDoorsOfFate.Game.V140.EncounterDirector, Assembly-CSharp";
        private const string HistoryTypeName =
            "ThreeDoorsOfFate.Game.V140.EnemyIntentHistory, Assembly-CSharp";
        private const string StateTypeName =
            "ThreeDoorsOfFate.Game.V140.EnemySelectionState, Assembly-CSharp";
        private const string DifficultyTypeName =
            "ThreeDoorsOfFate.Game.V140.EncounterDifficulty, Assembly-CSharp";
        private const string RandomTypeName =
            "ThreeDoorsOfFate.Game.V140.SeededRunRandom, Assembly-CSharp";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CatalogPath =
            "Assets/Resources/GameData/V140/enemy_behaviors.json";

        [Test]
        public void EveryStandardEnemyHasAtLeastTwoUniqueActions()
        {
            object catalog = LoadCatalog();
            object[] profiles = ((IEnumerable)ReadProperty(catalog, "Profiles"))
                .Cast<object>()
                .Where(profile => !ReadProperty(profile, "EnemyId")
                    .ToString()
                    .StartsWith("boss_", StringComparison.Ordinal))
                .ToArray();

            Assert.That(profiles, Has.Length.EqualTo(20));
            foreach (object profile in profiles)
            {
                object[] unique = ((IEnumerable)ReadProperty(profile, "Actions"))
                    .Cast<object>()
                    .Where(action => (bool)ReadProperty(action, "Unique"))
                    .ToArray();
                Assert.That(
                    unique,
                    Has.Length.GreaterThanOrEqualTo(2),
                    ReadProperty(profile, "EnemyId").ToString());
            }
        }

        [TestCase("Easy")]
        [TestCase("Normal")]
        public void NonHardDifficultyNeverChoosesTheSameActionThreeTimes(
            string difficultyName)
        {
            object profile = Invoke(LoadCatalog(), "Get", "monster_cave_lurker");
            object history = Activator.CreateInstance(RequireType(HistoryTypeName));
            Invoke(history, "Record", "lurker.ambush");
            Invoke(history, "Record", "lurker.ambush");
            object selected = InvokeStatic(
                RequireType(DirectorTypeName),
                "SelectAction",
                profile,
                0,
                Enum.Parse(RequireType(DifficultyTypeName), difficultyName),
                CreateState(),
                Activator.CreateInstance(RequireType(RandomTypeName), 140081),
                history);

            Assert.That(
                ReadProperty(selected, "Id"),
                Is.Not.EqualTo("lurker.ambush"));
        }

        [Test]
        public void BossPhaseThresholdsAreSeventyAndThirtyFivePercent()
        {
            Type director = RequireType(DirectorTypeName);
            Assert.That(InvokeStatic(director, "GetBossPhaseIndex", 71, 100), Is.EqualTo(0));
            Assert.That(InvokeStatic(director, "GetBossPhaseIndex", 70, 100), Is.EqualTo(1));
            Assert.That(InvokeStatic(director, "GetBossPhaseIndex", 36, 100), Is.EqualTo(1));
            Assert.That(InvokeStatic(director, "GetBossPhaseIndex", 35, 100), Is.EqualTo(2));
        }

        [Test]
        public void EliteAffixSelectionIsSeededAndFromTheAllowlist()
        {
            Type director = RequireType(DirectorTypeName);
            object first = InvokeStatic(
                director,
                "SelectEliteAffix",
                Activator.CreateInstance(RequireType(RandomTypeName), 90210));
            object second = InvokeStatic(
                director,
                "SelectEliteAffix",
                Activator.CreateInstance(RequireType(RandomTypeName), 90210));

            Assert.That(first.ToString(), Is.EqualTo(second.ToString()));
            Assert.That(
                new[] { "Usury", "Seal", "Frenzy" },
                Does.Contain(first.ToString()));
        }

        [Test]
        public void EveryBossHasThreeOrderedPhasePools()
        {
            object catalog = LoadCatalog();
            object[] bosses = ((IEnumerable)ReadProperty(catalog, "Profiles"))
                .Cast<object>()
                .Where(profile => ReadProperty(profile, "EnemyId")
                    .ToString()
                    .StartsWith("boss_", StringComparison.Ordinal))
                .ToArray();
            Assert.That(bosses, Has.Length.EqualTo(4));
            foreach (object boss in bosses)
            {
                object[] phases = ((IEnumerable)ReadProperty(boss, "Phases"))
                    .Cast<object>()
                    .ToArray();
                Assert.That(phases, Has.Length.EqualTo(3));
                Assert.That(
                    phases.Select(phase => (int)ReadProperty(phase, "MaximumHealthPercent")),
                    Is.EqualTo(new[] { 100, 70, 35 }));
            }
        }

        [Test]
        public void ControllerUsesProfileAndAnnouncesBossPhaseWithoutTransitionDamage()
        {
            bool hadLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            string previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            Type controllerType = RequireType(ControllerTypeName);
            GameObject host = new("Enemy Behavior 1.4 Controller Test");
            EventSystem originalEventSystem =
                UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            Component controller = null;
            try
            {
                controller = host.AddComponent(controllerType);
                GameLocalization.SetLanguage(GameLanguage.English);
                SetControllerField(controller, "playerHealth", 60);
                SetControllerField(controller, "playerMaxHealth", 60);
                SetControllerField(controller, "playerBlock", 0);
                SetControllerField(controller, "luck", 3);
                SetControllerEnum(controller, "currentDifficulty", "Normal");
                Invoke(controller, "ResetRunRandom", 140088);
                object boss = Invoke(
                    controller,
                    "CreateScaledEnemyState",
                    "boss_debt_adjudicator_normal",
                    "Debt Adjudicator",
                    100,
                    14,
                    8,
                    true,
                    true,
                    0);
                int maximumHealth = (int)ReadProperty(boss, "MaxHealth");
                SetMember(boss, "Health", Mathf.FloorToInt(maximumHealth * 0.70f));
                SetControllerField(controller, "enemy", boss);
                int healthBefore = (int)ReadControllerField(controller, "playerHealth");

                Invoke(controller, "PrepareEnemyIntent");

                Assert.That(
                    ReadControllerField(controller, "currentBossBehaviorPhase"),
                    Is.EqualTo(1));
                Assert.That(
                    ReadProperty(boss, "IntentCardName").ToString(),
                    Does.Contain("Phase 2"));
                Assert.That(
                    ReadControllerField(controller, "playerHealth"),
                    Is.EqualTo(healthBefore));
                object history = ReadControllerField(controller, "enemyIntentHistory");
                Assert.That(
                    ((IEnumerable)ReadProperty(history, "RecentActionIds"))
                        .Cast<object>()
                        .Count(),
                    Is.EqualTo(1));
            }
            finally
            {
                RectTransform canvasRoot = controller == null
                    ? null
                    : ReadControllerField(controller, "canvasRoot") as RectTransform;
                if (canvasRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(host);
                if (originalEventSystem == null)
                {
                    EventSystem created =
                        UnityEngine.Object.FindAnyObjectByType<EventSystem>();
                    if (created != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created.gameObject);
                    }
                }

                if (hadLanguage)
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
        }

        private static object CreateState()
        {
            object state = Activator.CreateInstance(RequireType(StateTypeName));
            SetMember(state, "EnemyHealth", 40);
            SetMember(state, "EnemyMaxHealth", 60);
            SetMember(state, "EnemyBlock", 0);
            SetMember(state, "EnemyBaseAttack", 10);
            SetMember(state, "EnemyBaseBlock", 6);
            SetMember(state, "PlayerHealth", 35);
            SetMember(state, "PlayerBlock", 4);
            SetMember(state, "PlayerDebt", 2);
            return state;
        }

        private static object LoadCatalog()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(source, Is.Not.Null);
            return InvokeStatic(RequireType(CatalogTypeName), "Load", source);
        }

        private static object Invoke(object instance, string methodName, params object[] values)
        {
            MethodInfo method = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == values.Length);
            return method.Invoke(instance, values);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] values)
        {
            MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == methodName
                    && candidate.GetParameters().Length == values.Length);
            return method.Invoke(null, values);
        }

        private static object ReadProperty(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, propertyName);
            return property.GetValue(instance);
        }

        private static void SetMember(object instance, string name, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                property.SetValue(instance, value);
                return;
            }

            FieldInfo field = instance.GetType().GetField(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(instance, value);
        }

        private static object ReadControllerField(object controller, string name)
        {
            FieldInfo field = controller.GetType().GetField(
                name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, name);
            return field.GetValue(controller);
        }

        private static void SetControllerField(object controller, string name, object value)
        {
            FieldInfo field = controller.GetType().GetField(
                name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(controller, value);
        }

        private static void SetControllerEnum(object controller, string name, string value)
        {
            FieldInfo field = controller.GetType().GetField(
                name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(controller, Enum.Parse(field.FieldType, value));
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, typeName);
            return type;
        }
    }
}
