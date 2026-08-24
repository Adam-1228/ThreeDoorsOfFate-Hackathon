using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using UnityEngine;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const float NormalStandardEnemyHealthMultiplier = 0.86f;
        private const float NormalStandardEnemyAttackMultiplier = 0.90f;
        private const float NormalStandardEnemyBlockMultiplier = 0.90f;
        private const int NormalStandardEnemyRegenerationCap = 4;

        private bool ShouldApplyNormalStandardEnemyRetune(bool isBoss)
        {
            return currentDifficulty == RunDifficulty.Normal
                && !endlessModeActive
                && !isBoss;
        }

        private void ApplyNormalStandardEnemyRetune(
            bool isBoss,
            ref float healthScale,
            ref float attackScale,
            ref float blockScale)
        {
            if (!ShouldApplyNormalStandardEnemyRetune(isBoss))
            {
                return;
            }

            healthScale *= NormalStandardEnemyHealthMultiplier;
            attackScale *= NormalStandardEnemyAttackMultiplier;
            blockScale *= NormalStandardEnemyBlockMultiplier;
        }

        private int GetEnemyRegenerationAmount(EnemyState state)
        {
            if (state == null)
            {
                return 0;
            }

            int amount = Mathf.Max(4, state.MaxHealth / 12);
            return ShouldApplyNormalStandardEnemyRetune(state.IsBoss)
                ? Mathf.Min(NormalStandardEnemyRegenerationCap, amount)
                : amount;
        }

        private void EnsureAttackOffer(
            List<CardData> offers,
            IEnumerable<CardSource> sources)
        {
            if (offers == null
                || offers.Count != 3
                || offers.Any(card => card != null
                    && card.Category == CardCategory.Attack))
            {
                return;
            }

            HashSet<CardSource> allowedSources = new(sources);
            List<CardData> eligibleAttacks = cardPool
                .Where(card => card != null
                    && card.Category == CardCategory.Attack
                    && card.Rarity != CardRarity.Rare
                    && card.Rarity != CardRarity.Curse
                    && card.Source != CardSource.HardReward
                    && IsCardEligible(card, allowedSources)
                    && offers.All(offer => offer == null
                        || offer.CardId != card.CardId))
                .ToList();
            CardData guaranteedAttack = PickWeightedCard(
                eligibleAttacks,
                offers);
            if (guaranteedAttack == null)
            {
                return;
            }

            int replacementIndex = offers.FindLastIndex(card =>
                card != null
                && card.Source != CardSource.HardReward);
            if (replacementIndex < 0)
            {
                replacementIndex = offers.Count - 1;
            }

            offers[replacementIndex] = guaranteedAttack;
        }
    }
}
