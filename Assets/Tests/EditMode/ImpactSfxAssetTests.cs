using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class ImpactSfxAssetTests
    {
        private static readonly string[] RequiredClipPaths =
        {
            "Assets/Audio/SFX/Impact/impact_attack_01.wav",
            "Assets/Audio/SFX/Impact/impact_attack_02.wav",
            "Assets/Audio/SFX/Impact/impact_attack_03.wav",
            "Assets/Audio/SFX/Impact/impact_critical.wav",
            "Assets/Audio/SFX/Impact/impact_defense_01.wav",
            "Assets/Audio/SFX/Impact/impact_defense_02.wav",
            "Assets/Audio/SFX/Impact/impact_blocked_01.wav",
            "Assets/Audio/SFX/Impact/impact_blocked_02.wav",
            "Assets/Audio/SFX/Impact/detail_plate_settle.wav",
            "Assets/Audio/SFX/Impact/detail_prophecy.wav",
            "Assets/Audio/SFX/Impact/detail_trait.wav",
            "Assets/Audio/SFX/Impact/detail_combo.wav",
            "Assets/Audio/SFX/Impact/detail_curse.wav",
            "Assets/Audio/SFX/Impact/impact_boss_start.wav",
            "Assets/Audio/SFX/Impact/impact_boss_victory.wav"
        };

        [TestCaseSource(nameof(RequiredClipPaths))]
        public void RequiredImpactClip_IsImportedAtGameReadyFormat(string assetPath)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);

            Assert.That(clip, Is.Not.Null, $"Missing AudioClip: {assetPath}");
            Assert.That(clip.frequency, Is.EqualTo(48000), assetPath);
            Assert.That(clip.length, Is.GreaterThan(0.04f).And.LessThan(4.0f), assetPath);
        }
    }
}
