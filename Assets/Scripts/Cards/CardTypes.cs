namespace ThreeDoorsOfFate.Cards
{
    public enum CharacterClass
    {
        Any,
        Gambler,
        Oracle,
        Exile
    }

    public enum CardCategory
    {
        Attack,
        Defense,
        Skill,
        Curse
    }

    public enum CardRarity
    {
        Common,
        Rare,
        Curse
    }

    public enum CardSource
    {
        Starter,
        CombatReward,
        ShopOnly,
        EventOnly,
        BossReward,
        Curse,
        HardReward
    }

    public enum BuildTag
    {
        Attack,
        Defense,
        Dice,
        Debt,
        Prophecy,
        DeckControl,
        Curse,
        LowHealth,
        Gold,
        Door,
        Sustain
    }

    public enum CardTarget
    {
        None,
        Self,
        SingleEnemy,
        AllEnemies,
        Door
    }

    public enum CardEffectTiming
    {
        OnPlay,
        OnTurnStart,
        OnTurnEnd,
        Passive
    }

    public enum CardEffectType
    {
        DealDamage,
        GainBlock,
        Heal,
        DrawCards,
        DiscardCards,
        RerollLuck,
        KeepLuckNextTurn,
        GainAction,
        LoseHealth,
        ApplyBleed,
        ApplyVulnerable,
        RevealDoorEffect,
        GainGold,
        AddCurse,
        RemoveCurse,
        ReflectDamage,
        PreventDeathThisTurn,
        ChangeLuck,
        StoreLuck,
        ConditionalBonusDamage,
        ConditionalBonusBlock,
        RepeatDamageByLuck,
        ReduceCurseDamage,
        RetainBlockNextTurn,
        ReduceNextDamage
    }

    public enum CardConditionType
    {
        None,
        LuckAtLeast,
        LuckAtMost,
        LuckIsOdd,
        EnemyIntentIsAttack,
        PlayerHealthAtOrBelowPercent,
        EnemyHealthAtOrBelowPercent,
        OncePerCombat,
        EnemyHasBlock,
        EnemyHasBleed,
        PlayerHasDebt
    }
}
