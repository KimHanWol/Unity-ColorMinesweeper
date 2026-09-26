using System;

namespace ColorMinesweeper.Core
{
    /// <summary>
    /// 스테이지 난이도. 푸는 수고(단계별 추론, 판 크기, 색 수)에 "끝까지 푸는 데 필요한 가장 어려운 추론"의
    /// 가산점을 더한 <see cref="Total"/> 로 순서를 정한다.
    /// </summary>
    public readonly struct Difficulty : IComparable<Difficulty>
    {
        public static readonly string[] TierNames = { "쉬움", "보통", "어려움", "매우 어려움" };

        /// <summary>칸 이만큼마다 1점.</summary>
        const float SizeWeight = 20f;

        /// <summary>배경 말고 색이 하나 늘 때마다 더하는 점수.</summary>
        const float ColorWeight = 4f;

        /// <summary>
        /// 필요한 추론 단계마다 더하는 점수. 순서대로 풀리는 판이 대체로 앞에 오되, 색이 아주 많거나 큰 판까지
        /// 무조건 앞에 오지는 않도록 한 줄 점수로 합친다(초반은 색이 적고 작은 그림이 먼저 나와야 한다).
        /// </summary>
        static readonly float[] TechniquePenalty = { 0f, 8f, 20f };

        public readonly Technique Hardest;

        /// <summary>
        /// 단계 하나하나의 수고를 더한 값.
        /// - 순서대로 1, 제외 기억 2, 조합 4(경우의 수를 머릿속으로 따져야 한다).
        /// - 그 순간 확정할 수 있는 칸이 1~2개뿐이면 1.5배(어디를 봐야 할지 찾기 어렵다). 6개 이상이면 0.7배.
        /// - 판이 크면 훑어볼 칸이 많고, 색이 많으면 칩마다 읽을 숫자가 늘어서 크게 더한다.
        ///   초반은 색이 적고 작은 그림부터 나오게 하려고 이 두 항의 비중을 일부러 높였다.
        /// </summary>
        public readonly float Score;

        public readonly int PairSteps;
        public readonly int Steps;

        public Difficulty(Technique hardest, float score, int pairSteps, int steps)
        {
            Hardest = hardest;
            Score = score;
            PairSteps = pairSteps;
            Steps = steps;
        }

        /// <summary>순서를 정하는 최종 점수: 푸는 수고 + 필요한 추론 단계의 가산점.</summary>
        public float Total => Score + TechniquePenalty[(int)Hardest];

        /// <summary>0 쉬움, 1 보통, 2 어려움, 3 매우 어려움.</summary>
        public int Tier => Total < 18f ? 0 : Total < 32f ? 1 : Total < 50f ? 2 : 3;

        public string TierName => TierNames[Tier];

        public static Difficulty Rate(Stage stage, SolveResult result)
        {
            float score = 0f;
            int pairSteps = 0;
            foreach (SolveStep step in result.Steps)
            {
                float cost = step.Needed == Technique.Pair ? 4f : step.Needed == Technique.Single ? 2f : 1f;
                if (step.Options <= 2)
                {
                    cost *= 1.5f;
                }
                else if (step.Options >= 6)
                {
                    cost *= 0.7f;
                }

                if (step.Needed == Technique.Pair)
                {
                    pairSteps++;
                }

                score += cost;
            }

            score += stage.CellCount / SizeWeight;
            score += (stage.ColorCount - 2) * ColorWeight;
            return new Difficulty(result.Hardest, score, pairSteps, result.Steps.Count);
        }

        public int CompareTo(Difficulty other)
        {
            return Total.CompareTo(other.Total);
        }

        public override string ToString()
        {
            string technique = Hardest == Technique.Direct ? "순서대로" : Hardest == Technique.Single ? "제외 기억" : "조합 " + PairSteps + "번";
            return TierName + " " + Total.ToString("0.0") + "점(" + technique + ", " + Steps + "단계)";
        }
    }
}
