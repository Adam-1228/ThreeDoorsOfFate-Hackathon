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
        private const int PersistentAbyssCollectionSize = 30;
        private static readonly string[] PersistentCharacterNames =
        {
            "Gambler",
            "Oracle",
            "Exile"
        };
        private static readonly string[] PersistentDifficultyNames =
        {
            "Easy",
            "Normal",
            "Hard"
        };

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

        private static readonly AchievementDefinition GamblerCardReadingDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.gambler_card_reading",
            "combat.gambler_card_reading",
            "패를 읽은 자",
            "도박사로 한 전투에서 카드 뽑기와 버리기를 합쳐 15회 기록하세요.",
            "흐르는 패의 결을 읽고 운명의 고점을 붙잡았습니다.",
            "Achievements/achievement_gambler_card_reading",
            10);

        private static readonly AchievementDefinition OraclePrecisePredictionDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.oracle_precise_prediction",
            "combat.oracle_precise_prediction",
            "정확한 예언",
            "예언가로 공격 의도에 방어 카드로 세 번 대응하세요.",
            "세 번의 공격을 미리 읽고 정확한 방벽을 세웠습니다.",
            "Achievements/achievement_oracle_precise_prediction",
            10);

        private static readonly AchievementDefinition ExileCurseEaterDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.exile_curse_eater",
            "combat.exile_curse_eater",
            "저주를 삼킨 자",
            "추방자로 한 전투에서 빚을 2 이상 제거하세요.",
            "채무의 저주를 삼키고 끊어진 사슬을 힘으로 바꿨습니다.",
            "Achievements/achievement_exile_curse_eater",
            10);

        private static readonly AchievementDefinition FateCleaverDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.fate_cleaver_50",
            "combat.fate_cleaver_50",
            "운명을 가르는 일격",
            "카드 한 장으로 실제 체력 피해를 50 이상 입히세요.",
            "하나의 일격으로 적의 운명을 갈랐습니다.",
            "Achievements/achievement_fate_cleaver_50",
            15);

        private static readonly AchievementDefinition IronWallDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.iron_wall_40",
            "combat.iron_wall_40",
            "철벽의 맹세",
            "전투 중 방어도를 40 이상 쌓으세요.",
            "겹겹의 방벽으로 어떤 맹세도 꺾이지 않게 지켰습니다.",
            "Achievements/achievement_iron_wall_40",
            15);

        private static readonly AchievementDefinition FiveCardsTurnDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.five_cards_turn",
            "combat.five_cards_turn",
            "한 호흡의 다섯 패",
            "한 턴에 서로 다른 카드 다섯 장을 사용하세요.",
            "한 호흡 안에 다섯 갈래 운명을 이어 냈습니다.",
            "Achievements/achievement_five_cards_turn",
            15);

        private static readonly AchievementDefinition SameRerollThreeDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.same_reroll_three",
            "combat.same_reroll_three",
            "다시 굴리면 뭐가 달라지는데?",
            "한 전투에서 재굴림 결과로 같은 숫자를 세 번 연속 굴리세요.",
            "운명은 숫자 하나 바꿀 성의도 없었습니다.",
            "Achievements/achievement_same_reroll_three",
            15);

        private static readonly AchievementDefinition CliffsideVictoryDefinition = new(
            "com.adam.threedoorsfate.achievement.combat.cliffside_victory",
            "combat.cliffside_victory",
            "벼랑 끝의 생환",
            "최대 체력의 20% 이하로 전투에서 승리하세요.",
            "마지막 불씨를 지킨 채 벼랑 끝에서 돌아왔습니다.",
            "Achievements/achievement_cliffside_victory",
            20);

        private static readonly AchievementDefinition TripleContractDefinition = new(
            "com.adam.threedoorsfate.achievement.collection.triple_contract",
            "collection.triple_contract",
            "세 겹의 계약",
            "유물과 축복과 저주를 동시에 장착하세요.",
            "서로 다른 세 계약을 하나의 운명에 묶었습니다.",
            "Achievements/achievement_triple_contract",
            20);

        private static readonly AchievementDefinition BuildMasterpieceDefinition = new(
            "com.adam.threedoorsfate.achievement.build.masterpiece",
            "build.masterpiece",
            "완성된 설계",
            "현재 직업의 대표 빌드를 2단계까지 강화하세요.",
            "운명의 설계를 마지막 단계까지 완성했습니다.",
            "Achievements/achievement_build_masterpiece",
            20);

        private static readonly AchievementDefinition TwentiethDoorDefinition = new(
            "com.adam.threedoorsfate.achievement.endless.twentieth_door",
            "endless.twentieth_door",
            "스무 번째 문 너머",
            "무한 모드에서 스무 번째 문에 도달하세요.",
            "끝없는 길의 스무 번째 문 너머에 발자국을 남겼습니다.",
            "Achievements/achievement_twentieth_door",
            25);

        private static readonly AchievementDefinition ThreeSurvivorsDefinition = new(
            "com.adam.threedoorsfate.achievement.meta.three_survivors",
            "meta.three_survivors",
            "세 운명의 생존자",
            "세 직업 모두 어려움 귀환 칭호를 획득하세요.",
            "세 운명이 모두 심연의 문을 지나 살아 돌아왔습니다.",
            "Achievements/achievement_three_survivors",
            25);

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
                ExileLastOathDefinition,
                GamblerCardReadingDefinition,
                OraclePrecisePredictionDefinition,
                ExileCurseEaterDefinition,
                FateCleaverDefinition,
                IronWallDefinition,
                FiveCardsTurnDefinition,
                SameRerollThreeDefinition,
                CliffsideVictoryDefinition,
                TripleContractDefinition,
                BuildMasterpieceDefinition,
                TwentiethDoorDefinition,
                ThreeSurvivorsDefinition
            };

        public static AchievementDefinition AbyssCollector => AbyssCollectorDefinition;

        public static AchievementDefinition GamblerCardReading => GamblerCardReadingDefinition;

        public static AchievementDefinition OraclePrecisePrediction => OraclePrecisePredictionDefinition;

        public static AchievementDefinition ExileCurseEater => ExileCurseEaterDefinition;

        public static AchievementDefinition FateCleaver => FateCleaverDefinition;

        public static AchievementDefinition IronWall => IronWallDefinition;

        public static AchievementDefinition FiveCardsTurn => FiveCardsTurnDefinition;

        public static AchievementDefinition SameRerollThree => SameRerollThreeDefinition;

        public static AchievementDefinition CliffsideVictory => CliffsideVictoryDefinition;

        public static AchievementDefinition TripleContract => TripleContractDefinition;

        public static AchievementDefinition BuildMasterpiece => BuildMasterpieceDefinition;

        public static AchievementDefinition TwentiethDoor => TwentiethDoorDefinition;

        public static AchievementDefinition ThreeSurvivors => ThreeSurvivorsDefinition;

        public static AchievementDefinition GetDefinition(string storageSuffix)
        {
            return string.IsNullOrWhiteSpace(storageSuffix)
                ? null
                : NewDefinitions.FirstOrDefault(definition =>
                    string.Equals(
                        definition.StorageSuffix,
                        storageSuffix,
                        StringComparison.Ordinal));
        }

        public static bool IsFateCleaverDamage(int damage)
        {
            return damage >= 50;
        }

        public static bool IsIronWallBlock(int block)
        {
            return block >= 40;
        }

        public static bool IsFiveCardTurn(IEnumerable<string> cardIds)
        {
            return ToIdSet(cardIds).Count >= 5;
        }

        public static int UpdateSameRerollStreak(
            int previousResult,
            int currentStreak,
            int result)
        {
            return currentStreak > 0 && previousResult == result
                ? currentStreak + 1
                : 1;
        }

        public static bool IsCliffsideVictory(int health, int maximumHealth)
        {
            return health > 0
                && maximumHealth > 0
                && (long)health * 100L <= (long)maximumHealth * 20L;
        }

        public static bool HasTripleContract(
            bool hasRelic,
            bool hasBlessing,
            bool hasCurse)
        {
            return hasRelic && hasBlessing && hasCurse;
        }

        public static bool IsMasterpieceLevel(int level)
        {
            return level >= 2;
        }

        public static bool IsTwentiethDoorRecord(int roomCount)
        {
            return roomCount >= 20;
        }

        public static bool HasThreeSurvivors(int survivorCount)
        {
            return survivorCount >= 3;
        }

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

        public static bool BackfillPersistentPlayerPrefs(string keyPrefix)
        {
            ValidateKeyPart(keyPrefix, nameof(keyPrefix));
            bool changed = false;

            bool reachedTwentiethDoor = PersistentCharacterNames.Any(character =>
                PersistentDifficultyNames.Any(difficulty =>
                    IsTwentiethDoorRecord(PlayerPrefs.GetInt(
                        $"{keyPrefix}EndlessRecord.{character}.{difficulty}",
                        0))));
            if (reachedTwentiethDoor)
            {
                changed |= Complete(keyPrefix, TwentiethDoorDefinition);
            }

            int survivorCount = PersistentCharacterNames.Count(character =>
                PlayerPrefs.GetInt(
                    $"{keyPrefix}SurvivorTitle.{character}",
                    0) > 0);
            if (HasThreeSurvivors(survivorCount))
            {
                changed |= Complete(keyPrefix, ThreeSurvivorsDefinition);
            }

            foreach (string character in PersistentCharacterNames)
            {
                PersistentEquippedItemSaveData savedEquipment = ReadJson<
                    PersistentEquippedItemSaveData>(
                    PlayerPrefs.GetString(
                        $"{keyPrefix}EquippedItems.{character}",
                        string.Empty));
                PersistentEquippedItemSaveData savedDiscoveries = ReadJson<
                    PersistentEquippedItemSaveData>(
                    PlayerPrefs.GetString(
                        $"{keyPrefix}DiscoveredItems.{character}",
                        string.Empty));
                HashSet<string> discoveredItems = ToIdSet(savedDiscoveries?.itemIds);
                discoveredItems.UnionWith(ToIdSet(savedEquipment?.itemIds));
                if (discoveredItems.Count(IsRecognizedRunItemId)
                    >= PersistentAbyssCollectionSize)
                {
                    changed |= Complete(keyPrefix, AbyssCollectorDefinition);
                }

                if (!HasAllRunItemTypes(savedEquipment?.itemIds))
                {
                    continue;
                }

                changed |= Complete(keyPrefix, TripleContractDefinition);
                break;
            }

            PersistentRunSaveData savedRun = ReadJson<PersistentRunSaveData>(
                PlayerPrefs.GetString(
                    $"{keyPrefix}HardRunSave",
                    string.Empty));
            if (savedRun == null)
            {
                return changed;
            }

            IReadOnlyList<string> savedDeck =
                savedRun.deckCardIds ?? new List<string>();
            if (HasAllRunItemTypes(savedRun.equippedItemIds))
            {
                changed |= Complete(keyPrefix, TripleContractDefinition);
            }

            if ((savedRun.buildUpgradeLevels ?? new List<PersistentBuildUpgrade>())
                .Any(upgrade => upgrade != null && IsMasterpieceLevel(upgrade.level)))
            {
                changed |= Complete(keyPrefix, BuildMasterpieceDefinition);
            }

            foreach (KeyValuePair<string, AchievementDefinition> build in BuildDefinitions)
            {
                if (IsBuildComplete(build.Key, savedDeck))
                {
                    changed |= Complete(keyPrefix, build.Value);
                }
            }

            return changed;
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

        private static bool HasAllRunItemTypes(IEnumerable<string> itemIds)
        {
            HashSet<string> ids = ToIdSet(itemIds);
            return HasTripleContract(
                ids.Any(id => id.StartsWith("relic_", StringComparison.Ordinal)),
                ids.Any(id => id.StartsWith("blessing_", StringComparison.Ordinal)),
                ids.Any(id => id.StartsWith("curse_", StringComparison.Ordinal)));
        }

        private static bool IsRecognizedRunItemId(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId)
                && (itemId.StartsWith("relic_", StringComparison.Ordinal)
                    || itemId.StartsWith("blessing_", StringComparison.Ordinal)
                    || itemId.StartsWith("curse_", StringComparison.Ordinal));
        }

        private static T ReadJson<T>(string json)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        [Serializable]
        private sealed class PersistentEquippedItemSaveData
        {
            public List<string> itemIds = new();
        }

        [Serializable]
        private sealed class PersistentRunSaveData
        {
            public List<string> deckCardIds = new();
            public List<string> equippedItemIds = new();
            public List<PersistentBuildUpgrade> buildUpgradeLevels = new();
        }

        [Serializable]
        private sealed class PersistentBuildUpgrade
        {
            public string id = string.Empty;
            public int level;
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
