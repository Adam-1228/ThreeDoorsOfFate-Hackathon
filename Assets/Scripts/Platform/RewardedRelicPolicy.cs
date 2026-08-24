using System;
using System.Collections.Generic;
using System.Linq;

namespace ThreeDoorsOfFate.Platform
{
    public enum RewardedRelicDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum RewardedRelicCategory
    {
        Relic,
        Blessing,
        Curse
    }

    public readonly struct RewardedRelicCandidate
    {
        public RewardedRelicCandidate(string itemId, RewardedRelicCategory category)
        {
            ItemId = itemId ?? string.Empty;
            Category = category;
        }

        public string ItemId { get; }
        public RewardedRelicCategory Category { get; }
    }

    public static class RewardedRelicPolicy
    {
        public static bool IsCategoryEligible(
            RewardedRelicDifficulty difficulty,
            RewardedRelicCategory category)
        {
            return difficulty >= RewardedRelicDifficulty.Easy
                && difficulty <= RewardedRelicDifficulty.Hard
                && category >= RewardedRelicCategory.Relic
                && category <= RewardedRelicCategory.Curse
                && (int)category <= (int)difficulty;
        }

        public static IReadOnlyList<RewardedRelicCandidate> GetEligibleUndiscovered(
            RewardedRelicDifficulty difficulty,
            IEnumerable<RewardedRelicCandidate> candidates,
            ISet<string> discoveredIds)
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            return (candidates ?? Array.Empty<RewardedRelicCandidate>())
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ItemId))
                .Where(candidate => IsCategoryEligible(difficulty, candidate.Category))
                .Where(candidate => discoveredIds == null
                    || !discoveredIds.Contains(candidate.ItemId))
                .Where(candidate => seen.Add(candidate.ItemId))
                .ToArray();
        }
    }
}
