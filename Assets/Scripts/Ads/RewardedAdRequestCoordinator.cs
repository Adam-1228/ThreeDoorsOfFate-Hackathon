using System;

namespace ThreeDoorsOfFate.Ads
{
    public enum RewardedAdOutcome
    {
        Unavailable,
        ClosedWithoutReward,
        RewardCommitted,
        PresentationFailed
    }

    public sealed class RewardedAdRequestCoordinator
    {
        private Func<bool> commitReward;
        private Action<RewardedAdOutcome> completion;
        private bool rewardCallbackReceived;
        private bool rewardCommitted;

        public bool IsActive => completion != null;

        public bool TryBegin(
            Func<bool> commitRewardAction,
            Action<RewardedAdOutcome> completionAction)
        {
            if (IsActive
                || commitRewardAction == null
                || completionAction == null)
            {
                return false;
            }

            commitReward = commitRewardAction;
            completion = completionAction;
            rewardCallbackReceived = false;
            rewardCommitted = false;
            return true;
        }

        public bool CommitReward()
        {
            if (!IsActive || rewardCallbackReceived)
            {
                return false;
            }

            rewardCallbackReceived = true;
            rewardCommitted = commitReward.Invoke();
            return rewardCommitted;
        }

        public void Finish(bool presentationFailed)
        {
            if (!IsActive)
            {
                return;
            }

            RewardedAdOutcome outcome = presentationFailed
                ? RewardedAdOutcome.PresentationFailed
                : rewardCommitted
                    ? RewardedAdOutcome.RewardCommitted
                    : RewardedAdOutcome.ClosedWithoutReward;
            Action<RewardedAdOutcome> completed = completion;
            commitReward = null;
            completion = null;
            rewardCallbackReceived = false;
            rewardCommitted = false;
            completed.Invoke(outcome);
        }
    }
}
