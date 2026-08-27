using System;
using System.Collections.Generic;
using System.Linq;
using ThreeDoorsOfFate.Audio;
using ThreeDoorsOfFate.Cards;
using ThreeDoorsOfFate.Platform;
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

                Button loadSavedRunButton = AddSettingsMenuButton(
                    modal,
                    L("save.savedRun"),
                    L("save.load"),
                    20);
                SetAnchors(
                    loadSavedRunButton.GetComponent<RectTransform>(),
                    new Vector2(0.35f, 0.245f),
                    new Vector2(0.65f, 0.315f));
                loadSavedRunButton.onClick.AddListener(ContinueSavedRunFromSettings);
                return;
            }

            if (!CanUseRunSaveSystem())
            {
                PlayGameSfx(GameSfxCue.SaveFailure);
                return;
            }

            string saveLabel = phase == GamePhase.DoorSelection
                ? L("save.now")
                : L("save.atNextDoor");
            Button saveButton = AddSettingsMenuButton(
                modal,
                L("save.run"),
                saveLabel,
                19);
            SetAnchors(
                saveButton.GetComponent<RectTransform>(),
                new Vector2(0.185f, 0.245f),
                new Vector2(0.455f, 0.315f));
            saveButton.onClick.AddListener(SaveRunFromSettingsPanel);

            bool hasSave = HasRestorableRunSave();
            Button loadButton = AddSettingsMenuButton(
                modal,
                L("save.run.load"),
                hasSave ? L("save.load") : L("save.none"),
                19);
            SetAnchors(
                loadButton.GetComponent<RectTransform>(),
                new Vector2(0.545f, 0.245f),
                new Vector2(0.815f, 0.315f));
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
                AddLog(L("save.log.deferred"));
                HideSettingsPanel();
                RefreshLog();
                return;
            }

            bool saved = SaveRunCheckpoint();
            PlayGameSfx(saved ? GameSfxCue.SaveSuccess : GameSfxCue.SaveFailure);
            AddLog(L(saved ? "save.log.success" : "save.log.failure"));
            HideSettingsPanel();
            RefreshTopBar();
            RefreshLog();
        }

        private bool CanUseRunSaveSystem()
        {
            return playerHealth > 0
                && phase != GamePhase.MainMenu
                && phase != GamePhase.ClassSelection
                && phase != GamePhase.ClassDetails
                && phase != GamePhase.ContractSelection
                && phase != GamePhase.GameOver;
        }

        private bool CanSaveRunCheckpoint()
        {
            return CanUseRunSaveSystem()
                && deck.Count > 0
                && phase is GamePhase.DoorSelection
                    or GamePhase.Shop
                    or GamePhase.Reward
                    or GamePhase.Rest
                    or GamePhase.Curse;
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
            return CanSaveRunCheckpoint() && PersistRunCheckpointV2();
        }

        private bool SaveRunCheckpointAtResolvedSurface()
        {
            return playerHealth > 0 && deck.Count > 0 && PersistRunCheckpointV2();
        }

        private bool PersistRunCheckpointV2()
        {
            string json = CaptureRunCheckpointV2();
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            PlayerPrefs.SetString(hardRunSaveKey, json);
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
                AddLog(L("save.log.continued"));
                ResumeRestoredRunCheckpoint();
                return;
            }

            PlayGameSfx(GameSfxCue.LoadFailure);
            string failureMessage = GetRunRestoreFailureMessage();
            Debug.LogWarning(
                $"Run checkpoint restore failed: {lastRunRestoreErrorKey} "
                + $"({lastRunRestoreErrorDetail})");
            ShowMainMenu();
            AddLog(failureMessage);
            if (subtitleText != null)
            {
                subtitleText.text = failureMessage;
                SetSubtitleBoxVisible(true);
            }
        }

        private bool TryLoadHardRunSave()
        {
            string originalJson = PlayerPrefs.GetString(hardRunSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(originalJson))
            {
                return FailRunRestore("save.restore.error.none", "empty");
            }

            RunSaveVersionProbe version;
            try
            {
                version = JsonUtility.FromJson<RunSaveVersionProbe>(originalJson);
            }
            catch (ArgumentException)
            {
                return FailRunRestore("save.restore.error.corrupt", "version-json");
            }

            if (version == null)
            {
                return FailRunRestore("save.restore.error.corrupt", "version-null");
            }

            if (version.version == HardRunSaveVersion)
            {
                return TryRestoreRunCheckpointV2(originalJson);
            }

            if (version.version != LegacyRunSaveVersion
                || !TryMigrateRunCheckpointV1(originalJson, out RunSaveDataV2 migrated))
            {
                return FailRunRestore(
                    "save.restore.error.unsupported",
                    $"version-{version.version}");
            }

            PlayerPrefs.SetString(hardRunSaveBackupKey, originalJson);
            PlayerPrefs.Save();
            string migratedJson = JsonUtility.ToJson(migrated);
            if (!TryRestoreRunCheckpointV2(migratedJson))
            {
                return false;
            }

            PlayerPrefs.SetString(hardRunSaveKey, migratedJson);
            PlayerPrefs.DeleteKey(hardRunSaveBackupKey);
            PlayerPrefs.Save();
            return true;
        }

        private bool HasRestorableRunSave()
        {
            string json = PlayerPrefs.GetString(hardRunSaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            RunSaveVersionProbe version;
            try
            {
                version = JsonUtility.FromJson<RunSaveVersionProbe>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (version == null)
            {
                return false;
            }

            if (version.version == HardRunSaveVersion)
            {
                return CanRestoreRunCheckpointV2(json);
            }

            return version.version == LegacyRunSaveVersion
                && TryMigrateRunCheckpointV1(json, out RunSaveDataV2 migrated)
                && CanRestoreRunCheckpointV2(JsonUtility.ToJson(migrated));
        }

        private bool TryRestoreDeck(
            IReadOnlyList<string> cardIds,
            List<CardData> restoredDeck)
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
                if (string.IsNullOrWhiteSpace(cardId)
                    || !cardsById.TryGetValue(cardId, out CardData card))
                {
                    return false;
                }

                restoredDeck.Add(card);
            }

            return true;
        }

        private void BackfillAchievementsAfterCheckpointRestore()
        {
            TryCompletePersistentAchievements();
        }

        private void ClearHardRunSave()
        {
            PlayerPrefsProgressStore.RecordDeletedRun(
                hardRunSaveKey,
                activeRunId);
            bool changed = false;
            if (PlayerPrefs.HasKey(hardRunSaveKey))
            {
                PlayerPrefs.DeleteKey(hardRunSaveKey);
                changed = true;
            }

            if (PlayerPrefs.HasKey(hardRunSaveBackupKey))
            {
                PlayerPrefs.DeleteKey(hardRunSaveBackupKey);
                changed = true;
            }

            if (changed)
            {
                PlayerPrefs.Save();
            }
        }
    }
}
