using System;
using UnityEngine;

namespace ThreeDoorsOfFate.Cards
{
    [Serializable]
    public sealed class CardEffectDefinition
    {
        [SerializeField] private CardEffectType effectType;
        [SerializeField] private CardEffectTiming timing = CardEffectTiming.OnPlay;
        [SerializeField] private CardConditionType condition = CardConditionType.None;
        [SerializeField] private int amount;
        [SerializeField] private int secondaryAmount;
        [SerializeField, Range(1, 6)] private int luckThreshold = 1;
        [SerializeField, Range(1, 100)] private int percentThreshold = 50;
        [SerializeField] private string note = string.Empty;

        public CardEffectDefinition()
        {
        }

        public CardEffectDefinition(
            CardEffectType effectType,
            int amount,
            string note = "",
            CardEffectTiming timing = CardEffectTiming.OnPlay,
            CardConditionType condition = CardConditionType.None,
            int secondaryAmount = 0,
            int luckThreshold = 1,
            int percentThreshold = 50)
        {
            this.effectType = effectType;
            this.amount = amount;
            this.note = note ?? string.Empty;
            this.timing = timing;
            this.condition = condition;
            this.secondaryAmount = secondaryAmount;
            this.luckThreshold = Mathf.Clamp(luckThreshold, 1, 6);
            this.percentThreshold = Mathf.Clamp(percentThreshold, 1, 100);
        }

        public CardEffectType EffectType => effectType;
        public CardEffectTiming Timing => timing;
        public CardConditionType Condition => condition;
        public int Amount => amount;
        public int SecondaryAmount => secondaryAmount;
        public int LuckThreshold => luckThreshold;
        public int PercentThreshold => percentThreshold;
        public string Note => note;
        public bool HasCondition => condition != CardConditionType.None;
    }
}
