using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class EndlessMutation140Tests
    {
        private const string CatalogTypeName =
            "ThreeDoorsOfFate.Game.V140.EndlessMutationCatalog, Assembly-CSharp";
        private const string RandomTypeName =
            "ThreeDoorsOfFate.Game.V140.SeededRunRandom, Assembly-CSharp";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CatalogPath =
            "Assets/Resources/GameData/V140/endless_mutations.json";

        [Test]
        public void CatalogHasSixRiskRewardPairsWithClampedValues()
        {
            object catalog = LoadCatalog();
            object[] mutations = ((IEnumerable)ReadProperty(catalog, "Mutations"))
                .Cast<object>()
                .ToArray();

            Assert.That(mutations, Has.Length.EqualTo(6));
            foreach (object mutation in mutations)
            {
                object[] risks = ((IEnumerable)ReadProperty(mutation, "Risks"))
                    .Cast<object>()
                    .ToArray();
                object[] rewards = ((IEnumerable)ReadProperty(mutation, "Rewards"))
                    .Cast<object>()
                    .ToArray();
                Assert.That(risks, Has.Length.EqualTo(1));
                Assert.That(rewards, Has.Length.EqualTo(1));
                foreach (object effect in risks.Concat(rewards))
                {
                    float minimum = Convert.ToSingle(ReadProperty(effect, "Minimum"));
                    float maximum = Convert.ToSingle(ReadProperty(effect, "Maximum"));
                    float value = Convert.ToSingle(ReadProperty(effect, "ClampedValue"));
                    Assert.That(value, Is.InRange(minimum, maximum));
                }
            }
        }

        [Test]
        public void ChoicesAreUniqueInactiveAndDeterministic()
        {
            object catalog = LoadCatalog();
            string[] active = { "abyss.compound_interest" };
            string[] first = GetChoiceIds(catalog, active, 140091);
            string[] second = GetChoiceIds(catalog, active, 140091);

            Assert.That(first, Has.Length.EqualTo(3));
            Assert.That(first, Is.Unique);
            Assert.That(first, Does.Not.Contain("abyss.compound_interest"));
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void ControllerNamedHelpersResolveAllSixTradeoffs()
        {
            Component controller = CreateController(out GameObject host);
            try
            {
                SetField(controller, "endlessModeActive", true);
                ISet<string> active = (ISet<string>)ReadField(
                    controller,
                    "activeEndlessMutationIds");
                foreach (string id in GetAllIds(LoadCatalog()))
                {
                    active.Add(id);
                }

                AssertFloat(controller, "GetEndlessEnemyAttackMultiplier", 1.15f);
                AssertFloat(controller, "GetEndlessCombatGoldMultiplier", 1.25f);
                AssertFloat(controller, "GetEndlessEnemyBlockMultiplier", 1.20f);
                AssertFloat(controller, "GetEndlessRareCardWeightMultiplier", 1.30f);
                AssertFloat(controller, "GetEndlessRestHealingMultiplier", 0.70f);
                AssertFloat(controller, "GetEndlessRemovalCostMultiplier", 0.60f);
                Assert.That(Invoke(controller, "GetEndlessDebtGainBonus"), Is.EqualTo(1));
                Assert.That(Invoke(controller, "GetEndlessDoorInsightBonus"), Is.EqualTo(1));
                AssertFloat(controller, "GetEndlessShopPriceMultiplier", 1.20f);
                Assert.That(Invoke(controller, "GetEndlessShopOfferBonus"), Is.EqualTo(1));
                Assert.That(Invoke(controller, "GetEndlessOpeningHandAdjustment"), Is.EqualTo(-1));
                Assert.That(Invoke(controller, "GetEndlessFirstTurnActionBonus"), Is.EqualTo(1));
            }
            finally
            {
                DestroyController(host, controller);
            }
        }

        [Test]
        public void MutationCanOnlyBeActivatedOnceAndStatusShowsBothSides()
        {
            bool hadLanguage = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);
            string previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            Component controller = CreateController(out GameObject host);
            try
            {
                GameLocalization.SetLanguage(GameLanguage.English);
                SetField(controller, "endlessModeActive", true);
                Assert.That(
                    Invoke(controller, "ActivateEndlessMutation", "abyss.compound_interest"),
                    Is.EqualTo(true));
                Assert.That(
                    Invoke(controller, "ActivateEndlessMutation", "abyss.compound_interest"),
                    Is.EqualTo(false));

                string status = Invoke(
                    controller,
                    "BuildActiveEndlessMutationStatusText").ToString();
                Assert.That(status, Does.Contain("Enemy Attack"));
                Assert.That(status, Does.Contain("combat Gold"));
                Assert.That(
                    ((ISet<string>)ReadField(controller, "activeEndlessMutationIds")).Count,
                    Is.EqualTo(1));
            }
            finally
            {
                DestroyController(host, controller);
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

        [Test]
        public void IntegratedAdjustmentsApplyWithoutEscapingDeclaredBounds()
        {
            Component controller = CreateController(out GameObject host);
            try
            {
                SetField(controller, "endlessModeActive", true);
                ISet<string> active = (ISet<string>)ReadField(
                    controller,
                    "activeEndlessMutationIds");
                foreach (string id in GetAllIds(LoadCatalog()))
                {
                    active.Add(id);
                }

                Assert.That(Invoke(controller, "GetRunItemAdjustedCombatGoldReward", 100, false), Is.EqualTo(125));
                Assert.That(Invoke(controller, "GetRunItemAdjustedShopPrice", 100), Is.EqualTo(120));
                Assert.That(Invoke(controller, "GetRestHealAmount"), Is.EqualTo(12));
                Assert.That(Invoke(controller, "GetDeckRemovalPrice"), Is.EqualTo(27));
                Assert.That(Invoke(controller, "GetEndlessAdjustedDebtGain", 1), Is.EqualTo(2));
            }
            finally
            {
                DestroyController(host, controller);
            }
        }

        private static string[] GetChoiceIds(object catalog, string[] active, int seed)
        {
            object random = Activator.CreateInstance(RequireType(RandomTypeName), seed);
            object choices = Invoke(catalog, "GetChoices", active, random, 3);
            return ((IEnumerable)choices)
                .Cast<object>()
                .Select(choice => ReadProperty(choice, "Id").ToString())
                .ToArray();
        }

        private static string[] GetAllIds(object catalog)
        {
            return ((IEnumerable)ReadProperty(catalog, "Mutations"))
                .Cast<object>()
                .Select(mutation => ReadProperty(mutation, "Id").ToString())
                .ToArray();
        }

        private static object LoadCatalog()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(source, Is.Not.Null);
            return InvokeStatic(RequireType(CatalogTypeName), "Load", source);
        }

        private static Component CreateController(out GameObject host)
        {
            host = new GameObject("Endless Mutation 1.4 Test Host");
            return host.AddComponent(RequireType(ControllerTypeName));
        }

        private static void DestroyController(GameObject host, Component controller)
        {
            RectTransform canvasRoot = controller == null
                ? null
                : ReadField(controller, "canvasRoot") as RectTransform;
            if (canvasRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(host);
        }

        private static void AssertFloat(
            object controller,
            string methodName,
            float expected)
        {
            Assert.That(
                Convert.ToSingle(Invoke(controller, methodName)),
                Is.EqualTo(expected).Within(0.001f));
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

        private static object ReadField(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            return field.GetValue(instance);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            FieldInfo field = instance.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
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
