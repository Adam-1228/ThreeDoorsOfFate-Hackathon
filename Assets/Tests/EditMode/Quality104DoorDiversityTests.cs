using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class Quality104DoorDiversityTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        private Type controllerType;
        private GameObject controllerHost;
        private Component controller;
        private RectTransform canvasRoot;
        private EventSystem originalEventSystem;
        private UnityEngine.Random.State previousRandomState;

        [SetUp]
        public void SetUp()
        {
            previousRandomState = UnityEngine.Random.state;
            originalEventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);
            controllerHost = new GameObject("Quality 1.0.4 Door Diversity Test Host");
            controller = controllerHost.AddComponent(controllerType);
            canvasRoot = GetField<RectTransform>("canvasRoot");

            SetField("roomsCleared", 0);
            SetField("combatEncountersCompleted", 3);
            SetField("consecutiveNonCombatDoors", 0);
            SetField("debt", 0);
            SetField("endlessModeActive", false);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Random.state = previousRandomState;
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
        }

        [Test]
        public void NormalGeneration_UsesThreeDistinctTypesAndAtLeastOneSafeChoice()
        {
            List<string> violations = new();
            for (int seed = 0; seed < 256; seed += 1)
            {
                UnityEngine.Random.InitState(seed);
                string[] types = GenerateTypes();
                bool distinct = types.Distinct().Count() == 3;
                bool hasSafe = types.Any(IsSafeChoice);
                if (!distinct || !hasSafe)
                {
                    violations.Add(
                        $"seed {seed}: {string.Join(",", types)} "
                        + $"distinct={distinct} safe={hasSafe}");
                }
            }

            Assert.That(
                violations,
                Is.Empty,
                string.Join("\n", violations.Take(12)));
        }

        [Test]
        public void ForcedProgression_RemainsCombatForcingAndCanReachBossEligibility()
        {
            SetField("roomsCleared", 0);
            SetField("combatEncountersCompleted", 0);
            SetField("consecutiveNonCombatDoors", 3);
            Assert.That(Invoke<bool>("ShouldForceCombatDoorOptions"), Is.True);

            string[] forced = GenerateTypes();
            Assert.That(forced, Has.Length.EqualTo(3));
            Assert.That(forced.All(IsCombatForcing), Is.True);

            SetField("roomsCleared", 10);
            SetField("combatEncountersCompleted", 3);
            SetField("consecutiveNonCombatDoors", 0);
            Assert.That(Invoke<bool>("IsBossDoorReady"), Is.True);
        }

        private string[] GenerateTypes()
        {
            IList options = Invoke<IList>("GenerateDoorOptions");
            return options.Cast<object>()
                .Select(option => option.GetType()
                    .GetProperty("Type")
                    .GetValue(option)
                    .ToString())
                .ToArray();
        }

        private static bool IsSafeChoice(string type)
        {
            return type is "Shop" or "Treasure" or "Event" or "Rest";
        }

        private static bool IsCombatForcing(string type)
        {
            return type is "Battle" or "Elite" or "Curse";
        }

        private T Invoke<T>(string methodName)
        {
            MethodInfo method = controllerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}'.");
            return (T)method.Invoke(controller, Array.Empty<object>());
        }

        private T GetField<T>(string fieldName)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            return (T)field.GetValue(controller);
        }

        private void SetField<T>(string fieldName, T value)
        {
            FieldInfo field = controllerType.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            field.SetValue(controller, value);
        }
    }
}
