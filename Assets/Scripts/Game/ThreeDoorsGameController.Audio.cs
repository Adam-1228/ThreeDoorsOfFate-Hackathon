using System.Collections.Generic;
using ThreeDoorsOfFate.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDoorsOfFate.Game
{
    public sealed partial class ThreeDoorsGameController
    {
        private static bool SoundEffectsEnabled => false;
        private float EffectiveSfxVolume => SoundEffectsEnabled ? sfxVolume : 0f;

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
        [SerializeField] private List<AudioClip> attackImpactClips = new();
        [SerializeField] private AudioClip criticalImpactClip;
        [SerializeField] private List<AudioClip> defenseImpactClips = new();
        [SerializeField] private List<AudioClip> blockedImpactClips = new();
        [SerializeField] private AudioClip plateSettleClip;
        [SerializeField] private AudioClip prophecyDetailClip;
        [SerializeField] private AudioClip traitDetailClip;
        [SerializeField] private AudioClip comboDetailClip;
        [SerializeField] private AudioClip curseDetailClip;
        [SerializeField] private AudioClip bossStartImpactClip;
        [SerializeField] private AudioClip bossVictoryImpactClip;

        private readonly Dictionary<GameSfxCue, float> lastGameSfxTimes = new();
        private AudioSource uiSfxSource;
        private AudioSource gameSfxSource;
        private AudioSource impactSfxSource;
        private AudioSource detailSfxSource;
        private ImpactSfxCue lastImpactSfxCue = ImpactSfxCue.None;
        private float lastImpactSfxTime = -100f;

        private Button AddSfxButton(GameObject target, GameSfxCue cue = GameSfxCue.None)
        {
            Button button = target.AddComponent<Button>();
            if (cue != GameSfxCue.None)
            {
                // Add this before callers register their action so feedback still plays
                // when that action hides or destroys the clicked object.
                button.onClick.AddListener(() => PlayGameSfx(cue));
            }

            return button;
        }

        private void PlayGameSfx(GameSfxCue cue)
        {
            if (!SoundEffectsEnabled)
            {
                return;
            }

            SelectedUiSfxRole role = SelectedUiSfxRolePolicy.Resolve(cue);
            if (role != SelectedUiSfxRole.None)
            {
                PlaySelectedUiSfxRole(role);
                return;
            }

            PlayGenericGameSfx(cue);
        }

        private void PlaySelectedUiSfxRole(SelectedUiSfxRole role)
        {
            if (role == SelectedUiSfxRole.ImportantConfirm)
            {
                if (lastImportantUiSfxFrame == Time.frameCount)
                {
                    return;
                }

                lastImportantUiSfxFrame = Time.frameCount;
                if (selectedImportantConfirmSfxClip != null)
                {
                    importantUiSfxPriorityUntil = Mathf.Max(
                        importantUiSfxPriorityUntil,
                        Time.unscaledTime + selectedImportantConfirmSfxClip.length);
                }

                PlaySelectedUiSfx(selectedImportantConfirmSfxClip);
                return;
            }

            if (lastImportantUiSfxFrame == Time.frameCount
                || !SelectedUiSfxRolePolicy.CanPlayGeneral(
                    Time.unscaledTime,
                    importantUiSfxPriorityUntil))
            {
                return;
            }

            PlaySelectedUiSfx(selectedGeneralUiSfxClip);
        }

        private void PlayGenericGameSfx(GameSfxCue cue)
        {
            if (cue == GameSfxCue.None)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (lastGameSfxTimes.TryGetValue(cue, out float lastPlayed)
                && now - lastPlayed < 0.045f)
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
                    PlayOneShot(uiSfxSource, uiDeniedClip, 0.74f);
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
                uiSfxSource.volume = EffectiveSfxVolume;
            }

            if (gameSfxSource != null)
            {
                gameSfxSource.volume = EffectiveSfxVolume;
            }
        }

        private static void PlayOneShot(
            AudioSource source,
            AudioClip clip,
            float volume,
            float pitchVariance = 0f)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.pitch = pitchVariance > 0f
                ? 1f + Random.Range(-pitchVariance, pitchVariance)
                : 1f;
            source.PlayOneShot(clip, volume);
        }

        private static void PlayVariedOneShot(
            AudioSource source,
            IReadOnlyList<AudioClip> clips,
            float volume,
            float pitchVariance)
        {
            if (clips == null || clips.Count == 0)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Count)];
            PlayOneShot(source, clip, volume, pitchVariance);
        }

        private void PlayCombatFeedbackSfx(string message)
        {
            if (!SoundEffectsEnabled)
            {
                return;
            }

            ImpactSfxCue cue = ImpactSfxCueResolver.FromFeedbackMessage(message);
            if (cue == ImpactSfxCue.None)
            {
                return;
            }

            if (cue == lastImpactSfxCue
                && Time.unscaledTime - lastImpactSfxTime < 0.09f)
            {
                return;
            }

            EnsureAudioSources();
            lastImpactSfxCue = cue;
            lastImpactSfxTime = Time.unscaledTime;

            if (ImpactSfxCueResolver.UsesPlateLayer(cue))
            {
                PlayOneShot(detailSfxSource, plateSettleClip, 0.58f, 0.018f);
            }

            switch (cue)
            {
                case ImpactSfxCue.Attack:
                    PlayVariedOneShot(impactSfxSource, attackImpactClips, 0.92f, 0.045f);
                    break;
                case ImpactSfxCue.Critical:
                    PlayOneShot(impactSfxSource, criticalImpactClip, 0.98f, 0.03f);
                    break;
                case ImpactSfxCue.Defense:
                    PlayVariedOneShot(impactSfxSource, defenseImpactClips, 0.88f, 0.035f);
                    break;
                case ImpactSfxCue.Blocked:
                    PlayVariedOneShot(impactSfxSource, blockedImpactClips, 0.92f, 0.03f);
                    break;
                case ImpactSfxCue.Prophecy:
                    PlayOneShot(impactSfxSource, prophecyDetailClip, 0.78f, 0.025f);
                    break;
                case ImpactSfxCue.Trait:
                    PlayOneShot(impactSfxSource, traitDetailClip, 0.78f, 0.025f);
                    break;
                case ImpactSfxCue.Combo:
                    PlayOneShot(impactSfxSource, comboDetailClip, 0.82f, 0.02f);
                    break;
                case ImpactSfxCue.Curse:
                    PlayOneShot(impactSfxSource, curseDetailClip, 0.84f, 0.018f);
                    break;
            }
        }

        private void PlayBossStartSfx()
        {
            if (!SoundEffectsEnabled)
            {
                return;
            }

            EnsureAudioSources();
            PlayOneShot(impactSfxSource, bossStartImpactClip, 0.90f);
        }

        private void PlayBossVictorySfx()
        {
            if (!SoundEffectsEnabled)
            {
                return;
            }

            EnsureAudioSources();
            PlayOneShot(impactSfxSource, bossVictoryImpactClip, 0.92f);
        }

        private void PlaySelectedUiSfx(AudioClip clip)
        {
            if (!SoundEffectsEnabled || clip == null)
            {
                return;
            }

            EnsureAudioSources();
            selectedUiSfxSource.Stop();
            selectedUiSfxSource.pitch = 1f;
            selectedUiSfxSource.PlayOneShot(clip, 1f);
        }
    }
}
