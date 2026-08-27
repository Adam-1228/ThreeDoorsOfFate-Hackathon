using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Game.V140;
using UnityEngine;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private const int NoPendingDoorType = -1;

        private string activeRunId = string.Empty;
        private readonly List<DoorType> pendingDoorTypes = new();
        private readonly HashSet<string> seenRunEventIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> activeEndlessMutationIds = new(StringComparer.Ordinal);
        private readonly List<string> pendingRewardCardIds = new();
        private string pendingRunEventId = string.Empty;
        private int pendingResolvedDoorTypeId = NoPendingDoorType;
        private int currentEncounterSeed;
        private GamePhase checkpointResumePhase = GamePhase.DoorSelection;
        private RunSaveDataV2 restoredRunCheckpoint;
        private string lastRunRestoreErrorKey = string.Empty;
        private string lastRunRestoreErrorDetail = string.Empty;

        private string CaptureRunCheckpointV2()
        {
            if (runRandom == null)
            {
                ResetRunRandom(runSeed == 0 ? 1 : runSeed);
            }

            if (string.IsNullOrWhiteSpace(activeRunId))
            {
                activeRunId = Guid.NewGuid().ToString("N");
            }

            RunRandomSnapshot random = runRandom.Capture();
            int logStart = Mathf.Max(0, combatLog.Count - 24);
            RunSaveDataV2 checkpoint = new()
            {
                version = HardRunSaveVersion,
                runId = activeRunId,
                runSeed = random.Seed,
                randomState = random.State,
                randomCursor = random.Cursor,
                selectedStarterContractId = selectedStarterContractId ?? string.Empty,
                cardsRemovedThisRun = Mathf.Max(0, cardsRemovedThisRun),
                seenEventIds = seenRunEventIds.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                seenEventSegment = seenRunEventSegment,
                activeMutationIds = activeEndlessMutationIds.OrderBy(value => value, StringComparer.Ordinal).ToList(),
                pendingDoorTypeIds = pendingDoorTypes.Select(value => (int)value).ToList(),
                pendingResolvedDoorTypeId = pendingResolvedDoorTypeId,
                savedPhase = (int)phase,
                selectedClass = (int)selectedClass,
                currentDifficulty = (int)currentDifficulty,
                currentJourneyEndingKind = (int)currentJourneyEndingKind,
                endlessModeActive = endlessModeActive,
                nextEndlessBossRoom = nextEndlessBossRoom,
                endlessBossesDefeated = endlessBossesDefeated,
                playerMaxHealth = playerMaxHealth,
                playerHealth = playerHealth,
                playerBlock = playerBlock,
                action = action,
                luck = luck,
                gold = gold,
                debt = debt,
                roomsCleared = roomsCleared,
                combatEncountersCompleted = combatEncountersCompleted,
                consecutiveNonCombatDoors = consecutiveNonCombatDoors,
                storedLuck = storedLuck,
                curseReduction = curseReduction,
                hasStoredLuck = hasStoredLuck,
                keepLuckNextTurn = keepLuckNextTurn,
                doorInsightLevel = doorInsightLevel,
                retainBlockNextTurn = retainBlockNextTurn,
                deckCardIds = deck
                    .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CardId))
                    .Select(card => card.CardId)
                    .ToList(),
                equippedItemIds = equippedRunItemIds.Take(GetRunItemSlotLimit()).ToList(),
                combatLog = combatLog.Skip(logStart).ToList(),
                buildUpgradeLevels = buildUpgradeLevels
                    .Select(pair => new RunSaveBuildUpgrade { id = pair.Key, level = pair.Value })
                    .ToList(),
                pendingShopCardIds = currentShopCards
                    .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CardId))
                    .Select(card => card.CardId)
                    .ToList(),
                purchasedShopCardSlotIds = purchasedShopCardSlots.OrderBy(index => index).ToList(),
                pendingShopRunItemId = currentShopRunItemId ?? string.Empty,
                pendingShopRunItemPurchased = currentShopRunItemPurchased,
                pendingShopRemovalUsed = currentShopRemovalUsed,
                pendingShopOffersReady = currentShopOffersReady,
                pendingRewardCardIds = pendingRewardCardIds.ToList(),
                pendingEventId = pendingRunEventId ?? string.Empty,
                currentCombatDoorTypeId = (int)currentCombatDoorType,
                encounterSeed = currentEncounterSeed
            };

            if (phase == GamePhase.Combat && enemy != null)
            {
                checkpoint.encounterEnemyId = enemy.Id ?? string.Empty;
                checkpoint.encounterEnemyName = enemy.Name ?? string.Empty;
                checkpoint.encounterMaxHealth = enemy.MaxHealth;
                checkpoint.encounterBaseAttack = enemy.BaseAttack;
                checkpoint.encounterBaseBlock = enemy.BaseBlock;
                checkpoint.encounterWasElite = enemy.WasElite;
                checkpoint.encounterIsBoss = enemy.IsBoss;
                checkpoint.encounterBaseGoldReward = enemy.BaseGoldReward;
            }

            return JsonUtility.ToJson(checkpoint);
        }

        private bool TryRestoreRunCheckpointV2(string json)
        {
            if (!TryPrepareRunCheckpointV2(
                    json,
                    out RunSaveDataV2 checkpoint,
                    out List<CardData> restoredDeck,
                    out List<CardData> restoredShopCards,
                    out List<CardData> restoredRewards))
            {
                return false;
            }

            StopCombatVictorySequence();
            selectedClass = (CharacterClass)checkpoint.selectedClass;
            LoadDiscoveredRunItemsForSelectedClass();
            currentDifficulty = (RunDifficulty)checkpoint.currentDifficulty;
            currentJourneyEndingKind = (JourneyEndingKind)checkpoint.currentJourneyEndingKind;
            endlessModeActive = checkpoint.endlessModeActive;
            nextEndlessBossRoom = Mathf.Max(0, checkpoint.nextEndlessBossRoom);
            endlessBossesDefeated = Mathf.Max(0, checkpoint.endlessBossesDefeated);
            playerMaxHealth = Mathf.Max(1, checkpoint.playerMaxHealth);
            playerHealth = Mathf.Clamp(checkpoint.playerHealth, 1, playerMaxHealth);
            playerBlock = Mathf.Max(0, checkpoint.playerBlock);
            action = Mathf.Max(0, checkpoint.action);
            luck = Mathf.Clamp(checkpoint.luck, 1, 6);
            gold = Mathf.Max(0, checkpoint.gold);
            debt = Mathf.Max(0, checkpoint.debt);
            roomsCleared = Mathf.Max(0, checkpoint.roomsCleared);
            combatEncountersCompleted = Mathf.Max(0, checkpoint.combatEncountersCompleted);
            consecutiveNonCombatDoors = Mathf.Max(0, checkpoint.consecutiveNonCombatDoors);
            storedLuck = Mathf.Clamp(checkpoint.storedLuck, 0, 6);
            reflectedDamage = 0;
            curseReduction = Mathf.Max(0, checkpoint.curseReduction);
            pendingDamageReduction = 0;
            hasStoredLuck = checkpoint.hasStoredLuck;
            keepLuckNextTurn = checkpoint.keepLuckNextTurn;
            doorInsightLevel = Mathf.Clamp(checkpoint.doorInsightLevel, 0, 3);
            predictedBossRunItemRewardId = string.Empty;
            retainBlockNextTurn = checkpoint.retainBlockNextTurn;
            preventDeathThisTurn = false;
            combatVictorySequenceActive = false;
            enemy = null;
            phase = GamePhase.DoorSelection;

            deck.Clear();
            deck.AddRange(restoredDeck.Take(GetMaxDeckSize()));
            equippedRunItemIds.Clear();
            runItemBottleHealthBonusApplied = false;
            if (checkpoint.useProfileEquippedItems)
            {
                LoadEquippedRunItemsForSelectedClass();
            }
            else
            {
                foreach (string itemId in checkpoint.equippedItemIds)
                {
                    if (equippedRunItemIds.Count >= GetRunItemSlotLimit())
                    {
                        break;
                    }

                    RunItemDefinition item = GetRunItemDefinition(itemId);
                    if (item != null
                        && IsRunItemTypeSlotUnlocked(item.Type)
                        && !equippedRunItemIds.Contains(itemId))
                    {
                        equippedRunItemIds.Add(itemId);
                    }
                }

                SaveEquippedRunItemsForSelectedClass();
            }

            EnsureEquippedRunItemsAreDiscovered();
            runItemBottleHealthBonusApplied = HasRunItem("blessing_bottle_of_light");

            hand.Clear();
            drawPile.Clear();
            discardPile.Clear();
            oncePerCombatUsed.Clear();
            cardsPlayedThisTurn.Clear();
            cardsPlayedThisCombat.Clear();
            combinationTriggersThisTurn.Clear();
            combinationTriggersThisCombat.Clear();
            runItemTriggersThisCombat.Clear();
            runItemSkillDiscountsRemaining = 0;
            activeCard = null;
            activeCardHandIndex = -1;
            activeCardDamageBonusApplied = false;
            activeCardBlockBonusApplied = false;
            activeCardRunItemDamageBonusApplied = false;
            activeCardRunItemBlockBonusApplied = false;
            forbiddenCycleActiveThisTurn = false;
            pendingCombinationDamageBonus = 0;
            pendingCombinationDamageBonusSourceId = string.Empty;

            buildUpgradeLevels.Clear();
            foreach (RunSaveBuildUpgrade upgrade in checkpoint.buildUpgradeLevels)
            {
                if (!string.IsNullOrWhiteSpace(upgrade.id) && upgrade.level > 0)
                {
                    buildUpgradeLevels[upgrade.id] = upgrade.level;
                }
            }

            activeRunId = checkpoint.runId;
            runSeed = checkpoint.runSeed;
            runRandom = new SeededRunRandom(new RunRandomSnapshot
            {
                Seed = checkpoint.runSeed,
                State = checkpoint.randomState,
                Cursor = checkpoint.randomCursor
            });
            selectedStarterContractId = checkpoint.selectedStarterContractId ?? string.Empty;
            cardsRemovedThisRun = Mathf.Max(0, checkpoint.cardsRemovedThisRun);
            ReplaceSet(seenRunEventIds, checkpoint.seenEventIds);
            seenRunEventSegment = Mathf.Max(0, checkpoint.seenEventSegment);
            ReplaceSet(activeEndlessMutationIds, checkpoint.activeMutationIds);
            pendingDoorTypes.Clear();
            pendingDoorTypes.AddRange(checkpoint.pendingDoorTypeIds.Select(id => (DoorType)id));
            pendingResolvedDoorTypeId = checkpoint.pendingResolvedDoorTypeId;
            checkpointResumePhase = (GamePhase)checkpoint.savedPhase;
            currentEncounterSeed = checkpoint.encounterSeed;
            currentCombatDoorType = (DoorType)checkpoint.currentCombatDoorTypeId;
            pendingRunEventId = checkpoint.pendingEventId ?? string.Empty;

            currentShopCards.Clear();
            currentShopCards.AddRange(restoredShopCards);
            purchasedShopCardSlots.Clear();
            foreach (int slot in checkpoint.purchasedShopCardSlotIds)
            {
                if (slot >= 0 && slot < currentShopCards.Count)
                {
                    purchasedShopCardSlots.Add(slot);
                }
            }
            currentShopRunItemId = checkpoint.pendingShopRunItemId ?? string.Empty;
            currentShopRunItemPurchased = checkpoint.pendingShopRunItemPurchased;
            currentShopRemovalUsed = checkpoint.pendingShopRemovalUsed;
            currentShopOffersReady = checkpoint.pendingShopOffersReady;
            pendingRewardCardIds.Clear();
            pendingRewardCardIds.AddRange(restoredRewards.Select(card => card.CardId));
            restoredRunCheckpoint = checkpoint;

            BackfillAchievementsAfterCheckpointRestore();
            combatLog.Clear();
            foreach (string entry in checkpoint.combatLog)
            {
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    AddLog(entry);
                }
            }

            lastRunRestoreErrorKey = string.Empty;
            lastRunRestoreErrorDetail = string.Empty;
            return true;
        }

        private bool TryMigrateRunCheckpointV1(string json, out RunSaveDataV2 migrated)
        {
            migrated = null;
            RunSaveDataV1 legacy;
            try
            {
                legacy = JsonUtility.FromJson<RunSaveDataV1>(json);
            }
            catch (ArgumentException)
            {
                return FailRunRestore("save.restore.error.corrupt", "v1-json");
            }

            if (!IsValidLegacyRunSave(legacy))
            {
                return FailRunRestore("save.restore.error.unsupported", "v1-schema");
            }

            uint hash = StableCheckpointHash(json);
            int migratedSeed = unchecked((int)(hash & 0x7fffffffu));
            if (migratedSeed == 0)
            {
                migratedSeed = 1;
            }

            RunRandomSnapshot random = new SeededRunRandom(migratedSeed).Capture();
            migrated = new RunSaveDataV2
            {
                version = HardRunSaveVersion,
                runId = $"legacy-{hash:x8}",
                runSeed = random.Seed,
                randomState = random.State,
                randomCursor = random.Cursor,
                savedPhase = (int)GamePhase.DoorSelection,
                pendingResolvedDoorTypeId = NoPendingDoorType,
                selectedClass = legacy.selectedClass,
                currentDifficulty = legacy.currentDifficulty,
                currentJourneyEndingKind = legacy.currentJourneyEndingKind,
                endlessModeActive = legacy.endlessModeActive,
                nextEndlessBossRoom = legacy.nextEndlessBossRoom,
                endlessBossesDefeated = legacy.endlessBossesDefeated,
                playerMaxHealth = legacy.playerMaxHealth,
                playerHealth = legacy.playerHealth,
                playerBlock = legacy.playerBlock,
                action = legacy.action,
                luck = legacy.luck,
                gold = legacy.gold,
                debt = legacy.debt,
                roomsCleared = legacy.roomsCleared,
                combatEncountersCompleted = legacy.combatEncountersCompleted,
                consecutiveNonCombatDoors = legacy.consecutiveNonCombatDoors,
                storedLuck = legacy.storedLuck,
                curseReduction = legacy.curseReduction,
                hasStoredLuck = legacy.hasStoredLuck,
                keepLuckNextTurn = legacy.keepLuckNextTurn,
                doorInsightLevel = legacy.doorInsightLevel,
                retainBlockNextTurn = legacy.retainBlockNextTurn,
                deckCardIds = CopyList(legacy.deckCardIds),
                equippedItemIds = CopyList(legacy.equippedItemIds),
                useProfileEquippedItems = legacy.equippedItemIds == null
                    || legacy.equippedItemIds.Count == 0,
                combatLog = CopyList(legacy.combatLog),
                buildUpgradeLevels = (legacy.buildUpgradeLevels ?? new List<RunSaveBuildUpgrade>())
                    .Where(value => value != null)
                    .Select(value => new RunSaveBuildUpgrade { id = value.id, level = value.level })
                    .ToList(),
                currentCombatDoorTypeId = (int)DoorType.Battle
            };
            return true;
        }

        private bool CanRestoreRunCheckpointV2(string json)
        {
            return TryPrepareRunCheckpointV2(
                json,
                out _,
                out _,
                out _,
                out _);
        }

        private bool TryPrepareRunCheckpointV2(
            string json,
            out RunSaveDataV2 checkpoint,
            out List<CardData> restoredDeck,
            out List<CardData> restoredShopCards,
            out List<CardData> restoredRewards)
        {
            checkpoint = null;
            restoredDeck = new List<CardData>();
            restoredShopCards = new List<CardData>();
            restoredRewards = new List<CardData>();
            try
            {
                checkpoint = JsonUtility.FromJson<RunSaveDataV2>(json);
            }
            catch (ArgumentException)
            {
                return FailRunRestore("save.restore.error.corrupt", "v2-json");
            }

            NormalizeCheckpointLists(checkpoint);
            if (!IsValidRunCheckpointV2(checkpoint))
            {
                return FailRunRestore("save.restore.error.unsupported", "v2-schema");
            }

            if (!TryRestoreDeck(checkpoint.deckCardIds, restoredDeck))
            {
                return FailRunRestore("save.restore.error.unknownCard", "deck");
            }

            if (!TryResolveOptionalCards(checkpoint.pendingShopCardIds, restoredShopCards)
                || !TryResolveOptionalCards(checkpoint.pendingRewardCardIds, restoredRewards))
            {
                return FailRunRestore("save.restore.error.unknownCard", "offer");
            }

            foreach (string itemId in checkpoint.equippedItemIds)
            {
                if (string.IsNullOrWhiteSpace(itemId) || GetRunItemDefinition(itemId) == null)
                {
                    return FailRunRestore("save.restore.error.unknownItem", itemId);
                }
            }

            if (!string.IsNullOrWhiteSpace(checkpoint.pendingShopRunItemId)
                && GetRunItemDefinition(checkpoint.pendingShopRunItemId) == null)
            {
                return FailRunRestore(
                    "save.restore.error.unknownItem",
                    checkpoint.pendingShopRunItemId);
            }

            return true;
        }

        private bool TryResolveOptionalCards(
            IReadOnlyList<string> cardIds,
            List<CardData> restoredCards)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return true;
            }

            return TryRestoreDeck(cardIds, restoredCards);
        }

        private bool IsValidRunCheckpointV2(RunSaveDataV2 checkpoint)
        {
            return checkpoint != null
                && checkpoint.version == HardRunSaveVersion
                && !string.IsNullOrWhiteSpace(checkpoint.runId)
                && checkpoint.runSeed != 0
                && checkpoint.randomState != 0u
                && checkpoint.randomCursor >= 0
                && checkpoint.selectedClass >= (int)CharacterClass.Gambler
                && checkpoint.selectedClass <= (int)CharacterClass.Exile
                && checkpoint.currentDifficulty >= (int)RunDifficulty.Easy
                && checkpoint.currentDifficulty <= (int)RunDifficulty.Hard
                && checkpoint.currentJourneyEndingKind >= (int)JourneyEndingKind.Return
                && checkpoint.currentJourneyEndingKind <= (int)JourneyEndingKind.EndlessReturn
                && checkpoint.deckCardIds.Count > 0
                && checkpoint.playerMaxHealth > 0
                && checkpoint.playerHealth > 0
                && Enum.IsDefined(typeof(GamePhase), checkpoint.savedPhase)
                && checkpoint.savedPhase != (int)GamePhase.MainMenu
                && checkpoint.savedPhase != (int)GamePhase.ClassSelection
                && checkpoint.savedPhase != (int)GamePhase.ClassDetails
                && checkpoint.savedPhase != (int)GamePhase.ContractSelection
                && checkpoint.savedPhase != (int)GamePhase.GameOver
                && checkpoint.pendingDoorTypeIds.All(IsKnownDoorTypeId)
                && (checkpoint.pendingResolvedDoorTypeId == NoPendingDoorType
                    || IsKnownDoorTypeId(checkpoint.pendingResolvedDoorTypeId))
                && IsKnownDoorTypeId(checkpoint.currentCombatDoorTypeId);
        }

        private static bool IsValidLegacyRunSave(RunSaveDataV1 checkpoint)
        {
            return checkpoint != null
                && checkpoint.version == LegacyRunSaveVersion
                && checkpoint.selectedClass >= (int)CharacterClass.Gambler
                && checkpoint.selectedClass <= (int)CharacterClass.Exile
                && checkpoint.currentDifficulty >= (int)RunDifficulty.Easy
                && checkpoint.currentDifficulty <= (int)RunDifficulty.Hard
                && checkpoint.currentJourneyEndingKind >= (int)JourneyEndingKind.Return
                && checkpoint.currentJourneyEndingKind <= (int)JourneyEndingKind.EndlessReturn
                && (checkpoint.currentDifficulty == (int)RunDifficulty.Hard
                    || checkpoint.endlessModeActive)
                && checkpoint.deckCardIds != null
                && checkpoint.deckCardIds.Count > 0
                && checkpoint.playerMaxHealth > 0
                && checkpoint.playerHealth > 0;
        }

        private static void NormalizeCheckpointLists(RunSaveDataV2 checkpoint)
        {
            if (checkpoint == null)
            {
                return;
            }

            checkpoint.deckCardIds ??= new List<string>();
            checkpoint.equippedItemIds ??= new List<string>();
            checkpoint.combatLog ??= new List<string>();
            checkpoint.buildUpgradeLevels ??= new List<RunSaveBuildUpgrade>();
            checkpoint.seenEventIds ??= new List<string>();
            checkpoint.activeMutationIds ??= new List<string>();
            checkpoint.pendingDoorTypeIds ??= new List<int>();
            checkpoint.pendingShopCardIds ??= new List<string>();
            checkpoint.purchasedShopCardSlotIds ??= new List<int>();
            checkpoint.pendingRewardCardIds ??= new List<string>();
        }

        private static bool IsKnownDoorTypeId(int value)
        {
            return Enum.IsDefined(typeof(DoorType), value);
        }

        private bool FailRunRestore(string localizationKey, string detail)
        {
            lastRunRestoreErrorKey = localizationKey ?? string.Empty;
            lastRunRestoreErrorDetail = detail ?? string.Empty;
            return false;
        }

        private string GetRunRestoreFailureMessage()
        {
            string key = string.IsNullOrWhiteSpace(lastRunRestoreErrorKey)
                ? "save.restore.error.corrupt"
                : lastRunRestoreErrorKey;
            return L(key);
        }

        private void ResetCheckpointStateForNewRun()
        {
            activeRunId = Guid.NewGuid().ToString("N");
            pendingDoorTypes.Clear();
            seenRunEventIds.Clear();
            seenRunEventSegment = 0;
            activeEndlessMutationIds.Clear();
            pendingEndlessMutationChoices =
                Array.Empty<EndlessMutationDefinition>();
            pendingRewardCardIds.Clear();
            pendingRunEventId = string.Empty;
            pendingResolvedDoorTypeId = NoPendingDoorType;
            currentEncounterSeed = 0;
            checkpointResumePhase = GamePhase.DoorSelection;
            restoredRunCheckpoint = null;
            lastRunRestoreErrorKey = string.Empty;
            lastRunRestoreErrorDetail = string.Empty;
        }

        private void ResumeRestoredRunCheckpoint()
        {
            if (pendingResolvedDoorTypeId != NoPendingDoorType)
            {
                DoorType destination = (DoorType)pendingResolvedDoorTypeId;
                pendingResolvedDoorTypeId = NoPendingDoorType;
                ResumeResolvedDoor(destination);
                return;
            }

            switch (checkpointResumePhase)
            {
                case GamePhase.Combat:
                    if (TryCreateCheckpointEnemy(restoredRunCheckpoint, out EnemyState restoredEnemy))
                    {
                        StartCombat(restoredEnemy);
                        return;
                    }
                    break;
                case GamePhase.Shop:
                    ShowShop();
                    return;
                case GamePhase.Reward:
                    if (TryResolvePendingRewards(out List<CardData> rewards))
                    {
                        ShowReward(rewards);
                        return;
                    }
                    break;
                case GamePhase.Event:
                    ShowEvent();
                    return;
                case GamePhase.Rest:
                    ShowRest();
                    return;
                case GamePhase.Curse:
                    ShowCurseEvent();
                    return;
                case GamePhase.DoorSelection:
                    ShowDoors();
                    return;
            }

            FailRunRestore("save.restore.error.corrupt", "resume-phase");
            ShowMainMenu();
        }

        private void ResumeResolvedDoor(DoorType type)
        {
            switch (type)
            {
                case DoorType.Battle:
                    currentCombatDoorType = DoorType.Battle;
                    StartCombat(CreateEnemy(false, false));
                    break;
                case DoorType.Elite:
                    currentCombatDoorType = DoorType.Elite;
                    StartCombat(CreateEnemy(true, false));
                    break;
                case DoorType.Shop:
                    ResetCurrentShopOffers();
                    ShowShop();
                    break;
                case DoorType.Treasure:
                    ShowTreasure();
                    break;
                case DoorType.Event:
                    ShowEvent();
                    break;
                case DoorType.Rest:
                    ShowRest();
                    break;
                case DoorType.Curse:
                    ShowCurseEvent();
                    break;
                case DoorType.Boss:
                    currentCombatDoorType = DoorType.Boss;
                    StartCombat(CreateEnemy(true, true));
                    break;
            }
        }

        private bool TryResolvePendingRewards(out List<CardData> rewards)
        {
            rewards = new List<CardData>();
            return TryResolveOptionalCards(pendingRewardCardIds, rewards)
                && rewards.Count > 0;
        }

        private static bool TryCreateCheckpointEnemy(
            RunSaveDataV2 checkpoint,
            out EnemyState restoredEnemy)
        {
            restoredEnemy = null;
            if (checkpoint == null
                || string.IsNullOrWhiteSpace(checkpoint.encounterEnemyId)
                || checkpoint.encounterMaxHealth <= 0)
            {
                return false;
            }

            restoredEnemy = new EnemyState(
                checkpoint.encounterEnemyId,
                string.IsNullOrWhiteSpace(checkpoint.encounterEnemyName)
                    ? checkpoint.encounterEnemyId
                    : checkpoint.encounterEnemyName,
                checkpoint.encounterMaxHealth,
                Mathf.Max(1, checkpoint.encounterBaseAttack),
                Mathf.Max(0, checkpoint.encounterBaseBlock),
                checkpoint.encounterWasElite,
                checkpoint.encounterIsBoss,
                Mathf.Max(0, checkpoint.encounterBaseGoldReward));
            return true;
        }

        private static void ReplaceSet(ISet<string> destination, IEnumerable<string> values)
        {
            destination.Clear();
            foreach (string value in values ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    destination.Add(value);
                }
            }
        }

        private static List<string> CopyList(IEnumerable<string> values)
        {
            return values?.ToList() ?? new List<string>();
        }

        private static uint StableCheckpointHash(string value)
        {
            const uint offset = 2166136261u;
            const uint prime = 16777619u;
            uint hash = offset;
            foreach (byte item in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= item;
                hash *= prime;
            }

            return hash == 0u ? 1u : hash;
        }

        [Serializable]
        private sealed class RunSaveVersionProbe
        {
            public int version;
        }

        [Serializable]
        private sealed class RunSaveDataV2
        {
            public int version;
            public string runId = string.Empty;
            public int runSeed;
            public uint randomState;
            public int randomCursor;
            public string selectedStarterContractId = string.Empty;
            public int cardsRemovedThisRun;
            public List<string> seenEventIds = new();
            public int seenEventSegment;
            public List<string> activeMutationIds = new();
            public List<int> pendingDoorTypeIds = new();
            public int pendingResolvedDoorTypeId = NoPendingDoorType;
            public int savedPhase;
            public int selectedClass;
            public int currentDifficulty;
            public int currentJourneyEndingKind;
            public bool endlessModeActive;
            public int nextEndlessBossRoom;
            public int endlessBossesDefeated;
            public int playerMaxHealth;
            public int playerHealth;
            public int playerBlock;
            public int action;
            public int luck;
            public int gold;
            public int debt;
            public int roomsCleared;
            public int combatEncountersCompleted;
            public int consecutiveNonCombatDoors;
            public int storedLuck;
            public int curseReduction;
            public bool hasStoredLuck;
            public bool keepLuckNextTurn;
            public int doorInsightLevel;
            public bool retainBlockNextTurn;
            public List<string> deckCardIds = new();
            public List<string> equippedItemIds = new();
            public bool useProfileEquippedItems;
            public List<string> combatLog = new();
            public List<RunSaveBuildUpgrade> buildUpgradeLevels = new();
            public List<string> pendingShopCardIds = new();
            public List<int> purchasedShopCardSlotIds = new();
            public string pendingShopRunItemId = string.Empty;
            public bool pendingShopRunItemPurchased;
            public bool pendingShopRemovalUsed;
            public bool pendingShopOffersReady;
            public List<string> pendingRewardCardIds = new();
            public string pendingEventId = string.Empty;
            public int currentCombatDoorTypeId;
            public int encounterSeed;
            public string encounterEnemyId = string.Empty;
            public string encounterEnemyName = string.Empty;
            public int encounterMaxHealth;
            public int encounterBaseAttack;
            public int encounterBaseBlock;
            public bool encounterWasElite;
            public bool encounterIsBoss;
            public int encounterBaseGoldReward;
        }
    }
}
