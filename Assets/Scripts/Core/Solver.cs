using System;
using System.Collections.Generic;

namespace ColorMinesweeper.Core
{
    /// <summary>
    /// 플레이어에게 요구하는 추론. 둘 다 <b>한 색만 보고</b> 끝나는 추론이다.
    /// 화면은 팔레트에서 고른 색의 숫자만 보여 주므로, 여러 색을 번갈아 보며 조합해야 하는 추론은 쓰지 않는다
    /// (플레이 피드백: 한 칸에 여러 색이 들어가면 읽기 어렵고, 색을 번갈아 누르며 생각하기 번거롭다).
    /// 배경도 하나의 색으로 친다.
    /// </summary>
    public enum Technique
    {
        /// <summary>
        /// 순서대로: 열린 단서 하나에서 고른 색의 남은 개수가 가려진 이웃 수와 같으면 가려진 이웃이 모두 그 색이다.
        /// </summary>
        Direct = 0,

        /// <summary>
        /// 같은 색 두 단서 비교: 가까운 두 단서의 같은 색 남은 개수를 겹치는 칸/한쪽에만 있는 칸으로 나눠 따진다.
        /// 지뢰찾기의 1-2 패턴과 같다. 칠할 수 있는 결론(전부 그 색)만 쓴다.
        /// </summary>
        Pair = 1,
    }

    /// <summary>솔버가 한 번에 확정한 묶음 하나. 난이도 측정에 쓴다.</summary>
    public sealed class SolveStep
    {
        /// <summary>이 순간 확정할 수 있던 칸 수(펼침 전). 적을수록 찾기 어렵다.</summary>
        public int Options;

        /// <summary>이 단계에서 나아가는 데 필요했던 가장 쉬운 추론.</summary>
        public Technique Needed;
    }

    /// <summary>한 번의 추론: clue 칸(과 pair 면 clue2)의 color 개수로 cells 가 모두 color 라는 것을 안다.</summary>
    public sealed class Deduction
    {
        public Technique Technique;
        public int Color;
        public int Clue;
        public int Clue2 = -1;
        public List<int> Cells = new List<int>();
    }

    public sealed class SolveResult
    {
        public bool Solved;

        /// <summary>givens 와 그 펼침으로 처음부터 열린 칸 수.</summary>
        public int InitialRevealed;

        /// <summary>추론으로 한 번에 확정된 칸 묶음. 사람이 푸는 "단계"에 가깝다(펼침 포함).</summary>
        public List<int[]> Waves = new List<int[]>();

        /// <summary><see cref="Waves"/> 와 같은 순서의 단계별 기록.</summary>
        public List<SolveStep> Steps = new List<SolveStep>();

        /// <summary>끝까지 푸는 데 필요했던 가장 어려운 추론.</summary>
        public Technique Hardest = Technique.Direct;

        public bool UsedPair => Hardest == Technique.Pair;

        public bool[] Revealed;

