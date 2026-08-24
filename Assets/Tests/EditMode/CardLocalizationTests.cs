using System;
using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class CardLocalizationTests
    {
        private string previousLanguage = string.Empty;
        private bool hadPreviousLanguage;

        [SetUp]
        public void SetUp()
        {
            hadPreviousLanguage = PlayerPrefs.HasKey(
                GameLanguagePolicy.PreferenceKey);
            previousLanguage = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
            GameLocalization.Initialize(SystemLanguage.Korean);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadPreviousLanguage)
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

        [Test]
        public void Catalog_HasAll72CardsAndSwitchesTextWithoutChangingFallback()
        {
            const string cardId = "card_absorb_curse";
            const string koreanName = "빚 흡수";
            const string koreanRules = "빚 1 감소. 행동력 1 획득.";

            Assert.That(CardLocalization.EntryCount, Is.EqualTo(72));
            Assert.That(
                CardLocalization.GetName(cardId, koreanName),
                Is.EqualTo(koreanName));
            Assert.That(
                CardLocalization.GetRules(cardId, koreanRules),
                Is.EqualTo(koreanRules));

            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(
                CardLocalization.GetName(cardId, koreanName),
                Is.EqualTo("Absorb Debt"));
            Assert.That(
                CardLocalization.GetRules(cardId, koreanRules),
                Is.EqualTo("Reduce Debt by 1. Gain 1 Action."));

            GameLocalization.SetLanguage(GameLanguage.Korean);
            Assert.That(
                CardLocalization.GetName(cardId, koreanName),
                Is.EqualTo(koreanName));
            Assert.That(
                CardLocalization.GetRules(cardId, koreanRules),
                Is.EqualTo(koreanRules));
        }

        [Test]
        public void RegisteredKoreanName_TranslatesOnlyInEnglish()
        {
            const string cardId = "card_absorb_curse";
            const string koreanName = "빚 흡수";
            CardLocalization.RegisterKoreanSource(cardId, koreanName);

            Assert.That(
                CardLocalization.TryTranslateKoreanSource(
                    koreanName,
                    out string koreanResult),
                Is.False);
            Assert.That(koreanResult, Is.EqualTo(koreanName));

            GameLocalization.SetLanguage(GameLanguage.English);
            Assert.That(
                CardLocalization.TryTranslateKoreanSource(
                    koreanName,
                    out string englishResult),
                Is.True);
            Assert.That(englishResult, Is.EqualTo("Absorb Debt"));
        }

        [Test]
        public void CapturedCardNames_TranslateInsideFormattedAndCompositeText()
        {
            CardLocalization.RegisterKoreanSource(
                "card_worn_dagger",
                "낡은 단검");
            CardLocalization.RegisterKoreanSource(
                "card_worn_shield",
                "낡은 방패");
            GameLocalization.SetLanguage(GameLanguage.English);

            Assert.That(
                GameLocalization.TextFromSource(
                    "보물: 금화 20, 낡은 단검."),
                Is.EqualTo("Treasure: 20 Gold, Worn Dagger."));
            Assert.That(
                GameLocalization.TextFromSource(
                    "공격 1  낡은 단검 x2"),
                Is.EqualTo("Attack 1  Worn Dagger x2"));
            Assert.That(
                GameLocalization.TextFromSource(
                    "공격 1  낡은 단검 x2\n방어 1  낡은 방패"),
                Is.EqualTo(
                    "Attack 1  Worn Dagger x2\nDefense 1  Worn Shield"));
            Assert.That(
                GameLocalization.TextFromSource(
                    "빌드 완성까지 낡은 단검, 낡은 방패 필요."),
                Is.EqualTo(
                    "Need Worn Dagger, Worn Shield to complete the build."));
        }

        [Test]
        public void ImportedEnglishCardAssets_ResolveAll72WithExpectedSettings()
        {
            TextAsset manifestAsset = Resources.Load<TextAsset>(
                "Localization/english_cards");
            Assert.That(manifestAsset, Is.Not.Null);
            TestManifest manifest = JsonUtility.FromJson<TestManifest>(
                manifestAsset.text);
            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.cards, Has.Length.EqualTo(72));

            GameLocalization.SetLanguage(GameLanguage.English);
            Sprite[] sprites = Resources.LoadAll<Sprite>("Cards/EnglishLocalized");
            Assert.That(sprites, Has.Length.EqualTo(72));

            foreach (TestManifestEntry entry in manifest.cards)
            {
                Assert.That(entry.card_id, Is.Not.Empty);
                Assert.That(entry.english_display_name, Is.Not.Empty);
                Assert.That(entry.english_rules_text, Is.Not.Empty);
                Assert.That(
                    CardLocalization.GetName(entry.card_id, string.Empty),
                    Is.EqualTo(entry.english_display_name),
                    entry.card_id);
                Assert.That(
                    CardLocalization.GetRules(entry.card_id, string.Empty),
                    Is.EqualTo(entry.english_rules_text),
                    entry.card_id);
                Assert.That(
                    CardLocalization.GetFullCardSprite(entry.card_id, null),
                    Is.Not.Null,
                    entry.card_id);
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:Texture2D",
                new[] { "Assets/Resources/Cards/EnglishLocalized" });
            Assert.That(guids, Has.Length.EqualTo(72));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path)
                    as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(
                    importer.textureType,
                    Is.EqualTo(TextureImporterType.Sprite),
                    path);
                Assert.That(
                    importer.spriteImportMode,
                    Is.EqualTo(SpriteImportMode.Single),
                    path);
                Assert.That(importer.maxTextureSize, Is.EqualTo(2048), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(
                    importer.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed),
                    path);

                TextureImporterSettings settings = new();
                importer.ReadTextureSettings(settings);
                Assert.That(
                    settings.spriteMeshType,
                    Is.EqualTo(SpriteMeshType.FullRect),
                    path);

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(987), path);
                Assert.That(texture.height, Is.EqualTo(1495), path);
            }
        }

        [Serializable]
        private sealed class TestManifest
        {
            public TestManifestEntry[] cards = Array.Empty<TestManifestEntry>();
        }

        [Serializable]
        private sealed class TestManifestEntry
        {
            public string card_id = string.Empty;
            public string english_display_name = string.Empty;
            public string english_rules_text = string.Empty;
        }
    }
}
