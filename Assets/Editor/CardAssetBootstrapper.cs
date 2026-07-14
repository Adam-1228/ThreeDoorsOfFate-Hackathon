using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Editor
{
    public static class CardAssetBootstrapper
    {
        private const string CardDataRoot = "Assets/Data/Cards/MVP";
        private const string CardPrefabPath = "Assets/Prefabs/Cards/CardView.prefab";
        private const string FontPath = "Assets/Fonts/NotoSansKR-VF.ttf";
        private const string UnifiedCardRoot = "Assets/Art/Cards/UnifiedRendered";

        private static readonly CardConfig[] Cards =
        {
            Attack("card_worn_dagger", "낡은 단검", "피해 6", 1, "Assets/Art/Cards/Illustrations/MVP/card_worn_dagger.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 6)),
            Attack("card_deep_stab", "깊은 찌르기", "피해 4. 행운이 4 이상이면 출혈 2.", 1, "Assets/Art/Cards/Illustrations/MVP/card_deep_stab.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 4), new CardEffectDefinition(CardEffectType.ApplyBleed, 2, "행운 4 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 4)),
            Attack("card_heavy_blow", "묵직한 일격", "피해 12.", 2, "Assets/Art/Cards/Illustrations/MVP/card_heavy_blow.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 12)),
            Attack("card_fate_strike", "운명의 일격", "피해 5. 행운이 5 이상이면 추가 피해 5.", 1, "Assets/Art/Cards/Illustrations/MVP/card_fate_strike.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 5), new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 5, "행운 5 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 5)),
            Attack("card_reckless_attack", "무리한 공격", "피해 10. 자신에게 피해 2.", 1, "Assets/Art/Cards/Illustrations/MVP/card_reckless_attack.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 10), new CardEffectDefinition(CardEffectType.LoseHealth, 2, "자해")),
            Attack("card_double_slash", "연속 베기", "피해 4를 두 번 준다.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_double_slash.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 4), new CardEffectDefinition(CardEffectType.DealDamage, 4)),
            Attack("card_exploit_opening", "빈틈 노리기", "취약 2. 피해 3.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_exploit_opening.png", Common(), new CardEffectDefinition(CardEffectType.ApplyVulnerable, 2), new CardEffectDefinition(CardEffectType.DealDamage, 3)),
            Attack("card_throwing_dagger", "투척 단검", "피해 4. 비용이 낮다.", 0, "Assets/Art/Cards/Illustrations/Expansion/card_throwing_dagger.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 4)),
            Attack("card_counter_ready", "반격 준비", "방어도 5. 적이 공격하려 하면 피해 5.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_counter_ready.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 5), new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 5, "적 공격 예고", condition: CardConditionType.EnemyIntentIsAttack)),
            Attack("card_finish", "마무리", "피해 7. 적 체력이 절반 이하이면 추가 피해 6.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_finish.png", Common(), new CardEffectDefinition(CardEffectType.DealDamage, 7), new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 6, "적 체력 50% 이하", condition: CardConditionType.EnemyHealthAtOrBelowPercent, percentThreshold: 50)),
            Attack("card_fate_beheading", "운명의 참수", "피해 18.", 3, "Assets/Art/Cards/Illustrations/Expansion/card_fate_beheading.png", Rare(), new CardEffectDefinition(CardEffectType.DealDamage, 18)),
            Attack("card_blood_gamble", "피의 도박", "피해 14. 자신에게 피해 4. 행운이 5 이상이면 카드 1장 뽑기.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_blood_gamble.png", Rare(), new CardEffectDefinition(CardEffectType.DealDamage, 14), new CardEffectDefinition(CardEffectType.LoseHealth, 4), new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 5 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 5)),
            Attack("card_starlight_barrage", "별빛 난사", "행운 수치만큼 피해를 반복한다.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_starlight_barrage.png", Rare(), new CardEffectDefinition(CardEffectType.RepeatDamageByLuck, 2)),
            Attack("class_gambler_attack_wager_dagger", "승부의 단검", "피해 7. 행운이 5 이상이면 추가 피해 7.", 1, UnifiedCardPath("class_gambler_attack_wager_dagger"), Common(), new CardEffectDefinition(CardEffectType.DealDamage, 7), new CardEffectDefinition(CardEffectType.ConditionalBonusDamage, 7, "행운 5 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 5)),
            Attack("class_oracle_attack_constellation_cut", "별자리 절단", "피해 2를 행운 수치만큼 반복한다.", 2, UnifiedCardPath("class_oracle_attack_constellation_cut"), Common(), new CardEffectDefinition(CardEffectType.RepeatDamageByLuck, 2)),
            Attack("class_exile_attack_chain_execution", "속박 절단", "피해 8. 취약 2. 빚 1 감소.", 2, UnifiedCardPath("class_exile_attack_chain_execution"), Common(), new CardEffectDefinition(CardEffectType.DealDamage, 8), new CardEffectDefinition(CardEffectType.ApplyVulnerable, 2), new CardEffectDefinition(CardEffectType.RemoveCurse, 1)),

            Defense("card_worn_shield", "낡은 방패", "방어도 7.", 1, "Assets/Art/Cards/Illustrations/MVP/card_worn_shield.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 7)),
            Defense("card_duck_low", "몸 낮추기", "방어도 5. 행운이 4 이상이면 카드 1장 뽑기.", 1, "Assets/Art/Cards/Illustrations/MVP/card_duck_low.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 5), new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 4 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 4)),
            Defense("card_endure", "버티기", "방어도 14.", 2, "Assets/Art/Cards/Illustrations/MVP/card_endure.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 14)),
            new CardConfig("card_emergency_treatment", "응급 처치", "체력 4 회복. 전투마다 1번.", 1, CardCategory.Defense, CardTarget.Self, "Assets/Art/Cards/Illustrations/MVP/card_emergency_treatment.png", Common(), new[] { new CardEffectDefinition(CardEffectType.Heal, 4, condition: CardConditionType.OncePerCombat) }, oncePerCombat: true),
            Defense("card_last_defense", "최후의 방어", "방어도 6. 체력이 절반 이하이면 방어도 6 추가.", 1, "Assets/Art/Cards/Illustrations/MVP/card_last_defense.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 6), new CardEffectDefinition(CardEffectType.ConditionalBonusBlock, 6, "체력 50% 이하", condition: CardConditionType.PlayerHealthAtOrBelowPercent, percentThreshold: 50)),
            Defense("card_evade", "회피", "방어도 4. 카드 1장 뽑기.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_evade.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 4), new CardEffectDefinition(CardEffectType.DrawCards, 1)),
            Defense("card_shield_bash", "방패 밀치기", "방어도 6. 피해 4.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_shield_bash.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 6), new CardEffectDefinition(CardEffectType.DealDamage, 4)),
            Defense("card_guard_stance", "경계 태세", "방어도 9. 다음 턴까지 방어도 유지.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_guard_stance.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 9), new CardEffectDefinition(CardEffectType.RetainBlockNextTurn, 1)),
            Defense("card_catch_breath", "숨 고르기", "방어도 3. 체력 3 회복.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_catch_breath.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 3), new CardEffectDefinition(CardEffectType.Heal, 3)),
            Defense("card_protection_charm", "보호 부적", "방어도 8. 다음 빚 증가 1 감소.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_protection_charm.png", Common(), new CardEffectDefinition(CardEffectType.GainBlock, 8), new CardEffectDefinition(CardEffectType.ReduceCurseDamage, 1)),
            Defense("card_absolute_barrier", "절대 방벽", "방어도 22.", 3, "Assets/Art/Cards/Illustrations/Expansion/card_absolute_barrier.png", Rare(), new CardEffectDefinition(CardEffectType.GainBlock, 22)),
            Defense("card_indomitable", "불굴", "방어도 10. 이번 턴 죽음 1회 방지.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_indomitable.png", Rare(), new CardEffectDefinition(CardEffectType.GainBlock, 10), new CardEffectDefinition(CardEffectType.PreventDeathThisTurn, 1)),
            Defense("card_mirror_shield", "거울 방패", "방어도 8. 반사 피해 6.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_mirror_shield.png", Rare(), new CardEffectDefinition(CardEffectType.GainBlock, 8), new CardEffectDefinition(CardEffectType.ReflectDamage, 6)),
            Defense("class_gambler_defense_stake_shield", "판돈 방패", "방어도 5. 행운이 2 이하이면 카드 1장 뽑기.", 1, UnifiedCardPath("class_gambler_defense_stake_shield"), Common(), new CardEffectDefinition(CardEffectType.GainBlock, 5), new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 2 이하", condition: CardConditionType.LuckAtMost, luckThreshold: 2)),
            Defense("class_oracle_defense_foreseen_barrier", "예견된 방벽", "방어도 8. 다음 턴까지 방어도 유지.", 1, UnifiedCardPath("class_oracle_defense_foreseen_barrier"), Common(), new CardEffectDefinition(CardEffectType.GainBlock, 8), new CardEffectDefinition(CardEffectType.RetainBlockNextTurn, 1)),
            Defense("class_exile_defense_oath_of_exile", "추방자의 맹세", "방어도 10. 이번 턴 죽음 1회 방지.", 2, UnifiedCardPath("class_exile_defense_oath_of_exile"), Common(), new CardEffectDefinition(CardEffectType.GainBlock, 10), new CardEffectDefinition(CardEffectType.PreventDeathThisTurn, 1)),

            Skill("card_reroll", "다시 굴리기", "이번 턴 행운 주사위를 다시 굴린다.", 1, "Assets/Art/Cards/Illustrations/MVP/card_reroll.png", Common(), new CardEffectDefinition(CardEffectType.RerollLuck, 1)),
            Skill("card_fix_fate", "운명 고정", "현재 행운 수치를 다음 턴에도 유지한다.", 1, "Assets/Art/Cards/Illustrations/MVP/card_fix_fate.png", Common(), new CardEffectDefinition(CardEffectType.KeepLuckNextTurn, 1)),
            Skill("card_card_exchange", "카드 교환", "손패 1장을 버리고 카드 2장 뽑기.", 0, "Assets/Art/Cards/Illustrations/MVP/card_card_exchange.png", Common(), new CardEffectDefinition(CardEffectType.DiscardCards, 1), new CardEffectDefinition(CardEffectType.DrawCards, 2)),
            Skill("card_read_the_rift", "균열 읽기", "다음 문에 대한 정보를 더 선명하게 본다.", 1, "Assets/Art/Cards/Illustrations/MVP/card_read_the_rift.png", Common(), new CardEffectDefinition(CardEffectType.RevealDoorEffect, 1)),
            Skill("card_small_contract", "작은 계약", "체력 3을 잃고 행동력 1을 얻는다.", 0, "Assets/Art/Cards/Illustrations/MVP/card_small_contract.png", Common(), new CardEffectDefinition(CardEffectType.LoseHealth, 3), new CardEffectDefinition(CardEffectType.GainAction, 1)),
            Skill("card_store_luck", "행운 저장", "현재 행운을 저장하고 카드 1장 뽑기.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_store_luck.png", Common(), new CardEffectDefinition(CardEffectType.StoreLuck, 1), new CardEffectDefinition(CardEffectType.DrawCards, 1)),
            Skill("card_purify", "정화", "빚 1 감소. 체력 2 회복.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_purify.png", Common(), new CardEffectDefinition(CardEffectType.RemoveCurse, 1), new CardEffectDefinition(CardEffectType.Heal, 2)),
            Skill("card_find_weakness", "약점 간파", "취약 3.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_find_weakness.png", Common(), new CardEffectDefinition(CardEffectType.ApplyVulnerable, 3)),
            Skill("card_odd_pouch", "기묘한 주머니", "금화 12 획득. 행운이 홀수이면 카드 1장 뽑기.", 0, "Assets/Art/Cards/Illustrations/Expansion/card_odd_pouch.png", Common(), new CardEffectDefinition(CardEffectType.GainGold, 12), new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 홀수", condition: CardConditionType.LuckIsOdd)),
            Skill("card_absorb_curse", "빚 흡수", "빚 1 감소. 행동력 1 획득.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_absorb_curse.png", Common(), new CardEffectDefinition(CardEffectType.RemoveCurse, 1), new CardEffectDefinition(CardEffectType.GainAction, 1)),
            Skill("card_forbidden_choice", "금지된 선택", "카드 3장 뽑기. 빚 1 증가.", 1, "Assets/Art/Cards/Illustrations/Expansion/card_forbidden_choice.png", Rare(), new CardEffectDefinition(CardEffectType.DrawCards, 3), new CardEffectDefinition(CardEffectType.AddCurse, 1)),
            Skill("card_fate_manipulation", "운명 조작", "행운을 6으로 바꾸고 카드 1장 뽑기.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_fate_manipulation.png", Rare(), new CardEffectDefinition(CardEffectType.ChangeLuck, 6), new CardEffectDefinition(CardEffectType.DrawCards, 1)),
            Skill("card_third_door", "세 번째 문", "금화 30 획득. 다음 문 정보를 더 선명하게 본다.", 2, "Assets/Art/Cards/Illustrations/Expansion/card_third_door.png", Rare(), new CardEffectDefinition(CardEffectType.GainGold, 30), new CardEffectDefinition(CardEffectType.RevealDoorEffect, 1)),
            Skill("class_gambler_skill_turn_the_table", "판세 뒤집기", "이번 턴 행운 주사위를 다시 굴린다. 행운이 5 이상이면 카드 1장 뽑기.", 1, UnifiedCardPath("class_gambler_skill_turn_the_table"), Common(), new CardEffectDefinition(CardEffectType.RerollLuck, 1), new CardEffectDefinition(CardEffectType.DrawCards, 1, "행운 5 이상", condition: CardConditionType.LuckAtLeast, luckThreshold: 5)),
            Skill("class_oracle_skill_three_door_omen", "세 문 점지", "다음 문 정보를 더 선명하게 본다. 카드 1장 뽑기.", 1, UnifiedCardPath("class_oracle_skill_three_door_omen"), Common(), new CardEffectDefinition(CardEffectType.RevealDoorEffect, 1), new CardEffectDefinition(CardEffectType.DrawCards, 1)),
            Skill("class_exile_skill_brand_purification", "낙인의 정화", "빚 1 감소. 방어도 4 획득.", 1, UnifiedCardPath("class_exile_skill_brand_purification"), Common(), new CardEffectDefinition(CardEffectType.RemoveCurse, 1), new CardEffectDefinition(CardEffectType.GainBlock, 4)),
        };

        [MenuItem("Three Doors of Fate/Generate MVP Card Assets")]
        public static void GenerateMvpCardAssets()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Cards");
            EnsureFolder("Assets/Data/Cards", "MVP");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Cards");

            foreach (CardConfig cardConfig in Cards)
            {
                CreateOrUpdateCardData(cardConfig);
            }

            CreateCardViewPrefabIfMissing();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {Cards.Length} card data assets and ensured CardView prefab.");
        }

        private static CardRarity Common()
        {
            return CardRarity.Common;
        }

        private static CardRarity Rare()
        {
            return CardRarity.Rare;
        }

        private static CardConfig Attack(string id, string displayName, string rulesText, int cost, string spritePath, CardRarity rarity, params CardEffectDefinition[] effects)
        {
            return new CardConfig(id, displayName, rulesText, cost, CardCategory.Attack, CardTarget.SingleEnemy, spritePath, rarity, effects);
        }

        private static CardConfig Defense(string id, string displayName, string rulesText, int cost, string spritePath, CardRarity rarity, params CardEffectDefinition[] effects)
        {
            return new CardConfig(id, displayName, rulesText, cost, CardCategory.Defense, CardTarget.Self, spritePath, rarity, effects);
        }

        private static CardConfig Skill(string id, string displayName, string rulesText, int cost, string spritePath, CardRarity rarity, params CardEffectDefinition[] effects)
        {
            return new CardConfig(id, displayName, rulesText, cost, CardCategory.Skill, CardTarget.Self, spritePath, rarity, effects);
        }

        private static void CreateOrUpdateCardData(CardConfig config)
        {
            AssetDatabase.ImportAsset(config.SpritePath, ImportAssetOptions.ForceUpdate);
            Sprite illustration = AssetDatabase.LoadAssetAtPath<Sprite>(config.SpritePath);
            if (illustration == null)
            {
                Debug.LogWarning($"Card illustration was not found or was not imported as a sprite: {config.SpritePath}");
            }

            string fullCardPath = UnifiedCardPath(config.Id);
            AssetDatabase.ImportAsset(fullCardPath, ImportAssetOptions.ForceUpdate);
            Sprite fullCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullCardPath);
            if (fullCardSprite == null)
            {
                string fallbackPath = $"Assets/Art/Cards/FullRendered/{config.Id}_full.png";
                AssetDatabase.ImportAsset(fallbackPath, ImportAssetOptions.ForceUpdate);
                fullCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
                if (fullCardSprite == null)
                {
                    Debug.LogWarning($"Full rendered card sprite was not found or was not imported as a sprite: {fullCardPath}");
                }
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
                config.Rarity,
                GetCardSource(config),
                GetCardClass(config),
                config.Target,
                illustration,
                fullCardSprite,
                GetMinimumRoom(config),
                GetShopWeight(config),
                exhaustsAfterUse: false,
                oncePerCombat: config.OncePerCombat,
                tags: config.Tags,
                buildTags: GetBuildTags(config),
                effects: config.Effects);

            EditorUtility.SetDirty(cardData);
        }

        private static CardSource GetCardSource(CardConfig config)
        {
            return config.Id switch
            {
                "card_worn_dagger" or
                "card_worn_shield" or
                "card_deep_stab" or
                "card_fate_strike" or
                "card_duck_low" or
                "card_endure" or
                "card_reroll" or
                "card_small_contract" => CardSource.Starter,

                "class_gambler_attack_wager_dagger" or
                "class_oracle_attack_constellation_cut" or
                "class_exile_attack_chain_execution" => CardSource.Starter,

                "card_blood_gamble" or
                "card_protection_charm" or
                "card_indomitable" or
                "card_mirror_shield" or
                "card_fate_manipulation" or
                "card_third_door" or
                "class_gambler_defense_stake_shield" or
                "class_oracle_defense_foreseen_barrier" or
                "class_exile_defense_oath_of_exile" => CardSource.ShopOnly,

                "card_forbidden_choice" or
                "card_absorb_curse" or
                "card_purify" or
                "class_gambler_skill_turn_the_table" or
                "class_oracle_skill_three_door_omen" or
                "class_exile_skill_brand_purification" => CardSource.EventOnly,

                "card_fate_beheading" or
                "card_absolute_barrier" or
                "card_starlight_barrage" => CardSource.BossReward,

                _ => CardSource.CombatReward
            };
        }

        private static CharacterClass GetCardClass(CardConfig config)
        {
            return config.Id switch
            {
                "class_gambler_attack_wager_dagger" or
                "class_gambler_defense_stake_shield" or
                "class_gambler_skill_turn_the_table" => CharacterClass.Gambler,

                "class_oracle_attack_constellation_cut" or
                "class_oracle_defense_foreseen_barrier" or
                "class_oracle_skill_three_door_omen" => CharacterClass.Oracle,

                "class_exile_attack_chain_execution" or
                "class_exile_defense_oath_of_exile" or
                "class_exile_skill_brand_purification" => CharacterClass.Exile,

                _ => CharacterClass.Any
            };
        }

        private static int GetMinimumRoom(CardConfig config)
        {
            return config.Id switch
            {
                "card_fate_beheading" or
                "card_absolute_barrier" or
                "card_starlight_barrage" => 7,
                "card_blood_gamble" or
                "card_indomitable" or
                "card_fate_manipulation" or
                "card_third_door" or
                "class_gambler_defense_stake_shield" or
                "class_oracle_defense_foreseen_barrier" or
                "class_exile_defense_oath_of_exile" => 3,
                "card_forbidden_choice" => 2,
                _ => 0
            };
        }

        private static int GetShopWeight(CardConfig config)
        {
            return config.Id switch
            {
                "card_blood_gamble" or
                "card_protection_charm" or
                "card_fate_manipulation" or
                "class_gambler_defense_stake_shield" or
                "class_oracle_defense_foreseen_barrier" or
                "class_exile_defense_oath_of_exile" => 4,
                "card_indomitable" or
                "card_mirror_shield" or
                "card_third_door" => 2,
                _ => 1
            };
        }

        private static string UnifiedCardPath(string cardId)
        {
            string prefix = cardId.StartsWith("card_", System.StringComparison.Ordinal)
                ? "base_"
                : string.Empty;
            return $"{UnifiedCardRoot}/{prefix}{cardId}_unified.png";
        }

        private static IReadOnlyList<BuildTag> GetBuildTags(CardConfig config)
        {
            List<BuildTag> tags = new();
            switch (config.Category)
            {
                case CardCategory.Attack:
                    tags.Add(BuildTag.Attack);
                    break;
                case CardCategory.Defense:
                    tags.Add(BuildTag.Defense);
                    break;
                case CardCategory.Skill:
                    tags.Add(BuildTag.DeckControl);
                    break;
            }

            switch (config.Id)
            {
                case "card_fate_strike":
                case "card_reroll":
                case "card_odd_pouch":
                case "card_fate_manipulation":
                case "card_starlight_barrage":
                case "class_gambler_attack_wager_dagger":
                case "class_gambler_defense_stake_shield":
                case "class_gambler_skill_turn_the_table":
                case "class_oracle_attack_constellation_cut":
                    tags.Add(BuildTag.Dice);
                    break;
            }

            switch (config.Id)
            {
                case "card_blood_gamble":
                case "card_small_contract":
                case "card_forbidden_choice":
                case "class_gambler_attack_wager_dagger":
                    tags.Add(BuildTag.Debt);
                    break;
                case "card_read_the_rift":
                case "card_fix_fate":
                case "card_store_luck":
                case "card_third_door":
                case "class_oracle_attack_constellation_cut":
                case "class_oracle_defense_foreseen_barrier":
                case "class_oracle_skill_three_door_omen":
                    tags.Add(BuildTag.Prophecy);
                    tags.Add(BuildTag.Door);
                    break;
                case "card_card_exchange":
                case "card_evade":
                    tags.Add(BuildTag.DeckControl);
                    break;
                case "card_absorb_curse":
                case "card_purify":
                case "card_protection_charm":
                case "class_exile_attack_chain_execution":
                case "class_exile_skill_brand_purification":
                    tags.Add(BuildTag.Curse);
                    break;
                case "card_last_defense":
                case "card_indomitable":
                case "class_exile_defense_oath_of_exile":
                    tags.Add(BuildTag.LowHealth);
                    break;
                case "card_catch_breath":
                case "card_emergency_treatment":
                    tags.Add(BuildTag.Sustain);
                    break;
                case "card_odd_pouch":
                    tags.Add(BuildTag.Gold);
                    break;
            }

            return tags.Distinct().ToArray();
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            string folderPath = $"{parentPath}/{folderName}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private static void CreateCardViewPrefabIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null)
            {
                return;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Noto Sans KR", 18);
            }

            GameObject root = new("CardView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CardView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(220f, 330f);

            Image backgroundImage = root.GetComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.075f, 0.09f, 1f);

            Button button = root.GetComponent<Button>();
            button.targetGraphic = backgroundImage;
            button.colors = CreateButtonColors();

            Image categoryStripeImage = AddImage(rootRect, "CategoryStripe", new Color(0.45f, 0.28f, 0.72f, 1f));
            SetStretch(categoryStripeImage.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), Vector2.zero);

            Image illustrationImage = AddImage(rootRect, "Illustration", Color.white);
            SetStretch(illustrationImage.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
            illustrationImage.preserveAspect = true;

            Image costBadgeImage = AddImage(rootRect, "CostBadge", new Color(0.02f, 0.02f, 0.025f, 0.95f));
            RectTransform costBadgeRect = costBadgeImage.rectTransform;
            costBadgeRect.anchorMin = new Vector2(0f, 1f);
            costBadgeRect.anchorMax = new Vector2(0f, 1f);
            costBadgeRect.pivot = new Vector2(0f, 1f);
            costBadgeRect.anchoredPosition = new Vector2(10f, -12f);
            costBadgeRect.sizeDelta = new Vector2(38f, 38f);

            Text costText = AddText(costBadgeRect, "CostText", font, 22, TextAnchor.MiddleCenter, Color.white);
            SetStretch(costText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            costText.fontStyle = FontStyle.Bold;
            costText.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text nameText = AddText(rootRect, "NameText", font, 18, TextAnchor.MiddleCenter, Color.white);
            SetStretch(nameText.rectTransform, new Vector2(0.14f, 0.86f), new Vector2(0.92f, 0.975f), Vector2.zero, Vector2.zero);
            nameText.fontStyle = FontStyle.Bold;

            Text typeText = AddText(rootRect, "TypeText", font, 12, TextAnchor.MiddleCenter, new Color(0.78f, 0.74f, 0.68f, 1f));
            SetStretch(typeText.rectTransform, new Vector2(0.08f, 0.27f), new Vector2(0.92f, 0.335f), Vector2.zero, Vector2.zero);

            Text rulesText = AddText(rootRect, "RulesText", font, 13, TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.80f, 1f));
            SetStretch(rulesText.rectTransform, new Vector2(0.10f, 0.075f), new Vector2(0.90f, 0.265f), Vector2.zero, Vector2.zero);
            rulesText.resizeTextMinSize = 9;
            rulesText.alignByGeometry = true;
            rulesText.lineSpacing = 0.92f;

            CardView cardView = root.GetComponent<CardView>();
            SerializedObject serializedObject = new(cardView);
            serializedObject.FindProperty("illustrationImage").objectReferenceValue = illustrationImage;
            serializedObject.FindProperty("frameImage").objectReferenceValue = backgroundImage;
            serializedObject.FindProperty("categoryStripeImage").objectReferenceValue = categoryStripeImage;
            serializedObject.FindProperty("nameText").objectReferenceValue = nameText;
            serializedObject.FindProperty("costText").objectReferenceValue = costText;
            serializedObject.FindProperty("typeText").objectReferenceValue = typeText;
            serializedObject.FindProperty("rulesText").objectReferenceValue = rulesText;
            serializedObject.FindProperty("button").objectReferenceValue = button;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static Image AddImage(RectTransform parent, string name, Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform childRect = child.GetComponent<RectTransform>();
            childRect.SetParent(parent, false);

            Image image = child.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text AddText(RectTransform parent, string name, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            RectTransform childRect = child.GetComponent<RectTransform>();
            childRect.SetParent(parent, false);

            Text text = child.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(8, fontSize - 6);
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static void SetStretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static ColorBlock CreateButtonColors()
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            return colors;
        }

        private sealed class CardConfig
        {
            public CardConfig(
                string id,
                string displayName,
                string rulesText,
                int cost,
                CardCategory category,
                CardTarget target,
                string spritePath,
                CardRarity rarity,
                IReadOnlyList<CardEffectDefinition> effects,
                bool oncePerCombat = false)
            {
                Id = id;
                DisplayName = displayName;
                RulesText = rulesText;
                Cost = cost;
                Category = category;
                Target = target;
                SpritePath = spritePath;
                Rarity = rarity;
                Effects = effects;
                OncePerCombat = oncePerCombat;
                Tags = new[] { category.ToString().ToLowerInvariant(), rarity.ToString().ToLowerInvariant() };
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string RulesText { get; }
            public int Cost { get; }
            public CardCategory Category { get; }
            public CardTarget Target { get; }
            public string SpritePath { get; }
            public CardRarity Rarity { get; }
            public IReadOnlyList<string> Tags { get; }
            public IReadOnlyList<CardEffectDefinition> Effects { get; }
            public bool OncePerCombat { get; }
        }
    }
}
