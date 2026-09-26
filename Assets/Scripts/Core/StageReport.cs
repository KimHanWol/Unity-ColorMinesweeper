using System.Collections.Generic;

namespace ColorMinesweeper.Core
{
    /// <summary>
    /// 스테이지가 게임에 넣을 만한지 판단하는 지표. 에디터 프리뷰와 StageTool 이 같은 기준을 쓴다.
    /// 추론이 잘 안 되는 그림은 열어 줘야 하는 칸(givens)이 많아지므로 그 수를 가장 먼저 본다.
    /// </summary>
    public sealed class StageReport
    {
        public readonly Stage Stage;
        public readonly SolveResult Result;

        /// <summary>검사에 쓴 givens. 파일에 없으면 게임과 같은 시드로 생성한 값이다.</summary>
        public readonly int[] Givens;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        /// <summary>givens 가 이보다 많으면 "추론이 잘 안 되는 그림"으로 경고한다.</summary>
        public int GivensBudget => System.Math.Max(4, Stage.CellCount / 25);

        public bool IsPlayable => Errors.Count == 0;

        public StageReport(Stage stage)
        {
            Stage = stage;
            if (stage.Givens.Length == 0)
            {
                Warnings.Add("givens 가 비어 있습니다. 게임이 실행할 때 생성하지만, 배포 전에 생성해 저장해 두세요.");
                Givens = GivensGenerator.Generate(stage, XorShiftRandom.SeedFrom(stage.Id));
            }
            else
            {
                Givens = stage.Givens;
            }

            Result = new Solver(stage).Solve(Givens);

            if (!Result.Solved)
            {
                Errors.Add("지금 givens 로는 추론이 막힙니다(" + (stage.CellCount - Result.RevealedCount) +
                           "칸 남음). givens 를 다시 생성하세요.");
            }

            int givens = Givens.Length;
            if (givens > GivensBudget)
            {
                Warnings.Add("열어 줘야 하는 칸이 " + givens + "개로 많습니다(기준 " + GivensBudget +
                             "). 비슷한 색이 대칭으로 붙은 곳을 고치면 줄어듭니다.");
            }

            if (Result.InitialRevealed * 2 > stage.CellCount)
            {
                Warnings.Add("시작부터 절반 넘게 열려 있습니다. 배경이 넓은 그림은 판을 줄여 보세요.");
            }
        }

        public string Summary()
        {
            return Stage.Width + "x" + Stage.Height + ", " + Stage.ColorCount + "색, givens " + Givens.Length +
                   ", 시작 " + Result.InitialRevealed + "칸 열림, " + Result.Waves.Count + "단계" +
                   (Result.UsedPair ? ", 두 단서 추론 필요" : "");
        }
    }
}
