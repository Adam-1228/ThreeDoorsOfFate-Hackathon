using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThreeDoorsOfFate.Game.V140
{
    public enum EndlessMutationEffectType
    {
        EnemyAttackMultiplier,
        CombatGoldMultiplier,
        EnemyBlockMultiplier,
        RareCardWeightMultiplier,
        RestHealingMultiplier,
        RemovalCostMultiplier,
        DebtGainBonus,
        DoorInsightBonus,
        ShopPriceMultiplier,
        ShopOfferBonus,
        OpeningHandPenalty,
        FirstTurnActionBonus
    }

    [Serializable]
    internal sealed class EndlessMutationCatalogData
    {
        public int schemaVersion;
        public EndlessMutationDefinition[] mutations =
            Array.Empty<EndlessMutationDefinition>();
    }

    [Serializable]
    public sealed class EndlessMutationEffectDefinition
    {
        [SerializeField] private string type = string.Empty;
        [SerializeField] private float value;
        [SerializeField] private float minimum;
        [SerializeField] private float maximum;

        public EndlessMutationEffectType Type =>
            Enum.Parse<EndlessMutationEffectType>(type, false);
        public float Value => value;
        public float Minimum => minimum;
        public float Maximum => maximum;
        public float ClampedValue => Mathf.Clamp(value, minimum, maximum);
        internal string TypeName => type;
    }

    [Serializable]
    public sealed class EndlessMutationDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string nameKey = string.Empty;
        [SerializeField] private string riskKey = string.Empty;
        [SerializeField] private string rewardKey = string.Empty;
        [SerializeField] private EndlessMutationEffectDefinition[] risks =
            Array.Empty<EndlessMutationEffectDefinition>();
        [SerializeField] private EndlessMutationEffectDefinition[] rewards =
            Array.Empty<EndlessMutationEffectDefinition>();

        public string Id => id;
        public string NameKey => nameKey;
        public string RiskKey => riskKey;
        public string RewardKey => rewardKey;
        public IReadOnlyList<EndlessMutationEffectDefinition> Risks => risks;
        public IReadOnlyList<EndlessMutationEffectDefinition> Rewards => rewards;

        public IEnumerable<EndlessMutationEffectDefinition> AllEffects =>
            (risks ?? Array.Empty<EndlessMutationEffectDefinition>())
                .Concat(rewards ?? Array.Empty<EndlessMutationEffectDefinition>());
    }

    public sealed class EndlessMutationCatalog
    {
        private readonly IReadOnlyDictionary<string, EndlessMutationDefinition> byId;

        private EndlessMutationCatalog(EndlessMutationCatalogData data)
        {
            SchemaVersion = data.schemaVersion;
            Mutations = data.mutations.ToArray();
            byId = Mutations.ToDictionary(
                mutation => mutation.Id,
                mutation => mutation,
                StringComparer.Ordinal);
        }

        public int SchemaVersion { get; }
        public IReadOnlyList<EndlessMutationDefinition> Mutations { get; }

        public static EndlessMutationCatalog Load(TextAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EndlessMutationCatalogData data;
            try
            {
                data = JsonUtility.FromJson<EndlessMutationCatalogData>(source.text);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Endless mutation catalog is not valid JSON.",
                    exception);
            }

            Validate(data);
            return new EndlessMutationCatalog(data);
        }

        public EndlessMutationDefinition Get(string mutationId)
        {
            if (!TryGet(mutationId, out EndlessMutationDefinition mutation))
            {
                throw new InvalidOperationException(
                    $"Unknown endless mutation: {mutationId ?? string.Empty}");
            }

            return mutation;
        }

        public bool TryGet(
            string mutationId,
            out EndlessMutationDefinition mutation)
        {
            return byId.TryGetValue(mutationId ?? string.Empty, out mutation);
        }

        public IReadOnlyList<EndlessMutationDefinition> GetChoices(
            IEnumerable<string> activeIds,
            SeededRunRandom random,
            int count)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (count <= 0)
            {
                return Array.Empty<EndlessMutationDefinition>();
            }

            HashSet<string> active = new(
                activeIds ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            List<EndlessMutationDefinition> available = Mutations
                .Where(mutation => !active.Contains(mutation.Id))
                .ToList();
            List<EndlessMutationDefinition> choices = new(
                Mathf.Min(count, available.Count));
            while (choices.Count < count && available.Count > 0)
            {
                int index = random.Range(0, available.Count);
                choices.Add(available[index]);
                available.RemoveAt(index);
            }

            return choices;
        }

        private static void Validate(EndlessMutationCatalogData data)
        {
            if (data == null)
            {
                throw new InvalidOperationException(
                    "Endless mutation catalog is empty.");
            }

            if (data.schemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported endless mutation schema: {data.schemaVersion}.");
            }

            data.mutations ??= Array.Empty<EndlessMutationDefinition>();
            if (data.mutations.Length != 6)
            {
                throw new InvalidOperationException(
                    "Exactly six endless mutations are required.");
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            HashSet<EndlessMutationEffectType> effectTypes = new();
            foreach (EndlessMutationDefinition mutation in data.mutations)
            {
                if (mutation == null
                    || string.IsNullOrWhiteSpace(mutation.Id)
                    || !ids.Add(mutation.Id)
                    || string.IsNullOrWhiteSpace(mutation.NameKey)
                    || string.IsNullOrWhiteSpace(mutation.RiskKey)
                    || string.IsNullOrWhiteSpace(mutation.RewardKey)
                    || mutation.Risks == null
                    || mutation.Risks.Count != 1
                    || mutation.Rewards == null
                    || mutation.Rewards.Count != 1)
                {
                    throw new InvalidOperationException(
                        "Endless mutations require unique IDs and one risk/reward pair.");
                }

                foreach (EndlessMutationEffectDefinition effect in mutation.AllEffects)
                {
                    if (effect == null
                        || !Enum.TryParse(
                            effect.TypeName,
                            false,
                            out EndlessMutationEffectType effectType)
                        || float.IsNaN(effect.Value)
                        || float.IsInfinity(effect.Value)
                        || effect.Minimum > effect.Maximum)
                    {
                        throw new InvalidOperationException(
                            $"Endless mutation {mutation.Id} has an invalid effect.");
                    }

                    if (!effectTypes.Add(effectType))
                    {
                        throw new InvalidOperationException(
                            $"Endless mutation effect {effectType} is duplicated.");
                    }
                }
            }

            int expectedEffectCount = Enum
                .GetValues(typeof(EndlessMutationEffectType))
                .Length;
            if (effectTypes.Count != expectedEffectCount)
            {
                throw new InvalidOperationException(
                    "Every endless mutation effect type must be represented once.");
            }
        }
    }
}
