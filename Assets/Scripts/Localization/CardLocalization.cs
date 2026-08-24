using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDoorsOfFate.Localization
{
    public static class CardLocalization
    {
        public static bool Contains(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId)
                && Entries.ContainsKey(cardId.Trim());
        }

        private const string CatalogResourcePath = "Localization/english_cards";
        private const string EnglishCardResourceRoot = "Cards/EnglishLocalized/";

        private static readonly Dictionary<string, CardCatalogEntry> Entries =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> KoreanNameToCardId =
            new(StringComparer.Ordinal);
        private static readonly List<string> KoreanNamesLongestFirst = new();
        private static readonly Dictionary<string, Sprite> SpriteCache =
            new(StringComparer.Ordinal);
        private static readonly HashSet<string> ReportedMissing =
            new(StringComparer.Ordinal);

        private static bool catalogLoaded;

        public static int EntryCount
        {
            get
            {
                EnsureCatalogLoaded();
                return Entries.Count;
            }
        }

        public static string GetName(string cardId, string koreanFallback)
        {
            string fallback = koreanFallback ?? string.Empty;
            if (!GameLocalization.IsEnglish)
            {
                return fallback;
            }

            CardCatalogEntry entry = FindEntry(cardId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.english_display_name))
            {
                ReportMissing($"name:{cardId}", $"Missing English card name: {cardId}");
                return fallback;
            }

            return entry.english_display_name;
        }

        public static string GetRules(string cardId, string koreanFallback)
        {
            string fallback = koreanFallback ?? string.Empty;
            if (!GameLocalization.IsEnglish)
            {
                return fallback;
            }

            CardCatalogEntry entry = FindEntry(cardId);
            if (entry == null || string.IsNullOrWhiteSpace(entry.english_rules_text))
            {
                ReportMissing($"rules:{cardId}", $"Missing English card rules: {cardId}");
                return fallback;
            }

            return entry.english_rules_text;
        }

        public static Sprite GetFullCardSprite(string cardId, Sprite koreanFallback)
        {
            if (!GameLocalization.IsEnglish)
            {
                return koreanFallback;
            }

            CardCatalogEntry entry = FindEntry(cardId);
            if (entry == null)
            {
                return koreanFallback;
            }

            string resourcePath = BuildImageResourcePath(entry.image_relative_path);
            if (string.IsNullOrEmpty(resourcePath))
            {
                ReportMissing(
                    $"image-path:{cardId}",
                    $"Missing English card image path: {cardId}");
                return koreanFallback;
            }

            if (SpriteCache.TryGetValue(resourcePath, out Sprite cached))
            {
                return cached != null ? cached : koreanFallback;
            }

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                ReportMissing(
                    $"sprite:{cardId}",
                    $"Missing English card sprite at Resources/{resourcePath}: {cardId}");
                return koreanFallback;
            }

            SpriteCache.Add(resourcePath, sprite);
            return sprite;
        }

        public static void RegisterKoreanSource(
            string cardId,
            string koreanName)
        {
            if (string.IsNullOrWhiteSpace(cardId)
                || string.IsNullOrWhiteSpace(koreanName))
            {
                return;
            }

            EnsureCatalogLoaded();
            string normalizedId = cardId.Trim();
            if (!Entries.ContainsKey(normalizedId))
            {
                ReportMissing(
                    $"record:{normalizedId}",
                    $"Missing English card localization record: {normalizedId}");
                return;
            }

            string normalizedName = koreanName.Trim();
            if (KoreanNameToCardId.TryGetValue(normalizedName, out string existingId))
            {
                if (!string.Equals(existingId, normalizedId, StringComparison.Ordinal))
                {
                    ReportMissing(
                        $"duplicate-source:{normalizedName}",
                        $"Duplicate Korean card source name: {normalizedName}");
                }

                return;
            }

            KoreanNameToCardId.Add(normalizedName, normalizedId);
            KoreanNamesLongestFirst.Add(normalizedName);
            KoreanNamesLongestFirst.Sort(
                (left, right) => right.Length.CompareTo(left.Length));
        }

        public static bool TryTranslateKoreanSource(
            string source,
            out string translated)
        {
            translated = source ?? string.Empty;
            if (!GameLocalization.IsEnglish || string.IsNullOrEmpty(source))
            {
                return false;
            }

            EnsureCatalogLoaded();
            if (!KoreanNameToCardId.TryGetValue(source, out string cardId)
                || !Entries.TryGetValue(cardId, out CardCatalogEntry entry)
                || string.IsNullOrWhiteSpace(entry.english_display_name))
            {
                return false;
            }

            translated = entry.english_display_name;
            return true;
        }

        public static bool TryTranslateKoreanSourcesInText(
            string source,
            out string translated)
        {
            translated = source ?? string.Empty;
            if (!GameLocalization.IsEnglish || string.IsNullOrEmpty(source))
            {
                return false;
            }

            EnsureCatalogLoaded();
            bool changed = false;
            foreach (string koreanName in KoreanNamesLongestFirst)
            {
                if (translated.IndexOf(koreanName, StringComparison.Ordinal) < 0
                    || !KoreanNameToCardId.TryGetValue(
                        koreanName,
                        out string cardId)
                    || !Entries.TryGetValue(cardId, out CardCatalogEntry entry)
                    || string.IsNullOrWhiteSpace(entry.english_display_name))
                {
                    continue;
                }

                translated = translated.Replace(
                    koreanName,
                    entry.english_display_name,
                    StringComparison.Ordinal);
                changed = true;
            }

            return changed;
        }

        private static CardCatalogEntry FindEntry(string cardId)
        {
            EnsureCatalogLoaded();
            if (!string.IsNullOrWhiteSpace(cardId)
                && Entries.TryGetValue(cardId.Trim(), out CardCatalogEntry entry))
            {
                return entry;
            }

            string safeId = cardId ?? string.Empty;
            ReportMissing(
                $"record:{safeId}",
                $"Missing English card localization record: {safeId}");
            return null;
        }

        private static string BuildImageResourcePath(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return string.Empty;
            }

            string normalized = imagePath.Replace('\\', '/');
            int slashIndex = normalized.LastIndexOf('/');
            string fileName = slashIndex >= 0
                ? normalized.Substring(slashIndex + 1)
                : normalized;
            int extensionIndex = fileName.LastIndexOf('.');
            if (extensionIndex > 0)
            {
                fileName = fileName.Substring(0, extensionIndex);
            }

            return string.IsNullOrWhiteSpace(fileName)
                ? string.Empty
                : EnglishCardResourceRoot + fileName;
        }

        private static void EnsureCatalogLoaded()
        {
            if (catalogLoaded)
            {
                return;
            }

            TextAsset asset = Resources.Load<TextAsset>(CatalogResourcePath);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Card localization catalog was not found at Resources/{CatalogResourcePath}.json");
            }

            CardCatalogPayload payload =
                JsonUtility.FromJson<CardCatalogPayload>(asset.text);
            if (payload?.cards == null || payload.cards.Length == 0)
            {
                throw new InvalidOperationException(
                    "Card localization catalog has no entries.");
            }

            foreach (CardCatalogEntry entry in payload.cards)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.card_id))
                {
                    continue;
                }

                string cardId = entry.card_id.Trim();
                if (Entries.ContainsKey(cardId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate card localization id: {cardId}");
                }

                Entries.Add(cardId, entry);
            }

            if (Entries.Count == 0)
            {
                throw new InvalidOperationException(
                    "Card localization catalog has no valid entries.");
            }

            catalogLoaded = true;
        }

        private static void ReportMissing(string key, string message)
        {
            if (ReportedMissing.Add(key ?? string.Empty))
            {
                Debug.LogError(message);
            }
        }

        [Serializable]
        private sealed class CardCatalogPayload
        {
            public CardCatalogEntry[] cards = Array.Empty<CardCatalogEntry>();
        }

        [Serializable]
        private sealed class CardCatalogEntry
        {
            public string card_id = string.Empty;
            public string image_relative_path = string.Empty;
            public string english_display_name = string.Empty;
            public string english_rules_text = string.Empty;
        }
    }
}
