namespace ThreeDoorsOfFate.Audio
{
    public enum ImpactSfxCue
    {
        None,
        Attack,
        Critical,
        Defense,
        Blocked,
        Prophecy,
        Trait,
        Combo,
        Curse
    }

    public static class ImpactSfxCueResolver
    {
        public static ImpactSfxCue FromFeedbackMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return ImpactSfxCue.None;
            }

            if (message.Contains("치명", System.StringComparison.Ordinal)
                || message.Contains("移섎챸", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Critical;
            }

            if (message.Contains("방어에 막힘", System.StringComparison.Ordinal)
                || message.Contains("諛⑹뼱??留됲옒", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Blocked;
            }

            if (message.Contains("방어", System.StringComparison.Ordinal)
                || message.Contains("諛⑹뼱", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Defense;
            }

            if (message.Contains("예언", System.StringComparison.Ordinal)
                || message.Contains("?덉뼵", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Prophecy;
            }

            if (message.Contains("특성", System.StringComparison.Ordinal)
                || message.Contains("?뱀꽦", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Trait;
            }

            if (message.Contains("계약", System.StringComparison.Ordinal)
                || message.Contains("怨꾩빟", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Curse;
            }

            if (message.Contains("조합", System.StringComparison.Ordinal)
                || message.Contains("議고빀", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Combo;
            }

            if (message.Contains("공격", System.StringComparison.Ordinal)
                || message.Contains("怨듦꺽", System.StringComparison.Ordinal))
            {
                return ImpactSfxCue.Attack;
            }

            return ImpactSfxCue.None;
        }

        public static bool UsesPlateLayer(ImpactSfxCue cue)
        {
            return cue != ImpactSfxCue.None;
        }
    }
}
