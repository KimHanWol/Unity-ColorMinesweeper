using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class StageSerializerTests
    {
        const string Heart = @"{
  ""id"": ""heart"",
  ""name"": ""하트"",
  ""background"": ""."",
  ""palette"": [
    { ""key"": ""."", ""color"": ""#F3EEE4"" },
    { ""key"": ""R"", ""color"": ""#E8505B"" }
  ],
  ""pixels"": [
    "".RR.RR."",
    ""RRRRRRR"",
    "".RRRRR."",
    ""..RRR.."",
    ""...R...""
  ],
  ""givens"": [0, 6]
}";

        [Test]
        public void ParsesSizeColorsAndGivens()
        {
            Stage stage = StageSerializer.Parse(Heart);

            Assert.AreEqual("heart", stage.Id);
            Assert.AreEqual("하트", stage.Name);
            Assert.AreEqual(7, stage.Width);
            Assert.AreEqual(5, stage.Height);
            Assert.AreEqual(2, stage.ColorCount);
            Assert.AreEqual(0, stage.BackgroundColor);
            Assert.AreEqual(new Rgb(0xE8, 0x50, 0x5B), stage.Colors[1].Color);
            Assert.AreEqual(1, stage.ColorAt(1, 0));
            CollectionAssert.AreEqual(new[] { 0, 6 }, stage.Givens);
        }

        [Test]
        public void CluesCountNeighborsByColor()
        {
            Stage stage = StageSerializer.Parse(Heart);

            // (0,0) 의 이웃: (1,0)=R, (0,1)=R, (1,1)=R
            Assert.AreEqual(3, stage.Clue(0, 1));
            Assert.AreEqual(0, stage.Clue(0, 0));
            // (3,0) 은 배경. 이웃: (2,0)R (4,0)R (2,1)R (3,1)R (4,1)R
            Assert.AreEqual(5, stage.Clue(3, 1));
        }

        [Test]
        public void RoundTripsThroughJson()
        {
            Stage stage = StageSerializer.Parse(Heart);
            Stage again = StageSerializer.Parse(StageSerializer.ToJson(stage));

            Assert.AreEqual(stage.Width, again.Width);
            Assert.AreEqual(stage.Height, again.Height);
            Assert.AreEqual(stage.Name, again.Name);
            for (int i = 0; i < stage.CellCount; i++)
            {
                Assert.AreEqual(stage.ColorAt(i), again.ColorAt(i));
            }

            CollectionAssert.AreEqual(stage.Givens, again.Givens);
        }

        [Test]
        public void KeepsLocalizedNames()
        {
            Stage stage = StageSerializer.Parse(Heart.Replace("\"name\": \"하트\",", "\"name\": \"하트\", \"names\": { \"en\": \"Heart\" },"));

            Assert.AreEqual("Heart", stage.NameIn("en"));
            Assert.AreEqual("하트", stage.NameIn("ko"), "번역이 없으면 기본 이름");
            Assert.AreEqual("Heart", StageSerializer.Parse(StageSerializer.ToJson(stage)).NameIn("en"));
        }

        [TestCase(@"{ ""id"": ""x"", ""background"": ""."", ""palette"": [{ ""key"": ""."", ""color"": ""#000000"" }, { ""key"": ""A"", ""color"": ""#FFFFFF"" }], ""pixels"": ["".A"", ""...""] }", "2번째 줄 길이")]
        [TestCase(@"{ ""id"": ""x"", ""background"": ""."", ""palette"": [{ ""key"": ""."", ""color"": ""#000000"" }, { ""key"": ""A"", ""color"": ""#FFFFFF"" }], ""pixels"": ["".B""] }", "'B' 가 palette 에 없습니다")]
        [TestCase(@"{ ""id"": ""x"", ""background"": ""Z"", ""palette"": [{ ""key"": ""."", ""color"": ""#000000"" }, { ""key"": ""A"", ""color"": ""#FFFFFF"" }], ""pixels"": ["".A""] }", "background 'Z'")]
        [TestCase(@"{ ""id"": ""x"", ""background"": ""."", ""palette"": [{ ""key"": ""."", ""color"": ""red"" }], ""pixels"": ["".""] }", "#RRGGBB")]
        [TestCase(@"{ ""id"": ""x"", ""background"": ""."", ""palette"": [{ ""key"": ""."", ""color"": ""#000000"" }, { ""key"": ""A"", ""color"": ""#FFFFFF"" }], ""pixels"": [""..""] }", "'A' 가 그림에 쓰이지 않습니다")]
        [TestCase(@"{ ""id"": ""x"", }", "JSON 문법 오류")]
        public void ReportsReadableErrors(string json, string expected)
        {
            var e = Assert.Throws<StageFormatException>(() => StageSerializer.Parse(json));
            StringAssert.Contains(expected, e.Message);
        }
    }
}
