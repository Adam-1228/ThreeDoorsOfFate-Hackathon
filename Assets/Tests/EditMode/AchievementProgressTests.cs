using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThreeDoorsOfFate.Platform;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class AchievementProgressTests
    {
        [Test]
        public void NewDefinitions_FillTheTwentyAchievementPointBudget()
        {
            IReadOnlyList<AchievementDefinition> definitions =
                AchievementProgress.NewDefinitions;

            Assert.That(definitions.Count, Is.EqualTo(16));
            Assert.That(definitions.Select(definition => definition.GameCenterId), Is.EquivalentTo(
                new[]
                {
                    "com.adam.threedoorsfate.achievement.abyss_collector",
                    "com.adam.threedoorsfate.achievement.build.gambler_high_roll",
                    "com.adam.threedoorsfate.achievement.build.oracle_rift_engine",
                    "com.adam.threedoorsfate.achievement.build.exile_last_oath",
                    "com.adam.threedoorsfate.achievement.combat.gambler_card_reading",
                    "com.adam.threedoorsfate.achievement.combat.oracle_precise_prediction",
                    "com.adam.threedoorsfate.achievement.combat.exile_curse_eater",
                    "com.adam.threedoorsfate.achievement.combat.fate_cleaver_50",
                    "com.adam.threedoorsfate.achievement.combat.iron_wall_40",
                    "com.adam.threedoorsfate.achievement.combat.five_cards_turn",
                    "com.adam.threedoorsfate.achievement.combat.same_reroll_three",
                    "com.adam.threedoorsfate.achievement.combat.cliffside_victory",
                    "com.adam.threedoorsfate.achievement.collection.triple_contract",
                    "com.adam.threedoorsfate.achievement.build.masterpiece",
                    "com.adam.threedoorsfate.achievement.endless.twentieth_door",
                    "com.adam.threedoorsfate.achievement.meta.three_survivors"
                }));
            Assert.That(
                definitions.Select(definition => definition.GameCenterId).Distinct().Count(),
                Is.EqualTo(definitions.Count));
            Assert.That(
                definitions.Select(definition => definition.StorageSuffix).Distinct().Count(),
                Is.EqualTo(definitions.Count));
            Assert.That(
                definitions.Select(definition => definition.ImageResourcePath).Distinct().Count(),
                Is.EqualTo(definitions.Count));
            Assert.That(definitions.Sum(definition => definition.Points), Is.EqualTo(600));
            Assert.That(definitions.Sum(definition => definition.Points) + 400, Is.EqualTo(1000));
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.ImageResourcePath)), Is.True);
        }

        [Test]
        public void Version120Definitions_AddExactlyTwoHundredPoints()
        {
            string[] suffixes =
            {
                "combat.gambler_card_reading",
                "combat.oracle_precise_prediction",
                "combat.exile_curse_eater",
                "combat.fate_cleaver_50",
                "combat.iron_wall_40",
                "combat.five_cards_turn",
                "combat.same_reroll_three",
                "combat.cliffside_victory",
                "collection.triple_contract",
                "build.masterpiece",
                "endless.twentieth_door",
                "meta.three_survivors"
            };

            AchievementDefinition[] definitions = suffixes
                .Select(AchievementProgress.GetDefinition)
                .ToArray();

            Assert.That(definitions, Has.All.Not.Null);
            Assert.That(definitions.Sum(definition => definition.Points), Is.EqualTo(200));
        }

        [TestCase(49, false)]
        [TestCase(50, true)]
        public void FateCleaver_UsesFiftyDamageBoundary(int damage, bool expected)
        {
            Assert.That(AchievementProgress.IsFateCleaverDamage(damage), Is.EqualTo(expected));
        }

        [TestCase(39, false)]
        [TestCase(40, true)]
        public void IronWall_UsesFortyBlockBoundary(int block, bool expected)
        {
            Assert.That(AchievementProgress.IsIronWallBlock(block), Is.EqualTo(expected));
        }

        [Test]
        public void FiveCardTurn_RequiresFiveDistinctNonEmptyCards()
        {
            Assert.That(
                AchievementProgress.IsFiveCardTurn(new[] { "a", "b", "c", "d", "d", "" }),
                Is.False);
            Assert.That(
                AchievementProgress.IsFiveCardTurn(new[] { "a", "b", "c", "d", "e", "e" }),
                Is.True);
        }

        [TestCase(0, 0, 4, 1)]
        [TestCase(4, 1, 4, 2)]
        [TestCase(4, 2, 4, 3)]
        [TestCase(4, 2, 3, 1)]
        public void SameRerollStreak_TracksOnlyConsecutiveMatchingResults(
            int previousResult,
            int currentStreak,
            int result,
            int expected)
        {
            Assert.That(
                AchievementProgress.UpdateSameRerollStreak(
                    previousResult,
                    currentStreak,
                    result),
                Is.EqualTo(expected));
        }

        [Test]
        public void CliffsideVictory_IncludesExactlyTwentyPercentHealth()
        {
            Assert.That(AchievementProgress.IsCliffsideVictory(21, 100), Is.False);
            Assert.That(AchievementProgress.IsCliffsideVictory(20, 100), Is.True);
            Assert.That(AchievementProgress.IsCliffsideVictory(1, 5), Is.True);
            Assert.That(AchievementProgress.IsCliffsideVictory(0, 100), Is.False);
            Assert.That(AchievementProgress.IsCliffsideVictory(10, 0), Is.False);
        }

        [Test]
        public void TripleContract_RequiresAllThreeRunItemTypes()
        {
            Assert.That(AchievementProgress.HasTripleContract(true, true, false), Is.False);
            Assert.That(AchievementProgress.HasTripleContract(true, true, true), Is.True);
        }

        [TestCase(1, false)]
        [TestCase(2, true)]
        public void Masterpiece_UsesLevelTwoBoundary(int level, bool expected)
        {
            Assert.That(AchievementProgress.IsMasterpieceLevel(level), Is.EqualTo(expected));
        }

        [TestCase(19, false)]
        [TestCase(20, true)]
        public void TwentiethDoor_UsesRoomTwentyBoundary(int rooms, bool expected)
        {
            Assert.That(AchievementProgress.IsTwentiethDoorRecord(rooms), Is.EqualTo(expected));
        }

        [TestCase(2, false)]
        [TestCase(3, true)]
        public void ThreeSurvivors_UsesThreeTitleBoundary(int count, bool expected)
        {
            Assert.That(AchievementProgress.HasThreeSurvivors(count), Is.EqualTo(expected));
        }

        [Test]
        public void IsCollectionComplete_RequiresEveryCatalogItem()
        {
            string[] catalog = Enumerable.Range(1, 30)
                .Select(index => $"item_{index:00}")
                .ToArray();

            Assert.That(
                AchievementProgress.IsCollectionComplete(catalog.Take(29), catalog),
                Is.False);
            Assert.That(
                AchievementProgress.IsCollectionComplete(catalog, catalog),
                Is.True);
        }

        [Test]
        public void IsAnyCharacterCollectionComplete_DoesNotCombineCharacters()
        {
            string[] catalog = Enumerable.Range(1, 30)
                .Select(index => $"item_{index:00}")
                .ToArray();
            IReadOnlyList<IEnumerable<string>> splitCollections = new IEnumerable<string>[]
            {
                catalog.Take(10),
                catalog.Skip(10).Take(10),
                catalog.Skip(20).Take(10)
            };
            IReadOnlyList<IEnumerable<string>> oneCompleteCollection = new IEnumerable<string>[]
            {
                catalog.Take(10),
                catalog,
                Array.Empty<string>()
            };

            Assert.That(
                AchievementProgress.IsAnyCharacterCollectionComplete(splitCollections, catalog),
                Is.False);
            Assert.That(
                AchievementProgress.IsAnyCharacterCollectionComplete(oneCompleteCollection, catalog),
                Is.True);
        }

        [TestCase("gambler_high_roll", new[]
        {
            "class_gambler_attack_wager_dagger",
            "class_gambler_defense_stake_shield",
            "class_gambler_skill_turn_the_table"
        })]
        [TestCase("oracle_rift_engine", new[]
        {
            "class_oracle_attack_constellation_cut",
            "class_oracle_defense_foreseen_barrier",
            "class_oracle_skill_three_door_omen"
        })]
        [TestCase("exile_last_oath", new[]
        {
            "class_exile_attack_chain_execution",
            "class_exile_defense_oath_of_exile",
            "class_exile_skill_brand_purification"
        })]
        public void IsBuildComplete_RequiresAllThreeCharacterCards(
            string buildId,
            string[] requiredCards)
        {
            Assert.That(
                AchievementProgress.IsBuildComplete(buildId, requiredCards.Take(2)),
                Is.False);
            Assert.That(
                AchievementProgress.IsBuildComplete(buildId, requiredCards),
                Is.True);
        }

        [Test]
        public void Complete_IsMonotonicAndUsesProductionCompatibleKey()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            AchievementDefinition definition = AchievementProgress.NewDefinitions[0];
            string key = AchievementProgress.GetCompletionKey(prefix, definition.StorageSuffix);

            try
            {
                Assert.That(AchievementProgress.IsCompleted(prefix, definition), Is.False);
                Assert.That(AchievementProgress.Complete(prefix, definition), Is.True);
                Assert.That(AchievementProgress.Complete(prefix, definition), Is.False);
                Assert.That(AchievementProgress.IsCompleted(prefix, definition), Is.True);
                Assert.That(PlayerPrefs.GetInt(key, 0), Is.EqualTo(1));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        [Test]
        public void PersistentBackfill_DerivesSavedRunEquipmentRecordsAndTitles()
        {
            string prefix = $"ThreeDoorsOfFate.Tests.{Guid.NewGuid():N}.";
            string[] rawKeys =
            {
                prefix + "EndlessRecord.Gambler.Easy",
                prefix + "SurvivorTitle.Gambler",
                prefix + "SurvivorTitle.Oracle",
                prefix + "SurvivorTitle.Exile",
                prefix + "EquippedItems.Gambler",
                prefix + "DiscoveredItems.Gambler",
                prefix + "HardRunSave"
            };
            string[] deckCardIds = new[]
            {
                "class_gambler_attack_wager_dagger",
                "class_gambler_defense_stake_shield",
                "class_gambler_skill_turn_the_table"
            }
                .Concat(Enumerable.Range(0, 47).Select(index => $"filler_{index:00}"))
                .ToArray();

            try
            {
                PlayerPrefs.SetInt(rawKeys[0], 20);
                PlayerPrefs.SetInt(rawKeys[1], 1);
                PlayerPrefs.SetInt(rawKeys[2], 1);
                PlayerPrefs.SetInt(rawKeys[3], 1);
                PlayerPrefs.SetString(
                    rawKeys[4],
                    "{\"itemIds\":[\"relic_test\",\"blessing_test\",\"curse_test\"]}");
                PlayerPrefs.SetString(
                    rawKeys[5],
                    "{\"itemIds\":[\""
                    + string.Join("\",\"", Enumerable.Range(0, 30)
                        .Select(index => $"relic_discovered_{index:00}"))
                    + "\"]}");
                PlayerPrefs.SetString(
                    rawKeys[6],
                    "{\"deckCardIds\":[\""
                    + string.Join("\",\"", deckCardIds)
                    + "\"],\"equippedItemIds\":[],"
                    + "\"buildUpgradeLevels\":[{\"id\":\"gambler_high_roll\",\"level\":2}]}");
                PlayerPrefs.Save();

                Assert.That(
                    AchievementProgress.BackfillPersistentPlayerPrefs(prefix),
                    Is.True);
                Assert.That(
                    AchievementProgress.IsCompleted(
                        prefix,
                        AchievementProgress.SameRerollThree),
                    Is.False,
                    "A saved 50-card deck must not backfill the combat-only reroll achievement.");
                Assert.That(
                    AchievementProgress.IsCompleted(prefix, AchievementProgress.TripleContract),
                    Is.True);
                Assert.That(
                    AchievementProgress.IsCompleted(prefix, AchievementProgress.BuildMasterpiece),
                    Is.True);
                Assert.That(
                    AchievementProgress.IsCompleted(prefix, AchievementProgress.TwentiethDoor),
                    Is.True);
                Assert.That(
                    AchievementProgress.IsCompleted(prefix, AchievementProgress.ThreeSurvivors),
                    Is.True);
                Assert.That(
                    AchievementProgress.IsCompleted(prefix, AchievementProgress.AbyssCollector),
                    Is.True);
                Assert.That(
                    AchievementProgress.IsCompleted(
                        prefix,
                        AchievementProgress.GetDefinitionForBuild("gambler_high_roll")),
                    Is.True);
                Assert.That(
                    AchievementProgress.BackfillPersistentPlayerPrefs(prefix),
                    Is.False,
                    "Backfill must be monotonic and report no duplicate changes.");
            }
            finally
            {
                foreach (string key in rawKeys)
                {
                    PlayerPrefs.DeleteKey(key);
                }

                foreach (string key in AchievementProgress.GetCompletionKeys(prefix))
                {
                    PlayerPrefs.DeleteKey(key);
                }

                PlayerPrefs.Save();
            }
        }
    }
}
