using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class ApplePortabilityPolicyTests
    {
        private const string ControllerTypeName =
            "ThreeDoorsOfFate.Game.ThreeDoorsGameController, Assembly-CSharp";

        [TestCase(RuntimePlatform.IPhonePlayer, false)]
        [TestCase(RuntimePlatform.Android, false)]
        [TestCase(RuntimePlatform.OSXPlayer, true)]
        [TestCase(RuntimePlatform.WindowsPlayer, true)]
        public void SupportsDesktopWindowControls_MatchesPlatformCapabilities(
            RuntimePlatform platform,
            bool expected)
        {
            Type controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);

            MethodInfo method = controllerType.GetMethod(
                "SupportsDesktopWindowControls",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            bool actual = (bool)method.Invoke(null, new object[] { platform });
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Controller_HasApplicationPauseSaveHook()
        {
            Type controllerType = Type.GetType(ControllerTypeName);
            Assert.That(controllerType, Is.Not.Null);

            MethodInfo method = controllerType.GetMethod(
                "OnApplicationPause",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);
        }
    }
}
