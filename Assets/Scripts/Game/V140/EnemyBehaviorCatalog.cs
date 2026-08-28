using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThreeDoorsOfFate.Game.V140
{
    public enum EnemyArchetype
    {
        Attack,
        Guard,
        Collector,
        Disruptor,
        Regenerator
    }

    public enum EnemyActionKind
    {
        Attack,
        AttackVulnerable,
        Debt,
        DebtAttack,
        LuckDown,
        LuckAttack,
        DrawPenalty,
        ActionDrain,
        Guard,
        GuardAttack,
        MultiAttack,
        Regenerate,
        GoldAttack,
        GuardGold,
        AttackHeal,
        LowHealthAttack,
        Discard,
        GoldDebt
    }

    public enum EncounterDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum EliteAffix
    {
        Usury,
        Seal,
        Frenzy
    }

    [Serializable]
    internal sealed class EnemyBehaviorCatalogData
    {
        public int schemaVersion;
        public EnemyBehaviorProfile[] behaviors = Array.Empty<EnemyBehaviorProfile>();
    }

    [Serializable]
    public sealed class EnemyPhaseDefinition
    {
        [SerializeField] private int maximumHealthPercent;
        [SerializeField] private string[] actionIds = Array.Empty<string>();

        public int MaximumHealthPercent => maximumHealthPercent;
        public IReadOnlyList<string> ActionIds => actionIds;
    }

    [Serializable]
    public sealed class EnemyActionDefinition
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string nameKey = string.Empty;
        [SerializeField] private string kind = string.Empty;
        [SerializeField] private int weight;
        [SerializeField] private int power;
        [SerializeField] private bool unique;

        public string Id => id;
        public string NameKey => nameKey;
        public EnemyActionKind Kind => Enum.Parse<EnemyActionKind>(kind, false);
        public int Weight => weight;
        public int Power => power;
        public bool Unique => unique;
        internal string KindName => kind;
    }

    [Serializable]
    public sealed class EnemyBehaviorProfile
    {
        [SerializeField] private string enemyId = string.Empty;
        [SerializeField] private string archetype = string.Empty;
        [SerializeField] private EnemyPhaseDefinition[] phases =
            Array.Empty<EnemyPhaseDefinition>();
        [SerializeField] private EnemyActionDefinition[] actions =
            Array.Empty<EnemyActionDefinition>();

        public string EnemyId => enemyId;
        public EnemyArchetype Archetype => Enum.Parse<EnemyArchetype>(archetype, false);
        public IReadOnlyList<EnemyPhaseDefinition> Phases => phases;
        public IReadOnlyList<EnemyActionDefinition> Actions => actions;
        internal string ArchetypeName => archetype;

        public IReadOnlyList<EnemyActionDefinition> GetActionsForPhase(int phaseIndex)
        {
            if (phases == null || phases.Length == 0)
            {
                return actions;
            }

            int safeIndex = Mathf.Clamp(phaseIndex, 0, phases.Length - 1);
            HashSet<string> allowed = new(
                phases[safeIndex].ActionIds,
                StringComparer.Ordinal);
            return actions.Where(action => allowed.Contains(action.Id)).ToArray();
        }
    }

    public sealed class EnemyBehaviorCatalog
    {
        private readonly IReadOnlyDictionary<string, EnemyBehaviorProfile> byEnemyId;

        private EnemyBehaviorCatalog(EnemyBehaviorCatalogData data)
        {
            SchemaVersion = data.schemaVersion;
            Profiles = data.behaviors.ToArray();
            byEnemyId = Profiles.ToDictionary(
                profile => profile.EnemyId,
                profile => profile,
                StringComparer.Ordinal);
        }

        public int SchemaVersion { get; }
        public IReadOnlyList<EnemyBehaviorProfile> Profiles { get; }

        public static EnemyBehaviorCatalog Load(TextAsset source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EnemyBehaviorCatalogData data;
            try
            {
                data = JsonUtility.FromJson<EnemyBehaviorCatalogData>(source.text);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    "Enemy behavior catalog is not valid JSON.",
                    exception);
            }

            Validate(data);
            return new EnemyBehaviorCatalog(data);
        }

        public EnemyBehaviorProfile Get(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId)
                || !byEnemyId.TryGetValue(enemyId, out EnemyBehaviorProfile profile))
            {
                throw new InvalidOperationException(
                    $"Unknown enemy behavior: {enemyId ?? string.Empty}");
            }

            return profile;
        }

        public bool TryGet(string enemyId, out EnemyBehaviorProfile profile)
        {
            return byEnemyId.TryGetValue(enemyId ?? string.Empty, out profile);
        }

        private static void Validate(EnemyBehaviorCatalogData data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Enemy behavior catalog is empty.");
            }

            if (data.schemaVersion != 1)
            {
                throw new InvalidOperationException(
                    $"Unsupported enemy behavior schema: {data.schemaVersion}.");
            }

            data.behaviors ??= Array.Empty<EnemyBehaviorProfile>();
            if (data.behaviors.Length != 24)
            {
                throw new InvalidOperationException(
                    "Exactly twenty standard enemies and four bosses are required.");
            }

            HashSet<string> enemyIds = new(StringComparer.Ordinal);
            int bossCount = 0;
            foreach (EnemyBehaviorProfile profile in data.behaviors)
            {
                if (profile == null
                    || string.IsNullOrWhiteSpace(profile.EnemyId)
                    || !enemyIds.Add(profile.EnemyId)
                    || !Enum.TryParse(
                        profile.ArchetypeName,
                        false,
                        out EnemyArchetype _)
                    || profile.Actions == null
                    || profile.Actions.Count < 3)
                {
                    throw new InvalidOperationException(
                        "Enemy profiles must be unique and complete.");
                }

                ValidateActions(profile);
                bool isBoss = profile.EnemyId.StartsWith(
                    "boss_",
                    StringComparison.Ordinal);
                if (isBoss)
                {
                    bossCount += 1;
                    ValidateBossPhases(profile);
                }
                else if (profile.Actions.Count(action => action.Unique) < 2
                    || profile.Phases.Count != 0)
                {
                    throw new InvalidOperationException(
                        $"Standard enemy {profile.EnemyId} needs two unique actions and no phases.");
                }
            }

            if (bossCount != 4)
            {
                throw new InvalidOperationException("Exactly four boss profiles are required.");
            }
        }

        private static void ValidateActions(EnemyBehaviorProfile profile)
        {
            HashSet<string> actionIds = new(StringComparer.Ordinal);
            foreach (EnemyActionDefinition action in profile.Actions)
            {
                if (action == null
                    || string.IsNullOrWhiteSpace(action.Id)
                    || !actionIds.Add(action.Id)
                    || string.IsNullOrWhiteSpace(action.NameKey)
                    || action.Weight <= 0
                    || action.Power <= 0
                    || !Enum.TryParse(
                        action.KindName,
                        false,
                        out EnemyActionKind _))
                {
                    throw new InvalidOperationException(
                        $"Enemy {profile.EnemyId} has an invalid action.");
                }
            }
        }

        private static void ValidateBossPhases(EnemyBehaviorProfile profile)
        {
            int[] expectedThresholds = { 100, 70, 35 };
            if (profile.Phases == null || profile.Phases.Count != expectedThresholds.Length)
            {
                throw new InvalidOperationException(
                    $"Boss {profile.EnemyId} needs three phases.");
            }

            HashSet<string> actionIds = new(
                profile.Actions.Select(action => action.Id),
                StringComparer.Ordinal);
            for (int index = 0; index < expectedThresholds.Length; index += 1)
            {
                EnemyPhaseDefinition phase = profile.Phases[index];
                if (phase == null
                    || phase.MaximumHealthPercent != expectedThresholds[index]
                    || phase.ActionIds == null
                    || phase.ActionIds.Count < 2
                    || phase.ActionIds.Any(id => !actionIds.Contains(id)))
                {
                    throw new InvalidOperationException(
                        $"Boss {profile.EnemyId} phase {index} is invalid.");
                }
            }
        }
    }

    public sealed class EnemyIntentHistory
    {
        private readonly List<string> recentActionIds = new(2);

        public IReadOnlyList<string> RecentActionIds => recentActionIds;

        public void Record(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            recentActionIds.Add(actionId);
            while (recentActionIds.Count > 2)
            {
                recentActionIds.RemoveAt(0);
            }
        }

        public void Clear()
        {
            recentActionIds.Clear();
        }

        public bool WouldRepeatThirdTime(string actionId)
        {
            return recentActionIds.Count == 2
                && string.Equals(recentActionIds[0], actionId, StringComparison.Ordinal)
                && string.Equals(recentActionIds[1], actionId, StringComparison.Ordinal);
        }
    }

    public sealed class EnemySelectionState
    {
        public int EnemyHealth { get; set; }
        public int EnemyMaxHealth { get; set; }
        public int EnemyBlock { get; set; }
        public int EnemyBaseAttack { get; set; }
        public int EnemyBaseBlock { get; set; }
        public int PlayerHealth { get; set; }
        public int PlayerBlock { get; set; }
        public int PlayerDebt { get; set; }
        public bool IsElite { get; set; }
        public bool IsEndless { get; set; }
    }

    public static class EncounterDirector
    {
        public static EnemyActionDefinition SelectAction(
            EnemyBehaviorProfile profile,
            int phaseIndex,
            EncounterDifficulty difficulty,
            EnemySelectionState state,
            SeededRunRandom random,
            EnemyIntentHistory history)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            history ??= new EnemyIntentHistory();
            List<EnemyActionDefinition> candidates = profile
                .GetActionsForPhase(phaseIndex)
                .ToList();
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Enemy {profile.EnemyId} has no actions for phase {phaseIndex}.");
            }

            if (difficulty != EncounterDifficulty.Hard && candidates.Count > 1)
            {
                List<EnemyActionDefinition> guarded = candidates
                    .Where(action => !history.WouldRepeatThirdTime(action.Id))
                    .ToList();
                if (guarded.Count > 0)
                {
                    candidates = guarded;
                }
            }

            float[] weights = candidates
                .Select(action => GetActionWeight(
                    profile.Archetype,
                    action,
                    difficulty,
                    state))
                .ToArray();
            float total = weights.Sum();
            float roll = random.Value() * total;
            for (int index = 0; index < candidates.Count; index += 1)
            {
                roll -= weights[index];
                if (roll <= 0f)
                {
                    return candidates[index];
                }
            }

            return candidates[^1];
        }

        public static int GetBossPhaseIndex(int health, int maximumHealth)
        {
            if (maximumHealth <= 0)
            {
                return 0;
            }

            float percent = Mathf.Clamp01(health / (float)maximumHealth) * 100f;
            if (percent <= 35f)
            {
                return 2;
            }

            return percent <= 70f ? 1 : 0;
        }

        public static EliteAffix SelectEliteAffix(SeededRunRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return (EliteAffix)random.Range(0, 3);
        }

        private static float GetActionWeight(
            EnemyArchetype archetype,
            EnemyActionDefinition action,
            EncounterDifficulty difficulty,
            EnemySelectionState state)
        {
            float score = Mathf.Max(1, action.Weight);
            bool attacks = IsAttack(action.Kind);
            bool guards = action.Kind is EnemyActionKind.Guard
                or EnemyActionKind.GuardAttack
                or EnemyActionKind.GuardGold;
            bool regenerates = action.Kind is EnemyActionKind.Regenerate
                or EnemyActionKind.AttackHeal;
            bool collects = action.Kind is EnemyActionKind.Debt
                or EnemyActionKind.DebtAttack
                or EnemyActionKind.GoldAttack
                or EnemyActionKind.GuardGold
                or EnemyActionKind.GoldDebt;
            bool disrupts = action.Kind is EnemyActionKind.LuckDown
                or EnemyActionKind.LuckAttack
                or EnemyActionKind.DrawPenalty
                or EnemyActionKind.ActionDrain
                or EnemyActionKind.Discard
                or EnemyActionKind.AttackVulnerable;

            score *= archetype switch
            {
                EnemyArchetype.Attack when attacks => 1.30f,
                EnemyArchetype.Guard when guards => 1.32f,
                EnemyArchetype.Collector when collects => 1.34f,
                EnemyArchetype.Disruptor when disrupts => 1.34f,
                EnemyArchetype.Regenerator when regenerates => 1.35f,
                _ => 1f
            };

            float enemyHealthRatio = state.EnemyMaxHealth <= 0
                ? 1f
                : state.EnemyHealth / (float)state.EnemyMaxHealth;
            if ((guards || regenerates) && enemyHealthRatio <= 0.45f)
            {
                score *= 1.55f;
            }

            if (collects && state.PlayerDebt <= 2)
            {
                score *= 1.22f;
            }

            if (attacks && state.PlayerBlock == 0)
            {
                score *= 1.14f;
            }

            int estimatedDamage = EstimateAttackDamage(action, state.EnemyBaseAttack);
            if (estimatedDamage >= state.PlayerHealth && state.PlayerHealth > 0)
            {
                score *= difficulty == EncounterDifficulty.Hard || state.IsEndless
                    ? 1.85f
                    : 1.08f;
            }

            return Mathf.Clamp(score, 0.25f, 100f);
        }

        private static bool IsAttack(EnemyActionKind kind)
        {
            return kind is EnemyActionKind.Attack
                or EnemyActionKind.AttackVulnerable
                or EnemyActionKind.DebtAttack
                or EnemyActionKind.LuckAttack
                or EnemyActionKind.GuardAttack
                or EnemyActionKind.MultiAttack
                or EnemyActionKind.GoldAttack
                or EnemyActionKind.AttackHeal
                or EnemyActionKind.LowHealthAttack;
        }

        private static int EstimateAttackDamage(
            EnemyActionDefinition action,
            int baseAttack)
        {
            if (!IsAttack(action.Kind))
            {
                return 0;
            }

            float multiplier = action.Kind == EnemyActionKind.MultiAttack
                ? action.Power * 0.02f
                : action.Power * 0.01f;
            return Mathf.Max(1, Mathf.RoundToInt(baseAttack * multiplier));
        }
    }
}
