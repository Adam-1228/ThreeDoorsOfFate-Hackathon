namespace ThreeDoorsOfFate.Audio
{
    public enum SelectedCombatSfxCue
    {
        None,
        Attack,
        Defense
    }

    public static class SelectedCombatSfxPolicy
    {
        public static SelectedCombatSfxCue Resolve(
            SelectedCombatSfxCue eligibleCue,
            int appliedDamage,
            int grantedBlock)
        {
            if (eligibleCue == SelectedCombatSfxCue.Attack && appliedDamage > 0)
            {
                return SelectedCombatSfxCue.Attack;
            }

            if (eligibleCue == SelectedCombatSfxCue.Defense && grantedBlock > 0)
            {
                return SelectedCombatSfxCue.Defense;
            }

            return SelectedCombatSfxCue.None;
        }
    }
}
