using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class StageReportTests
    {
        static Stage Picture(string background, string white)
        {
            return StageSerializer.Parse(@"{
  ""id"": ""cup"",
  ""name"": ""컵"",
  ""background"": ""."",
  ""palette"": [
    { ""key"": ""."", ""color"": """ + background + @""" },
    { ""key"": ""W"", ""color"": """ + white + @""" }
  ],
  ""pixels"": [
    "".W."",
    ""WWW"",
    "".W.""
  ]
}");
        }

        [Test]
        public void TwoWhitesInOnePictureAreAnError()
        {
            var report = new StageReport(Picture("#F4F1FF", "#FFFFFF"));

            Assert.IsFalse(report.IsPlayable);
            StringAssert.Contains("흰색", string.Join(" ", report.Errors));
        }

        [Test]
        public void TintedBackgroundNextToWhiteIsFine()
        {
            var report = new StageReport(Picture("#D7CCFF", "#FFFFFF"));

            CollectionAssert.IsEmpty(report.Errors.FindAll(e => e.Contains("흰색")));
        }

        [Test]
        public void LooksWhiteMatchesPaleNeutralsOnly()
        {
            Assert.IsTrue(new Rgb(0xFF, 0xFF, 0xFF).LooksWhite());
            Assert.IsTrue(new Rgb(0xEA, 0xF4, 0xFF).LooksWhite());
            Assert.IsFalse(new Rgb(0xCA, 0xF0, 0xF8).LooksWhite());
            Assert.IsFalse(new Rgb(0xC4, 0xC4, 0xC4).LooksWhite());
        }
    }
}
