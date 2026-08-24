using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class GameSfxAssetTests
    {
        private static readonly string[] RequiredClipPaths =
        {
            "Assets/Audio/SFX/UI/ui_accept_01.wav",
            "Assets/Audio/SFX/UI/ui_accept_02.wav",
            "Assets/Audio/SFX/UI/ui_accept_03.wav",
            "Assets/Audio/SFX/UI/ui_back.wav",
            "Assets/Audio/SFX/UI/ui_denied.wav",
            "Assets/Audio/SFX/UI/panel_open.wav",
            "Assets/Audio/SFX/UI/panel_close.wav",
            "Assets/Audio/SFX/Cards/card_draw_01.wav",
            "Assets/Audio/SFX/Cards/card_draw_02.wav",
            "Assets/Audio/SFX/Cards/card_play_01.wav",
            "Assets/Audio/SFX/Cards/card_play_02.wav",
            "Assets/Audio/SFX/Cards/card_discard.wav",
            "Assets/Audio/SFX/World/run_start.wav",
            "Assets/Audio/SFX/World/door_open.wav",
            "Assets/Audio/SFX/World/turn_commit.wav",
            "Assets/Audio/SFX/World/dice_roll.wav",
            "Assets/Audio/SFX/World/player_hit.wav",
            "Assets/Audio/SFX/World/heal.wav",
            "Assets/Audio/SFX/World/combat_start.wav",
            "Assets/Audio/SFX/World/enemy_defeat.wav",
            "Assets/Audio/SFX/World/treasure_open.wav",
            "Assets/Audio/SFX/World/event_choice.wav",
            "Assets/Audio/SFX/World/rest.wav",
            "Assets/Audio/SFX/World/curse_accept.wav",
            "Assets/Audio/SFX/World/defeat.wav",
            "Assets/Audio/SFX/World/victory.wav",
            "Assets/Audio/SFX/World/ending.wav",
            "Assets/Audio/SFX/Rewards/reward_reveal.wav",
            "Assets/Audio/SFX/Rewards/reward_claim.wav",
            "Assets/Audio/SFX/Rewards/gold_gain.wav",
            "Assets/Audio/SFX/Rewards/purchase.wav",
            "Assets/Audio/SFX/Rewards/upgrade.wav",
            "Assets/Audio/SFX/Rewards/item_equip.wav",
            "Assets/Audio/SFX/Rewards/save_success.wav",
            "Assets/Audio/SFX/Rewards/save_failure.wav",
            "Assets/Audio/SFX/Rewards/load_success.wav",
            "Assets/Audio/SFX/Rewards/load_failure.wav"
        };

        [TestCaseSource(nameof(RequiredClipPaths))]
        public void RequiredGameClip_IsAudibleAndMobileReady(string assetPath)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;

            Assert.That(clip, Is.Not.Null, $"Missing AudioClip: {assetPath}");
            Assert.That(importer, Is.Not.Null, $"Missing AudioImporter: {assetPath}");
            Assert.That(clip.frequency, Is.EqualTo(48000), assetPath);
            Assert.That(clip.channels, Is.EqualTo(1), assetPath);
            Assert.That(clip.length, Is.GreaterThan(0.04f).And.LessThan(3.5f), assetPath);
            Assert.That(importer.forceToMono, Is.True, assetPath);
            Assert.That(importer.loadInBackground, Is.False, assetPath);

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Assert.That(settings.loadType, Is.EqualTo(AudioClipLoadType.DecompressOnLoad), assetPath);
            Assert.That(settings.compressionFormat, Is.EqualTo(AudioCompressionFormat.ADPCM), assetPath);
            Assert.That(settings.preloadAudioData, Is.True, assetPath);

            float[] samples = new float[clip.samples];
            Assert.That(clip.GetData(samples, 0), Is.True, assetPath);
            float peak = 0f;
            for (int i = 0; i < samples.Length; i += 1)
            {
                peak = Mathf.Max(peak, Mathf.Abs(samples[i]));
            }

            Assert.That(peak, Is.GreaterThan(0.02f), $"Silent or inaudible clip: {assetPath}");
        }
    }
}
