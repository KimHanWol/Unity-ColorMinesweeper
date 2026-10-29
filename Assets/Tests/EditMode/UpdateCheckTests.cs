using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class UpdateCheckTests
    {
        [Test]
        public void UpToDateNeedsNothing()
        {
            Assert.AreEqual(UpdateNeed.None, UpdateCheck.Compare(12, 12, 1));
        }

        [Test]
        public void OlderThanLatestIsOptional()
        {
            Assert.AreEqual(UpdateNeed.Optional, UpdateCheck.Compare(11, 12, 1));
        }

        [Test]
        public void OlderThanMinimumIsRequired()
        {
            Assert.AreEqual(UpdateNeed.Required, UpdateCheck.Compare(9, 12, 10));
        }

        [Test]
        public void UnknownValuesNeverBlock()
        {
            Assert.AreEqual(UpdateNeed.None, UpdateCheck.Compare(0, 12, 10));
            Assert.AreEqual(UpdateNeed.None, UpdateCheck.Compare(11, 0, 0));
        }
    }
}
