using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;

namespace ThreeDoorsOfFate.Game.V140
{
    public sealed class StarterDeckBuilder
    {
        private readonly StarterContractCatalog catalog;

        public StarterDeckBuilder(StarterContractCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public List<CardData> Build(
            CharacterClass characterClass,
            string contractId,
            IReadOnlyDictionary<string, CardData> cards)
        {
            if (characterClass == CharacterClass.Any)
            {
                throw new InvalidOperationException("A playable character class is required.");
            }

            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            StarterBaseDeckDefinition baseDeck = catalog.GetBaseDeck(characterClass);
            StarterContractDefinition contract = catalog.GetContract(contractId);
            if (!string.Equals(
                    contract.CharacterClassName,
                    characterClass.ToString(),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Contract {contractId} does not belong to {characterClass}.");
            }

            List<CardData> deck = new(24);
            foreach (StarterCardCountDefinition entry in baseDeck.Cards)
            {
                CardData card = RequireLegalCard(entry.CardId, characterClass, cards);
                for (int index = 0; index < entry.Count; index += 1)
                {
                    deck.Add(card);
                }
            }

            foreach (StarterCardSwapDefinition swap in contract.Swaps)
            {
                CardData removedCard = RequireLegalCard(
                    swap.RemoveCardId,
                    characterClass,
                    cards);
                CardData addedCard = RequireLegalCard(
                    swap.AddCardId,
                    characterClass,
                    cards);
                if (removedCard.Category != addedCard.Category)
                {
                    throw new InvalidOperationException(
                        $"Contract {contractId} changes the starter category balance.");
                }

                for (int index = 0; index < swap.Count; index += 1)
                {
                    int removalIndex = deck.FindIndex(card =>
                        string.Equals(
                            card.CardId,
                            removedCard.CardId,
                            StringComparison.Ordinal));
                    if (removalIndex < 0)
                    {
                        throw new InvalidOperationException(
                            $"Contract {contractId} cannot remove {removedCard.CardId}.");
                    }

                    deck[removalIndex] = addedCard;
                }
            }

            ValidateFinalDeck(deck, characterClass, contractId);
            return deck;
        }

        private static CardData RequireLegalCard(
            string cardId,
            CharacterClass characterClass,
            IReadOnlyDictionary<string, CardData> cards)
        {
            if (string.IsNullOrWhiteSpace(cardId)
                || !cards.TryGetValue(cardId, out CardData card)
                || card == null)
            {
                throw new InvalidOperationException(
                    $"Starter deck references missing card {cardId ?? string.Empty}.");
            }

            if (!string.Equals(card.CardId, cardId, StringComparison.Ordinal)
                || card.Rarity != CardRarity.Common
                || card.Category == CardCategory.Curse
                || card.Source == CardSource.HardReward
                || cardId.StartsWith("hard_", StringComparison.Ordinal)
                || (card.CharacterClass != CharacterClass.Any
                    && card.CharacterClass != characterClass))
            {
                throw new InvalidOperationException(
                    $"Starter deck card is not legal for {characterClass}: {cardId}.");
            }

            return card;
        }

        private static void ValidateFinalDeck(
            IReadOnlyList<CardData> deck,
            CharacterClass characterClass,
            string contractId)
        {
            if (deck.Count != 24)
            {
                throw new InvalidOperationException(
                    $"Contract {contractId} produced {deck.Count} cards instead of 24.");
            }

            int attacks = deck.Count(card => card.Category == CardCategory.Attack);
            int defenses = deck.Count(card => card.Category == CardCategory.Defense);
            int skills = deck.Count(card => card.Category == CardCategory.Skill);
            int classCards = deck.Count(card => card.CharacterClass == characterClass);
            if (attacks != 10 || defenses != 8 || skills != 6 || classCards < 4)
            {
                throw new InvalidOperationException(
                    $"Contract {contractId} produced an invalid 10/8/6 starter deck.");
            }
        }
    }
}
