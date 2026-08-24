using System.Collections.Generic;
using NUnit.Framework;
using ThreeDoorsOfFate.Ads;

namespace ThreeDoorsOfFate.Tests
{
    public sealed class RewardedAdRequestCoordinatorTests
    {
        [Test]
        public void CommitReward_InvokesCommitAndCompletionExactlyOnce()
        {
            RewardedAdRequestCoordinator coordinator = new();
            int commitCalls = 0;
            List<RewardedAdOutcome> outcomes = new();
            Assert.That(
                coordinator.TryBegin(
                    () =>
                    {
                        commitCalls += 1;
                        return true;
                    },
                    outcomes.Add),
                Is.True);

            Assert.That(coordinator.CommitReward(), Is.True);
            Assert.That(coordinator.CommitReward(), Is.False);
            coordinator.Finish(false);
            coordinator.Finish(false);

            Assert.That(commitCalls, Is.EqualTo(1));
            Assert.That(
                outcomes,
                Is.EqualTo(new[] { RewardedAdOutcome.RewardCommitted }));
            Assert.That(coordinator.IsActive, Is.False);
        }

        [Test]
        public void TryBegin_RejectsSecondActiveRequest()
        {
            RewardedAdRequestCoordinator coordinator = new();
            int firstCompletions = 0;
            int secondCompletions = 0;
            Assert.That(
                coordinator.TryBegin(() => true, _ => firstCompletions += 1),
                Is.True);

            bool beganSecond = coordinator.TryBegin(
                () => true,
                _ => secondCompletions += 1);
            coordinator.Finish(false);

            Assert.That(beganSecond, Is.False);
            Assert.That(firstCompletions, Is.EqualTo(1));
            Assert.That(secondCompletions, Is.Zero);
        }

        [Test]
        public void Finish_WithoutRewardCallbackGrantsNothing()
        {
            RewardedAdRequestCoordinator coordinator = new();
            int commitCalls = 0;
            RewardedAdOutcome? outcome = null;
            coordinator.TryBegin(
                () =>
                {
                    commitCalls += 1;
                    return true;
                },
                value => outcome = value);

            coordinator.Finish(false);

            Assert.That(commitCalls, Is.Zero);
            Assert.That(outcome, Is.EqualTo(RewardedAdOutcome.ClosedWithoutReward));
        }

        [Test]
        public void Finish_PresentationFailureReportsFailureWithoutCommit()
        {
            RewardedAdRequestCoordinator coordinator = new();
            int commitCalls = 0;
            RewardedAdOutcome? outcome = null;
            coordinator.TryBegin(
                () =>
                {
                    commitCalls += 1;
                    return true;
                },
                value => outcome = value);

            coordinator.Finish(true);

            Assert.That(commitCalls, Is.Zero);
            Assert.That(outcome, Is.EqualTo(RewardedAdOutcome.PresentationFailed));
        }

        [Test]
        public void CommitReward_FailedCommitDoesNotReportRewardCommitted()
        {
            RewardedAdRequestCoordinator coordinator = new();
            RewardedAdOutcome? outcome = null;
            coordinator.TryBegin(() => false, value => outcome = value);

            Assert.That(coordinator.CommitReward(), Is.False);
            coordinator.Finish(false);

            Assert.That(outcome, Is.EqualTo(RewardedAdOutcome.ClosedWithoutReward));
        }
    }
}
