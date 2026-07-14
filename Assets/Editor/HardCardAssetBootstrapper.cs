using System.Collections.Generic;
using ThreeDoorsOfFate.Cards;
using UnityEditor;
using UnityEngine;

namespace ThreeDoorsOfFate.Editor
{
    public static class HardCardAssetBootstrapper
    {
        private const string CardDataRoot = "Assets/Data/Cards/MVP";
        private const string RenderedRoot = "Assets/Art/Cards/HardRendered";

        private static readonly HardCardConfig[] Cards =
        {
            Attack(
                "hard_attack_abyss_cleave",
                "심연 절단",
                "피해 18. 취약 1. 적 체력이 절반 이하면 추가 피해 6.",
                2,
                new[] { BuildTag.Attack, BuildTag.Door },
                new CardEffectDefinition(CardEffectType.DealDamage, 18),
                new CardEffectDefinition(CardEffectType.ApplyVulnerable, 1),
                new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 6, "적 체력 50% 이하", condition: CardConditionType.EnemyHealthAtOrBelowPercent, percentThreshold: 50)),
            Attack(
                "hard_attack_chain_rend",
                "사슬 가르기",
                "피해 12. 출혈 3. 적에게 출혈이 있으면 카드 1장 뽑기.",
                1,
                new[] { BuildTag.Attack, BuildTag.Curse, BuildTag.DeckControl },
                new CardEffectDefinition(CardEffectType.DealDamage, 12),
                new CardEffectDefinition(CardEffectType.ApplyBleed, 3)),
            Attack(
                "hard_attack_bronze_javelin",
                "황동 투창",
                "피해 14. 방어도가 있는 적에게 추가 피해 6.",
                1,
                new[] { BuildTag.Attack },
                new CardEffectDefinition(CardEffectType.DealDamage, 14),
                new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 6, "적 방어도 보유", condition: CardConditionType.EnemyHasBlock)),
            Attack(
                "hard_attack_crushing_handle",
                "파쇄 일격",
                "피해 22. 적 의도가 공격이면 방어도 8 획득.",
                2,
                new[] { BuildTag.Attack, BuildTag.Defense },
                new CardEffectDefinition(CardEffectType.DealDamage, 22),
                new CardEffectDefinition(CardEffectType.GainBlock, 8, "적 공격 의도", condition: CardConditionType.EnemyIntentIsAttack)),
            Attack(
                "hard_attack_gate_execution",
                "문지기 처형",
                "피해 34. 처치하면 금화 18 획득.",
                3,
                new[] { BuildTag.Attack, BuildTag.Gold, BuildTag.Door },
                new CardEffectDefinition(CardEffectType.DealDamage, 34)),

            Defense(
                "hard_defense_lion_aegis",
                "사자 방패",
                "방어도 24. 취약 1을 제거한다.",
                2,
                new[] { BuildTag.Defense, BuildTag.Sustain },
                new CardEffectDefinition(CardEffectType.GainBlock, 24)),
            Defense(
                "hard_defense_crystal_wall",
                "수정 방벽",
                "방어도 15. 다음 턴까지 방어도 5 유지.",
                1,
                new[] { BuildTag.Defense },
                new CardEffectDefinition(CardEffectType.GainBlock, 15),
                new CardEffectDefinition(CardEffectType.RetainBlockNextTurn, 1)),
            Defense(
                "hard_defense_glass_guard",
                "유리 성채",
                "방어도 20. 반사 피해 8.",
                2,
                new[] { BuildTag.Defense, BuildTag.Attack },
                new CardEffectDefinition(CardEffectType.GainBlock, 20),
                new CardEffectDefinition(CardEffectType.ReflectDamage, 8)),
            Defense(
                "hard_defense_broken_bulwark",
                "균열 방패",
                "방어도 13. 체력이 절반 이하면 방어도 10 추가.",
                1,
                new[] { BuildTag.Defense, BuildTag.LowHealth },
                new CardEffectDefinition(CardEffectType.GainBlock, 13),
                new CardEffectDefinition(CardEffectType.ConditionalBonusBlock, 10, "체력 50% 이하", condition: CardConditionType.PlayerHealthAtOrBelowPercent, percentThreshold: 50)),
            Defense(
                "hard_defense_silent_plate",
                "침묵의 갑주",
                "방어도 18. 이번 턴 받는 다음 피해 6 감소.",
                2,
                new[] { BuildTag.Defense, BuildTag.Sustain },
                new CardEffectDefinition(CardEffectType.GainBlock, 18),
                new CardEffectDefinition(CardEffectType.ReduceNextDamage, 6)),

