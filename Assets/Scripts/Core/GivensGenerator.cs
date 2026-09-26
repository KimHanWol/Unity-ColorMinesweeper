using System.Collections.Generic;

namespace ColorMinesweeper.Core
{
    /// <summary>
    /// 추론만으로 끝까지 풀리도록 처음부터 열어 둘 칸(givens)을 고른다.
    /// 가장 넓게 펼쳐지는 빈 칸 하나로 시작하고, 솔버가 막힐 때마다 가장 많이 풀어 주는 칸을 더한 뒤,
    /// 빼도 풀리는 칸은 다시 뺀다(시작 칸은 남긴다).
    /// </summary>
    public static class GivensGenerator
    {
        const int SamplesPerStep = 12;

        public static int[] Generate(Stage stage, int seed, Technique maxTechnique = Technique.Pair)
        {
            var rng = new XorShiftRandom(seed);
            var solver = new Solver(stage, maxTechnique);
            int opening = PickOpening(stage, rng);
            var givens = new List<int> { opening };

            while (true)
            {
                SolveResult result = solver.Solve(givens);
                if (result.Solved)
                {
                    break;
                }

                List<int> pool = StuckCells(stage, result.Revealed);
                int best = -1;
                int bestCount = -1;
                for (int i = 0; i < SamplesPerStep && pool.Count > 0; i++)
                {
                    int pick = rng.Next(pool.Count);
                    int cell = pool[pick];
                    pool[pick] = pool[pool.Count - 1];
                    pool.RemoveAt(pool.Count - 1);

                    givens.Add(cell);
                    int count = solver.Solve(givens).RevealedCount;
                    givens.RemoveAt(givens.Count - 1);
                    if (count > bestCount)
                    {
                        best = cell;
                        bestCount = count;
                    }
                }

                givens.Add(best);
            }

            var order = new List<int>(givens);
            order.Remove(opening);
            Shuffle(order, rng);
            foreach (int cell in order)
            {
                givens.Remove(cell);
                if (!solver.Solve(givens).Solved)
                {
                    givens.Add(cell);
                }
            }

            givens.Sort();
            return givens.ToArray();
        }

        /// <summary>
        /// 가장 넓게 펼쳐지는 빈 칸. 빈 칸이 없는 그림이면 배경색 칸 아무거나, 그것도 없으면 아무 칸.
        /// </summary>
        static int PickOpening(Stage stage, XorShiftRandom rng)
        {
            var seen = new bool[stage.CellCount];
            int best = -1;
            int bestSize = 0;
            var region = new List<RevealedCell>();
            for (int cell = 0; cell < stage.CellCount; cell++)
            {
                if (seen[cell] || !stage.IsBlank(cell))
                {
                    continue;
                }

                region.Clear();
                var revealed = new bool[stage.CellCount];
                stage.RevealWithFlood(cell, revealed, region);
                foreach (RevealedCell r in region)
                {
                    if (stage.IsBlank(r.Cell))
                    {
                        seen[r.Cell] = true;
                    }
                }

                if (region.Count > bestSize)
                {
                    bestSize = region.Count;
                    best = cell;
                }
            }

            if (best >= 0)
            {
                return best;
            }

            var backgrounds = new List<int>();
            for (int cell = 0; cell < stage.CellCount; cell++)
            {
                if (stage.ColorAt(cell) == stage.BackgroundColor)
                {
                    backgrounds.Add(cell);
                }
            }

            return backgrounds.Count > 0 ? backgrounds[rng.Next(backgrounds.Count)] : rng.Next(stage.CellCount);
        }

        /// <summary>막힌 뒤 아직 닫힌 칸. 열린 영역과 붙은 칸이 있으면 그것만 고른다(풀이 흐름이 이어지게).</summary>
        static List<int> StuckCells(Stage stage, bool[] revealed)
        {
            var frontier = new List<int>();
            var all = new List<int>();
            for (int cell = 0; cell < stage.CellCount; cell++)
            {
                if (revealed[cell])
                {
                    continue;
                }

                all.Add(cell);
                foreach (int n in stage.Neighbors(cell))
                {
                    if (revealed[n])
                    {
                        frontier.Add(cell);
                        break;
                    }
                }
            }

            return frontier.Count > 0 ? frontier : all;
        }

        static void Shuffle(List<int> list, XorShiftRandom rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }
    }

    /// <summary>
    /// 런타임(Mono/IL2CPP)과 dotnet 에서 같은 시드로 같은 결과를 내기 위한 난수기. System.Random 은 구현에 따라 다를 수 있다.
    /// </summary>
    public sealed class XorShiftRandom
    {
        uint state;

        public XorShiftRandom(int seed)
        {
            state = (uint)seed ^ 0x9E3779B9u;
            if (state == 0)
            {
                state = 0x6C078965u;
            }
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>[0, maxExclusive)</summary>
        public int Next(int maxExclusive)
        {
            return (int)(NextUInt() % (uint)maxExclusive);
        }

        /// <summary>문자열에서 안정적인 시드를 만든다(string.GetHashCode 는 실행마다 달라질 수 있다).</summary>
        public static int SeedFrom(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in text)
                {
                    hash = (hash ^ c) * 16777619u;
                }

                return (int)hash;
            }
        }
    }
}
