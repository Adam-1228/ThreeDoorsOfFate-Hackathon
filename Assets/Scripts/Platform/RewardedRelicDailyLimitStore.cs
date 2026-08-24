using System;
using System.Globalization;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    public readonly struct RewardedRelicDailyStatus
    {
        public RewardedRelicDailyStatus(
            string acceptedLocalDate,
            int usedCount,
            long greatestObservedUtcUnixSeconds,
            bool clockRollbackDetected)
        {
            AcceptedLocalDate = acceptedLocalDate ?? string.Empty;
            UsedCount = Mathf.Clamp(
                usedCount,
                0,
                RewardedRelicDailyLimitStore.DailyLimit);
            RemainingCount = RewardedRelicDailyLimitStore.DailyLimit - UsedCount;
            GreatestObservedUtcUnixSeconds = Math.Max(
                0,
                greatestObservedUtcUnixSeconds);
            ClockRollbackDetected = clockRollbackDetected;
        }

        public string AcceptedLocalDate { get; }
        public int UsedCount { get; }
        public int RemainingCount { get; }
        public long GreatestObservedUtcUnixSeconds { get; }
        public bool ClockRollbackDetected { get; }
    }

    public static class RewardedRelicDailyLimitStore
    {
        public const int DailyLimit = 3;

        private const string StorageSegment = "Ads.RewardedRelic.";
        private const string DateSuffix = "Date";
        private const string CountSuffix = "Count";
        private const string GreatestObservedUtcSuffix = "GreatestObservedUtc";
        private const string LocalDateFormat = "yyyy-MM-dd";

        public static RewardedRelicDailyStatus GetStatus(
            string keyPrefix,
            string characterId,
            DateTimeOffset now)
        {
            Validate(keyPrefix, characterId);
            string storagePrefix = GetStoragePrefix(keyPrefix, characterId);
            string dateKey = storagePrefix + DateSuffix;
            string countKey = storagePrefix + CountSuffix;
            string greatestObservedKey = storagePrefix + GreatestObservedUtcSuffix;
            string currentLocalDate = now.ToString(
                LocalDateFormat,
                CultureInfo.InvariantCulture);
            long currentUtcUnixSeconds = Math.Max(0, now.ToUnixTimeSeconds());
            long greatestObservedUtcUnixSeconds = ReadNonNegativeLong(
                greatestObservedKey);

            bool hasStoredDate = PlayerPrefs.HasKey(dateKey);
            string storedDate = PlayerPrefs.GetString(dateKey, string.Empty);
            int usedCount = Mathf.Clamp(
                PlayerPrefs.GetInt(countKey, 0),
                0,
                DailyLimit);
            bool validStoredDate = DateTime.TryParseExact(
                storedDate,
                LocalDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _);
            bool utcRollback = greatestObservedUtcUnixSeconds > 0
                && currentUtcUnixSeconds < greatestObservedUtcUnixSeconds;
            bool localDateRollback = validStoredDate
                && string.CompareOrdinal(currentLocalDate, storedDate) < 0;
            bool clockRollbackDetected = utcRollback || localDateRollback;
            string acceptedLocalDate;

            if (!hasStoredDate)
            {
                acceptedLocalDate = currentLocalDate;
                usedCount = 0;
            }
            else if (!validStoredDate)
            {
                acceptedLocalDate = currentLocalDate;
                usedCount = DailyLimit;
            }
            else if (!clockRollbackDetected
                && string.CompareOrdinal(currentLocalDate, storedDate) > 0)
            {
                acceptedLocalDate = currentLocalDate;
                usedCount = 0;
            }
            else
            {
                acceptedLocalDate = storedDate;
            }

            greatestObservedUtcUnixSeconds = Math.Max(
                greatestObservedUtcUnixSeconds,
                currentUtcUnixSeconds);
            Write(
                dateKey,
                countKey,
                greatestObservedKey,
                acceptedLocalDate,
                usedCount,
                greatestObservedUtcUnixSeconds);
            return new RewardedRelicDailyStatus(
                acceptedLocalDate,
                usedCount,
                greatestObservedUtcUnixSeconds,
                clockRollbackDetected);
        }

        public static bool TryConsume(
            string keyPrefix,
            string characterId,
            DateTimeOffset now,
            out RewardedRelicDailyStatus updatedStatus)
        {
            RewardedRelicDailyStatus status = GetStatus(
                keyPrefix,
                characterId,
                now);
            if (status.RemainingCount <= 0)
            {
                updatedStatus = status;
                return false;
            }

            int usedCount = status.UsedCount + 1;
            string storagePrefix = GetStoragePrefix(keyPrefix, characterId);
            Write(
                storagePrefix + DateSuffix,
                storagePrefix + CountSuffix,
                storagePrefix + GreatestObservedUtcSuffix,
                status.AcceptedLocalDate,
                usedCount,
                status.GreatestObservedUtcUnixSeconds);
            updatedStatus = new RewardedRelicDailyStatus(
                status.AcceptedLocalDate,
                usedCount,
                status.GreatestObservedUtcUnixSeconds,
                status.ClockRollbackDetected);
            return true;
        }

        private static string GetStoragePrefix(
            string keyPrefix,
            string characterId)
        {
            return $"{keyPrefix}{StorageSegment}{characterId}.";
        }

        private static long ReadNonNegativeLong(string key)
        {
            string value = PlayerPrefs.GetString(key, "0");
            return long.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long parsed)
                ? Math.Max(0, parsed)
                : 0;
        }

        private static void Write(
            string dateKey,
            string countKey,
            string greatestObservedKey,
            string acceptedLocalDate,
            int usedCount,
            long greatestObservedUtcUnixSeconds)
        {
            PlayerPrefs.SetString(dateKey, acceptedLocalDate);
            PlayerPrefs.SetInt(countKey, Mathf.Clamp(usedCount, 0, DailyLimit));
            PlayerPrefs.SetString(
                greatestObservedKey,
                Math.Max(0, greatestObservedUtcUnixSeconds).ToString(
                    CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        private static void Validate(string keyPrefix, string characterId)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException(
                    "A PlayerPrefs key prefix is required.",
                    nameof(keyPrefix));
            }

            if (string.IsNullOrWhiteSpace(characterId))
            {
                throw new ArgumentException(
                    "A character identifier is required.",
                    nameof(characterId));
            }
        }
    }
}
