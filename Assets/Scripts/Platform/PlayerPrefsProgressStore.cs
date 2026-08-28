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
        private const string DeletedRunIdsSuffix = ".DeletedRunIds";

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
            snapshot.deletedRunIds = ReadDeletedRunIds(
                $"{keyPrefix}HardRunSave{DeletedRunIdsSuffix}");
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
            HashSet<string> appliedStringKeys = new(StringComparer.Ordinal);
            foreach (ProgressStringValue entry in snapshot.strings ?? new List<ProgressStringValue>())
            {
                if (entry != null && allowedStringKeys.Contains(entry.key))
                {
                    PlayerPrefs.SetString(entry.key, entry.value ?? string.Empty);
                    appliedStringKeys.Add(entry.key);
                }
            }

            string checkpointKey = $"{keyPrefix}HardRunSave";
            if (!appliedStringKeys.Contains(checkpointKey))
            {
                PlayerPrefs.DeleteKey(checkpointKey);
            }

            WriteDeletedRunIds(
                checkpointKey + DeletedRunIdsSuffix,
                snapshot.deletedRunIds);

            PlayerPrefs.Save();
        }

        public static void RecordDeletedRun(string checkpointKey, string runId)
        {
            if (string.IsNullOrWhiteSpace(checkpointKey))
            {
                throw new ArgumentException(
                    "A checkpoint PlayerPrefs key is required.",
                    nameof(checkpointKey));
            }

            if (string.IsNullOrWhiteSpace(runId))
            {
                runId = ReadRunId(PlayerPrefs.GetString(checkpointKey, string.Empty));
            }

            if (string.IsNullOrWhiteSpace(runId))
            {
                return;
            }

            string tombstoneKey = checkpointKey + DeletedRunIdsSuffix;
            HashSet<string> deletedRunIds = new(
                ReadDeletedRunIds(tombstoneKey),
                StringComparer.Ordinal)
            {
                runId
            };
            WriteDeletedRunIds(tombstoneKey, deletedRunIds);
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
                int schemaVersion = Math.Max(0, data?.version ?? 0);
                snapshot.activeRunId = PlayerProgressRunIdentity.Resolve(
                    data?.runId,
                    schemaVersion,
                    checkpointJson);
                snapshot.activeRunSchemaVersion = schemaVersion;
                snapshot.activeRunRandomCursor = Math.Max(
                    0,
                    data?.randomCursor ?? 0);
            }
            catch (ArgumentException)
            {
                snapshot.activeRunId = PlayerProgressRunIdentity.Resolve(
                    string.Empty,
                    0,
                    checkpointJson);
                snapshot.activeRunSchemaVersion = 0;
                snapshot.activeRunRandomCursor = 0;
            }
        }

        private static string ReadRunId(string checkpointJson)
        {
            if (string.IsNullOrWhiteSpace(checkpointJson))
            {
                return string.Empty;
            }

            try
            {
                ActiveRunMetadata data = JsonUtility.FromJson<ActiveRunMetadata>(
                    checkpointJson);
                return PlayerProgressRunIdentity.Resolve(
                    data?.runId,
                    Math.Max(0, data?.version ?? 0),
                    checkpointJson);
            }
            catch (ArgumentException)
            {
                return PlayerProgressRunIdentity.Resolve(
                    string.Empty,
                    0,
                    checkpointJson);
            }
        }

        private static List<string> ReadDeletedRunIds(string key)
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                DeletedRunIdsData data = JsonUtility.FromJson<DeletedRunIdsData>(json);
                return (data?.runIds ?? new List<string>())
                    .Where(runId => !string.IsNullOrWhiteSpace(runId))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(runId => runId, StringComparer.Ordinal)
                    .ToList();
            }
            catch (ArgumentException)
            {
                return new List<string>();
            }
        }

        private static void WriteDeletedRunIds(
            string key,
            IEnumerable<string> runIds)
        {
            List<string> values = (runIds ?? Enumerable.Empty<string>())
                .Where(runId => !string.IsNullOrWhiteSpace(runId))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(runId => runId, StringComparer.Ordinal)
                .ToList();
            if (values.Count == 0)
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            PlayerPrefs.SetString(
                key,
                JsonUtility.ToJson(new DeletedRunIdsData { runIds = values }));
        }

        [Serializable]
        private sealed class ActiveRunMetadata
        {
            public int version;
            public string runId = string.Empty;
            public int randomCursor;
        }

        [Serializable]
        private sealed class DeletedRunIdsData
        {
            public List<string> runIds = new();
        }
    }
}