            Skill(
                "hard_skill_iron_tonic",
                "철혈 영약",
                "체력 10 회복. 전투마다 1번.",
                1,
                CharacterClass.Any,
                CardTarget.Self,
                true,
                new[] { BuildTag.Sustain },
                new CardEffectDefinition(CardEffectType.Heal, 10)),
            Skill(
                "hard_skill_door_breath",
                "문 너머의 숨",
                "카드 2장 뽑기. 행운이 4 이상이면 행동력 1 획득.",
                1,
                CharacterClass.Any,
                CardTarget.Self,
                false,
                new[] { BuildTag.DeckControl, BuildTag.Door },
                new CardEffectDefinition(CardEffectType.DrawCards, 2),
                new CardEffectDefinition(CardEffectType.GainAction, 1, "행운 4 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 4)),
            Skill(
                "hard_skill_fate_convergence",
                "운명 수렴",
                "이번 턴 행운을 다시 굴린다. 5 이상이면 카드 1장 뽑기.",
                0,
                CharacterClass.Any,
                CardTarget.Self,
                false,
                new[] { BuildTag.Dice, BuildTag.DeckControl },
                new CardEffectDefinition(CardEffectType.RerollLuck, 1),
                new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 5 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 5)),
            Skill(
                "hard_skill_debt_writ",
                "채무 문서",
                "빚 1 감소. 감소하지 못하면 금화 18 획득.",
                1,
                CharacterClass.Any,
                CardTarget.Self,
                false,
                new[] { BuildTag.Debt, BuildTag.Gold },
                new CardEffectDefinition(CardEffectType.RemoveCurse, 1)),
            Skill(
                "hard_skill_gold_rain",
                "금화 소나기",
                "금화 26 획득. 행운이 홀수이면 카드 1장 뽑기.",
                0,
                CharacterClass.Any,
                CardTarget.Self,
                false,
                new[] { BuildTag.Gold, BuildTag.Dice },
                new CardEffectDefinition(CardEffectType.GainGold, 26),
                new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 홀수", condition: CardConditionType.LuckIsOdd)),

            Skill(
                "hard_gambler_loaded_dice",
                "속임수 주사위",
                "행운을 다시 굴린다. 낮아지면 피해 18.",
                1,
                CharacterClass.Gambler,
                CardTarget.SingleEnemy,
                false,
                new[] { BuildTag.Dice, BuildTag.Attack },
                new CardEffectDefinition(CardEffectType.RerollLuck, 1),
                new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 18, "행운 3 이하", condition: CardConditionType.LuckAtMost, luckThreshold: 3)),
            Skill(
                "hard_gambler_debt_jackpot",
                "빚진 대박",
                "금화 20 획득. 행운이 5 이상이면 행동력 1 획득.",
                0,
                CharacterClass.Gambler,
                CardTarget.Self,
                false,
                new[] { BuildTag.Gold, BuildTag.Dice, BuildTag.Debt },
                new CardEffectDefinition(CardEffectType.GainGold, 20),
                new CardEffectDefinition(CardEffectType.GainAction, 1, "행운 5 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 5)),
            Attack(
                "hard_gambler_final_wager",
                "최후의 판돈",
                "피해 16. 보유 금화 30마다 추가 피해 4.",
                2,
                new[] { BuildTag.Gold, BuildTag.Attack, BuildTag.Dice },
                CharacterClass.Gambler,
                new CardEffectDefinition(CardEffectType.DealDamage, 16)),

            Skill(
                "hard_oracle_three_omens",
                "삼중 예언",
                "다음 문 정보를 본다. 카드 2장 뽑고 1장 버린다.",
                1,
                CharacterClass.Oracle,
                CardTarget.Self,
                false,
                new[] { BuildTag.Prophecy, BuildTag.DeckControl, BuildTag.Door },
                new CardEffectDefinition(CardEffectType.RevealDoorEffect, 1),
                new CardEffectDefinition(CardEffectType.DrawCards, 2),
                new CardEffectDefinition(CardEffectType.DiscardCards, 1)),
            Skill(
                "hard_oracle_crystal_sentence",
                "수정 판결",
                "피해 16. 방어도 16. 예언 성공 상태면 둘 다 6 증가.",
                2,
                CharacterClass.Oracle,
                CardTarget.SingleEnemy,
                false,
                new[] { BuildTag.Prophecy, BuildTag.Attack, BuildTag.Defense },
                new CardEffectDefinition(CardEffectType.DealDamage, 16),
                new CardEffectDefinition(CardEffectType.GainBlock, 16)),
            Skill(
                "hard_oracle_fixed_star",
                "고정된 별",
                "현재 행운을 다음 턴까지 유지. 방어도 10.",
                1,
                CharacterClass.Oracle,
                CardTarget.Self,
                false,
                new[] { BuildTag.Prophecy, BuildTag.Defense, BuildTag.Dice },
                new CardEffectDefinition(CardEffectType.KeepLuckNextTurn, 1),
                new CardEffectDefinition(CardEffectType.GainBlock, 10)),

            Defense(
                "hard_exile_red_oath",
                "붉은 맹세",
                "방어도 14. 빚이 있으면 빚 1 감소.",
                1,
                new[] { BuildTag.Curse, BuildTag.Defense, BuildTag.Debt },
                CharacterClass.Exile,
                new CardEffectDefinition(CardEffectType.GainBlock, 14),
                new CardEffectDefinition(CardEffectType.RemoveCurse, 1, "빚 보유", condition: CardConditionType.PlayerHasDebt)),
            Attack(
                "hard_exile_chain_breaker",
                "사슬 파쇄자",
                "피해 24. 빚이 있으면 취약 2 추가.",
                2,
                new[] { BuildTag.Curse, BuildTag.Attack, BuildTag.Debt },
                CharacterClass.Exile,
                new CardEffectDefinition(CardEffectType.DealDamage, 24),
                new CardEffectDefinition(CardEffectType.ApplyVulnerable, 2, "빚 보유", condition: CardConditionType.PlayerHasDebt)),
            Skill(
                "hard_exile_no_return",
                "돌아오지 않는 길",
                "저주 또는 빚 1 감소. 성공하면 행동력 1 획득.",
                0,
                CharacterClass.Exile,
                CardTarget.Self,
                false,
                new[] { BuildTag.Curse, BuildTag.Debt, BuildTag.Sustain },
                new CardEffectDefinition(CardEffectType.RemoveCurse, 1))
        };

        [MenuItem("Three Doors of Fate/Generate Hard Card Assets")]
        public static void GenerateHardCardAssets()
        {
            foreach (HardCardConfig card in Cards)
            {
                CreateOrUpdateCardData(card);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {Cards.Length} hard card data assets.");
        }

        private static HardCardConfig Attack(
            string id,
            string displayName,
            string rulesText,
            int cost,
            IReadOnlyList<BuildTag> buildTags,
            params CardEffectDefinition[] effects)
        {
            return Attack(id, displayName, rulesText, cost, buildTags, CharacterClass.Any, effects);
        }

        private static HardCardConfig Attack(
            string id,
            string displayName,
            string rulesText,
            int cost,
            IReadOnlyList<BuildTag> buildTags,
            CharacterClass characterClass,
            params CardEffectDefinition[] effects)
        {
            return new HardCardConfig(id, displayName, rulesText, cost, CardCategory.Attack, CardTarget.SingleEnemy, characterClass, false, buildTags, effects);
        }

        private static HardCardConfig Defense(
            string id,
            string displayName,
            string rulesText,
            int cost,
            IReadOnlyList<BuildTag> buildTags,
            params CardEffectDefinition[] effects)
        {
            return Defense(id, displayName, rulesText, cost, buildTags, CharacterClass.Any, effects);
        }

        private static HardCardConfig Defense(
            string id,
            string displayName,
            string rulesText,
            int cost,
            IReadOnlyList<BuildTag> buildTags,
            CharacterClass characterClass,
            params CardEffectDefinition[] effects)
        {
            return new HardCardConfig(id, displayName, rulesText, cost, CardCategory.Defense, CardTarget.Self, characterClass, false, buildTags, effects);
        }

        private static HardCardConfig Skill(
            string id,
            string displayName,
            string rulesText,
            int cost,
            CharacterClass characterClass,
            CardTarget target,
            bool oncePerCombat,
            IReadOnlyList<BuildTag> buildTags,
            params CardEffectDefinition[] effects)
        {
            return new HardCardConfig(id, displayName, rulesText, cost, CardCategory.Skill, target, characterClass, oncePerCombat, buildTags, effects);
        }

        private static void CreateOrUpdateCardData(HardCardConfig config)
        {
            string spritePath = $"{RenderedRoot}/{config.Id}.png";
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);
            Sprite fullCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (fullCardSprite == null)
            {
                Debug.LogWarning($"Hard card sprite was not found or was not imported as a sprite: {spritePath}");
            }

            string assetPath = $"{CardDataRoot}/{config.Id}.asset";
            CardData cardData = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            if (cardData == null)
            {
                cardData = ScriptableObject.CreateInstance<CardData>();
                AssetDatabase.CreateAsset(cardData, assetPath);
            }

            cardData.ConfigureForEditor(
                config.Id,
                config.DisplayName,
                config.DisplayName,
                config.RulesText,
                config.Cost,
                config.Category,
                CardRarity.Rare,
                CardSource.HardReward,
                config.CharacterClass,
                config.Target,
                fullCardSprite,
                fullCardSprite,
                0,
                1,
                exhaustsAfterUse: false,
                oncePerCombat: config.OncePerCombat,
                tags: new[] { "hard" },
                buildTags: config.BuildTags,
                effects: config.Effects);

            EditorUtility.SetDirty(cardData);
        }

        private readonly struct HardCardConfig
        {
            public HardCardConfig(
                string id,
                string displayName,
                string rulesText,
                int cost,
                CardCategory category,
                CardTarget target,
                CharacterClass characterClass,
                bool oncePerCombat,
                IReadOnlyList<BuildTag> buildTags,
                IReadOnlyList<CardEffectDefinition> effects)
            {
                Id = id;
                DisplayName = displayName;
                RulesText = rulesText;
                Cost = cost;
                Category = category;
                Target = target;
                CharacterClass = characterClass;
                OncePerCombat = oncePerCombat;
                BuildTags = buildTags;
                Effects = effects;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string RulesText { get; }
            public int Cost { get; }
            public CardCategory Category { get; }
            public CardTarget Target { get; }
            public CharacterClass CharacterClass { get; }
            public bool OncePerCombat { get; }
            public IReadOnlyList<BuildTag> BuildTags { get; }
            public IReadOnlyList<CardEffectDefinition> Effects { get; }
        }
    }
}
