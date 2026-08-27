using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using UnityEngine;

namespace ThreeDoorsOfFate.Game.V140
{
    [Serializable]
    internal sealed class StarterContractCatalogData
    {
        public int schemaVersion;
        public StarterBaseDeckDefinition[] baseDecks = Array.Empty<StarterBaseDeckDefinition>();
        public StarterContractDefinition[] contracts = Array.Empty<StarterContractDefinition>();
    }

    [Serializable]
    public sealed class StarterCardCountDefinition
    {
        [SerializeField] private string cardId = string.Empty;
        [SerializeField] private int count;

        public string CardId => cardId;
        public int Count => count;
    }

    [Serializable]
    public sealed class StarterBaseDeckDefinition
    {
        [SerializeField] private string characterClass = string.Empty;
        [SerializeField] private StarterCardCountDefinition[] cards =
            Array.Empty<StarterCardCountDefinition>();

        public string CharacterClassName => characterClass;
        public IReadOnlyList<StarterCardCountDefinition> Cards => cards;
    }

    [Serializable]
    public sealed class StarterCardSwapDefinition
    {
        [SerializeField] private string removeCardId = string.Empty;
        [SerializeField] private string addCardId = string.Empty;
        [SerializeField] private int count;

        public string RemoveCardId => removeCardId;
        public string AddCardId => addCardId;
        public int Count => count;
    }

    [Serializable]
    public sealed class StarterContractDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string characterClass = string.Empty;
        [SerializeField] private string nameKey = string.Empty;
        [SerializeField] private string roleKey = string.Empty;
        [SerializeField] private string descriptionKey = string.Empty;
        [SerializeField] private StarterCardSwapDefinition[] swaps =
            Array.Empty<StarterCardSwapDefinition>();
        [SerializeField] private int startingGoldDelta;
        [SerializeField] private int startingHealthDelta;
        [SerializeField] private int startingLuckDelta;
        [SerializeField] private int startingDebtDelta;

