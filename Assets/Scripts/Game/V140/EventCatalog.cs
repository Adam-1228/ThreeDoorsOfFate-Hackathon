using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using UnityEngine;

namespace ThreeDoorsOfFate.Game.V140
{
    public enum EventEffectType
    {
        Health,
        MaxHealth,
        Gold,
        Debt,
        AddCard,
        RemoveCard,
        DoorInsight,
        ItemDiscovery
    }

    [Serializable]
    internal sealed class EventCatalogData
    {
        public int schemaVersion;
        public EventDefinition[] events = Array.Empty<EventDefinition>();
    }

    [Serializable]
    public sealed class EventEffectDefinition
    {
        [SerializeField] private string type = string.Empty;
        [SerializeField] private int amount;
        [SerializeField] private string cardId = string.Empty;
        [SerializeField] private string itemId = string.Empty;

        public EventEffectType Type => Enum.Parse<EventEffectType>(type, false);
        public int Amount => amount;
        public string CardId => cardId;
        public string ItemId => itemId;
        internal string TypeName => type;
    }

    [Serializable]
    public sealed class EventChoiceDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string labelKey = string.Empty;
        [SerializeField] private string previewKey = string.Empty;
        [SerializeField] private EventEffectDefinition[] effects =
            Array.Empty<EventEffectDefinition>();

        public string Id => id;
        public string LabelKey => labelKey;
        public string PreviewKey => previewKey;
        public IReadOnlyList<EventEffectDefinition> Effects => effects;
    }

    [Serializable]
    public sealed class EventDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string titleKey = string.Empty;
        [SerializeField] private string bodyKey = string.Empty;
        [SerializeField] private string requiredClass = string.Empty;
        [SerializeField] private int minimumDebt;
        [SerializeField] private bool oncePerTenDoors;
        [SerializeField] private EventChoiceDefinition[] choices =
            Array.Empty<EventChoiceDefinition>();

        public string Id => id;
        public string TitleKey => titleKey;
        public string BodyKey => bodyKey;
        public string RequiredClassName => requiredClass;
        public int MinimumDebt => minimumDebt;
        public bool OncePerTenDoors => oncePerTenDoors;
        public IReadOnlyList<EventChoiceDefinition> Choices => choices;

        public bool IsEligible(
            CharacterClass characterClass,
            int currentDebt,
            IReadOnlyCollection<string> seenIds)
        {
            CharacterClass required = ParseCharacterClass(requiredClass);
            return (required == CharacterClass.Any || required == characterClass)
                && currentDebt >= minimumDebt
                && (!oncePerTenDoors
                    || seenIds == null
                    || !seenIds.Contains(id));
        }

        internal static CharacterClass ParseCharacterClass(string value)
        {
            if (!Enum.TryParse(value, false, out CharacterClass parsed))
            {
                throw new InvalidOperationException(
                    $"Unknown event character class: {value ?? string.Empty}");
            }

            return parsed;
        }
    }

    public sealed class EventCatalog
    {
        private readonly IReadOnlyDictionary<string, EventDefinition> byId;

        private EventCatalog(EventCatalogData data)
        {
            SchemaVersion = data.schemaVersion;
            Events = data.events.ToArray();
            byId = Events.ToDictionary(
                definition => definition.Id,
                definition => definition,
                StringComparer.Ordinal);
        }

        public int SchemaVersion { get; }
        public IReadOnlyList<EventDefinition> Events { get; }

        public static EventCatalog Load(TextAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EventCatalogData data;
            try
            {
                data = JsonUtility.FromJson<EventCatalogData>(source.text);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Event catalog is not valid JSON.",
                    exception);
            }

            Validate(data);
            return new EventCatalog(data);
        }

        public EventDefinition Get(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)
                || !byId.TryGetValue(eventId, out EventDefinition definition))
            {
                throw new InvalidOperationException(
                    $"Unknown event: {eventId ?? string.Empty}");
            }

            return definition;
        }

        public EventDefinition GetSafeFallback()
        {
            return Events.First(definition =>
                definition.RequiredClassName == CharacterClass.Any.ToString()
                && definition.MinimumDebt == 0);
        }

        private static void Validate(EventCatalogData data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Event catalog is empty.");
            }

            if (data.schemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported event catalog schema: {data.schemaVersion}.");
            }

            data.events ??= Array.Empty<EventDefinition>();
            if (data.events.Length != 8)
            {
                throw new InvalidOperationException("Exactly eight events are required.");
            }

            HashSet<string> eventIds = new(StringComparer.Ordinal);
            foreach (EventDefinition definition in data.events)
            {
                if (definition == null
                    || string.IsNullOrWhiteSpace(definition.Id)
                    || !eventIds.Add(definition.Id))
                {
                    throw new InvalidOperationException(
                        "Event IDs must be present and unique.");
                }

                EventDefinition.ParseCharacterClass(definition.RequiredClassName);
                if (string.IsNullOrWhiteSpace(definition.TitleKey)
                    || string.IsNullOrWhiteSpace(definition.BodyKey)
                    || definition.MinimumDebt < 0
                    || definition.Choices == null
                    || definition.Choices.Count < 2
                    || definition.Choices.Count > 3)
                {
                    throw new InvalidOperationException(
                        $"Event {definition.Id} has invalid metadata.");
                }

                ValidateChoices(definition);
            }
        }

        private static void ValidateChoices(EventDefinition definition)
        {
            HashSet<string> choiceIds = new(StringComparer.Ordinal);
            foreach (EventChoiceDefinition choice in definition.Choices)
            {
                if (choice == null
                    || string.IsNullOrWhiteSpace(choice.Id)
                    || !choiceIds.Add(choice.Id)
                    || string.IsNullOrWhiteSpace(choice.LabelKey)
                    || string.IsNullOrWhiteSpace(choice.PreviewKey)
                    || choice.Effects == null
                    || choice.Effects.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Event {definition.Id} has an invalid choice.");
                }

                foreach (EventEffectDefinition effect in choice.Effects)
                {
                    ValidateEffect(definition.Id, choice.Id, effect);
                }
            }
        }

        private static void ValidateEffect(
            string eventId,
            string choiceId,
            EventEffectDefinition effect)
        {
            if (effect == null
                || !Enum.TryParse(
                    effect.TypeName,
                    false,
                    out EventEffectType effectType))
            {
                throw new InvalidOperationException(
                    $"Event {eventId}/{choiceId} contains an unknown effect.");
            }

            bool valid = effectType switch
            {
                EventEffectType.Health => effect.Amount is >= -100 and <= 100,
                EventEffectType.MaxHealth => effect.Amount is >= -40 and <= 40,
                EventEffectType.Gold => effect.Amount is >= -500 and <= 500,
                EventEffectType.Debt => effect.Amount is >= -10 and <= 10,
                EventEffectType.DoorInsight => effect.Amount is >= -3 and <= 3,
                EventEffectType.AddCard => effect.Amount == 1
                    && !string.IsNullOrWhiteSpace(effect.CardId),
                EventEffectType.RemoveCard => effect.Amount == 1,
                EventEffectType.ItemDiscovery => effect.Amount == 1,
                _ => false
            };
            if (!valid)
            {
                throw new InvalidOperationException(
                    $"Event {eventId}/{choiceId} has invalid {effectType} data.");
            }
        }
    }
}
