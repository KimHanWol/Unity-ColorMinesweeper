using System;
using System.Collections.Generic;

namespace ColorMinesweeper.Core
{
    /// <summary>솔버가 쓸 수 있는 추론의 세기. 위로 갈수록 사람이 떠올리기 어렵다.</summary>
    public enum Technique
    {
        /// <summary>단서 하나만 보고 판단한다. "남은 빨강이 0 → 나머지는 빨강 아님", "남은 빨강 = 후보 수 → 전부 빨강".</summary>
        Single = 1,

        /// <summary>겹치는 두 단서를 함께 본다. 지뢰찾기의 1-2 패턴을 색마다 적용한 것.</summary>
        Pair = 2,
    }

    public sealed class SolveResult
    {
        public bool Solved;

        /// <summary>givens 와 그 펼침으로 처음부터 열린 칸 수.</summary>
        public int InitialRevealed;

        /// <summary>추론으로 한 번에 확정된 칸 묶음. 사람이 푸는 "단계"에 가깝다(펼침 포함).</summary>
        public List<int[]> Waves = new List<int[]>();

        /// <summary><see cref="Technique.Pair"/> 추론이 한 번이라도 필요했는지.</summary>
        public bool UsedPair;

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
    /// 사람이 쓰는 추론만으로 퍼즐을 푼다. 찍어야 하는 순간이 오면 멈춘다.
    /// 확정된 칸은 플레이어가 칠해서 열 수 있는 칸이므로 즉시 열고, 새 단서를 이어서 쓴다.
    /// </summary>
    public sealed class Solver
    {
        readonly Stage stage;
        readonly Technique maxTechnique;
        readonly int allMask;
        readonly bool[] revealed;
        readonly int[] candidates;
        readonly List<int> scratchA = new List<int>(8);
        readonly List<int> scratchB = new List<int>(8);
        readonly List<int> scratchO = new List<int>(8);
        bool usedPair;

        public Solver(Stage stage, Technique maxTechnique = Technique.Pair)
        {
            this.stage = stage;
            this.maxTechnique = maxTechnique;
            allMask = (1 << stage.ColorCount) - 1;
            revealed = new bool[stage.CellCount];
            candidates = new int[stage.CellCount];
        }

        public SolveResult Solve(IEnumerable<int> givens)
        {
            Array.Clear(revealed, 0, revealed.Length);
            foreach (int g in givens)
            {
                stage.RevealWithFlood(g, revealed, null);
            }

            ResetCandidates();
            usedPair = false;

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
                List<int> determined = Deduce();
                if (determined.Count == 0)
                {
                    break;
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
                    candidates[cells[i]] = 1 << stage.ColorAt(cells[i]);
                }

                result.Waves.Add(cells);
            }

            result.UsedPair = usedPair;
            result.Revealed = (bool[])revealed.Clone();
            result.Solved = Array.TrueForAll(revealed, r => r);
            return result;
        }

        /// <summary>
        /// 지금 열린 상태에서 추론으로 바로 확정할 수 있는 칸들. 힌트에 쓴다. 비어 있으면 더 추론할 수 없다.
        /// </summary>
        public List<int> NextDeductions(bool[] revealedState)
        {
            Array.Copy(revealedState, revealed, revealed.Length);
            ResetCandidates();
            return Deduce();
        }

