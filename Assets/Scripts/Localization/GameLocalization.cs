using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ThreeDoorsOfFate.Localization
{
    public static class GameLocalization
    {
        private const string CatalogResourcePath = "Localization/game_text";

        private static readonly Dictionary<string, CatalogEntry> Entries =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, CatalogEntry> EntriesByKoreanSource =
            new(StringComparer.Ordinal);
        private static readonly List<SourcePattern> SourcePatterns = new();
        private static readonly HashSet<string> ReportedMissingKeys =
            new(StringComparer.Ordinal);

        private static bool initialized;

        public static GameLanguage CurrentLanguage { get; private set; } =
            GameLanguage.English;

        public static bool IsEnglish
        {
            get
            {
                EnsureInitialized();
                return CurrentLanguage == GameLanguage.English;
            }
        }

        public static event Action LanguageChanged;

        public static void Initialize(SystemLanguage systemLanguage)
        {
            EnsureCatalogLoaded();
            string savedValue = PlayerPrefs.GetString(
                GameLanguagePolicy.PreferenceKey,
                string.Empty);
            CurrentLanguage = GameLanguagePolicy.Resolve(savedValue, systemLanguage);
            initialized = true;
        }

        public static void SetLanguage(GameLanguage language)
        {
            EnsureInitialized();
            bool changed = CurrentLanguage != language;
            CurrentLanguage = language;
            PlayerPrefs.SetString(
                GameLanguagePolicy.PreferenceKey,
                GameLanguagePolicy.Serialize(language));
            PlayerPrefs.Save();

            if (changed)
            {
                LanguageChanged?.Invoke();
            }
        }

        public static string Text(string key)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(key) && Entries.TryGetValue(key, out CatalogEntry entry))
            {
                return IsEnglish ? entry.en : entry.ko;
            }

            string safeKey = key ?? string.Empty;
            if (ReportedMissingKeys.Add(safeKey))
            {
                Debug.LogError($"Missing localization key: {safeKey}");
            }

            return $"[[{safeKey}]]";
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                Text(key),
                args ?? Array.Empty<object>());
        }

        public static string TextFromSource(string source)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(source)
                && EntriesByKoreanSource.TryGetValue(source, out CatalogEntry entry))
            {
                return IsEnglish ? entry.en : entry.ko;
            }

            if (IsEnglish
                && CardLocalization.TryTranslateKoreanSource(
                    source,
                    out string translatedCardSource))
            {
                return translatedCardSource;
            }

            if (IsEnglish && TryTranslateFormattedSource(source, out string translated))
            {
                return translated;
            }

            if (IsEnglish
                && TryTranslateCompositeLines(
                    source,
                    out string translatedComposite))
            {
                return translatedComposite;
            }

            if (IsEnglish
                && CardLocalization.TryTranslateKoreanSourcesInText(
                    source,
                    out string translatedCardText))
            {
                return translatedCardText;
            }

            return source ?? string.Empty;
        }

        private static bool TryTranslateFormattedSource(
            string source,
            out string translated)
        {
            if (!string.IsNullOrEmpty(source))
            {
                foreach (SourcePattern pattern in SourcePatterns)
                {
                    if (pattern.TryTranslate(source, out translated))
                    {
                        return true;
                    }
                }
            }

            translated = source ?? string.Empty;
            return false;
        }

        private static string TranslateCapturedValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            if (EntriesByKoreanSource.TryGetValue(value, out CatalogEntry exact))
            {
                return exact.en;
            }

            if (CardLocalization.TryTranslateKoreanSource(
                value,
                out string translatedCardSource))
            {
                return translatedCardSource;
            }

            if (TryTranslateFormattedSource(value, out string translated))
            {
                return translated;
            }

            if (TryTranslateCompositeLines(value, out string translatedComposite))
            {
                return translatedComposite;
            }

            if (TryTranslateDelimitedCapturedValues(
                value,
                out string translatedDelimitedValues))
            {
                return translatedDelimitedValues;
            }

            return CardLocalization.TryTranslateKoreanSourcesInText(
                value,
                out string translatedCardText)
                ? translatedCardText
                : value;
        }

        private static bool TryTranslateDelimitedCapturedValues(
            string source,
            out string translated)
        {
            const string separator = ", ";
            translated = source ?? string.Empty;
            if (string.IsNullOrEmpty(source)
                || source.IndexOf(separator, StringComparison.Ordinal) < 0)
            {
                return false;
            }

            string[] values = source.Split(
                new[] { separator },
                StringSplitOptions.None);
            bool changed = false;
            for (int index = 0; index < values.Length; index += 1)
            {
                string value = values[index];
                string localizedValue = TranslateCapturedValue(value);
                values[index] = localizedValue;
                changed |= !string.Equals(
                    value,
                    localizedValue,
                    StringComparison.Ordinal);
            }

            translated = string.Join(separator, values);
            return changed;
        }

        private static bool TryTranslateCompositeLines(
            string source,
            out string translated)
        {
            translated = source ?? string.Empty;
            if (string.IsNullOrEmpty(source)
                || source.IndexOf('\n') < 0)
            {
                return false;
            }

            string[] lines = source.Split('\n');
            bool changed = false;
            for (int index = 0; index < lines.Length; index += 1)
            {
                string line = lines[index];
                string localizedLine = TranslateSourceFragment(line);
                lines[index] = localizedLine;
                changed |= !string.Equals(
                    line,
                    localizedLine,
                    StringComparison.Ordinal);
            }

            translated = string.Join("\n", lines);
            return changed;
        }

        private static bool TryTranslateCardInventoryLine(
            string source,
            out string translated)
        {
            translated = source ?? string.Empty;
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            int categorySeparator = source.IndexOf(' ');
            if (categorySeparator <= 0
                || !EntriesByKoreanSource.TryGetValue(
                    source.Substring(0, categorySeparator),
                    out CatalogEntry category))
            {
                return false;
            }

            int cardSeparator = source.IndexOf("  ", StringComparison.Ordinal);
            if (cardSeparator <= categorySeparator + 1)
            {
                return false;
            }

            string countText = source.Substring(
                categorySeparator + 1,
                cardSeparator - categorySeparator - 1);
            if (!int.TryParse(
                    countText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return false;
            }

            string categorySource = category.ko;
            if (!string.Equals(categorySource, "공격", StringComparison.Ordinal)
                && !string.Equals(categorySource, "방어", StringComparison.Ordinal)
                && !string.Equals(categorySource, "특수", StringComparison.Ordinal))
            {
                return false;
            }

            string cardText = source.Substring(cardSeparator + 2);
            string localizedCards = CardLocalization.TryTranslateKoreanSourcesInText(
                cardText,
                out string translatedCards)
                ? translatedCards
                : cardText;
            translated = $"{category.en} {countText}  {localizedCards}";
            return true;
        }

        private static string TranslateSourceFragment(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source ?? string.Empty;
            }

            if (EntriesByKoreanSource.TryGetValue(
                source,
                out CatalogEntry exact))
            {
                return exact.en;
            }

            if (CardLocalization.TryTranslateKoreanSource(
                source,
                out string translatedCardSource))
            {
                return translatedCardSource;
            }

            if (TryTranslateCardInventoryLine(source, out string inventoryLine))
            {
                return inventoryLine;
            }

            if (TryTranslateFormattedSource(source, out string translated))
            {
                return translated;
            }

            return CardLocalization.TryTranslateKoreanSourcesInText(
                source,
                out string translatedCardText)
                ? translatedCardText
                : source;
        }

        private static void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize(Application.systemLanguage);
            }
        }

        private static void EnsureCatalogLoaded()
        {
            if (Entries.Count > 0)
            {
                return;
            }

            TextAsset asset = Resources.Load<TextAsset>(CatalogResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Localization catalog was not found at Resources/{CatalogResourcePath}.json");
            }

            CatalogPayload payload = JsonUtility.FromJson<CatalogPayload>(asset.text);
            if (payload?.entries == null || payload.entries.Length == 0)
            {
                throw new InvalidOperationException("Localization catalog has no entries.");
            }

            foreach (CatalogEntry entry in payload.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                Entries.Add(entry.key.Trim(), entry);
                if (!string.IsNullOrEmpty(entry.ko))
                {
                    if (!EntriesByKoreanSource.ContainsKey(entry.ko))
                    {
                        EntriesByKoreanSource.Add(entry.ko, entry);
                    }

                    SourcePattern pattern = SourcePattern.Create(entry);
                    if (pattern != null)
                    {
                        SourcePatterns.Add(pattern);
                    }
                }
            }

            SourcePatterns.Sort(
                (left, right) => right.LiteralWeight.CompareTo(left.LiteralWeight));
        }

        [Serializable]
        private sealed class CatalogPayload
        {
            public CatalogEntry[] entries = Array.Empty<CatalogEntry>();
        }

        [Serializable]
        private sealed class CatalogEntry
        {
            public string key = string.Empty;
            public string ko = string.Empty;
            public string en = string.Empty;
        }

        private sealed class SourcePattern
        {
            private static readonly Regex PlaceholderRegex = new(
                @"\{(?<index>\d+)(?::[^{}]+)?\}",
                RegexOptions.CultureInvariant);

            private readonly Regex matcher;
            private readonly int[] placeholderIndexes;
            private readonly string englishTemplate;

            private SourcePattern(
                Regex matcher,
                int[] placeholderIndexes,
                string englishTemplate,
                int literalWeight)
            {
                this.matcher = matcher;
                this.placeholderIndexes = placeholderIndexes;
                this.englishTemplate = englishTemplate;
                LiteralWeight = literalWeight;
            }

            public int LiteralWeight { get; }

            public static SourcePattern Create(CatalogEntry entry)
            {
                MatchCollection placeholders = PlaceholderRegex.Matches(entry.ko);
                if (placeholders.Count == 0)
                {
                    return null;
                }

                StringBuilder expression = new("^");
                int[] indexes = new int[placeholders.Count];
                int sourceIndex = 0;
                int literalWeight = 0;
                for (int index = 0; index < placeholders.Count; index += 1)
                {
                    Match placeholder = placeholders[index];
                    string literal = entry.ko.Substring(
                        sourceIndex,
                        placeholder.Index - sourceIndex);
                    expression.Append(Regex.Escape(literal));
                    expression.Append("(.*?)");
                    literalWeight += literal.Length;
                    indexes[index] = int.Parse(
                        placeholder.Groups["index"].Value,
                        CultureInfo.InvariantCulture);
                    sourceIndex = placeholder.Index + placeholder.Length;
                }

                string tail = entry.ko.Substring(sourceIndex);
                expression.Append(Regex.Escape(tail));
                expression.Append("$");
                literalWeight += tail.Length;

                if (literalWeight == 0)
                {
                    return null;
                }

                return new SourcePattern(
                    new Regex(expression.ToString(), RegexOptions.CultureInvariant),
                    indexes,
                    entry.en,
                    literalWeight);
            }

            public bool TryTranslate(string source, out string translated)
            {
                Match match = matcher.Match(source);
                if (!match.Success)
                {
                    translated = source;
                    return false;
                }

                Dictionary<int, string> values = new();
                for (int index = 0; index < placeholderIndexes.Length; index += 1)
                {
                    values[placeholderIndexes[index]] = match.Groups[index + 1].Value;
                }

                translated = PlaceholderRegex.Replace(
                    englishTemplate,
                    placeholder =>
                    {
                        int index = int.Parse(
                            placeholder.Groups["index"].Value,
                            CultureInfo.InvariantCulture);
                        return values.TryGetValue(index, out string value)
                            ? TranslateCapturedValue(value)
                            : placeholder.Value;
                    });
                return true;
            }
        }
    }
}
