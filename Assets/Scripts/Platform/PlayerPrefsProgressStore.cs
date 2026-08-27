using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    public static class PlayerPrefsProgressStore
    {
        public const string ProductionPrefix = "ThreeDoorsOfFate.";

        private static readonly string[] CharacterNames = { "Gambler", "Oracle", "Exile" };
        private static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };
        private static readonly string[] RunItemTypeNames = { "Relic", "Blessing", "Curse" };

        public static string CaptureJson(
            string keyPrefix,
            string deviceId,
            long revision,
            long updatedAtUnixSeconds)
        {
            ValidatePrefix(keyPrefix);
            PlayerProgressSnapshot snapshot = new()
            {
                schemaVersion = PlayerProgressSnapshot.CurrentSchemaVersion,
                revision = Math.Max(0, revision),
                updatedAtUnixSeconds = Math.Max(0, updatedAtUnixSeconds),
                deviceId = deviceId ?? string.Empty
            };

            foreach (string key in GetIntegerKeys(keyPrefix))
            {
                if (PlayerPrefs.HasKey(key))
                {
                    snapshot.integers.Add(new ProgressIntValue
                    {
                        key = key,
                        value = PlayerPrefs.GetInt(key)
                    });
                }
            }

            foreach (string key in GetStringKeys(keyPrefix))
            {
                if (PlayerPrefs.HasKey(key))
                {
                    snapshot.strings.Add(new ProgressStringValue
                    {
                        key = key,
                        value = PlayerPrefs.GetString(key, string.Empty)
                    });
                }
            }

            snapshot.integers = snapshot.integers
                .OrderBy(entry => entry.key, StringComparer.Ordinal)
                .ToList();
            snapshot.strings = snapshot.strings
                .OrderBy(entry => entry.key, StringComparer.Ordinal)
                .ToList();
            PopulateActiveRunMetadata(snapshot, $"{keyPrefix}HardRunSave");
            return JsonUtility.ToJson(snapshot);
        }

        public static void ApplyJson(string keyPrefix, string json)
        {
            ValidatePrefix(keyPrefix);
            PlayerProgressSnapshot snapshot = JsonUtility.FromJson<PlayerProgressSnapshot>(json);
            if (snapshot == null || snapshot.schemaVersion != PlayerProgressSnapshot.CurrentSchemaVersion)
            {
                throw new InvalidOperationException("Unsupported player progress schema.");
            }

            HashSet<string> allowedIntegerKeys = new(GetIntegerKeys(keyPrefix), StringComparer.Ordinal);
            foreach (ProgressIntValue entry in snapshot.integers ?? new List<ProgressIntValue>())
            {
                if (entry != null && allowedIntegerKeys.Contains(entry.key))
                {
                    PlayerPrefs.SetInt(entry.key, entry.value);
                }
            }

            HashSet<string> allowedStringKeys = new(GetStringKeys(keyPrefix), StringComparer.Ordinal);
            foreach (ProgressStringValue entry in snapshot.strings ?? new List<ProgressStringValue>())
            {
                if (entry != null && allowedStringKeys.Contains(entry.key))
                {
                    PlayerPrefs.SetString(entry.key, entry.value ?? string.Empty);
                }
            }

            PlayerPrefs.Save();
        }

        public static IReadOnlyList<string> GetIntegerKeys(string keyPrefix)
        {
            ValidatePrefix(keyPrefix);
            List<string> keys = new()
            {
                $"{keyPrefix}DifficultyUnlocked",
                $"{keyPrefix}EndlessRecord.Seen"
            };

            foreach (string character in CharacterNames)
            {
                keys.Add($"{keyPrefix}TrueEnding.{character}");
                keys.Add($"{keyPrefix}SurvivorTitle.{character}");

                foreach (string difficulty in DifficultyNames)
                {
                    keys.Add($"{keyPrefix}EndlessRecord.{character}.{difficulty}");
                }

                foreach (string runItemType in RunItemTypeNames)
                {
                    keys.Add($"{keyPrefix}RunItemUnlock.{character}.{runItemType}");
                }
            }

            keys.AddRange(AchievementProgress.GetCompletionKeys(keyPrefix));

            return keys;
        }

        public static IReadOnlyList<string> GetStringKeys(string keyPrefix)
        {
            ValidatePrefix(keyPrefix);
            List<string> keys = new() { $"{keyPrefix}HardRunSave" };
            foreach (string character in CharacterNames)
            {
                keys.Add($"{keyPrefix}EquippedItems.{character}");
                keys.Add($"{keyPrefix}DiscoveredItems.{character}");
            }

            return keys;
        }

        private static void ValidatePrefix(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException("A PlayerPrefs key prefix is required.", nameof(keyPrefix));
            }
        }

        private static void PopulateActiveRunMetadata(
            PlayerProgressSnapshot snapshot,
            string checkpointKey)
        {
            string checkpointJson = snapshot.strings
                .LastOrDefault(entry => entry != null && entry.key == checkpointKey)
                ?.value;
            if (string.IsNullOrWhiteSpace(checkpointJson))
            {
                return;
            }

            try
            {
                ActiveRunMetadata data = JsonUtility.FromJson<ActiveRunMetadata>(checkpointJson);
                if (data == null || data.version <= 0)
                {
                    return;
                }

                snapshot.activeRunId = data.runId ?? string.Empty;
                snapshot.activeRunSchemaVersion = data.version;
                snapshot.activeRunRandomCursor = Math.Max(0, data.randomCursor);
            }
            catch (ArgumentException)
            {
                // Meta progression still syncs even when the run-local checkpoint is malformed.
            }
        }

        [Serializable]
        private sealed class ActiveRunMetadata
        {
            public int version;
            public string runId = string.Empty;
            public int randomCursor;
        }
    }
}
