using System;
using System.Globalization;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    public static class PlayerProgressSyncState
    {
        private const string DeviceIdSuffix = "Cloud.DeviceId";
        private const string RevisionSuffix = "Cloud.Revision";
        private const string UpdatedAtSuffix = "Cloud.UpdatedAt";
        private const string ContentHashSuffix = "Cloud.ContentHash";

        public static string CaptureLocalJson(string keyPrefix, long nowUnixSeconds)
        {
            string deviceId = GetOrCreateDeviceId(keyPrefix);
            long revision = ReadNonNegativeLong(keyPrefix + RevisionSuffix);
            long updatedAt = ReadNonNegativeLong(keyPrefix + UpdatedAtSuffix);
            string json = PlayerPrefsProgressStore.CaptureJson(
                keyPrefix,
                deviceId,
                revision,
                updatedAt);
            string contentHash = PlayerProgressFingerprint.ComputeContentHash(json);
            string storedHash = PlayerPrefs.GetString(keyPrefix + ContentHashSuffix, string.Empty);

            if (!string.Equals(contentHash, storedHash, StringComparison.Ordinal))
            {
                revision = revision == long.MaxValue ? long.MaxValue : revision + 1;
                updatedAt = Math.Max(0, nowUnixSeconds);
                json = PlayerPrefsProgressStore.CaptureJson(
                    keyPrefix,
                    deviceId,
                    revision,
                    updatedAt);
                StoreMetadata(keyPrefix, revision, updatedAt, contentHash);
            }

            return json;
        }

        public static string MergeAndApplyJson(
            string keyPrefix,
            string localJson,
            string cloudJson,
            long nowUnixSeconds)
        {
            string mergedJson = PlayerProgressMerger.MergeJson(localJson, cloudJson);
            PlayerProgressSnapshot merged = JsonUtility.FromJson<PlayerProgressSnapshot>(mergedJson);
            if (merged == null
                || merged.schemaVersion != PlayerProgressSnapshot.CurrentSchemaVersion)
            {
                throw new InvalidOperationException("Unsupported player progress schema.");
            }

            merged.deviceId = GetOrCreateDeviceId(keyPrefix);
            merged.updatedAtUnixSeconds = Math.Max(
                merged.updatedAtUnixSeconds,
                Math.Max(0, nowUnixSeconds));
            mergedJson = JsonUtility.ToJson(merged);

            PlayerPrefsProgressStore.ApplyJson(keyPrefix, mergedJson);
            StoreMetadata(
                keyPrefix,
                merged.revision,
                merged.updatedAtUnixSeconds,
                PlayerProgressFingerprint.ComputeContentHash(mergedJson));
            return mergedJson;
        }

        public static void AdoptRemoteMetadata(string keyPrefix, string cloudJson)
        {
            PlayerProgressSnapshot cloud = JsonUtility.FromJson<PlayerProgressSnapshot>(cloudJson);
            if (cloud == null
                || cloud.schemaVersion != PlayerProgressSnapshot.CurrentSchemaVersion)
            {
                throw new InvalidOperationException("Unsupported player progress schema.");
            }

            string localJson = CaptureLocalJson(keyPrefix, 0);
            string localHash = PlayerProgressFingerprint.ComputeContentHash(localJson);
            string cloudHash = PlayerProgressFingerprint.ComputeContentHash(cloudJson);
            if (!string.Equals(localHash, cloudHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Remote metadata can only be adopted for identical progress content.");
            }

            PlayerProgressSnapshot local = JsonUtility.FromJson<PlayerProgressSnapshot>(localJson);
            StoreMetadata(
                keyPrefix,
                Math.Max(local.revision, Math.Max(0, cloud.revision)),
                Math.Max(local.updatedAtUnixSeconds, Math.Max(0, cloud.updatedAtUnixSeconds)),
                localHash);
        }

        private static string GetOrCreateDeviceId(string keyPrefix)
        {
            string key = keyPrefix + DeviceIdSuffix;
            string deviceId = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                return deviceId;
            }

            deviceId = Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(key, deviceId);
            PlayerPrefs.Save();
            return deviceId;
        }

        private static long ReadNonNegativeLong(string key)
        {
            string value = PlayerPrefs.GetString(key, "0");
            return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out long parsed)
                ? Math.Max(0, parsed)
                : 0;
        }

        private static void StoreMetadata(
            string keyPrefix,
            long revision,
            long updatedAtUnixSeconds,
            string contentHash)
        {
            PlayerPrefs.SetString(
                keyPrefix + RevisionSuffix,
                revision.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(
                keyPrefix + UpdatedAtSuffix,
                updatedAtUnixSeconds.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(keyPrefix + ContentHashSuffix, contentHash);
            PlayerPrefs.Save();
        }
    }
}
