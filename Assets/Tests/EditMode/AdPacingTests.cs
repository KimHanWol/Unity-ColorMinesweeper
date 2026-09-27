using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class AdPacingTests
    {
        static AdPacing AfterGrace()
        {
            return new AdPacing(AdPacing.GraceCompletions);
        }

        static void Complete(AdPacing pacing, int count)
        {
            for (int i = 0; i < count; i++)
            {
                pacing.OnStageCompleted();
            }
        }

        [Test]
        public void NoInterstitialDuringGracePeriod()
        {
            var pacing = new AdPacing(0);
            Complete(pacing, AdPacing.GraceCompletions);

            Assert.IsFalse(pacing.ShouldShowInterstitial(10000, false));
        }

        [Test]
        public void ShowsEveryNthCompletionAfterGrace()
        {
            AdPacing pacing = AfterGrace();
            Complete(pacing, AdPacing.EveryNthCompletion - 1);
            Assert.IsFalse(pacing.ShouldShowInterstitial(1000, false));

            pacing.OnStageCompleted();
            Assert.IsTrue(pacing.ShouldShowInterstitial(1000, false));
        }

        [Test]
        public void RespectsMinimumIntervalBetweenInterstitials()
        {
            AdPacing pacing = AfterGrace();
            Complete(pacing, AdPacing.EveryNthCompletion);
            pacing.OnInterstitialShown(1000);

            Complete(pacing, AdPacing.EveryNthCompletion);
            Assert.IsFalse(pacing.ShouldShowInterstitial(1000 + AdPacing.MinSecondsBetween - 1, false));
            Assert.IsTrue(pacing.ShouldShowInterstitial(1000 + AdPacing.MinSecondsBetween, false));
        }

        [Test]
        public void RestsAfterRewardedAd()
        {
            AdPacing pacing = AfterGrace();
            Complete(pacing, AdPacing.EveryNthCompletion);
            pacing.OnRewardedShown(5000);

            Assert.IsFalse(pacing.ShouldShowInterstitial(5000 + 30, false));
            Assert.IsTrue(pacing.ShouldShowInterstitial(5000 + AdPacing.MinSecondsAfterRewarded, false));
        }

        [Test]
        public void NeverShowsWhenAdsRemoved()
        {
            AdPacing pacing = AfterGrace();
            Complete(pacing, 50);

            Assert.IsFalse(pacing.ShouldShowInterstitial(100000, true));
        }
    }
}
