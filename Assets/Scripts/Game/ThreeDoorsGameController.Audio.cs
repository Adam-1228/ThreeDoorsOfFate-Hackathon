using System.Collections.Generic;
using ThreeDoorsOfFate.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        [Header("Game SFX")]
        [SerializeField] private List<AudioClip> uiAcceptClips = new();
        [SerializeField] private AudioClip uiBackClip;
        [SerializeField] private AudioClip uiDeniedClip;
        [SerializeField] private AudioClip panelOpenClip;
        [SerializeField] private AudioClip panelCloseClip;
        [SerializeField] private List<AudioClip> cardDrawClips = new();
        [SerializeField] private List<AudioClip> cardPlayClips = new();
        [SerializeField] private AudioClip cardDiscardClip;
        [SerializeField] private AudioClip runStartClip;
        [SerializeField] private AudioClip doorOpenClip;
        [SerializeField] private AudioClip turnCommitClip;
        [SerializeField] private AudioClip diceRollClip;
        [SerializeField] private AudioClip playerHitClip;
        [SerializeField] private AudioClip healClip;
        [SerializeField] private AudioClip combatStartClip;
        [SerializeField] private AudioClip enemyDefeatClip;
        [SerializeField] private AudioClip treasureOpenClip;
        [SerializeField] private AudioClip eventChoiceClip;
        [SerializeField] private AudioClip restClip;
        [SerializeField] private AudioClip curseAcceptClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip endingClip;
        [SerializeField] private AudioClip rewardRevealClip;
        [SerializeField] private AudioClip rewardClaimClip;
        [SerializeField] private AudioClip goldGainClip;
        [SerializeField] private AudioClip purchaseClip;
        [SerializeField] private AudioClip upgradeClip;
        [SerializeField] private AudioClip itemEquipClip;
        [SerializeField] private AudioClip saveSuccessClip;
        [SerializeField] private AudioClip saveFailureClip;
        [SerializeField] private AudioClip loadSuccessClip;
        [SerializeField] private AudioClip loadFailureClip;

        private readonly Dictionary<GameSfxCue, float> lastGameSfxTimes = new();
        private AudioSource uiSfxSource;
        private AudioSource gameSfxSource;

        private Button AddSfxButton(GameObject target, GameSfxCue cue = GameSfxCue.None)
        {
            Button button = target.AddComponent<Button>();
            if (cue != GameSfxCue.None)
            {
                GameSfxButtonFeedback feedback = target.AddComponent<GameSfxButtonFeedback>();
                feedback.Configure(button, () => PlayGameSfx(cue));
            }

            return button;
        }

        private void EnsureGameSfxSources()
        {
            if (uiSfxSource == null)
            {
                uiSfxSource = CreateGameSfxSource();
            }

            if (gameSfxSource == null)
            {
                gameSfxSource = CreateGameSfxSource();
            }

            ApplyGameSfxVolumes();
        }

        private AudioSource CreateGameSfxSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplyGameSfxVolumes()
        {
            if (uiSfxSource != null)
            {
                uiSfxSource.volume = sfxVolume;
            }

            if (gameSfxSource != null)
            {
                gameSfxSource.volume = sfxVolume;
            }
        }

        private void PlayGameSfx(GameSfxCue cue)
        {
            if (cue == GameSfxCue.None)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (lastGameSfxTimes.TryGetValue(cue, out float lastPlayed) && now - lastPlayed < 0.045f)
            {
                return;
            }

            lastGameSfxTimes[cue] = now;
            EnsureGameSfxSources();

            switch (cue)
            {
                case GameSfxCue.UiAccept:
                    PlayVariedOneShot(uiSfxSource, uiAcceptClips, 0.64f, 0.025f);
                    break;
                case GameSfxCue.UiBack:
                    PlayOneShot(uiSfxSource, uiBackClip, 0.66f, 0.018f);
                    break;
                case GameSfxCue.UiDenied:
                    PlayOneShot(uiSfxSource, uiDeniedClip, 0.74f, 0f);
                    break;
                case GameSfxCue.PanelOpen:
                    PlayOneShot(uiSfxSource, panelOpenClip, 0.64f, 0.012f);
                    break;
                case GameSfxCue.PanelClose:
                    PlayOneShot(uiSfxSource, panelCloseClip, 0.62f, 0.012f);
                    break;
                case GameSfxCue.CardDraw:
                    PlayVariedOneShot(uiSfxSource, cardDrawClips, 0.62f, 0.025f);
                    break;
                case GameSfxCue.CardPlay:
                    PlayVariedOneShot(gameSfxSource, cardPlayClips, 0.76f, 0.022f);
                    break;
                case GameSfxCue.CardDiscard:
                    PlayOneShot(uiSfxSource, cardDiscardClip, 0.66f, 0.025f);
                    break;
                case GameSfxCue.RunStart:
                    PlayOneShot(gameSfxSource, runStartClip, 0.78f);
                    break;
                case GameSfxCue.DoorOpen:
                    PlayOneShot(gameSfxSource, doorOpenClip, 0.82f, 0.01f);
                    break;
                case GameSfxCue.TurnCommit:
                    PlayOneShot(gameSfxSource, turnCommitClip, 0.72f, 0.015f);
                    break;
                case GameSfxCue.DiceRoll:
                    PlayOneShot(gameSfxSource, diceRollClip, 0.72f, 0.025f);
                    break;
                case GameSfxCue.PlayerHit:
                    PlayOneShot(gameSfxSource, playerHitClip, 0.86f, 0.018f);
                    break;
                case GameSfxCue.Heal:
                    PlayOneShot(gameSfxSource, healClip, 0.70f, 0.012f);
                    break;
                case GameSfxCue.CombatStart:
                    PlayOneShot(gameSfxSource, combatStartClip, 0.78f);
                    break;
                case GameSfxCue.EnemyDefeat:
                    PlayOneShot(gameSfxSource, enemyDefeatClip, 0.82f);
                    break;
                case GameSfxCue.RewardReveal:
                    PlayOneShot(gameSfxSource, rewardRevealClip, 0.70f, 0.01f);
                    break;
                case GameSfxCue.RewardClaim:
                    PlayOneShot(gameSfxSource, rewardClaimClip, 0.68f, 0.015f);
                    break;
                case GameSfxCue.GoldGain:
                    PlayOneShot(gameSfxSource, goldGainClip, 0.68f, 0.025f);
                    break;
                case GameSfxCue.Purchase:
                    PlayOneShot(gameSfxSource, purchaseClip, 0.72f, 0.015f);
                    break;
                case GameSfxCue.Upgrade:
                    PlayOneShot(gameSfxSource, upgradeClip, 0.72f, 0.012f);
                    break;
                case GameSfxCue.ItemEquip:
                    PlayOneShot(gameSfxSource, itemEquipClip, 0.76f, 0.012f);
                    break;
                case GameSfxCue.TreasureOpen:
                    PlayOneShot(gameSfxSource, treasureOpenClip, 0.76f, 0.01f);
                    break;
                case GameSfxCue.EventChoice:
                    PlayOneShot(gameSfxSource, eventChoiceClip, 0.68f, 0.012f);
                    break;
                case GameSfxCue.Rest:
                    PlayOneShot(gameSfxSource, restClip, 0.66f, 0.01f);
                    break;
                case GameSfxCue.CurseAccept:
                    PlayOneShot(gameSfxSource, curseAcceptClip, 0.78f, 0.008f);
                    break;
                case GameSfxCue.SaveSuccess:
                    PlayOneShot(uiSfxSource, saveSuccessClip, 0.68f);
                    break;
                case GameSfxCue.SaveFailure:
                    PlayOneShot(uiSfxSource, saveFailureClip, 0.72f);
                    break;
                case GameSfxCue.LoadSuccess:
                    PlayOneShot(uiSfxSource, loadSuccessClip, 0.68f);
                    break;
                case GameSfxCue.LoadFailure:
                    PlayOneShot(uiSfxSource, loadFailureClip, 0.72f);
                    break;
                case GameSfxCue.Defeat:
                    PlayOneShot(gameSfxSource, defeatClip, 0.84f);
                    break;
                case GameSfxCue.Victory:
                    PlayOneShot(gameSfxSource, victoryClip, 0.82f);
                    break;
                case GameSfxCue.Ending:
                    PlayOneShot(gameSfxSource, endingClip, 0.80f);
                    break;
            }
        }
    }
}
