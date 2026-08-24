using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    public static class AppleGameServices
    {
        private static readonly string[] CharacterNames = { "Gambler", "Oracle", "Exile" };
        private static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };

        public const string CloudSaveName = "three-doors-progress-v1";
        public const string EndlessLeaderboardId =
            "com.adam.threedoorsfate.leaderboard.endless";
        public const string HardDifficultyAchievementId =
            "com.adam.threedoorsfate.achievement.hard_unlocked";
        public const string GamblerTrueEndingAchievementId =
            "com.adam.threedoorsfate.achievement.true_ending.gambler";
        public const string OracleTrueEndingAchievementId =
            "com.adam.threedoorsfate.achievement.true_ending.oracle";
        public const string ExileTrueEndingAchievementId =
            "com.adam.threedoorsfate.achievement.true_ending.exile";
        public const string AbyssCollectorAchievementId =
            "com.adam.threedoorsfate.achievement.abyss_collector";
        public const string GamblerHighRollAchievementId =
            "com.adam.threedoorsfate.achievement.build.gambler_high_roll";
        public const string OracleRiftEngineAchievementId =
            "com.adam.threedoorsfate.achievement.build.oracle_rift_engine";
        public const string ExileLastOathAchievementId =
            "com.adam.threedoorsfate.achievement.build.exile_last_oath";

        public static GameCenterProgressReport CaptureGameCenterProgress(string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                throw new ArgumentException("A PlayerPrefs key prefix is required.", nameof(keyPrefix));
            }

            long endlessScore = 0;
            foreach (string character in CharacterNames)
            {
                foreach (string difficulty in DifficultyNames)
                {
                    endlessScore = Math.Max(
                        endlessScore,
                        PlayerPrefs.GetInt(
                            $"{keyPrefix}EndlessRecord.{character}.{difficulty}",
                            0));
                }
            }

            List<string> achievements = new();
            if (PlayerPrefs.GetInt($"{keyPrefix}DifficultyUnlocked", 0) >= 2)
            {
                achievements.Add(HardDifficultyAchievementId);
            }

            AddTrueEndingAchievement(
                achievements,
                keyPrefix,
                "Gambler",
                GamblerTrueEndingAchievementId);
            AddTrueEndingAchievement(
                achievements,
                keyPrefix,
                "Oracle",
                OracleTrueEndingAchievementId);
            AddTrueEndingAchievement(
                achievements,
                keyPrefix,
                "Exile",
                ExileTrueEndingAchievementId);
            foreach (AchievementDefinition definition in AchievementProgress.NewDefinitions)
            {
                if (AchievementProgress.IsCompleted(keyPrefix, definition))
                {
                    achievements.Add(definition.GameCenterId);
                }
            }

            return new GameCenterProgressReport
            {
                endlessScore = endlessScore,
                completedAchievementIds = achievements.ToArray()
            };
        }

        private static void AddTrueEndingAchievement(
            ICollection<string> achievements,
            string keyPrefix,
            string character,
            string achievementId)
        {
            if (PlayerPrefs.GetInt($"{keyPrefix}TrueEnding.{character}", 0) > 0)
            {
                achievements.Add(achievementId);
            }
        }
    }

    [Serializable]
    public sealed class GameCenterProgressReport
    {
        public long endlessScore;
        public string[] completedAchievementIds = Array.Empty<string>();
    }
}
