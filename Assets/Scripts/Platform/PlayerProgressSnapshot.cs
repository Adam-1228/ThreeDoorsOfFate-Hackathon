using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    [Serializable]
    public sealed class PlayerProgressSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public long revision;
        public long updatedAtUnixSeconds;
        public string deviceId = string.Empty;
        public string activeRunId = string.Empty;
        public int activeRunSchemaVersion;
        public int activeRunRandomCursor;
        public List<ProgressIntValue> integers = new();
        public List<ProgressStringValue> strings = new();
    }

    [Serializable]
    public sealed class ProgressIntValue
    {
        public string key = string.Empty;
        public int value;
    }

    [Serializable]
    public sealed class ProgressStringValue
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    public static class PlayerProgressMerger
    {
        private const string DiscoveredItemKeyPrefix = "ThreeDoorsOfFate.DiscoveredItems.";

        public static string MergeJson(string localJson, string cloudJson)
        {
            PlayerProgressSnapshot local = Parse(localJson);
            PlayerProgressSnapshot cloud = Parse(cloudJson);
            PlayerProgressSnapshot newer = CompareRecency(local, cloud) >= 0 ? local : cloud;
            PlayerProgressSnapshot older = ReferenceEquals(newer, local) ? cloud : local;

            Dictionary<string, int> mergedIntegers = new(StringComparer.Ordinal);
            AddMaximumValues(mergedIntegers, local.integers);
            AddMaximumValues(mergedIntegers, cloud.integers);
            Dictionary<string, string> mergedStrings = MergeStrings(older, newer);
            ReadActiveRunMetadata(
                mergedStrings,
                out string activeRunId,
                out int activeRunSchemaVersion,
                out int activeRunRandomCursor);

            PlayerProgressSnapshot merged = new()
            {
                schemaVersion = PlayerProgressSnapshot.CurrentSchemaVersion,
                revision = Math.Max(local.revision, cloud.revision) + 1,
                updatedAtUnixSeconds = Math.Max(local.updatedAtUnixSeconds, cloud.updatedAtUnixSeconds),
                deviceId = newer.deviceId ?? string.Empty,
                activeRunId = activeRunId,
                activeRunSchemaVersion = activeRunSchemaVersion,
                activeRunRandomCursor = activeRunRandomCursor,
                integers = mergedIntegers
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new ProgressIntValue { key = pair.Key, value = pair.Value })
                    .ToList(),
                strings = mergedStrings
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new ProgressStringValue { key = pair.Key, value = pair.Value })
                    .ToList()
            };

            return JsonUtility.ToJson(merged);
        }

        private static PlayerProgressSnapshot Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PlayerProgressSnapshot();
            }

            PlayerProgressSnapshot snapshot = JsonUtility.FromJson<PlayerProgressSnapshot>(json);
            if (snapshot == null || snapshot.schemaVersion != PlayerProgressSnapshot.CurrentSchemaVersion)
            {
                throw new InvalidOperationException("Unsupported player progress schema.");
            }

            snapshot.integers ??= new List<ProgressIntValue>();
            snapshot.strings ??= new List<ProgressStringValue>();
            return snapshot;
        }

        private static void AddMaximumValues(
            IDictionary<string, int> destination,
            IEnumerable<ProgressIntValue> source)
        {
            foreach (ProgressIntValue entry in source)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                destination[entry.key] = destination.TryGetValue(entry.key, out int existing)
                    ? Math.Max(existing, entry.value)
                    : entry.value;
            }
        }

        private static Dictionary<string, string> MergeStrings(
            PlayerProgressSnapshot older,
            PlayerProgressSnapshot newer)
        {
            Dictionary<string, string> merged = new(StringComparer.Ordinal);
            AddStringValues(merged, older.strings);
            AddStringValues(merged, newer.strings);

            IEnumerable<string> discoveredKeys = older.strings
                .Concat(newer.strings)
                .Where(entry => entry != null
                    && entry.key != null
                    && entry.key.StartsWith(DiscoveredItemKeyPrefix, StringComparison.Ordinal))
                .Select(entry => entry.key)
                .Distinct(StringComparer.Ordinal);

            foreach (string key in discoveredKeys)
            {
                HashSet<string> itemIds = new(StringComparer.Ordinal);
                AddItemIds(itemIds, FindStringValue(older.strings, key));
                AddItemIds(itemIds, FindStringValue(newer.strings, key));
                merged[key] = JsonUtility.ToJson(new ItemListData
                {
                    itemIds = itemIds.OrderBy(itemId => itemId, StringComparer.Ordinal).ToList()
                });
            }

            return merged;
        }

        private static void AddStringValues(
            IDictionary<string, string> destination,
            IEnumerable<ProgressStringValue> source)
        {
            foreach (ProgressStringValue entry in source)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                {
                    continue;
                }

                destination[entry.key] = entry.value ?? string.Empty;
            }
        }

        private static string FindStringValue(IEnumerable<ProgressStringValue> entries, string key)
        {
            return entries.LastOrDefault(entry => entry != null && entry.key == key)?.value;
        }

        private static void AddItemIds(ISet<string> destination, string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            ItemListData data;
            try
            {
                data = JsonUtility.FromJson<ItemListData>(json);
            }
            catch (ArgumentException)
            {
                return;
            }

            if (data?.itemIds == null)
            {
                return;
            }

            foreach (string itemId in data.itemIds)
            {
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    destination.Add(itemId);
                }
            }
        }

        private static int CompareRecency(PlayerProgressSnapshot left, PlayerProgressSnapshot right)
        {
            int revisionComparison = left.revision.CompareTo(right.revision);
            if (revisionComparison != 0)
            {
                return revisionComparison;
            }

            int timeComparison = left.updatedAtUnixSeconds.CompareTo(right.updatedAtUnixSeconds);
            if (timeComparison != 0)
            {
                return timeComparison;
            }

            return string.Compare(left.deviceId, right.deviceId, StringComparison.Ordinal);
        }

        private static void ReadActiveRunMetadata(
            IReadOnlyDictionary<string, string> strings,
            out string runId,
            out int schemaVersion,
            out int randomCursor)
        {
            runId = string.Empty;
            schemaVersion = 0;
            randomCursor = 0;
            foreach (KeyValuePair<string, string> entry in strings)
            {
                if (!entry.Key.EndsWith("HardRunSave", StringComparison.Ordinal))
                {
                    continue;
                }

                string json = entry.Value;
                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                try
                {
                    ActiveRunMetadata data = JsonUtility.FromJson<ActiveRunMetadata>(json);
                    if (data != null && data.version > 0)
                    {
                        runId = data.runId ?? string.Empty;
                        schemaVersion = data.version;
                        randomCursor = Math.Max(0, data.randomCursor);
                    }
                }
                catch (ArgumentException)
                {
                    // A malformed active run must not block monotonic meta-progress merge.
                }
            }
        }

        [Serializable]
        private sealed class ItemListData
        {
            public List<string> itemIds = new();
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
