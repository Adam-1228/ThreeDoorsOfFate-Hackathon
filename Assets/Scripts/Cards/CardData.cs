using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThreeDoorsOfFate.Cards
{
    [CreateAssetMenu(menuName = "Three Doors of Fate/Card Data", fileName = "New Card Data")]
    public sealed class CardData : ScriptableObject
    {
        [SerializeField] private string cardId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private string englishName = string.Empty;
        [SerializeField, TextArea(2, 5)] private string rulesText = string.Empty;
        [SerializeField, Min(0)] private int cost;
        [SerializeField] private CardCategory category;
        [SerializeField] private CardRarity rarity = CardRarity.Common;
        [SerializeField] private CardSource source = CardSource.CombatReward;
        [SerializeField] private CharacterClass characterClass = CharacterClass.Any;
        [SerializeField] private CardTarget target = CardTarget.SingleEnemy;
        [SerializeField] private Sprite illustration;
        [SerializeField] private Sprite fullCardSprite;
        [SerializeField, Min(0)] private int minimumRoom;
        [SerializeField, Min(1)] private int shopWeight = 1;
        [SerializeField] private bool exhaustsAfterUse;
        [SerializeField] private bool oncePerCombat;
        [SerializeField] private List<string> tags = new();
        [SerializeField] private List<BuildTag> buildTags = new();
        [SerializeField] private List<CardEffectDefinition> effects = new();

        public string CardId => cardId;
        public string DisplayName => displayName;
        public string EnglishName => englishName;
        public string RulesText => rulesText;
        public int Cost => cost;
        public CardCategory Category => category;
        public CardRarity Rarity => rarity;
        public CardSource Source => source;
        public CharacterClass CharacterClass => characterClass;
        public CardTarget Target => target;
        public Sprite Illustration => illustration;
        public Sprite FullCardSprite => fullCardSprite;
        public int MinimumRoom => minimumRoom;
        public int ShopWeight => shopWeight;
        public bool ExhaustsAfterUse => exhaustsAfterUse;
        public bool OncePerCombat => oncePerCombat;
        public IReadOnlyList<string> Tags => tags;
        public IReadOnlyList<BuildTag> BuildTags => buildTags;
        public IReadOnlyList<CardEffectDefinition> Effects => effects;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string cardId,
            string displayName,
            string englishName,
            string rulesText,
            int cost,
            CardCategory category,
            CardRarity rarity,
            CardSource source,
            CharacterClass characterClass,
            CardTarget target,
            Sprite illustration,
            Sprite fullCardSprite,
            int minimumRoom,
            int shopWeight,
            bool exhaustsAfterUse,
            bool oncePerCombat,
            IReadOnlyList<string> tags,
            IReadOnlyList<BuildTag> buildTags,
            IReadOnlyList<CardEffectDefinition> effects)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException("Card id must be provided.", nameof(cardId));
            }

            this.cardId = cardId.Trim();
            this.displayName = displayName?.Trim() ?? string.Empty;
            this.englishName = englishName?.Trim() ?? string.Empty;
            this.rulesText = rulesText?.Trim() ?? string.Empty;
            this.cost = Mathf.Max(0, cost);
            this.category = category;
            this.rarity = rarity;
            this.source = source;
            this.characterClass = characterClass;
            this.target = target;
            this.illustration = illustration;
            this.fullCardSprite = fullCardSprite;
            this.minimumRoom = Mathf.Max(0, minimumRoom);
            this.shopWeight = Mathf.Max(1, shopWeight);
            this.exhaustsAfterUse = exhaustsAfterUse;
            this.oncePerCombat = oncePerCombat;

            this.tags.Clear();
            if (tags != null)
            {
                this.tags.AddRange(tags);
            }

            this.buildTags.Clear();
            if (buildTags != null)
            {
                this.buildTags.AddRange(buildTags);
            }

            this.effects.Clear();
            if (effects != null)
            {
                this.effects.AddRange(effects);
            }
        }
#endif

        private void OnValidate()
        {
            cost = Mathf.Max(0, cost);
            minimumRoom = Mathf.Max(0, minimumRoom);
            shopWeight = Mathf.Max(1, shopWeight);
            tags ??= new List<string>();
            buildTags ??= new List<BuildTag>();
            effects ??= new List<CardEffectDefinition>();
        }
    }
}
