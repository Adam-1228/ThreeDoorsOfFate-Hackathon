namespace ThreeDoorsOfFate.Audio
{
    public enum SelectedUiSfxRole
    {
        None,
        General,
        ImportantConfirm
    }

    public static class SelectedUiSfxRolePolicy
    {
        public static SelectedUiSfxRole Resolve(GameSfxCue cue)
        {
            return cue switch
            {
                GameSfxCue.UiAccept => SelectedUiSfxRole.General,
                GameSfxCue.UiBack => SelectedUiSfxRole.General,
                GameSfxCue.ImportantConfirm => SelectedUiSfxRole.ImportantConfirm,
                GameSfxCue.DoorOpen => SelectedUiSfxRole.ImportantConfirm,
                GameSfxCue.CardPlay => SelectedUiSfxRole.ImportantConfirm,
                GameSfxCue.RewardClaim => SelectedUiSfxRole.ImportantConfirm,
                _ => SelectedUiSfxRole.None
            };
        }

        public static bool CanPlayGeneral(
            float unscaledTime,
            float importantPriorityUntil)
        {
            return unscaledTime >= importantPriorityUntil;
        }
    }
}
