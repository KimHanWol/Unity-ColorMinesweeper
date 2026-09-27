using System.Collections.Generic;
using System.Text;
using ColorMinesweeper.Core;
using NUnit.Framework;

namespace ColorMinesweeper.Tests
{
    public class SolverTests
    {
        static Stage Make(params string[] rows)
        {
            var sb = new StringBuilder();
            sb.Append("{ \"id\": \"t\", \"background\": \".\", \"palette\": [");
            sb.Append("{ \"key\": \".\", \"color\": \"#FFFFFF\" }");
            var keys = new SortedSet<char>();
            foreach (string row in rows)
            {
                foreach (char c in row)
                {
                    if (c != '.')
                    {
                        keys.Add(c);
                    }
                }
            }

            foreach (char key in keys)
            {
                sb.Append(", { \"key\": \"").Append(key).Append("\", \"color\": \"#000000\" }");
            }

            sb.Append("], \"pixels\": [");
            for (int i = 0; i < rows.Length; i++)
            {
                sb.Append(i > 0 ? ", " : "").Append('"').Append(rows[i]).Append('"');
            }

            sb.Append("] }");
            return StageSerializer.Parse(sb.ToString());
        }

        [Test]
        public void BlankCellFloodsLikeMinesweeperZero()
        {
            Stage stage = Make(
                ".....",
                ".....",
                "....A");
            var revealed = new bool[stage.CellCount];
            var result = new List<RevealedCell>();

            stage.RevealWithFlood(0, revealed, result);

            // A 를 뺀 모든 칸이 펼쳐지고, A 옆의 숫자 칸에서 멈춘다.
            Assert.IsFalse(revealed[stage.CellCount - 1]);
            Assert.AreEqual(stage.CellCount - 1, result.Count);
            Assert.AreEqual(0, result[0].Depth);
        }

        [Test]
        public void SingleClueDeterminesNeighbors()
        {
            // 가운데 칸 하나만 열어도 주변 8칸이 전부 A 라는 게 확정된다.
            Stage stage = Make(
                "AAA",
                "A.A",
                "AAA");

            SolveResult result = new Solver(stage, Technique.Direct).Solve(new[] { 4 });

            Assert.IsTrue(result.Solved);
            Assert.IsFalse(result.UsedPair);
        }

        [Test]
        public void PairRuleSolvesWhatSingleCannot()
        {
            // 지뢰찾기의 1-1 패턴: 아래 줄 단서 두 개를 함께 봐야 오른쪽 끝이 배경인 걸 안다.
            Stage stage = Make(
                "A..",
                "...");

            var givens = new[] { 3, 4 };
            SolveResult single = new Solver(stage, Technique.Direct).Solve(givens);
            SolveResult pair = new Solver(stage, Technique.Pair).Solve(givens);

            Assert.IsFalse(single.Solved);
            Assert.IsTrue(pair.Solved);
            Assert.IsTrue(pair.UsedPair);
        }

        [Test]
        public void StopsInsteadOfGuessing()
        {
            // 열린 칸이 하나뿐이면 A 가 두 칸 중 어디인지 알 수 없다.
            Stage stage = Make(
                "A.",
                "..");

            SolveResult result = new Solver(stage).Solve(new[] { 3 });

            Assert.IsFalse(result.Solved);
            Assert.IsFalse(result.Revealed[0]);
        }

        [Test]
        public void NextDeductionsFindsHintCells()
        {
            Stage stage = Make(
                "AAA",
                "A.A",
                "AAA");
            var revealed = new bool[stage.CellCount];
            revealed[4] = true;

            List<int> next = new Solver(stage).NextDeductions(revealed);

            Assert.AreEqual(8, next.Count);
        }

        [Test]
        public void HintExplainsWhichClueProvesTheCells()
        {
            Stage stage = Make(
                "AAA",
                "A.A",
                "AAA");
            var revealed = new bool[stage.CellCount];
            revealed[4] = true;

            Deduction hint = new Solver(stage).Hint(revealed, 1);

            Assert.AreEqual(Technique.Direct, hint.Technique);
            Assert.AreEqual(4, hint.Clue);
            Assert.AreEqual(8, hint.Cells.Count);
            Assert.IsNull(new Solver(stage).Hint(revealed, 0), "배경은 남은 칸이 없어 힌트가 없다");
        }

        [Test]
        public void GeneratedGivensAlwaysSolveRandomPictures()
        {
            var rng = new XorShiftRandom(1234);
            for (int trial = 0; trial < 40; trial++)
            {
                int width = 5 + rng.Next(10);
                int height = 5 + rng.Next(10);
                int colors = 2 + rng.Next(3);
                var rows = new string[height];
                for (int y = 0; y < height; y++)
                {
                    var row = new StringBuilder();
                    for (int x = 0; x < width; x++)
                    {
                        // 배경이 절반쯤 되게 해서 실제 도트 그림과 비슷한 밀도로 만든다.
                        int roll = rng.Next(2 * (colors - 1));
                        row.Append(roll < colors - 1 ? '.' : (char)('A' + rng.Next(colors - 1)));
                    }

                    rows[y] = row.ToString();
                }

                Stage stage;
                try
                {
                    stage = Make(rows);
                }
                catch (StageFormatException)
                {
                    continue; // 무작위로 한 색이 안 나온 경우
                }

                int[] givens = GivensGenerator.Generate(stage, trial);
                SolveResult result = new Solver(stage).Solve(givens);

                Assert.IsTrue(result.Solved, "trial " + trial);
                Assert.Less(givens.Length, stage.CellCount, "trial " + trial);
            }
        }

        [Test]
        public void GenerationIsDeterministicForSeed()
        {
            Stage stage = Make(
                "..AA....",
                ".ABBA...",
                ".ABBA.C.",
                "..AA..C.",
                "........");

            CollectionAssert.AreEqual(GivensGenerator.Generate(stage, 7), GivensGenerator.Generate(stage, 7));
        }
    }
}