        public string Id => id;
        public string CharacterClassName => characterClass;
        public string NameKey => nameKey;
        public string RoleKey => roleKey;
        public string DescriptionKey => descriptionKey;
        public IReadOnlyList<StarterCardSwapDefinition> Swaps => swaps;
        public int StartingGoldDelta => startingGoldDelta;
        public int StartingHealthDelta => startingHealthDelta;
        public int StartingLuckDelta => startingLuckDelta;
        public int StartingDebtDelta => startingDebtDelta;
    }

    public sealed class StarterContractCatalog
    {
        private readonly IReadOnlyDictionary<CharacterClass, StarterBaseDeckDefinition> baseDecks;
        private readonly IReadOnlyDictionary<string, StarterContractDefinition> contracts;

        private StarterContractCatalog(StarterContractCatalogData data)
        {
            SchemaVersion = data.schemaVersion;
            baseDecks = data.baseDecks.ToDictionary(
                definition => ParseCharacterClass(definition.CharacterClassName),
                definition => definition);
            contracts = data.contracts.ToDictionary(
                definition => definition.Id,
                definition => definition,
                StringComparer.Ordinal);
        }

        public int SchemaVersion { get; }

        public static StarterContractCatalog Load(TextAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            StarterContractCatalogData data;
            try
            {
                data = JsonUtility.FromJson<StarterContractCatalogData>(source.text);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Starter contract catalog is not valid JSON.",
                    exception);
            }

            Validate(data);
            return new StarterContractCatalog(data);
        }

        public StarterBaseDeckDefinition GetBaseDeck(CharacterClass characterClass)
        {
            if (!baseDecks.TryGetValue(characterClass, out StarterBaseDeckDefinition definition))
            {
                throw new InvalidOperationException(
                    $"No base deck is defined for {characterClass}.");
            }

            return definition;
        }

        public IReadOnlyList<StarterContractDefinition> GetContracts(
            CharacterClass characterClass)
        {
            string className = characterClass.ToString();
            return contracts.Values
                .Where(definition => string.Equals(
                    definition.CharacterClassName,
                    className,
                    StringComparison.Ordinal))
                .OrderBy(definition => definition.Id, StringComparer.Ordinal)
                .ToArray();
        }

        public StarterContractDefinition GetContract(string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId)
                || !contracts.TryGetValue(contractId, out StarterContractDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Unknown starter contract: {contractId ?? string.Empty}");
            }

            return definition;
        }

        private static void Validate(StarterContractCatalogData data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Starter contract catalog is empty.");
            }

            if (data.schemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported starter contract schema: {data.schemaVersion}.");
            }

            data.baseDecks ??= Array.Empty<StarterBaseDeckDefinition>();
            data.contracts ??= Array.Empty<StarterContractDefinition>();
            if (data.baseDecks.Length != 3)
            {
                throw new InvalidOperationException("Exactly three base decks are required.");
            }

            HashSet<CharacterClass> baseClasses = new();
            foreach (StarterBaseDeckDefinition baseDeck in data.baseDecks)
            {
                if (baseDeck == null)
                {
                    throw new InvalidOperationException("A base deck entry is null.");
                }

                CharacterClass characterClass = ParseCharacterClass(
                    baseDeck.CharacterClassName);
                if (characterClass == CharacterClass.Any || !baseClasses.Add(characterClass))
                {
                    throw new InvalidOperationException(
                        $"Duplicate or invalid base deck class: {baseDeck.CharacterClassName}.");
                }

                if (baseDeck.Cards == null || baseDeck.Cards.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Base deck {baseDeck.CharacterClassName} has no cards.");
                }

                HashSet<string> cardIds = new(StringComparer.Ordinal);
                int total = 0;
                foreach (StarterCardCountDefinition card in baseDeck.Cards)
                {
                    if (card == null
                        || string.IsNullOrWhiteSpace(card.CardId)
                        || card.Count <= 0
                        || !cardIds.Add(card.CardId))
                    {
                        throw new InvalidOperationException(
                            $"Base deck {baseDeck.CharacterClassName} has an invalid card entry.");
                    }

                    total += card.Count;
                }

                if (total != 24)
                {
                    throw new InvalidOperationException(
                        $"Base deck {baseDeck.CharacterClassName} must contain 24 cards.");
                }
            }

            HashSet<string> contractIds = new(StringComparer.Ordinal);
            Dictionary<CharacterClass, int> contractCounts = new();
            foreach (StarterContractDefinition contract in data.contracts)
            {
                if (contract == null
                    || string.IsNullOrWhiteSpace(contract.Id)
                    || !contractIds.Add(contract.Id))
                {
                    throw new InvalidOperationException(
                        "Starter contract IDs must be present and unique.");
                }

                CharacterClass characterClass = ParseCharacterClass(
                    contract.CharacterClassName);
                if (characterClass == CharacterClass.Any || !baseClasses.Contains(characterClass))
                {
                    throw new InvalidOperationException(
                        $"Contract {contract.Id} has an invalid class.");
                }

                if (string.IsNullOrWhiteSpace(contract.NameKey)
                    || string.IsNullOrWhiteSpace(contract.RoleKey)
                    || string.IsNullOrWhiteSpace(contract.DescriptionKey))
                {
                    throw new InvalidOperationException(
                        $"Contract {contract.Id} is missing localization keys.");
                }

                if (contract.StartingGoldDelta < -10 || contract.StartingGoldDelta > 10
                    || contract.StartingHealthDelta < -6 || contract.StartingHealthDelta > 6
                    || contract.StartingLuckDelta < -1 || contract.StartingLuckDelta > 1
                    || contract.StartingDebtDelta < -1 || contract.StartingDebtDelta > 1
                    || Math.Abs(contract.StartingLuckDelta)
                        + Math.Abs(contract.StartingDebtDelta) > 1)
                {
                    throw new InvalidOperationException(
                        $"Contract {contract.Id} exceeds its resource limits.");
                }

                int swappedCards = 0;
                foreach (StarterCardSwapDefinition swap in contract.Swaps
                    ?? Array.Empty<StarterCardSwapDefinition>())
                {
                    if (swap == null
                        || string.IsNullOrWhiteSpace(swap.RemoveCardId)
                        || string.IsNullOrWhiteSpace(swap.AddCardId)
                        || swap.Count <= 0
                        || string.Equals(
                            swap.RemoveCardId,
                            swap.AddCardId,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Contract {contract.Id} has an invalid card swap.");
                    }

                    swappedCards += swap.Count;
                }

                if (swappedCards > 4)
                {
                    throw new InvalidOperationException(
                        $"Contract {contract.Id} swaps more than four cards.");
                }

                contractCounts.TryGetValue(characterClass, out int count);
                contractCounts[characterClass] = count + 1;
            }

            foreach (CharacterClass characterClass in baseClasses)
            {
                if (!contractCounts.TryGetValue(characterClass, out int count) || count != 3)
                {
                    throw new InvalidOperationException(
                        $"Exactly three contracts are required for {characterClass}.");
                }
            }
        }

        private static CharacterClass ParseCharacterClass(string value)
        {
            if (!Enum.TryParse(value, false, out CharacterClass characterClass))
            {
                throw new InvalidOperationException(
                    $"Unknown character class: {value ?? string.Empty}.");
            }

            return characterClass;
        }
    }
}
