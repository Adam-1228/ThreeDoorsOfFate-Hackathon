using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThreeDoorsOfFate.Platform;
using UnityEngine;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class RewardedRelicPolicyTests
    {
        private string keyPrefix;

        [SetUp]
        public void SetUp()
        {
            keyPrefix = $"ThreeDoorsOfFate.Tests.RewardedRelic.{Guid.NewGuid():N}.";
        }

        [TearDown]
        public void TearDown()
        {
            DeleteDailyKeys("Gambler");
            DeleteDailyKeys("Oracle");
            DeleteDailyKeys("Exile");
            PlayerPrefs.Save();
        }

        [TestCase(RewardedRelicDifficulty.Easy, RewardedRelicCategory.Relic, true)]
        [TestCase(RewardedRelicDifficulty.Easy, RewardedRelicCategory.Blessing, false)]
        [TestCase(RewardedRelicDifficulty.Easy, RewardedRelicCategory.Curse, false)]
        [TestCase(RewardedRelicDifficulty.Normal, RewardedRelicCategory.Relic, true)]
        [TestCase(RewardedRelicDifficulty.Normal, RewardedRelicCategory.Blessing, true)]
        [TestCase(RewardedRelicDifficulty.Normal, RewardedRelicCategory.Curse, false)]
        [TestCase(RewardedRelicDifficulty.Hard, RewardedRelicCategory.Relic, true)]
        [TestCase(RewardedRelicDifficulty.Hard, RewardedRelicCategory.Blessing, true)]
        [TestCase(RewardedRelicDifficulty.Hard, RewardedRelicCategory.Curse, true)]
        public void IsCategoryEligible_UsesCumulativeDifficultyPool(
            RewardedRelicDifficulty difficulty,
            RewardedRelicCategory category,
            bool expected)
        {
            Assert.That(
                RewardedRelicPolicy.IsCategoryEligible(difficulty, category),
                Is.EqualTo(expected));
        }

        [Test]
        public void GetEligibleUndiscovered_ExcludesDiscoveredAndDisallowedItems()
        {
            RewardedRelicCandidate[] catalog =
            {
                new("relic-a", RewardedRelicCategory.Relic),
                new("relic-b", RewardedRelicCategory.Relic),
                new("blessing-a", RewardedRelicCategory.Blessing),
                new("curse-a", RewardedRelicCategory.Curse)
            };
            HashSet<string> discovered = new(StringComparer.Ordinal) { "relic-a" };

            string[] ids = RewardedRelicPolicy
                .GetEligibleUndiscovered(
                    RewardedRelicDifficulty.Normal,
                    catalog,
                    discovered)
                .Select(candidate => candidate.ItemId)
                .ToArray();

            Assert.That(ids, Is.EqualTo(new[] { "relic-b", "blessing-a" }));
        }

        [Test]
        public void GetEligibleUndiscovered_RemovesDuplicateAndBlankIds()
        {
            RewardedRelicCandidate[] catalog =
            {
                new("relic-a", RewardedRelicCategory.Relic),
                new("relic-a", RewardedRelicCategory.Relic),
                new(string.Empty, RewardedRelicCategory.Relic)
            };

            IReadOnlyList<RewardedRelicCandidate> candidates =
                RewardedRelicPolicy.GetEligibleUndiscovered(
                    RewardedRelicDifficulty.Hard,
                    catalog,
                    new HashSet<string>(StringComparer.Ordinal));

            Assert.That(candidates.Select(candidate => candidate.ItemId),
                Is.EqualTo(new[] { "relic-a" }));
        }

        [Test]
        public void DailyLimit_StopsAfterThreeSuccessfulConsumes()
        {
            DateTimeOffset now =
                new(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(9));

            Assert.That(TryConsume("Gambler", now), Is.True);
            Assert.That(TryConsume("Gambler", now), Is.True);
            Assert.That(TryConsume("Gambler", now), Is.True);
            Assert.That(TryConsume("Gambler", now), Is.False);

            RewardedRelicDailyStatus status =
                RewardedRelicDailyLimitStore.GetStatus(keyPrefix, "Gambler", now);
            Assert.That(status.UsedCount, Is.EqualTo(3));
            Assert.That(status.RemainingCount, Is.Zero);
        }

        [Test]
        public void DailyLimit_IsIndependentPerCharacter()
        {
            DateTimeOffset now =
                new(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(9));
            Assert.That(TryConsume("Gambler", now), Is.True);
            Assert.That(TryConsume("Gambler", now), Is.True);

            RewardedRelicDailyStatus oracle =
                RewardedRelicDailyLimitStore.GetStatus(keyPrefix, "Oracle", now);

            Assert.That(oracle.UsedCount, Is.Zero);
            Assert.That(oracle.RemainingCount, Is.EqualTo(3));
        }

        [Test]
        public void DailyLimit_ForwardLocalDayResetsCount()
        {
            DateTimeOffset dayOne =
                new(2026, 8, 4, 23, 50, 0, TimeSpan.FromHours(9));
            DateTimeOffset dayTwo = dayOne.AddMinutes(20);
            Assert.That(TryConsume("Gambler", dayOne), Is.True);
            Assert.That(TryConsume("Gambler", dayOne), Is.True);

            RewardedRelicDailyStatus status =
                RewardedRelicDailyLimitStore.GetStatus(keyPrefix, "Gambler", dayTwo);

            Assert.That(status.AcceptedLocalDate, Is.EqualTo("2026-08-05"));
            Assert.That(status.UsedCount, Is.Zero);
            Assert.That(status.RemainingCount, Is.EqualTo(3));
            Assert.That(status.ClockRollbackDetected, Is.False);
        }

        [Test]
        public void DailyLimit_BackwardClockDoesNotRestoreAllowance()
        {
            DateTimeOffset dayOne =
                new(2026, 8, 4, 10, 0, 0, TimeSpan.FromHours(9));
            DateTimeOffset dayTwo = dayOne.AddDays(1);
            Assert.That(TryConsume("Gambler", dayOne), Is.True);
            Assert.That(
                RewardedRelicDailyLimitStore.GetStatus(
                    keyPrefix,
                    "Gambler",
                    dayTwo).RemainingCount,
                Is.EqualTo(3));
            Assert.That(TryConsume("Gambler", dayTwo), Is.True);

            RewardedRelicDailyStatus rolledBack =
                RewardedRelicDailyLimitStore.GetStatus(
                    keyPrefix,
                    "Gambler",
                    dayOne);

            Assert.That(rolledBack.AcceptedLocalDate, Is.EqualTo("2026-08-05"));
            Assert.That(rolledBack.UsedCount, Is.EqualTo(1));
            Assert.That(rolledBack.RemainingCount, Is.EqualTo(2));
            Assert.That(rolledBack.ClockRollbackDetected, Is.True);
        }

        private bool TryConsume(string characterId, DateTimeOffset now)
        {
            return RewardedRelicDailyLimitStore.TryConsume(
                keyPrefix,
                characterId,
                now,
                out _);
        }

        private void DeleteDailyKeys(string characterId)
        {
            string prefix =
                $"{keyPrefix}Ads.RewardedRelic.{characterId}.";
            PlayerPrefs.DeleteKey(prefix + "Date");
            PlayerPrefs.DeleteKey(prefix + "Count");
            PlayerPrefs.DeleteKey(prefix + "GreatestObservedUtc");
        }
    }
}
