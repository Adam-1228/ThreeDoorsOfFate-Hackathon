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
        public void NewDefinitions_UseStableIdsAndLeaveFuturePointCapacity()
        {
            IReadOnlyList<AchievementDefinition> definitions =
                AchievementProgress.NewDefinitions;

            Assert.That(definitions.Count, Is.EqualTo(4));
            Assert.That(definitions.Select(definition => definition.GameCenterId), Is.EquivalentTo(
                new[]
                {
                    "com.adam.threedoorsfate.achievement.abyss_collector",
                    "com.adam.threedoorsfate.achievement.build.gambler_high_roll",
                    "com.adam.threedoorsfate.achievement.build.oracle_rift_engine",
                    "com.adam.threedoorsfate.achievement.build.exile_last_oath"
                }));
            Assert.That(definitions.All(definition => definition.Points == 100), Is.True);
            Assert.That(definitions.Sum(definition => definition.Points) + 400, Is.EqualTo(800));
            Assert.That(definitions.All(definition => !string.IsNullOrWhiteSpace(definition.ImageResourcePath)), Is.True);
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
    }
}
