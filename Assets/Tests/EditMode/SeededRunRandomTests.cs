using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class SeededRunRandomTests
    {
        private const string RandomTypeName =
            "ThreeDoorsOfFate.Game.V140.SeededRunRandom, Assembly-CSharp";
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        [Test]
        public void SameSeedProducesSameSequence()
        {
            object first = CreateRandom(14000);
            object second = CreateRandom(14000);

            Assert.That(NextTen(first), Is.EqualTo(NextTen(second)));
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            object first = CreateRandom(14000);
            object second = CreateRandom(14001);

            Assert.That(NextTen(first), Is.Not.EqualTo(NextTen(second)));
        }

        [Test]
        public void SnapshotResumesAtExactCursor()
        {
            object random = CreateRandom(77);
            InvokeRange(random, 0, 100);
            InvokeRange(random, 0, 100);
            object snapshot = Invoke(random, "Capture");
            int expected = InvokeRange(random, 0, 100);

            object resumed = CreateRandom(snapshot);

            Assert.That(InvokeRange(resumed, 0, 100), Is.EqualTo(expected));
            Assert.That(ReadMember<int>(snapshot, "Seed"), Is.EqualTo(77));
            Assert.That(ReadMember<int>(snapshot, "Cursor"), Is.EqualTo(2));
            Assert.That(ReadMember<uint>(snapshot, "State"), Is.Not.EqualTo(0u));
        }

        [Test]
        public void ZeroSeedStillProducesChangingBoundedValues()
        {
            object random = CreateRandom(0);
            List<int> values = NextTen(random);

            Assert.That(values, Has.All.InRange(3, 16));
            Assert.That(new HashSet<int>(values).Count, Is.GreaterThan(1));
        }

        [Test]
        public void RangeRejectsAnEmptyInterval()
        {
            object random = CreateRandom(9);
            TargetInvocationException error = Assert.Throws<TargetInvocationException>(
                () => InvokeRange(random, 5, 5));

            Assert.That(error.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ValueIsDeterministicAndInsideTheHalfOpenUnitInterval()
        {
            object first = CreateRandom(901);
            object second = CreateRandom(901);

            for (int index = 0; index < 20; index += 1)
            {
                float left = (float)Invoke(first, "Value");
                float right = (float)Invoke(second, "Value");
                Assert.That(left, Is.EqualTo(right));
                Assert.That(left, Is.GreaterThanOrEqualTo(0f));
                Assert.That(left, Is.LessThan(1f));
            }
        }

        [Test]
        public void ControllerBridgeCanRestartTheSameGameplaySequence()
        {
            Type controllerType = RequireType(ControllerTypeName);
            GameObject host = new("Seeded RNG Controller Test Host");
            Component controller = null;
            try
            {
                controller = host.AddComponent(controllerType);
                Invoke(controller, "ResetRunRandom", 314159);
                List<int> first = NextControllerValues(controller);
                Invoke(controller, "ResetRunRandom", 314159);
                List<int> second = NextControllerValues(controller);

                Assert.That(second, Is.EqualTo(first));
            }
            finally
            {
                if (controller != null)
                {
                    FieldInfo canvasRootField = controllerType.GetField(
                        "canvasRoot",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    RectTransform canvasRoot = canvasRootField?.GetValue(controller) as RectTransform;
                    if (canvasRoot != null)
                    {
                        UnityEngine.Object.DestroyImmediate(canvasRoot.gameObject);
                    }
                }

                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static object CreateRandom(int seed)
        {
            return Activator.CreateInstance(RequireType(RandomTypeName), seed);
        }

        private static object CreateRandom(object snapshot)
        {
            return Activator.CreateInstance(RequireType(RandomTypeName), snapshot);
        }

        private static List<int> NextTen(object random)
        {
            List<int> values = new(10);
            for (int index = 0; index < 10; index += 1)
            {
                values.Add(InvokeRange(random, 3, 17));
            }

            return values;
        }

        private static List<int> NextControllerValues(Component controller)
        {
            List<int> values = new(8);
            for (int index = 0; index < 8; index += 1)
            {
                values.Add((int)Invoke(controller, "RunRange", 2, 91));
            }

            return values;
        }

        private static int InvokeRange(object instance, int minimum, int maximum)
        {
            return (int)Invoke(instance, "Range", minimum, maximum);
        }

        private static object Invoke(object instance, string methodName, params object[] arguments)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null, $"Missing method: {methodName}");
            return method.Invoke(instance, arguments);
        }

        private static T ReadMember<T>(object instance, string name)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                return (T)property.GetValue(instance);
            }

            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"Missing member: {name}");
            return (T)field.GetValue(instance);
        }

        private static Type RequireType(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.That(type, Is.Not.Null, $"Missing type: {typeName}");
            return type;
        }
    }
}
