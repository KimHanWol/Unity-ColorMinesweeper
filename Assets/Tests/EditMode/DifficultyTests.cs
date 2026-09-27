using System.Collections.Generic;
using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class DifficultyTests
    {
        const string Heart = @"{ ""id"": ""heart"", ""background"": ""."",
            ""palette"": [{ ""key"": ""."", ""color"": ""#F3EEE4"" }, { ""key"": ""R"", ""color"": ""#E8505B"" }, { ""key"": ""P"", ""color"": ""#FFB3C1"" }],
            ""pixels"": [""........."", "".RR...RR."", ""RRPR.RRRR"", ""RPRRRRRRR"", ""RRRRRRRRR"", "".RRRRRRR."", ""..RRRRR.."", ""...RRR..."", ""....R....""] }";

        /// <summary>
        /// 실제 플레이에서 막혔던 판면(3번째 줄 가운데 세 칸과 위 두 줄만 남음). 어려워 보이지만
        /// 가려진 이웃이 하나뿐인 단서 칸이 있어서 단서 하나만으로 양 끝 칸이 빨강으로 확정된다.
        /// </summary>
        [Test]
        public void StuckLookingPositionIsSolvableWithSingleClues()
        {
            Stage stage = StageSerializer.Parse(Heart);
            var revealed = new bool[stage.CellCount];
            for (int cell = 2 * stage.Width; cell < stage.CellCount; cell++)
            {
                revealed[cell] = true;
            }

            revealed[21] = revealed[22] = revealed[23] = false;

            List<int> next = new Solver(stage, Technique.Direct).NextDeductions(revealed);

            CollectionAssert.Contains(next, 21);
            CollectionAssert.Contains(next, 23);
        }

        [Test]
        public void MoreColorsAndBiggerBoardsScoreHigher()
        {
            Stage small = StageSerializer.Parse(@"{ ""id"": ""a"", ""background"": ""."",
                ""palette"": [{ ""key"": ""."", ""color"": ""#FFFFFF"" }, { ""key"": ""A"", ""color"": ""#000000"" }],
                ""pixels"": ["".A."", ""AAA"", "".A.""] }");
            Stage big = StageSerializer.Parse(Heart);

            Difficulty a = new StageReport(small).Difficulty;
            Difficulty b = new StageReport(big).Difficulty;

            Assert.Less(a.Score, b.Score);
        }
    }
}
