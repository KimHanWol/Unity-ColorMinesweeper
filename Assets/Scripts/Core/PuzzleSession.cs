using System.Collections.Generic;

namespace ColorMinesweeper.Core
{
    public enum PaintOutcome
    {
        /// <summary>이미 열린 칸이거나 끝난 판이라 아무 일도 없었다.</summary>
        Ignored,
        Correct,
        Wrong,
    }

    public sealed class PaintResult
    {
        public PaintOutcome Outcome;

        /// <summary>이번에 열린 칸(펼침 포함), BFS 순서.</summary>
        public readonly List<RevealedCell> Revealed = new List<RevealedCell>();

        public bool Cleared;
        public bool GameOver;
    }

    /// <summary>한 판의 진행 상태. 화면과 무관한 규칙만 담는다.</summary>
    public sealed class PuzzleSession
    {
        public const int MaxLives = 3;

        /// <summary>광고를 보고 이어 할 때 돌려주는 목숨.</summary>
        public const int ReviveLives = 1;

        public Stage Stage { get; }
        public int Lives { get; private set; }
        public int Mistakes { get; private set; }
        public int Revives { get; private set; }
        public bool IsCleared { get; private set; }
        public bool IsGameOver => Lives <= 0;

        readonly bool[] revealed;
        readonly int[] remaining;
        int revealedCount;

        public PuzzleSession(Stage stage, int[] givens)
        {
            Stage = stage;
            Lives = MaxLives;
            revealed = new bool[stage.CellCount];
            remaining = new int[stage.ColorCount];
            for (int k = 0; k < stage.ColorCount; k++)
            {
                remaining[k] = stage.ColorTotal(k);
            }

            var opened = new List<RevealedCell>();
            foreach (int g in givens)
            {
                stage.RevealWithFlood(g, revealed, opened);
            }

            Count(opened);
            IsCleared = revealedCount == stage.CellCount;
        }

        public bool IsRevealed(int cell)
        {
            return revealed[cell];
        }

        /// <summary>현재 열린 상태의 복사본. 힌트 계산 등에 쓴다.</summary>
        public bool[] SnapshotRevealed()
        {
            return (bool[])revealed.Clone();
        }

        /// <summary>아직 칠하지 않은 color 칸 수. 0 이 되면 팔레트에서 그 색을 흐리게 한다.</summary>
        public int Remaining(int color)
        {
            return remaining[color];
        }

        public int RevealedCount => revealedCount;

        public PaintResult Paint(int cell, int color)
        {
            var result = new PaintResult();
            if (IsCleared || IsGameOver || revealed[cell])
            {
                result.Outcome = PaintOutcome.Ignored;
                return result;
            }

            if (Stage.ColorAt(cell) != color)
            {
                result.Outcome = PaintOutcome.Wrong;
                Mistakes++;
                Lives--;
                result.GameOver = IsGameOver;
                return result;
            }

            result.Outcome = PaintOutcome.Correct;
            Stage.RevealWithFlood(cell, revealed, result.Revealed);
            Count(result.Revealed);
            IsCleared = revealedCount == Stage.CellCount;
            result.Cleared = IsCleared;
            return result;
        }

        public void Revive()
        {
            if (!IsGameOver)
            {
                return;
            }

            Lives = ReviveLives;
            Revives++;
        }

        /// <summary>실수 없이 3, 한 번 실수 2, 그 외 1. 광고로 이어 했으면 1.</summary>
        public int Stars
        {
            get
            {
                if (Revives > 0)
                {
                    return 1;
                }

                return Mistakes == 0 ? 3 : Mistakes == 1 ? 2 : 1;
            }
        }

        void Count(List<RevealedCell> opened)
        {
            foreach (RevealedCell r in opened)
            {
                revealedCount++;
                remaining[Stage.ColorAt(r.Cell)]--;
            }
        }
    }
}
