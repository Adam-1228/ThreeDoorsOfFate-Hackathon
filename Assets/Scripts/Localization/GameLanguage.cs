using UnityEngine;

namespace ThreeDoorsOfFate.Localization
{
    public enum GameLanguage
    {
        Korean,
        English
    }

    public static class GameLanguagePolicy
    {
        public const string PreferenceKey = "ThreeDoorsOfFate.Language";
        public const string KoreanValue = "ko";
        public const string EnglishValue = "en";

        public static GameLanguage Resolve(
            string savedValue,
            SystemLanguage systemLanguage)
        {
            if (savedValue == "ko")
            {
                return GameLanguage.Korean;
            }

            if (savedValue == "en")
            {
                return GameLanguage.English;
            }

            return systemLanguage == SystemLanguage.Korean
                ? GameLanguage.Korean
                : GameLanguage.English;
        }

        public static string Serialize(GameLanguage language)
        {
            return language == GameLanguage.Korean ? KoreanValue : EnglishValue;
        }
    }
}
