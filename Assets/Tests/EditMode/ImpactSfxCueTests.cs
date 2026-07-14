using System;
using System.Reflection;
using NUnit.Framework;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class ImpactSfxCueTests
    {
        private const string ResolverTypeName =
            "ThreeDoorsOfFate.Audio.ImpactSfxCueResolver, ThreeDoorsOfFate.Audio";

        [TestCase("공격 성공", "Attack")]
        [TestCase("치명타 성공", "Critical")]
        [TestCase("방어 성공", "Defense")]
        [TestCase("방어에 막힘", "Blocked")]
        [TestCase("예언 성공", "Prophecy")]
        [TestCase("특성 발현", "Trait")]
        [TestCase("조합 발현", "Combo")]
        [TestCase("계약 발현", "Curse")]
        [TestCase("", "None")]
        [TestCase("알 수 없는 표시", "None")]
        public void FromFeedbackMessage_MapsKnownKoreanFeedback(string message, string expectedCueName)
        {
            Type resolverType = Type.GetType(ResolverTypeName);
            Assert.That(resolverType, Is.Not.Null, "ImpactSfxCueResolver must exist.");

            MethodInfo method = resolverType.GetMethod(
                "FromFeedbackMessage",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "FromFeedbackMessage must be public and static.");

            object cue = method.Invoke(null, new object[] { message });
            Assert.That(cue.ToString(), Is.EqualTo(expectedCueName));
        }

        [Test]
        public void UsesPlateLayer_RejectsNoneAndAcceptsCombatFeedback()
        {
            Type resolverType = Type.GetType(ResolverTypeName);
            Assert.That(resolverType, Is.Not.Null, "ImpactSfxCueResolver must exist.");

            MethodInfo fromMessage = resolverType.GetMethod(
                "FromFeedbackMessage",
                BindingFlags.Public | BindingFlags.Static);
            MethodInfo usesPlate = resolverType.GetMethod(
                "UsesPlateLayer",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(fromMessage, Is.Not.Null);
            Assert.That(usesPlate, Is.Not.Null);

            object noneCue = fromMessage.Invoke(null, new object[] { string.Empty });
            object attackCue = fromMessage.Invoke(null, new object[] { "공격 성공" });

            Assert.That((bool)usesPlate.Invoke(null, new[] { noneCue }), Is.False);
            Assert.That((bool)usesPlate.Invoke(null, new[] { attackCue }), Is.True);
        }
    }
}
