using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class ReviewPromptTests
    {
        [Test]
        public void DoesNotAskBeforeEnoughPictures()
        {
            Assert.IsFalse(ReviewPrompt.ShouldAsk(ReviewState.NotAsked, ReviewPrompt.MinCleared - 1, 30, 30, 0));
        }

        [Test]
        public void AsksAfterEnoughPicturesAndDays()
        {
            Assert.IsTrue(ReviewPrompt.ShouldAsk(ReviewState.NotAsked, ReviewPrompt.MinCleared, ReviewPrompt.MinDays, 1, 0));
        }

        [Test]
        public void AsksAfterEnoughPicturesAndLaunches()
        {
            Assert.IsTrue(ReviewPrompt.ShouldAsk(ReviewState.NotAsked, ReviewPrompt.MinCleared, 0, ReviewPrompt.MinLaunches, 0));
        }

        [Test]
        public void WaitsWhenPlayedOnlyOnceOnTheFirstDay()
        {
            Assert.IsFalse(ReviewPrompt.ShouldAsk(ReviewState.NotAsked, 40, 0.5, 1, 0));
        }

        [Test]
        public void AsksAgainOnlyAfterMorePicturesWhenPostponed()
        {
            Assert.IsFalse(ReviewPrompt.ShouldAsk(ReviewState.Postponed, 20 + ReviewPrompt.PostponeCleared - 1, 30, 30, 20));
            Assert.IsTrue(ReviewPrompt.ShouldAsk(ReviewState.Postponed, 20 + ReviewPrompt.PostponeCleared, 30, 30, 20));
        }

        [Test]
        public void NeverAsksAgainWhenFinished()
        {
            Assert.IsFalse(ReviewPrompt.ShouldAsk(ReviewState.Finished, 300, 300, 300, 0));
        }
    }
}