        void ResetCandidates()
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                candidates[i] = revealed[i] ? 1 << stage.ColorAt(i) : allMask;
            }
        }

        /// <summary>후보를 좁히다가 하나로 확정되는 칸이 생기면 그 칸들을 돌려준다.</summary>
        List<int> Deduce()
        {
            var determined = new List<int>();
            while (true)
            {
                while (ApplySingles())
                {
                }

                for (int i = 0; i < candidates.Length; i++)
                {
                    if (!revealed[i] && IsSingleBit(candidates[i]))
                    {
                        determined.Add(i);
                    }
                }

                if (determined.Count > 0 || maxTechnique < Technique.Pair || !ApplyPairs())
                {
                    return determined;
                }

                usedPair = true;
            }
        }

        bool ApplySingles()
        {
            bool changed = false;
            for (int c = 0; c < revealed.Length; c++)
            {
                if (!revealed[c])
                {
                    continue;
                }

                int[] neighbors = stage.Neighbors(c);
                for (int k = 0; k < stage.ColorCount; k++)
                {
                    int bit = 1 << k;
                    int remaining = Remaining(c, k);
                    int possible = 0;
                    foreach (int n in neighbors)
                    {
                        if (!revealed[n] && (candidates[n] & bit) != 0)
                        {
                            possible++;
                        }
                    }

                    if (possible == 0)
                    {
                        continue;
                    }

                    if (remaining == 0)
                    {
                        foreach (int n in neighbors)
                        {
                            if (!revealed[n] && (candidates[n] & bit) != 0)
                            {
                                Restrict(n, candidates[n] & ~bit);
                                changed = true;
                            }
                        }
                    }
                    else if (remaining == possible)
                    {
                        foreach (int n in neighbors)
                        {
                            if (!revealed[n] && candidates[n] != bit && (candidates[n] & bit) != 0)
                            {
                                Restrict(n, bit);
                                changed = true;
                            }
                        }
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// 가까운 두 단서 c1, c2 의 같은 색 후보 집합을 겹치는 부분 O 와 한쪽에만 있는 A, B 로 나눠
        /// O 에 들어갈 수 있는 개수 범위로 A, B, O 를 확정한다. 한 번이라도 바꾸면 바로 돌아간다.
        /// </summary>
        bool ApplyPairs()
        {
            int width = stage.Width;
            for (int c1 = 0; c1 < revealed.Length; c1++)
            {
                if (!IsFrontier(c1))
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
                        if (c2 <= c1 || !IsFrontier(c2))
                        {
                            continue;
                        }

                        for (int k = 0; k < stage.ColorCount; k++)
                        {
                            if (ApplyPair(c1, c2, k))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        bool ApplyPair(int c1, int c2, int k)
        {
            int bit = 1 << k;
            scratchA.Clear();
            scratchO.Clear();
            scratchB.Clear();
            int[] n1 = stage.Neighbors(c1);
            int[] n2 = stage.Neighbors(c2);
            foreach (int n in n1)
            {
                if (revealed[n] || (candidates[n] & bit) == 0)
                {
                    continue;
                }

                if (Array.IndexOf(n2, n) >= 0)
                {
                    scratchO.Add(n);
                }
                else
                {
                    scratchA.Add(n);
                }
            }

            if (scratchO.Count == 0)
            {
                return false;
            }

            foreach (int n in n2)
            {
                if (!revealed[n] && (candidates[n] & bit) != 0 && Array.IndexOf(n1, n) < 0)
                {
                    scratchB.Add(n);
                }
            }

            int rem1 = Remaining(c1, k);
            int rem2 = Remaining(c2, k);
            int lo = Math.Max(0, Math.Max(rem1 - scratchA.Count, rem2 - scratchB.Count));
            int hi = Math.Min(scratchO.Count, Math.Min(rem1, rem2));
            if (lo > hi)
            {
                throw new InvalidOperationException("solver contradiction at pair " + c1 + "," + c2);
            }

            bool changed = false;
            changed |= Settle(scratchA, rem1 - hi, rem1 - lo, bit);
            changed |= Settle(scratchB, rem2 - hi, rem2 - lo, bit);
            changed |= Settle(scratchO, lo, hi, bit);
            return changed;
        }

        /// <summary>cells 중 bit 색인 칸의 수가 [min, max] 일 때 전부/전무로 정해지면 반영한다.</summary>
        bool Settle(List<int> cells, int min, int max, int bit)
        {
            if (cells.Count == 0)
            {
                return false;
            }

            if (max == 0)
            {
                foreach (int n in cells)
                {
                    Restrict(n, candidates[n] & ~bit);
                }

                return true;
            }

            if (min == cells.Count)
            {
                bool changed = false;
                foreach (int n in cells)
                {
                    if (candidates[n] != bit)
                    {
                        Restrict(n, bit);
                        changed = true;
                    }
                }

                return changed;
            }

            return false;
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

        bool IsFrontier(int cell)
        {
            if (!revealed[cell])
            {
                return false;
            }

            foreach (int n in stage.Neighbors(cell))
            {
                if (!revealed[n])
                {
                    return true;
                }
            }

            return false;
        }

        void Restrict(int cell, int mask)
        {
            // 정답이 항상 모든 단서를 만족하므로, 정답 색이 후보에서 빠지면 추론 규칙에 버그가 있는 것이다.
            if ((mask & (1 << stage.ColorAt(cell))) == 0)
            {
                throw new InvalidOperationException("solver removed the true color of cell " + cell);
            }

            candidates[cell] = mask;
        }

        static bool IsSingleBit(int mask)
        {
            return mask != 0 && (mask & (mask - 1)) == 0;
        }
    }
}
