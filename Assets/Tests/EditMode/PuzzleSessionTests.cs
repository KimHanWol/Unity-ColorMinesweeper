using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class PuzzleSessionTests
    {
        static Stage Stage3x3()
        {
            return StageSerializer.Parse(@"{ ""id"": ""t"", ""background"": ""."",
                ""palette"": [{ ""key"": ""."", ""color"": ""#FFFFFF"" }, { ""key"": ""A"", ""color"": ""#FF0000"" }],
                ""pixels"": [""AAA"", ""A.A"", ""AAA""] }");
        }

        [Test]
        public void GivensAreRevealedAndCounted()
        {
            var session = new PuzzleSession(Stage3x3(), new[] { 4 });

            Assert.IsTrue(session.IsRevealed(4));
            Assert.AreEqual(1, session.RevealedCount);
            Assert.AreEqual(0, session.Remaining(0));
            Assert.AreEqual(8, session.Remaining(1));
        }

        [Test]
        public void WrongColorCostsALifeAndEndsAtZero()
        {
            var session = new PuzzleSession(Stage3x3(), new[] { 4 });

            for (int i = 0; i < PuzzleSession.MaxLives - 1; i++)
            {
                Assert.AreEqual(PaintOutcome.Wrong, session.Paint(0, 0).Outcome);
            }

            PaintResult last = session.Paint(0, 0);

            Assert.IsTrue(last.GameOver);
            Assert.AreEqual(PaintOutcome.Ignored, session.Paint(0, 1).Outcome);

            session.Revive();

            Assert.AreEqual(PuzzleSession.ReviveLives, session.Lives);
            Assert.AreEqual(PaintOutcome.Correct, session.Paint(0, 1).Outcome);
            Assert.AreEqual(1, session.Stars);
        }

        [Test]
        public void PaintingEveryCellClears()
        {
            var session = new PuzzleSession(Stage3x3(), new[] { 4 });
            PaintResult result = null;
            for (int cell = 0; cell < 9; cell++)
            {
                if (cell != 4)
                {
                    result = session.Paint(cell, 1);
                }
            }

            Assert.IsTrue(result.Cleared);
            Assert.AreEqual(3, session.Stars);
        }
    }
}
