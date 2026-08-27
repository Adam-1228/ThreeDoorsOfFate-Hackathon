using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Game.V140;
using UnityEngine;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const string EnemyBehaviorCatalogResourcePath =
            "GameData/V140/enemy_behaviors";

        private EnemyBehaviorCatalog cachedEnemyBehaviorCatalog;
        private bool enemyBehaviorCatalogLoadAttempted;
        private readonly EnemyIntentHistory enemyIntentHistory = new();
        private EliteAffix? activeEliteAffix;
        private int currentBossBehaviorPhase = -1;
        private int playerVulnerableTurns;
        private int enemyActionPenaltyNextTurn;
        private int enemyLuckPenaltyNextTurn;
        private int enemyDrawPenaltyNextTurn;

        private void InitializeEncounterBehavior(EnemyState state)
        {
            if (runRandom == null)
            {
                ResetRunRandom(runSeed == 0 ? 1 : runSeed);
            }

            enemyIntentHistory.Clear();
            currentBossBehaviorPhase = -1;
            playerVulnerableTurns = 0;
            enemyActionPenaltyNextTurn = 0;
            enemyLuckPenaltyNextTurn = 0;
            enemyDrawPenaltyNextTurn = 0;
            activeEliteAffix = state != null && state.WasElite && !state.IsBoss
                ? EncounterDirector.SelectEliteAffix(runRandom)
                : null;
            if (activeEliteAffix.HasValue)
            {
                AddLog(LF(
                    "enemy.affix.applied",
                    state.Name,
                    GetEliteAffixName(activeEliteAffix.Value)));
            }
        }

        private bool TryPrepareCatalogEnemyIntent()
        {
            if (enemy == null
                || !TryGetEnemyBehaviorCatalog(out EnemyBehaviorCatalog catalog)
                || !catalog.TryGet(enemy.Id, out EnemyBehaviorProfile profile))
            {
                return false;
            }

            int phaseIndex = enemy.IsBoss
                ? EncounterDirector.GetBossPhaseIndex(enemy.Health, enemy.MaxHealth)
                : 0;
            bool phaseChanged = enemy.IsBoss && phaseIndex != currentBossBehaviorPhase;
            if (phaseChanged)
            {
                currentBossBehaviorPhase = phaseIndex;
                AddLog(LF(
                    "enemy.phase.changed",
                    enemy.Name,
                    phaseIndex + 1));
            }

            EnemySelectionState state = new()
            {
                EnemyHealth = enemy.Health,
                EnemyMaxHealth = enemy.MaxHealth,
                EnemyBlock = enemy.Block,
                EnemyBaseAttack = enemy.BaseAttack,
                EnemyBaseBlock = enemy.BaseBlock,
                PlayerHealth = playerHealth,
                PlayerBlock = playerBlock,
                PlayerDebt = debt,
                IsElite = enemy.WasElite,
                IsEndless = endlessModeActive
            };
            EnemyActionDefinition selected = EncounterDirector.SelectAction(
                profile,
                phaseIndex,
                GetEncounterDifficulty(),
                state,
                runRandom,
                enemyIntentHistory);
            ApplyEnemyBehaviorAction(selected);
            ApplyEliteAffixToIntent();
            enemyIntentHistory.Record(selected.Id);

            string actionName = L(selected.NameKey);
            if (phaseChanged)
            {
                actionName = LF(
                    "enemy.phase.intentPrefix",
                    phaseIndex + 1,
                    actionName);
            }

            if (activeEliteAffix.HasValue)
            {
                actionName = LF(
                    "enemy.affix.intentPrefix",
                    GetEliteAffixName(activeEliteAffix.Value),
                    actionName);
            }

            enemy.IntentCardName = actionName;
            enemy.CandidateLabel = string.Join(
                ", ",
                profile.GetActionsForPhase(phaseIndex).Select(action => L(action.NameKey)));
            RefreshEnemyIntentLabelForCurrentLuck();
            AddLog(LF(
                "enemy.intent.selected",
                enemy.Name,
                enemy.CandidateLabel,
                actionName));
            return true;
        }

        private void ApplyEnemyBehaviorAction(EnemyActionDefinition actionDefinition)
        {
            int attack = ScaleEnemyAttack(actionDefinition.Power, false);
            int multiAttack = ScaleEnemyAttack(actionDefinition.Power, true);
            int guard = Mathf.Max(
                2,
                Mathf.RoundToInt(enemy.BaseBlock * actionDefinition.Power / 100f));
            switch (actionDefinition.Kind)
            {
                case EnemyActionKind.Attack:
                    enemy.IntentAttack = attack;
                    break;
                case EnemyActionKind.AttackVulnerable:
                    enemy.IntentAttack = attack;
                    SetEnemySpecial(
                        EnemySpecialEffect.PlayerVulnerable,
                        1,
                        L("enemy.intent.vulnerable"));
                    break;
                case EnemyActionKind.Debt:
                    enemy.IntentDebt = Mathf.Clamp(actionDefinition.Power, 1, 3);
                    break;
                case EnemyActionKind.DebtAttack:
                    enemy.IntentAttack = attack;
                    enemy.IntentDebt = actionDefinition.Power >= 120 ? 2 : 1;
                    break;
                case EnemyActionKind.LuckDown:
                    SetEnemySpecial(
                        EnemySpecialEffect.LuckDown,
                        Mathf.Clamp(actionDefinition.Power, 1, 2),
                        L("enemy.intent.luckDown"));
                    break;
                case EnemyActionKind.LuckAttack:
                    enemy.IntentAttack = attack;
                    SetEnemySpecial(
                        EnemySpecialEffect.LuckDown,
                        1,
                        L("enemy.intent.luckDown"));
                    break;
                case EnemyActionKind.DrawPenalty:
                case EnemyActionKind.Discard:
                    SetEnemySpecial(
                        EnemySpecialEffect.DrawPenalty,
                        Mathf.Clamp(actionDefinition.Power, 1, 2),
                        L("enemy.intent.drawPenalty"));
                    break;
                case EnemyActionKind.ActionDrain:
                    SetEnemySpecial(
                        EnemySpecialEffect.ActionDrain,
                        Mathf.Clamp(actionDefinition.Power, 1, 2),
                        L("enemy.intent.actionDrain"));
                    break;
                case EnemyActionKind.Guard:
                    enemy.IntentBlock = guard;
                    break;
                case EnemyActionKind.GuardAttack:
                    enemy.IntentAttack = attack;
                    enemy.IntentBlock = Mathf.Max(2, guard / 2);
                    break;
                case EnemyActionKind.MultiAttack:
                    enemy.IntentAttack = multiAttack;
                    enemy.IntentSpecialLabel = L("enemy.intent.multiAttack");
                    break;
                case EnemyActionKind.Regenerate:
                    enemy.IntentHeal = Mathf.Clamp(
                        actionDefinition.Power,
                        1,
                        Mathf.Max(1, GetEnemyRegenerationAmount(enemy)));
                    break;
                case EnemyActionKind.GoldAttack:
                    enemy.IntentAttack = attack;
                    SetEnemySpecial(
                        EnemySpecialEffect.GoldLoss,
                        1,
                        L("enemy.intent.goldLoss"));
                    break;
                case EnemyActionKind.GuardGold:
                    enemy.IntentBlock = guard;
                    SetEnemySpecial(
                        EnemySpecialEffect.GoldLoss,
                        1,
                        L("enemy.intent.goldLoss"));
                    break;
                case EnemyActionKind.AttackHeal:
                    enemy.IntentAttack = attack;
                    enemy.IntentHeal = Mathf.Max(3, GetEnemyRegenerationAmount(enemy) / 2);
                    break;
                case EnemyActionKind.LowHealthAttack:
                    enemy.IntentAttack = attack;
                    break;
                case EnemyActionKind.GoldDebt:
                    enemy.IntentDebt = 1;
                    SetEnemySpecial(
                        EnemySpecialEffect.GoldLoss,
                        1,
                        L("enemy.intent.goldLoss"));
                    break;
            }
        }

        private int ScaleEnemyAttack(int powerPercent, bool multiAttack)
        {
            float multiplier = Mathf.Max(1, powerPercent) / 100f;
            if (multiAttack)
            {
                multiplier *= 2f;
            }

            return Mathf.Max(1, Mathf.RoundToInt(enemy.BaseAttack * multiplier));
        }

        private void SetEnemySpecial(
            EnemySpecialEffect effect,
            int amount,
            string label)
        {
            enemy.IntentSpecialEffect = effect;
            enemy.IntentSpecialAmount = Mathf.Max(1, amount);
            enemy.IntentSpecialLabel = label ?? string.Empty;
        }

        private void ApplyEliteAffixToIntent()
        {
            if (!activeEliteAffix.HasValue || enemy == null)
            {
                return;
            }

            switch (activeEliteAffix.Value)
            {
                case EliteAffix.Usury:
                    enemy.IntentDebt = Mathf.Clamp(enemy.IntentDebt + 1, 1, 3);
                    break;
                case EliteAffix.Seal:
                    enemy.IntentBlock += Mathf.Max(2, enemy.BaseBlock / 2);
                    break;
                case EliteAffix.Frenzy:
                    if (enemy.IntentAttack > 0)
                    {
                        enemy.IntentAttack = Mathf.Max(
                            1,
                            Mathf.CeilToInt(enemy.IntentAttack * 1.12f));
                    }
                    break;
            }
        }

        private void ApplyEnemyTurnStartPenaltiesAndDraw()
        {
            if (enemyActionPenaltyNextTurn > 0)
            {
                int penalty = enemyActionPenaltyNextTurn;
                action = Mathf.Max(0, action - penalty);
                enemyActionPenaltyNextTurn = 0;
                AddLog(LF("enemy.effect.actionDrain", penalty));
            }

            if (enemyLuckPenaltyNextTurn > 0)
            {
                int before = luck;
                luck = Mathf.Clamp(luck - enemyLuckPenaltyNextTurn, 1, 6);
                enemyLuckPenaltyNextTurn = 0;
                AddLog(LF("enemy.effect.luckDown", before, luck));
            }

            int targetHandSize = Mathf.Max(
                1,
                StartingHandSize - enemyDrawPenaltyNextTurn);
            if (enemyDrawPenaltyNextTurn > 0)
            {
                AddLog(LF(
                    "enemy.effect.drawPenalty",
                    enemyDrawPenaltyNextTurn));
                enemyDrawPenaltyNextTurn = 0;
            }

            DrawCards(Mathf.Max(0, targetHandSize - hand.Count));
        }

        private void ResolveEnemyBehaviorSpecial(
            EnemySpecialEffect specialEffect,
            int amount)
        {
            switch (specialEffect)
            {
                case EnemySpecialEffect.PlayerVulnerable:
                    playerVulnerableTurns = Mathf.Max(
                        playerVulnerableTurns,
                        amount);
                    AddLog(LF("enemy.effect.vulnerable", amount));
                    break;
                case EnemySpecialEffect.LuckDown:
                    enemyLuckPenaltyNextTurn = Mathf.Max(
                        enemyLuckPenaltyNextTurn,
                        amount);
                    break;
                case EnemySpecialEffect.DrawPenalty:
                    enemyDrawPenaltyNextTurn = Mathf.Max(
                        enemyDrawPenaltyNextTurn,
                        amount);
                    break;
                case EnemySpecialEffect.ActionDrain:
                    enemyActionPenaltyNextTurn = Mathf.Max(
                        enemyActionPenaltyNextTurn,
                        amount);
                    break;
                case EnemySpecialEffect.GoldLoss:
                    int lostGold = Mathf.Min(gold, amount * 8);
                    gold -= lostGold;
                    AddLog(LF("enemy.effect.goldLoss", lostGold));
                    break;
            }
        }

        private EncounterDifficulty GetEncounterDifficulty()
        {
            return currentDifficulty switch
            {
                RunDifficulty.Hard => EncounterDifficulty.Hard,
                RunDifficulty.Normal => EncounterDifficulty.Normal,
                _ => EncounterDifficulty.Easy
            };
        }

        private string GetEliteAffixName(EliteAffix affix)
        {
            return affix switch
            {
                EliteAffix.Seal => L("enemy.affix.seal"),
                EliteAffix.Frenzy => L("enemy.affix.frenzy"),
                _ => L("enemy.affix.usury")
            };
        }

        private bool TryGetEnemyBehaviorCatalog(out EnemyBehaviorCatalog catalog)
        {
            if (!enemyBehaviorCatalogLoadAttempted)
            {
                enemyBehaviorCatalogLoadAttempted = true;
                TextAsset source = Resources.Load<TextAsset>(
                    EnemyBehaviorCatalogResourcePath);
                try
                {
                    cachedEnemyBehaviorCatalog = EnemyBehaviorCatalog.Load(source);
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"Enemy behavior catalog fallback: {exception.Message}");
                }
            }

            catalog = cachedEnemyBehaviorCatalog;
            return catalog != null;
        }
    }
}
