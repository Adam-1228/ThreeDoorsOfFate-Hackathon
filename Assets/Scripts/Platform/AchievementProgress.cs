using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    public sealed class AchievementDefinition
    {
        public AchievementDefinition(
            string gameCenterId,
            string storageSuffix,
            string displayName,
            string lockedDescription,
            string earnedDescription,
            string imageResourcePath,
            int points)
        {
            GameCenterId = gameCenterId;
            StorageSuffix = storageSuffix;
            DisplayName = displayName;
            LockedDescription = lockedDescription;
            EarnedDescription = earnedDescription;
            ImageResourcePath = imageResourcePath;
            Points = points;
        }

        public string GameCenterId { get; }

        public string StorageSuffix { get; }

        public string DisplayName { get; }

        public string LockedDescription { get; }

        public string EarnedDescription { get; }

        public string ImageResourcePath { get; }

        public int Points { get; }
    }

    public static class AchievementProgress
    {
        private const string CompletionKeySegment = "Achievement.";

        private static readonly AchievementDefinition AbyssCollectorDefinition = new(
            "com.adam.threedoorsfate.achievement.abyss_collector",
            "abyss_collector",
            "심연의 수집가",
            "한 캐릭터로 유물·축복·저주 30종을 모두 발견하세요.",
            "한 운명이 심연에 숨은 모든 계약을 수집했습니다.",
            "Achievements/achievement_abyss_collector",
            100);

        private static readonly AchievementDefinition GamblerHighRollDefinition = new(
            "com.adam.threedoorsfate.achievement.build.gambler_high_roll",
            "build.gambler_high_roll",
            "운명을 건 판돈",
            "도박사의 판돈 단검·판돈 방패·판 뒤집기를 한 덱에 모으세요.",
            "모든 것을 건 판돈이 운명을 뒤집었습니다.",
            "Achievements/achievement_gambler_high_roll",
            100);

        private static readonly AchievementDefinition OracleRiftEngineDefinition = new(
            "com.adam.threedoorsfate.achievement.build.oracle_rift_engine",
            "build.oracle_rift_engine",
            "세 문의 예언",
            "예언가의 별자리 절단·예견된 방벽·세 문의 징조를 한 덱에 모으세요.",
            "세 문의 징조가 하나의 예언으로 이어졌습니다.",
            "Achievements/achievement_oracle_rift_engine",
            100);

        private static readonly AchievementDefinition ExileLastOathDefinition = new(
            "com.adam.threedoorsfate.achievement.build.exile_last_oath",
            "build.exile_last_oath",
            "끊어진 맹세",
            "추방자의 사슬 처형·추방의 맹세·낙인 정화를 한 덱에 모으세요.",
            "끊어진 맹세가 마지막 심판으로 완성됐습니다.",
            "Achievements/achievement_exile_last_oath",
            100);

        private static readonly IReadOnlyDictionary<string, string[]> BuildRequirements =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["gambler_high_roll"] = new[]
                {
                    "class_gambler_attack_wager_dagger",
                    "class_gambler_defense_stake_shield",
                    "class_gambler_skill_turn_the_table"
                },
                ["oracle_rift_engine"] = new[]
                {
                    "class_oracle_attack_constellation_cut",
                    "class_oracle_defense_foreseen_barrier",
                    "class_oracle_skill_three_door_omen"
                },
                ["exile_last_oath"] = new[]
                {
                    "class_exile_attack_chain_execution",
                    "class_exile_defense_oath_of_exile",
                    "class_exile_skill_brand_purification"
                }
            };

        private static readonly IReadOnlyDictionary<string, AchievementDefinition>
            BuildDefinitions = new Dictionary<string, AchievementDefinition>(StringComparer.Ordinal)
            {
                ["gambler_high_roll"] = GamblerHighRollDefinition,
                ["oracle_rift_engine"] = OracleRiftEngineDefinition,
                ["exile_last_oath"] = ExileLastOathDefinition
            };

        public static IReadOnlyList<AchievementDefinition> NewDefinitions { get; } =
            new[]
            {
                AbyssCollectorDefinition,
                GamblerHighRollDefinition,
                OracleRiftEngineDefinition,
                ExileLastOathDefinition
            };

        public static AchievementDefinition AbyssCollector => AbyssCollectorDefinition;

        public static bool IsCollectionComplete(
            IEnumerable<string> discoveredItemIds,
            IEnumerable<string> catalogItemIds)
        {
            HashSet<string> catalog = ToIdSet(catalogItemIds);
            if (catalog.Count == 0)
            {
                return false;
            }

            HashSet<string> discovered = ToIdSet(discoveredItemIds);
            return catalog.All(discovered.Contains);
        }

        public static bool IsAnyCharacterCollectionComplete(
            IEnumerable<IEnumerable<string>> characterCollections,
            IEnumerable<string> catalogItemIds)
        {
            if (characterCollections == null)
            {
                return false;
            }

            string[] catalog = ToIdSet(catalogItemIds).ToArray();
            return catalog.Length > 0
                && characterCollections.Any(collection => IsCollectionComplete(collection, catalog));
        }

        public static bool IsBuildComplete(string buildId, IEnumerable<string> cardIds)
        {
            if (string.IsNullOrWhiteSpace(buildId)
                || !BuildRequirements.TryGetValue(buildId, out string[] requiredCardIds))
            {
                return false;
            }

            HashSet<string> availableCards = ToIdSet(cardIds);
            return requiredCardIds.All(availableCards.Contains);
        }

        public static AchievementDefinition GetDefinitionForBuild(string buildId)
        {
            return !string.IsNullOrWhiteSpace(buildId)
                && BuildDefinitions.TryGetValue(buildId, out AchievementDefinition definition)
                    ? definition
                    : null;
        }

        public static string GetCompletionKey(string keyPrefix, string storageSuffix)
        {
            ValidateKeyPart(keyPrefix, nameof(keyPrefix));
            ValidateKeyPart(storageSuffix, nameof(storageSuffix));
            return $"{keyPrefix}{CompletionKeySegment}{storageSuffix}";
        }

        public static IReadOnlyList<string> GetCompletionKeys(string keyPrefix)
        {
            return NewDefinitions
                .Select(definition => GetCompletionKey(keyPrefix, definition.StorageSuffix))
                .ToArray();
        }

        public static bool IsCompleted(
            string keyPrefix,
            AchievementDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return PlayerPrefs.GetInt(
                GetCompletionKey(keyPrefix, definition.StorageSuffix),
                0) > 0;
        }

        public static bool Complete(
            string keyPrefix,
            AchievementDefinition definition)
        {
            if (IsCompleted(keyPrefix, definition))
            {
                return false;
            }

            PlayerPrefs.SetInt(
                GetCompletionKey(keyPrefix, definition.StorageSuffix),
                1);
            PlayerPrefs.Save();
            return true;
        }

        private static HashSet<string> ToIdSet(IEnumerable<string> ids)
        {
            return ids == null
                ? new HashSet<string>(StringComparer.Ordinal)
                : new HashSet<string>(
                    ids.Where(id => !string.IsNullOrWhiteSpace(id)),
                    StringComparer.Ordinal);
        }

        private static void ValidateKeyPart(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-empty PlayerPrefs key part is required.", parameterName);
            }
        }
    }
}
