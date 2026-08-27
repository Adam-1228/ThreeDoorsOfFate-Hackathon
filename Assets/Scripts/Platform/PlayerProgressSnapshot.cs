using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public List<string> deletedRunIds = new();
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

    internal static class PlayerProgressRunIdentity
    {
        public static string Resolve(string runId, int version, string checkpointJson)
        {
            if (!string.IsNullOrWhiteSpace(runId))
            {
                return runId;
            }

            if (string.IsNullOrWhiteSpace(checkpointJson))
            {
                return string.Empty;
            }

            string prefix = version == 1 ? "legacy-" : "opaque-";
            return prefix + StableHash(checkpointJson).ToString("x16");
        }

        private static ulong StableHash(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            foreach (byte item in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= item;
                hash *= prime;
            }

            return hash == 0UL ? 1UL : hash;
        }
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
            HashSet<string> deletedRunIds = new(
                local.deletedRunIds.Concat(cloud.deletedRunIds),
                StringComparer.Ordinal);

            Dictionary<string, int> mergedIntegers = new(StringComparer.Ordinal);
            AddMaximumValues(mergedIntegers, local.integers);
            AddMaximumValues(mergedIntegers, cloud.integers);
            Dictionary<string, string> mergedStrings = MergeStrings(older, newer);
            RemoveActiveRunValues(mergedStrings);
            ActiveRunCandidate activeRun = SelectActiveRun(
                local,
                ReadActiveRun(local),
                cloud,
                ReadActiveRun(cloud),
                deletedRunIds);
            if (activeRun != null)
            {
                mergedStrings[activeRun.Key] = activeRun.Json;
            }

            PlayerProgressSnapshot merged = new()
            {
                schemaVersion = PlayerProgressSnapshot.CurrentSchemaVersion,
                revision = Math.Max(local.revision, cloud.revision) + 1,
                updatedAtUnixSeconds = Math.Max(local.updatedAtUnixSeconds, cloud.updatedAtUnixSeconds),
                deviceId = newer.deviceId ?? string.Empty,
                activeRunId = activeRun?.RunId ?? string.Empty,
                activeRunSchemaVersion = activeRun?.SchemaVersion ?? 0,
                activeRunRandomCursor = activeRun?.RandomCursor ?? 0,
                deletedRunIds = deletedRunIds
                    .Where(runId => !string.IsNullOrWhiteSpace(runId))
                    .OrderBy(runId => runId, StringComparer.Ordinal)
                    .ToList(),
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
            snapshot.deletedRunIds ??= new List<string>();
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

        private static ActiveRunCandidate ReadActiveRun(
            PlayerProgressSnapshot snapshot)
        {
            foreach (ProgressStringValue entry in snapshot.strings)
            {
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.key)
                    || !entry.key.EndsWith("HardRunSave", StringComparison.Ordinal)
                    || string.IsNullOrWhiteSpace(entry.value))
                {
                    continue;
                }

                ActiveRunMetadata data = null;
                try
                {
                    data = JsonUtility.FromJson<ActiveRunMetadata>(
                        entry.value);
                }
                catch (ArgumentException)
                {
                    // Preserve opaque data so the runtime can report a restore error.
                }

                int schemaVersion = Math.Max(0, data?.version ?? 0);
                return new ActiveRunCandidate
                {
                    Snapshot = snapshot,
                    Key = entry.key,
                    Json = entry.value,
                    RunId = PlayerProgressRunIdentity.Resolve(
                        data?.runId,
                        schemaVersion,
                        entry.value),
                    SchemaVersion = schemaVersion,
                    RandomCursor = Math.Max(0, data?.randomCursor ?? 0)
                };
            }

            return null;
        }

        private static ActiveRunCandidate SelectActiveRun(
            PlayerProgressSnapshot localSnapshot,
            ActiveRunCandidate local,
            PlayerProgressSnapshot cloudSnapshot,
            ActiveRunCandidate cloud,
            ISet<string> deletedRunIds)
        {
            if (local != null && deletedRunIds.Contains(local.RunId))
            {
                local = null;
            }

            if (cloud != null && deletedRunIds.Contains(cloud.RunId))
            {
                cloud = null;
            }

            if (local == null && cloud == null)
            {
                return null;
            }

            if (local == null)
            {
                return CompareRecency(cloud.Snapshot, localSnapshot) >= 0
                    ? cloud
                    : null;
            }

            if (cloud == null)
            {
                return CompareRecency(local.Snapshot, cloudSnapshot) >= 0
                    ? local
                    : null;
            }

            if (string.Equals(local.RunId, cloud.RunId, StringComparison.Ordinal))
            {
                int cursorComparison = local.RandomCursor.CompareTo(cloud.RandomCursor);
                if (cursorComparison != 0)
                {
                    return cursorComparison > 0 ? local : cloud;
                }
            }

            return CompareRecency(local.Snapshot, cloud.Snapshot) >= 0
                ? local
                : cloud;
        }

        private static void RemoveActiveRunValues(
            IDictionary<string, string> strings)
        {
            foreach (string key in strings.Keys
                .Where(key => key.EndsWith("HardRunSave", StringComparison.Ordinal))
                .ToArray())
            {
                strings.Remove(key);
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

        private sealed class ActiveRunCandidate
        {
            public PlayerProgressSnapshot Snapshot;
            public string Key = string.Empty;
            public string Json = string.Empty;
            public string RunId = string.Empty;
            public int SchemaVersion;
            public int RandomCursor;
        }
    }
}
