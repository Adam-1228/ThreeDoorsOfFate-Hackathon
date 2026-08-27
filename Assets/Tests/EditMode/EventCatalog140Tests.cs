using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class EventCatalog140Tests
    {
        private const string CatalogTypeName =
            "ThreeDoorsOfFate.Game.V140.EventCatalog, Assembly-CSharp";
        private const string CharacterClassTypeName =
            "ThreeDoorsOfFate.Cards.CharacterClass, Assembly-CSharp";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";
        private const string CatalogPath =
            "Assets/Resources/GameData/V140/events.json";

        [Test]
        public void DebtEventRequiresFourDebtAndEventsDoNotRepeat()
        {
            object catalog = LoadCatalog();
            object debtEvent = Invoke(catalog, "Get", "event.compound_broker");
            object gambler = Enum.Parse(RequireType(CharacterClassTypeName), "Gambler");

            Assert.That(IsEligible(debtEvent, gambler, 3, Array.Empty<string>()), Is.False);
            Assert.That(IsEligible(debtEvent, gambler, 4, Array.Empty<string>()), Is.True);
            Assert.That(
                IsEligible(
                    debtEvent,
                    gambler,
                    4,
                    new[] { "event.compound_broker" }),
                Is.False);
        }

        [TestCase("event.gambler_last_table", "Gambler", "Oracle")]
        [TestCase("event.oracle_blind_star", "Oracle", "Exile")]
        [TestCase("event.exile_broken_chain", "Exile", "Gambler")]
        public void ClassEventsOnlyAppearForTheirOwner(
            string eventId,
            string ownerName,
            string otherName)
        {
            object definition = Invoke(LoadCatalog(), "Get", eventId);
            Type classType = RequireType(CharacterClassTypeName);
            Assert.That(
                IsEligible(
                    definition,
                    Enum.Parse(classType, ownerName),
                    0,
                    Array.Empty<string>()),
                Is.True);
            Assert.That(
                IsEligible(
                    definition,
                    Enum.Parse(classType, otherName),
                    0,
                    Array.Empty<string>()),
                Is.False);
        }

        [Test]
        public void CatalogContainsEightEventsAndOnlyAllowlistedEffects()
        {
            object catalog = LoadCatalog();
            IEnumerable events = (IEnumerable)ReadProperty(catalog, "Events");
            object[] definitions = events.Cast<object>().ToArray();
            Assert.That(definitions, Has.Length.EqualTo(8));

            string[] allowlist =
            {
                "Health",
                "MaxHealth",
                "Gold",
                "Debt",
                "AddCard",
                "RemoveCard",
                "DoorInsight",
                "ItemDiscovery"
            };
            foreach (object definition in definitions)
            {
                IEnumerable choices = (IEnumerable)ReadProperty(definition, "Choices");
                Assert.That(choices.Cast<object>().Count(), Is.InRange(2, 3));
                foreach (object choice in choices)
                {
                    IEnumerable effects = (IEnumerable)ReadProperty(choice, "Effects");
                    foreach (string effectType in effects.Cast<object>()
                        .Select(effect => ReadProperty(effect, "Type").ToString()))
                    {
                        Assert.That(allowlist, Does.Contain(effectType));
                    }
                }
            }
        }

        [Test]
        public void UnknownEffectTypeIsRejectedDuringLoad()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(source, Is.Not.Null);
            TextAsset invalid = new(source.text.Replace(
                "\"type\": \"Health\"",
                "\"type\": \"ExecuteCode\""));
            try
            {
                TargetInvocationException error = Assert.Throws<TargetInvocationException>(
                    () => InvokeStatic(RequireType(CatalogTypeName), "Load", invalid));
                Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(invalid);
            }
        }

        [Test]
        public void ControllerSelectionDoesNotRepeatASeenEligibleEvent()
        {
            Type controllerType = RequireType(ControllerTypeName);
            GameObject host = new("Event 1.4 Selection Test");
            try
            {
                Component controller = host.AddComponent(controllerType);
                SetEnumField(controller, "selectedClass", "Gambler");
                SetField(controller, "debt", 5);
                SetField(controller, "roomsCleared", 3);
                Invoke(controller, "ResetRunRandom", 140071);

                object first = Invoke(controller, "PickRunEvent");
                string firstId = (string)ReadProperty(first, "Id");
                object seen = ReadField(controller, "seenRunEventIds");
                seen.GetType().GetMethod("Add")?.Invoke(seen, new object[] { firstId });
                object second = Invoke(controller, "PickRunEvent");

                Assert.That(ReadProperty(second, "Id"), Is.Not.EqualTo(firstId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static object LoadCatalog()
        {
            TextAsset source = AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath);
            Assert.That(source, Is.Not.Null);
            return InvokeStatic(RequireType(CatalogTypeName), "Load", source);
        }

        private static bool IsEligible(
            object definition,
            object characterClass,
            int debt,
            IEnumerable<string> seenIds)
        {
            return (bool)Invoke(
                definition,
                "IsEligible",
                characterClass,
                debt,
                seenIds.ToArray());
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

        private static void SetEnumField(object instance, string fieldName, string value)
        {
            FieldInfo field = instance.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(instance, Enum.Parse(field.FieldType, value));
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, typeName);
            return type;
        }
    }
}
