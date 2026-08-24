using System.IO;
using NUnit.Framework;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class GameSfxIntegrationPolicyTests
    {
        private const string ControllerPath = "Assets/Scripts/Game/ThreeDoorsGameController.cs";
        private const string PersistencePath = "Assets/Scripts/Game/ThreeDoorsGameController.Persistence.cs";
        private const string AudioControllerPath = "Assets/Scripts/Game/ThreeDoorsGameController.Audio.cs";
        private const string BuilderPath = "Assets/Editor/PlayableGameBuilder.cs";

        [Test]
        public void SfxPlayback_DoesNotDuckOrMutateBackgroundMusic()
        {
            string source = File.ReadAllText(ControllerPath);

            Assert.That(source, Does.Not.Contain("DuckMusicForSfx"));
            Assert.That(source, Does.Not.Contain("sfxMusicDuckRoutine"));
        }

        [Test]
        public void ButtonFactory_DefaultsToSilenceWithoutRawButtonConstruction()
        {
            string source = File.ReadAllText(ControllerPath);

            Assert.That(source, Does.Contain("AddSfxButton("));
            Assert.That(source, Does.Not.Contain(".AddComponent<Button>()"));

            string audioSource = File.ReadAllText(AudioControllerPath);
            Assert.That(audioSource, Does.Contain("GameSfxButtonFeedback"));
            Assert.That(
                audioSource,
                Does.Contain("GameSfxCue cue = GameSfxCue.None"),
                "Generic UI buttons must stay silent unless a semantic cue is explicitly requested.");
            Assert.That(audioSource, Does.Not.Contain("button.onClick.AddListener"));
        }

        [Test]
        public void MenuNavigation_DoesNotEmitOpenCloseOrBackClickCues()
        {
            string source = File.ReadAllText(ControllerPath);

            Assert.That(source, Does.Not.Contain("PlayGameSfx(GameSfxCue.PanelOpen)"));
            Assert.That(source, Does.Not.Contain("PlayGameSfx(GameSfxCue.PanelClose)"));
            Assert.That(source, Does.Not.Contain("AddSfxButton(overlay.gameObject, GameSfxCue.UiBack)"));
        }

        [Test]
        public void CharacterConfirmation_DoesNotEmitOrRebindRunStartClickCue()
        {
            string source = File.ReadAllText(ControllerPath);
            string builder = File.ReadAllText(BuilderPath);

            Assert.That(source, Does.Not.Contain("PlayGameSfx(GameSfxCue.RunStart)"));
            Assert.That(builder, Does.Not.Contain("AssignAudioClip(serializedObject, \"runStartClip\""));
        }

        [Test]
        public void GameplayActions_EmitRequiredSemanticCues()
        {
            string source = File.ReadAllText(ControllerPath);
            string[] requiredCalls =
            {
                "PlayGameSfx(GameSfxCue.DoorOpen)",
                "PlayGameSfx(GameSfxCue.CardDraw)",
                "PlayGameSfx(GameSfxCue.CardPlay)",
                "PlayGameSfx(GameSfxCue.CardDiscard)",
                "PlayGameSfx(GameSfxCue.TurnCommit)",
                "PlayGameSfx(GameSfxCue.DiceRoll)",
                "PlayGameSfx(GameSfxCue.PlayerHit)",
                "PlayGameSfx(GameSfxCue.Heal)",
                "PlayGameSfx(GameSfxCue.EnemyDefeat)",
                "PlayGameSfx(GameSfxCue.RewardReveal)",
                "PlayGameSfx(GameSfxCue.GoldGain)",
                "PlayGameSfx(GameSfxCue.Purchase)",
                "PlayGameSfx(GameSfxCue.ItemEquip)",
                "PlayGameSfx(GameSfxCue.TreasureOpen)",
                "PlayGameSfx(GameSfxCue.EventChoice)",
                "PlayGameSfx(GameSfxCue.Rest)",
                "PlayGameSfx(GameSfxCue.CurseAccept)",
                "PlayGameSfx(GameSfxCue.Defeat)",
                "PlayGameSfx(GameSfxCue.Victory)",
                "PlayGameSfx(GameSfxCue.Ending)"
            };

            foreach (string requiredCall in requiredCalls)
            {
                Assert.That(source, Does.Contain(requiredCall), requiredCall);
            }
        }

        [Test]
        public void PersistenceActions_EmitSuccessAndFailureCues()
        {
            string source = File.ReadAllText(PersistencePath);

            Assert.That(source, Does.Contain("GameSfxCue.SaveSuccess"));
            Assert.That(source, Does.Contain("GameSfxCue.SaveFailure"));
            Assert.That(source, Does.Contain("GameSfxCue.LoadSuccess"));
            Assert.That(source, Does.Contain("GameSfxCue.LoadFailure"));
        }

        [Test]
        public void SceneBuilder_AssignsAndConfiguresGameSfx()
        {
            string source = File.ReadAllText(BuilderPath);

            Assert.That(source, Does.Contain("GameSfxRoot"));
            Assert.That(source, Does.Contain("ConfigureSfxAudioImporter"));
            Assert.That(
                source,
                Does.Not.Contain("\"uiAcceptClips\""),
                "Regenerating the playable scene must not reconnect the removed generic click clips.");
            Assert.That(source, Does.Not.Contain("\"uiBackClip\""));
            Assert.That(source, Does.Not.Contain("\"panelOpenClip\""));
            Assert.That(source, Does.Not.Contain("\"panelCloseClip\""));
            Assert.That(source, Does.Contain("victoryClip"));
        }
    }
}
