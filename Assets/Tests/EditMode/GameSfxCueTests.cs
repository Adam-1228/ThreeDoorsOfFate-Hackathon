using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class GameSfxCueTests
    {
        private const string CueTypeName =
            "ThreeDoorsOfFate.Audio.GameSfxCue, ThreeDoorsOfFate.Audio";

        private static readonly string[] RequiredCueNames =
        {
            "None",
            "UiAccept",
            "UiBack",
            "UiDenied",
            "PanelOpen",
            "PanelClose",
            "RunStart",
            "DoorOpen",
            "CardDraw",
            "CardPlay",
            "CardDiscard",
            "TurnCommit",
            "DiceRoll",
            "PlayerHit",
            "Heal",
            "CombatStart",
            "EnemyDefeat",
            "RewardReveal",
            "RewardClaim",
            "GoldGain",
            "Purchase",
            "Upgrade",
            "ItemEquip",
            "TreasureOpen",
            "EventChoice",
            "Rest",
            "CurseAccept",
            "SaveSuccess",
            "SaveFailure",
            "LoadSuccess",
            "LoadFailure",
            "Defeat",
            "Victory",
            "Ending"
        };

        [Test]
        public void GameSfxCue_ContainsEveryRequiredSemanticCue()
        {
            Type cueType = Type.GetType(CueTypeName);

            Assert.That(cueType, Is.Not.Null, "GameSfxCue must exist in the audio assembly.");
            Assert.That(cueType.IsEnum, Is.True);
            CollectionAssert.IsSubsetOf(RequiredCueNames, new HashSet<string>(Enum.GetNames(cueType)));
        }
    }
}
