using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 처음 실행하면 보여 주는 튜토리얼. 작은 연습 판(1색 하트) 위에서 말풍선으로 규칙을 안내하고,
    /// 솔버의 <see cref="Solver.Hint"/> 로 "지금 칠할 수 있는 칸과 그 근거"를 실제 숫자로 짚어 준다.
    ///
    /// 흐름: 소개 → 팔레트 설명 → 짚어 주는 추론 한 번 → 근거 칸만 짚어 주고 스스로 찾기 → 배경도 색이라는 것
    /// → 혼자서 끝까지 → 완성하면 이름 공개 안내. 진행 기록에는 남기지 않는다.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        const string DoneKey = "tutorial.done";
        const int GuidedSteps = 2;

        /// <summary>연습 판. 배경이 넓어 시작 칸이 크게 펼쳐지고, 한 색만 보고 순서대로 풀린다.</summary>
        const string StageJson = @"{
  ""id"": ""tutorial"",
  ""name"": ""하트"",
  ""logic"": ""basic"",
  ""background"": ""."",
  ""palette"": [
    { ""key"": ""."", ""color"": ""#FFF1F3"" },
    { ""key"": ""R"", ""color"": ""#E8505B"" }
  ],
  ""pixels"": [
    ""........."",
    ""..RR.RR.."",
    "".RRRRRRR."",
    "".RRRRRRR."",
    ""..RRRRR.."",
    ""...RRR..."",
    ""....R...."",
    "".........""
  ]
}";

        enum Phase
        {
            Talking,
            WaitColor,
            WaitPaint,
            Free,
            Done,
        }

        PlayScreen play;
        SpeechBubble bubble;
        Solver solver;
        Phase phase = Phase.Talking;
        int targetColor = -1;
        int guidedDone;
        bool backgroundTaught;
        readonly HashSet<int> allowed = new HashSet<int>();
        readonly List<Transform> marks = new List<Transform>();

        public static bool IsDone
        {
            get => PlayerPrefs.GetInt(DoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static Stage CreateStage()
        {
            return StageSerializer.Parse(StageJson);
        }

        public void Begin(PlayScreen screen, SpeechBubble speech)
        {
            play = screen;
            bubble = speech;
            solver = new Solver(screen.Stage, Technique.Direct);
            Talk("숨겨진 도트 그림을 추리해서 완성하는 퍼즐이에요.", ExplainPalette);
        }

        void ExplainPalette()
        {
            ClearMarks();
            MarkSwatch(play.Palette.Selected);
            Talk("아래에서 색을 고르면, 열린 칸마다 둘레의 8칸 중 그 색이 몇 칸인지 숫자로 보여요.", NextGuided);
        }

        /// <summary>다음으로 가르칠 추론을 고른다. 두 번 짚어 준 뒤 배경을 가르치고, 그다음은 혼자 하게 둔다.</summary>
        void NextGuided()
        {
            ClearMarks();
            Stage stage = play.Stage;
            bool[] revealed = play.Session.SnapshotRevealed();

            if (guidedDone >= GuidedSteps && !backgroundTaught)
            {
                Deduction background = solver.Hint(revealed, stage.BackgroundColor);
                if (background != null)
                {
                    if (play.Palette.Selected != stage.BackgroundColor)
                    {
                        AskColor(stage.BackgroundColor, "배경도 하나의 색이에요. 아래의 네모 칸(배경)을 골라 보세요.");
                        return;
                    }

                    Guide(background);
                    return;
                }
            }

            if (guidedDone >= GuidedSteps + 1 || (guidedDone >= GuidedSteps && backgroundTaught))
            {
                StartFreePlay();
                return;
            }

            Deduction hint = solver.Hint(revealed, play.Palette.Selected) ?? AnyHint(revealed);
            if (hint == null)
            {
                StartFreePlay();
                return;
            }

            if (hint.Color != play.Palette.Selected)
            {
                AskColor(hint.Color, "이번엔 이 색을 골라 보세요.");
                return;
            }

            Guide(hint);
        }

        Deduction AnyHint(bool[] revealed)
        {
            Stage stage = play.Stage;
            for (int k = 0; k < stage.ColorCount; k++)
            {
                if (k == stage.BackgroundColor && !backgroundTaught)
                {
                    continue;
                }

                Deduction hint = solver.Hint(revealed, k);
                if (hint != null)
                {
                    return hint;
                }
            }

            return solver.Hint(revealed, stage.BackgroundColor);
        }

        void AskColor(int color, string message)
        {
            targetColor = color;
            phase = Phase.WaitColor;
            MarkSwatch(color);
            bubble.Show(message);
        }

        /// <summary>근거 칸(파란 테두리)과 칠할 칸(노란 테두리)을 짚고, 실제 숫자로 이유를 설명한다.</summary>
        void Guide(Deduction hint)
        {
            ClearMarks();
            Stage stage = play.Stage;
            int total = stage.Clue(hint.Clue, hint.Color);
            int opened = 0;
            foreach (int n in stage.Neighbors(hint.Clue))
            {
                if (play.Session.IsRevealed(n) && stage.ColorAt(n) == hint.Color)
                {
                    opened++;
                }
            }

            int hidden = hint.Cells.Count;
            bool background = hint.Color == stage.BackgroundColor;
            string colorName = background ? "배경" : "이 색";
            MarkNeighborhood(hint.Clue);

            allowed.Clear();
            allowed.UnionWith(hint.Cells);
            phase = Phase.WaitPaint;
            if (guidedDone == 1 && !background)
            {
                // 두 번째는 근거 칸만 짚고, 칠할 칸은 스스로 찾게 한다.
                bubble.Show("이번엔 스스로 찾아볼까요? 파란 테두리 칸 둘레의 8칸을 보고, 숫자만큼 칠할 칸을 찾아보세요.");
                return;
            }

            foreach (int cell in hint.Cells)
            {
                MarkCell(cell, Theme.Gold, true);
            }

            string meaning = "파란 테두리 칸의 숫자 " + total + "은 둘레 8칸 중 " + colorName + "이 " + total + "칸이라는 뜻이에요.";
            string reason = opened == 0
                ? " 그런데 가려진 칸도 딱 " + hidden + "칸뿐이에요."
                : " 이미 열린 " + opened + "칸을 빼면 " + (total - opened) + "칸이 남았는데, 가려진 칸도 " + hidden + "칸뿐이에요.";
            bubble.Show(meaning + reason + " 그러니 노란 칸은 모두 " + colorName + "! 눌러서 칠해 보세요.");
        }

        void StartFreePlay()
        {
            ClearMarks();
            phase = Phase.Free;
            Talk("틀리면 하트가 하나 줄어요. 이제 혼자서 끝까지 완성해 보세요!", () =>
            {
                phase = Phase.Free;
                bubble.Hide();
            });
            phase = Phase.Free;
        }

        void Talk(string message, System.Action next)
        {
            phase = Phase.Talking;
            bubble.Show(message, next);
        }

        public void OnColorSelected(int color)
        {
            if (phase == Phase.WaitColor && color == targetColor)
            {
                Deduction hint = solver.Hint(play.Session.SnapshotRevealed(), color);
                if (hint == null)
                {
                    NextGuided();
                    return;
                }

                Guide(hint);
            }
        }

        /// <summary>짚어 주는 중에는 짚은 칸만, 혼자 하는 동안에는 어디든 칠할 수 있다.</summary>
        public bool AllowPaint(int cell)
        {
            return phase == Phase.Free || (phase == Phase.WaitPaint && allowed.Contains(cell));
        }

        public void OnPainted(int cell, int color)
        {
            if (phase != Phase.WaitPaint)
            {
                return;
            }

            if (color == play.Stage.BackgroundColor)
            {
                backgroundTaught = true;
            }

            guidedDone++;
            phase = Phase.Talking;
            ClearMarks();
            bubble.Hide();
            Tween.Delay(this, 0.7f, NextGuided);
        }

        /// <summary>완성하면 기록을 남기고 시작 화면으로 보낼 창을 띄운다.</summary>
        public void OnCleared()
        {
            phase = Phase.Done;
            ClearMarks();
            bubble.Hide();
            IsDone = true;
        }

        void MarkCell(int cell, Color color, bool pulse)
        {
            Transform board = play.Board.transform;
            SpriteRenderer ring = Draw.Sprite(board, "TutorialMark", SpriteFactory.OutlineRect(), color, 60,
                play.Board.CellCenter(cell), new Vector2(1.08f, 1.08f));
            AddMark(ring.transform, pulse ? 1.08f : 0f);
        }

        /// <summary>
        /// 근거 칸과 그 둘레 8칸을 한 덩어리로 감싸고, 둘레 8칸을 옅게 칠해 "이 칸을 뺀 주변 8칸에서 센다"는 것을 보여 준다.
        /// </summary>
        void MarkNeighborhood(int cell)
        {
            Transform board = play.Board.transform;
            Vector2 center = play.Board.CellCenter(cell);
            foreach (int n in play.Stage.Neighbors(cell))
            {
                SpriteRenderer tint = Draw.Sprite(board, "TutorialNeighbor", SpriteFactory.RoundedRect(0.2f),
                    Theme.WithAlpha(Theme.Accent, 0.22f), 55, play.Board.CellCenter(n), new Vector2(CellView.TileSize, CellView.TileSize));
                AddMark(tint.transform, 0f);
            }

            SpriteRenderer area = Draw.Sprite(board, "TutorialArea", SpriteFactory.OutlineRect(), Theme.WithAlpha(Theme.Accent, 0.7f), 58,
                center, new Vector2(3.1f, 3.1f));
            AddMark(area.transform, 0f);
            MarkCell(cell, Theme.Accent, false);
        }

        void MarkSwatch(int color)
        {
            Transform swatch = play.Palette.SwatchTransform(color);
            if (swatch == null)
            {
                return;
            }

            SpriteRenderer ring = Draw.Sprite(swatch, "TutorialMark", SpriteFactory.OutlineCircle(), Theme.Gold, 99,
                Vector2.zero, new Vector2(1.9f, 1.9f));
            AddMark(ring.transform, 1.9f);
        }

        /// <summary>pulseSize 가 0 보다 크면 그 크기를 기준으로 계속 숨 쉬듯 커졌다 작아진다.</summary>
        void AddMark(Transform mark, float pulseSize)
        {
            marks.Add(mark);
            if (pulseSize > 0f)
            {
                PulseLoop(mark, pulseSize);
            }
        }

        void PulseLoop(Transform mark, float size)
        {
            if (mark == null)
            {
                return;
            }

            Tween.Run(mark, 0.8f, t =>
            {
                float s = size * (1f + Ease.Pulse(t) * 0.12f);
                mark.localScale = new Vector3(s, s, 1f);
            }, Ease.Linear, 0f, () => PulseLoop(mark, size));
        }

        void ClearMarks()
        {
            foreach (Transform mark in marks)
            {
                if (mark != null)
                {
                    Tween.Kill(mark);
                    Destroy(mark.gameObject);
                }
            }

            marks.Clear();
            allowed.Clear();
        }
    }
}