        public int RevealedCount
        {
            get
            {
                int count = 0;
                foreach (bool r in Revealed)
                {
                    if (r)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }

    /// <summary>
    /// 한 색만 보는 추론으로 퍼즐을 푼다. 찍어야 하는 순간이 오면 멈춘다.
    /// 매 순간 가장 쉬운 추론부터 시도해서, 그 단계에 실제로 무엇이 필요했는지 기록한다.
    /// 확정된 칸은 플레이어가 칠해서 열 수 있는 칸이므로 즉시 열고, 새 단서를 이어서 쓴다.
    /// 기억해 둔 제외 정보(이 칸은 무슨 색이 아니다)는 쓰지 않는다. 화면에 보이는 열린 칸과 숫자만으로 판단한다.
    /// </summary>
    public sealed class Solver
    {
        readonly Stage stage;
        readonly Technique maxTechnique;
        readonly bool[] revealed;
        readonly List<int> scratchA = new List<int>(8);
        readonly List<int> scratchB = new List<int>(8);
        readonly List<int> scratchO = new List<int>(8);

        public Solver(Stage stage, Technique maxTechnique = Technique.Pair)
        {
            this.stage = stage;
            this.maxTechnique = maxTechnique;
            revealed = new bool[stage.CellCount];
        }

        public SolveResult Solve(IEnumerable<int> givens)
        {
            Array.Clear(revealed, 0, revealed.Length);
            foreach (int g in givens)
            {
                stage.RevealWithFlood(g, revealed, null);
            }

            var result = new SolveResult();
            for (int i = 0; i < revealed.Length; i++)
            {
                if (revealed[i])
                {
                    result.InitialRevealed++;
                }
            }

            var wave = new List<RevealedCell>();
            while (true)
            {
                List<int> determined = Deduce(out Technique needed);
                if (determined.Count == 0)
                {
                    break;
                }

                result.Steps.Add(new SolveStep { Options = determined.Count, Needed = needed });
                if (needed > result.Hardest)
                {
                    result.Hardest = needed;
                }

                wave.Clear();
                foreach (int cell in determined)
                {
                    stage.RevealWithFlood(cell, revealed, wave);
                }

                var cells = new int[wave.Count];
                for (int i = 0; i < wave.Count; i++)
                {
                    cells[i] = wave[i].Cell;
                }

                result.Waves.Add(cells);
            }

            result.Revealed = (bool[])revealed.Clone();
            result.Solved = Array.TrueForAll(revealed, r => r);
            return result;
        }

        /// <summary>
        /// 지금 열린 상태에서 추론으로 바로 확정할 수 있는 칸들(가장 쉬운 추론으로 찾은 것). 힌트에 쓴다.
        /// 비어 있으면 더 추론할 수 없다.
        /// </summary>
        public List<int> NextDeductions(bool[] revealedState)
        {
            Array.Copy(revealedState, revealed, revealed.Length);
            return Deduce(out _);
        }

        /// <summary>
        /// 지금 상태에서 color 로 바로 칠할 수 있는 칸과 그 근거(단서 칸). 튜토리얼과 힌트가 "왜 이 칸인지" 보여 줄 때 쓴다.
        /// 순서대로 추론을 먼저 찾고, 없으면 두 단서 비교를 찾는다. 없으면 null.
        /// </summary>
        public Deduction Hint(bool[] revealedState, int color)
        {
            Array.Copy(revealedState, revealed, revealed.Length);
            Deduction direct = FindDirect(color);
            if (direct != null || maxTechnique == Technique.Direct)
            {
                return direct;
            }

            return FindPair(color);
        }

        /// <summary>쉬운 추론부터 시도해 확정되는 칸이 생기면 그 칸들과 쓴 추론을 돌려준다.</summary>
        List<int> Deduce(out Technique needed)
        {
            needed = Technique.Direct;
            var determined = new List<int>();
            var added = new HashSet<int>();
            for (int k = 0; k < stage.ColorCount; k++)
            {
                CollectDirect(k, determined, added);
            }

            if (determined.Count > 0 || maxTechnique == Technique.Direct)
            {
                return determined;
            }

            needed = Technique.Pair;
            for (int k = 0; k < stage.ColorCount; k++)
            {
                CollectPairs(k, determined, added);
            }

            return determined;
        }

        void CollectDirect(int color, List<int> into, HashSet<int> added)
        {
            for (int c = 0; c < revealed.Length; c++)
            {
                if (revealed[c] && IsDirect(c, color))
                {
                    foreach (int n in stage.Neighbors(c))
                    {
                        if (!revealed[n] && added.Add(n))
                        {
                            Check(n, color);
                            into.Add(n);
                        }
                    }
                }
            }
        }

        Deduction FindDirect(int color)
        {
            for (int c = 0; c < revealed.Length; c++)
            {
                if (!revealed[c] || !IsDirect(c, color))
                {
                    continue;
                }

                var deduction = new Deduction { Technique = Technique.Direct, Color = color, Clue = c };
                foreach (int n in stage.Neighbors(c))
                {
                    if (!revealed[n])
                    {
                        deduction.Cells.Add(n);
                    }
                }

                return deduction;
            }

            return null;
        }

        /// <summary>단서 c 의 color 남은 개수가 가려진 이웃 수와 같고 0 보다 크다.</summary>
        bool IsDirect(int c, int color)
        {
            int hidden = Hidden(c);
            return hidden > 0 && Remaining(c, color) == hidden;
        }

        void CollectPairs(int color, List<int> into, HashSet<int> added)
        {
            ForEachPair((c1, c2) =>
            {
                foreach (int n in PairCells(c1, c2, color))
                {
                    if (added.Add(n))
                    {
                        Check(n, color);
                        into.Add(n);
                    }
                }

                return false;
            });
        }

        Deduction FindPair(int color)
        {
            Deduction found = null;
            ForEachPair((c1, c2) =>
            {
                List<int> cells = PairCells(c1, c2, color);
                if (cells.Count == 0)
                {
                    return false;
                }

                found = new Deduction { Technique = Technique.Pair, Color = color, Clue = c1, Clue2 = c2 };
                found.Cells.AddRange(cells);
                return true;
            });
            return found;
        }

        /// <summary>가려진 이웃이 있는 가까운(5x5 안) 두 단서 칸 쌍을 돈다. visit 이 true 를 돌려주면 멈춘다.</summary>
        void ForEachPair(Func<int, int, bool> visit)
        {
            int width = stage.Width;
            for (int c1 = 0; c1 < revealed.Length; c1++)
            {
                if (!revealed[c1] || Hidden(c1) == 0)
                {
                    continue;
                }

                int x1 = c1 % width;
                int y1 = c1 / width;
                for (int y2 = Math.Max(0, y1 - 2); y2 <= Math.Min(stage.Height - 1, y1 + 2); y2++)
                {
                    for (int x2 = Math.Max(0, x1 - 2); x2 <= Math.Min(width - 1, x1 + 2); x2++)
                    {
                        int c2 = y2 * width + x2;
                        if (c2 != c1 && revealed[c2] && Hidden(c2) > 0 && visit(c1, c2))
                        {
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 두 단서의 가려진 이웃을 겹치는 칸 O, c1 에만 있는 칸 A, c2 에만 있는 칸 B 로 나눈다.
        /// O 에 들어갈 color 개수의 범위 [lo, hi] 로 A, B, O 중 "전부 color" 로 정해지는 칸을 돌려준다.
        /// </summary>
        List<int> PairCells(int c1, int c2, int color)
        {
            var result = new List<int>();
            scratchA.Clear();
            scratchO.Clear();
            scratchB.Clear();
            int[] n1 = stage.Neighbors(c1);
            int[] n2 = stage.Neighbors(c2);
            foreach (int n in n1)
            {
                if (!revealed[n])
                {
                    (Array.IndexOf(n2, n) >= 0 ? scratchO : scratchA).Add(n);
                }
            }

            if (scratchO.Count == 0)
            {
                return result;
            }

            foreach (int n in n2)
            {
                if (!revealed[n] && Array.IndexOf(n1, n) < 0)
                {
                    scratchB.Add(n);
                }
            }

            int rem1 = Remaining(c1, color);
            int rem2 = Remaining(c2, color);
            int lo = Math.Max(0, Math.Max(rem1 - scratchA.Count, rem2 - scratchB.Count));
            int hi = Math.Min(scratchO.Count, Math.Min(rem1, rem2));
            if (scratchA.Count > 0 && rem1 - hi == scratchA.Count)
            {
                result.AddRange(scratchA);
            }

            if (scratchB.Count > 0 && rem2 - hi == scratchB.Count)
            {
                result.AddRange(scratchB);
            }

            if (lo == scratchO.Count)
            {
                result.AddRange(scratchO);
            }

            return result;
        }

        int Hidden(int cell)
        {
            int hidden = 0;
            foreach (int n in stage.Neighbors(cell))
            {
                if (!revealed[n])
                {
                    hidden++;
                }
            }

            return hidden;
        }

        int Remaining(int cell, int color)
        {
            int remaining = stage.Clue(cell, color);
            foreach (int n in stage.Neighbors(cell))
            {
                if (revealed[n] && stage.ColorAt(n) == color)
                {
                    remaining--;
                }
            }

            return remaining;
        }

        /// <summary>정답은 항상 모든 단서를 만족하므로, 추론한 색이 정답과 다르면 규칙에 버그가 있는 것이다.</summary>
        void Check(int cell, int color)
        {
            if (stage.ColorAt(cell) != color)
            {
                throw new InvalidOperationException("solver deduced a wrong color for cell " + cell);
            }
        }
    }
}
