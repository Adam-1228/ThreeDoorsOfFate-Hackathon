using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThreeDoorsOfFate.Game.V140
{
    [Serializable]
    public sealed class RunHistoryEntry
    {
        public string RunId = string.Empty;
        public string GameVersion = string.Empty;
        public long StartedAtUnixSeconds;
        public long FinishedAtUnixSeconds;
        public string CharacterClass = string.Empty;
        public string Difficulty = string.Empty;
        public string StarterContractId = string.Empty;
        public string EndingKind = string.Empty;
        public string EndingCauseKey = string.Empty;
        public string EndingCauseFallback = string.Empty;
        public bool Victory;
        public int DoorsCleared;
        public int BattlesDefeated;
        public int BossesDefeated;
        public int FinalHealth;
        public int FinalMaxHealth;
        public int FinalGold;
        public int FinalDebt;
        public int CardsPlayed;
        public int DamageDealt;
        public int DamageTaken;
        public int CardsRemoved;
        public int ZeroGoldShopVisits;
        public int MaximumSameRerollStreak;
        public int LowLuckRolls;
        public List<string> FinalDeckCardIds = new();
        public List<string> EquippedItemIds = new();
        public List<string> ActiveMutationIds = new();
        public List<string> NewAchievementNames = new();

        internal void Normalize()
        {
            RunId ??= string.Empty;
            GameVersion ??= string.Empty;
            CharacterClass ??= string.Empty;
            Difficulty ??= string.Empty;
            StarterContractId ??= string.Empty;
            EndingKind ??= string.Empty;
            EndingCauseKey ??= string.Empty;
            EndingCauseFallback ??= string.Empty;
            StartedAtUnixSeconds = Math.Max(0L, StartedAtUnixSeconds);
            FinishedAtUnixSeconds = Math.Max(0L, FinishedAtUnixSeconds);
            DoorsCleared = Mathf.Max(0, DoorsCleared);
            BattlesDefeated = Mathf.Max(0, BattlesDefeated);
            BossesDefeated = Mathf.Max(0, BossesDefeated);
            FinalHealth = Mathf.Max(0, FinalHealth);
            FinalMaxHealth = Mathf.Max(0, FinalMaxHealth);
            FinalGold = Mathf.Max(0, FinalGold);
            FinalDebt = Mathf.Max(0, FinalDebt);
            CardsPlayed = Mathf.Max(0, CardsPlayed);
            DamageDealt = Mathf.Max(0, DamageDealt);
            DamageTaken = Mathf.Max(0, DamageTaken);
            CardsRemoved = Mathf.Max(0, CardsRemoved);
            ZeroGoldShopVisits = Mathf.Max(0, ZeroGoldShopVisits);
            MaximumSameRerollStreak = Mathf.Max(0, MaximumSameRerollStreak);
            LowLuckRolls = Mathf.Max(0, LowLuckRolls);
            FinalDeckCardIds = NormalizeList(FinalDeckCardIds);
            EquippedItemIds = NormalizeList(EquippedItemIds);
            ActiveMutationIds = NormalizeList(ActiveMutationIds);
            NewAchievementNames = NormalizeList(NewAchievementNames);
        }

        private static List<string> NormalizeList(IEnumerable<string> values)
        {
            return values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList()
                ?? new List<string>();
        }
    }

    public static class RunHistoryStore
    {
        public const int MaximumEntries = 10;
        private const int SchemaVersion = 1;
        private const string StorageSuffix = "RunHistory.v1";

        [Serializable]
        private sealed class RunHistoryPayload
        {
            public int schemaVersion = SchemaVersion;
            public List<RunHistoryEntry> entries = new();
        }

        public static string GetStorageKey(string keyPrefix)
        {
            return (keyPrefix ?? string.Empty) + StorageSuffix;
        }

        public static IReadOnlyList<RunHistoryEntry> Read(string keyPrefix)
        {
            string key = GetStorageKey(keyPrefix);
            if (!PlayerPrefs.HasKey(key))
            {
                return Array.Empty<RunHistoryEntry>();
            }

            try
            {
                string json = PlayerPrefs.GetString(key, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return Array.Empty<RunHistoryEntry>();
                }

                RunHistoryPayload payload =
                    JsonUtility.FromJson<RunHistoryPayload>(json);
                if (payload == null
                    || payload.schemaVersion != SchemaVersion
                    || payload.entries == null)
                {
                    return Array.Empty<RunHistoryEntry>();
                }

                List<RunHistoryEntry> entries = payload.entries
                    .Where(entry => entry != null
                        && !string.IsNullOrWhiteSpace(entry.RunId))
                    .ToList();
                foreach (RunHistoryEntry entry in entries)
                {
                    entry.Normalize();
                }

                return entries
                    .OrderByDescending(entry => entry.FinishedAtUnixSeconds)
                    .ThenBy(entry => entry.RunId, StringComparer.Ordinal)
                    .Take(MaximumEntries)
                    .ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<RunHistoryEntry>();
            }
        }

        public static void Append(string keyPrefix, RunHistoryEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            entry.Normalize();
            if (string.IsNullOrWhiteSpace(entry.RunId))
            {
                throw new ArgumentException(
                    "Run history requires a stable run ID.",
                    nameof(entry));
            }

            List<RunHistoryEntry> entries = Read(keyPrefix)
                .Where(candidate => !string.Equals(
                    candidate.RunId,
                    entry.RunId,
                    StringComparison.Ordinal))
                .ToList();
            entries.Add(entry);
            entries = entries
                .OrderByDescending(candidate => candidate.FinishedAtUnixSeconds)
                .ThenBy(candidate => candidate.RunId, StringComparer.Ordinal)
                .Take(MaximumEntries)
                .ToList();

            RunHistoryPayload payload = new()
            {
                entries = entries
            };
            PlayerPrefs.SetString(
                GetStorageKey(keyPrefix),
                JsonUtility.ToJson(payload));
            PlayerPrefs.Save();
        }
    }

    public static class RunHistoryEpithetPolicy
    {
        public static IReadOnlyList<string> Get(RunHistoryEntry entry)
        {
            if (entry == null)
            {
                return Array.Empty<string>();
            }

            List<string> keys = new();
            if (entry.MaximumSameRerollStreak >= 3)
            {
                keys.Add("runHistory.epithet.sameAgain");
            }

            if (entry.ZeroGoldShopVisits > 0)
            {
                keys.Add("runHistory.epithet.windowShopper");
            }

            if (entry.LowLuckRolls >= 6)
            {
                keys.Add("runHistory.epithet.unlucky");
            }

            if (entry.FinalDebt >= 8)
            {
                keys.Add("runHistory.epithet.debtMagnet");
            }

            if (entry.CardsPlayed >= 60)
            {
                keys.Add("runHistory.epithet.deckWhisperer");
            }

            if (entry.FinalMaxHealth > 0
                && entry.DamageTaken >= entry.FinalMaxHealth * 2)
            {
                keys.Add("runHistory.epithet.damageSponge");
            }

            if (entry.DoorsCleared >= 20)
            {
                keys.Add("runHistory.epithet.noDoorEnough");
            }

            return keys;
        }
    }
}
