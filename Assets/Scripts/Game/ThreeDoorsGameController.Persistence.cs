using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {

        private void AddHardRunSaveLoadControls(RectTransform modal)
        {
            if (phase == GamePhase.MainMenu)
            {
                if (!HasRestorableRunSave())
                {
                    return;
                }

                Button loadSavedRunButton = AddSettingsMenuButton(modal, "저장된 런 불러오기", "불러오기", 20);
                SetAnchors(loadSavedRunButton.GetComponent<RectTransform>(), new Vector2(0.35f, 0.245f), new Vector2(0.65f, 0.315f));
                loadSavedRunButton.onClick.AddListener(ContinueSavedRunFromSettings);
                return;
            }

            if (!CanUseRunSaveSystem())
            {
                PlayGameSfx(GameSfxCue.SaveFailure);
                return;
            }

            string saveLabel = phase == GamePhase.DoorSelection ? "저장하기" : "다음 문에서 저장";
            Button saveButton = AddSettingsMenuButton(modal, "런 저장", saveLabel, 19);
            SetAnchors(saveButton.GetComponent<RectTransform>(), new Vector2(0.185f, 0.245f), new Vector2(0.455f, 0.315f));
            saveButton.onClick.AddListener(SaveRunFromSettingsPanel);

            bool hasSave = HasRestorableRunSave();
            Button loadButton = AddSettingsMenuButton(modal, "런 불러오기", hasSave ? "불러오기" : "불러오기 없음", 19);
            SetAnchors(loadButton.GetComponent<RectTransform>(), new Vector2(0.545f, 0.245f), new Vector2(0.815f, 0.315f));
            loadButton.interactable = hasSave;
            if (hasSave)
            {
                loadButton.onClick.AddListener(ContinueSavedRunFromSettings);
            }
        }


        private void ContinueSavedRunFromSettings()
        {
            HideSettingsPanel();
            ContinueSavedRun();
        }


        private void SaveRunFromSettingsPanel()
        {
            if (!CanUseRunSaveSystem())
            {
                return;
            }

            if (phase != GamePhase.DoorSelection)
            {
                PlayGameSfx(GameSfxCue.SaveFailure);
                AddLog("저장은 다음 문 선택 화면에 도착하면 자동으로 진행됩니다.");
                HideSettingsPanel();
                RefreshLog();
                return;
            }

            bool saved = SaveRunCheckpoint();
            PlayGameSfx(saved ? GameSfxCue.SaveSuccess : GameSfxCue.SaveFailure);
            AddLog(saved ? "현재 런을 저장했습니다." : "저장할 카드 데이터가 부족해 현재 런을 저장하지 못했습니다.");
            HideSettingsPanel();
            RefreshTopBar();
            RefreshLog();
        }


        private bool CanUseRunSaveSystem()
        {
            return IsHardModeFeatureActive();
        }


        private bool CanSaveRunCheckpoint()
        {
            return phase == GamePhase.DoorSelection
                && CanUseRunSaveSystem()
                && playerHealth > 0
                && deck.Count > 0;
        }


        private void AutoSaveRunIfAllowed()
        {
            if (CanSaveRunCheckpoint())
            {
                SaveRunCheckpoint();
            }
        }


        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                AutoSaveRunIfAllowed();
            }
        }


        private bool SaveRunCheckpoint()
        {
            List<string> deckCardIds = deck
                .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CardId))
                .Select(card => card.CardId)
                .ToList();
            if (deckCardIds.Count == 0)
            {
                return false;
            }

            int logStart = Mathf.Max(0, combatLog.Count - 24);
            RunSaveData saveData = new()
            {
                version = HardRunSaveVersion,
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
                deckCardIds = deckCardIds,
                equippedItemIds = equippedRunItemIds.Take(GetRunItemSlotLimit()).ToList(),
                combatLog = combatLog.Skip(logStart).ToList(),
                buildUpgradeLevels = buildUpgradeLevels
                    .Select(pair => new RunSaveBuildUpgrade { id = pair.Key, level = pair.Value })
                    .ToList()
            };

            PlayerPrefs.SetString(HardRunSaveKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
            return true;
        }


        private void ContinueSavedRun()
        {
            if (TryLoadHardRunSave())
            {
                PlayGameSfx(GameSfxCue.LoadSuccess);
                topBar.gameObject.SetActive(true);
                SetLogVisible(true);
                AddLog("저장된 런을 이어갑니다.");
                ShowDoors();
                return;
            }

            PlayGameSfx(GameSfxCue.LoadFailure);
            ClearHardRunSave();
            ShowMainMenu();
        }


        private bool TryLoadHardRunSave()
        {
            string json = PlayerPrefs.GetString(HardRunSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            RunSaveData saveData;
            try
            {
                saveData = JsonUtility.FromJson<RunSaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (!IsValidRunSave(saveData))
            {
                return false;
            }

            List<CardData> restoredDeck = new();
            if (!TryRestoreDeck(saveData.deckCardIds, restoredDeck))
            {
                return false;
            }

            StopCombatVictorySequence();
            selectedClass = (CharacterClass)saveData.selectedClass;
            LoadDiscoveredRunItemsForSelectedClass();
            currentDifficulty = (RunDifficulty)saveData.currentDifficulty;
            currentJourneyEndingKind = (JourneyEndingKind)saveData.currentJourneyEndingKind;
            endlessModeActive = saveData.endlessModeActive;
            nextEndlessBossRoom = Mathf.Max(0, saveData.nextEndlessBossRoom);
            endlessBossesDefeated = Mathf.Max(0, saveData.endlessBossesDefeated);
            playerMaxHealth = Mathf.Max(1, saveData.playerMaxHealth);
            playerHealth = Mathf.Clamp(saveData.playerHealth, 1, playerMaxHealth);
            playerBlock = Mathf.Max(0, saveData.playerBlock);
            action = StartingAction;
            luck = Mathf.Clamp(saveData.luck, 1, 6);
            gold = Mathf.Max(0, saveData.gold);
            debt = Mathf.Max(0, saveData.debt);
            roomsCleared = Mathf.Max(0, saveData.roomsCleared);
            combatEncountersCompleted = Mathf.Max(0, saveData.combatEncountersCompleted);
            consecutiveNonCombatDoors = Mathf.Max(0, saveData.consecutiveNonCombatDoors);
            storedLuck = Mathf.Clamp(saveData.storedLuck, 0, 6);
            reflectedDamage = 0;
            curseReduction = Mathf.Max(0, saveData.curseReduction);
            pendingDamageReduction = 0;
            hasStoredLuck = saveData.hasStoredLuck;
            keepLuckNextTurn = saveData.keepLuckNextTurn;
            doorInsightLevel = Mathf.Clamp(saveData.doorInsightLevel, 0, 3);
            predictedBossRunItemRewardId = string.Empty;
            retainBlockNextTurn = saveData.retainBlockNextTurn;
            preventDeathThisTurn = false;
            combatVictorySequenceActive = false;
            enemy = null;
            phase = GamePhase.DoorSelection;

            deck.Clear();
            deck.AddRange(restoredDeck.Take(GetMaxDeckSize()));
            equippedRunItemIds.Clear();
            runItemBottleHealthBonusApplied = false;
            if (saveData.equippedItemIds == null || saveData.equippedItemIds.Count == 0)
            {
                LoadEquippedRunItemsForSelectedClass();
            }
            else
            {
                foreach (string itemId in saveData.equippedItemIds)
                {
                    if (equippedRunItemIds.Count >= GetRunItemSlotLimit())
                    {
                        break;
                    }

                    RunItemDefinition item = GetRunItemDefinition(itemId);
                    if (!string.IsNullOrWhiteSpace(itemId)
                        && item != null
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
            if (saveData.buildUpgradeLevels != null)
            {
                foreach (RunSaveBuildUpgrade upgrade in saveData.buildUpgradeLevels)
                {
                    if (!string.IsNullOrWhiteSpace(upgrade.id) && upgrade.level > 0)
                    {
                        buildUpgradeLevels[upgrade.id] = upgrade.level;
                    }
                }
            }

            combatLog.Clear();
            if (saveData.combatLog != null)
            {
                foreach (string entry in saveData.combatLog)
                {
                    if (!string.IsNullOrWhiteSpace(entry))
                    {
                        AddLog(entry);
                    }
                }
            }

            TryCompleteBuildAchievement();

            return true;
        }


        private bool HasRestorableRunSave()
        {
            string json = PlayerPrefs.GetString(HardRunSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                return IsValidRunSave(JsonUtility.FromJson<RunSaveData>(json));
            }
            catch (ArgumentException)
            {
                return false;
            }
        }


        private static bool IsValidRunSave(RunSaveData saveData)
        {
            return saveData != null
                && saveData.version == HardRunSaveVersion
                && saveData.selectedClass >= (int)CharacterClass.Gambler
                && saveData.selectedClass <= (int)CharacterClass.Exile
                && saveData.currentDifficulty >= (int)RunDifficulty.Easy
                && saveData.currentDifficulty <= (int)RunDifficulty.Hard
                && saveData.currentJourneyEndingKind >= (int)JourneyEndingKind.Return
                && saveData.currentJourneyEndingKind <= (int)JourneyEndingKind.EndlessReturn
                && (saveData.currentDifficulty == (int)RunDifficulty.Hard || saveData.endlessModeActive)
                && saveData.deckCardIds != null
                && saveData.deckCardIds.Count > 0
                && saveData.playerMaxHealth > 0
                && saveData.playerHealth > 0;
        }


        private bool TryRestoreDeck(IReadOnlyList<string> cardIds, List<CardData> restoredDeck)
        {
            if (cardIds == null || cardIds.Count == 0)
            {
                return false;
            }

            Dictionary<string, CardData> cardsById = cardPool
                .Where(card => card != null && !string.IsNullOrWhiteSpace(card.CardId))
                .GroupBy(card => card.CardId)
                .ToDictionary(group => group.Key, group => group.First());
            foreach (string cardId in cardIds)
            {
                if (string.IsNullOrWhiteSpace(cardId) || !cardsById.TryGetValue(cardId, out CardData card))
                {
                    return false;
                }

                restoredDeck.Add(card);
            }

            return true;
        }


        private static void ClearHardRunSave()
        {
            if (!PlayerPrefs.HasKey(HardRunSaveKey))
            {
                return;
            }

            PlayerPrefs.DeleteKey(HardRunSaveKey);
            PlayerPrefs.Save();
        }
    }
}
