using NUnit.Framework;
using ThreeDoorsOfFate.Localization;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class GameLocalizationTests
    {
        [TestCase("ko", SystemLanguage.English, GameLanguage.Korean)]
        [TestCase("en", SystemLanguage.Korean, GameLanguage.English)]
        [TestCase("", SystemLanguage.Korean, GameLanguage.Korean)]
        [TestCase("", SystemLanguage.English, GameLanguage.English)]
        [TestCase("unexpected", SystemLanguage.French, GameLanguage.English)]
        public void Resolve_UsesSavedChoiceThenKoreanOnlySystemDefault(
            string savedValue,
            SystemLanguage systemLanguage,
            GameLanguage expected)
        {
            Assert.That(
                GameLanguagePolicy.Resolve(savedValue, systemLanguage),
                Is.EqualTo(expected));
        }

        [Test]
        public void Catalog_ResolvesBothLanguages()
        {
            string previous = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            bool hadPrevious = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);

            try
            {
                PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
                GameLocalization.Initialize(SystemLanguage.Korean);
                Assert.That(GameLocalization.Text("menu.settings"), Is.EqualTo("설정"));

                GameLocalization.SetLanguage(GameLanguage.English);
                Assert.That(GameLocalization.Text("menu.settings"), Is.EqualTo("Settings"));
            }
            finally
            {
                if (hadPrevious)
                {
                    PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, previous);
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
        public void FormattedSources_TranslateCommaSeparatedCapturedValues()
        {
            string previous = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            bool hadPrevious = PlayerPrefs.HasKey(GameLanguagePolicy.PreferenceKey);

            try
            {
                GameLocalization.SetLanguage(GameLanguage.English);

                Assert.That(
                    GameLocalization.TextFromSource(
                        "동굴 잠복자 카드 후보: 그림자 일격, 어둠 속 방어 -> 그림자 일격."),
                    Is.EqualTo(
                        "Cave Lurker card candidates: Shadow Strike, Defense in the Dark → Shadow Strike."));
            }
            finally
            {
                if (hadPrevious)
                {
                    PlayerPrefs.SetString(GameLanguagePolicy.PreferenceKey, previous);
                }
                else
                {
                    PlayerPrefs.DeleteKey(GameLanguagePolicy.PreferenceKey);
                }

                PlayerPrefs.Save();
                GameLocalization.Initialize(Application.systemLanguage);
            }
        }
    }
}
