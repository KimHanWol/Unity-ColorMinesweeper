using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 처음 시작하기를 누르면 보여 주는 튜토리얼. 작은 연습 판(1색 하트) 위에서 말풍선으로 규칙을 안내하고,
    /// 솔버의 <see cref="Solver.Hint"/> 로 "지금 칠할 수 있는 칸과 그 근거"를 실제 숫자로 짚어 준다.
    ///
    /// 흐름: 소개 → 팔레트 설명 → 짚어 주는 추론 한 번 → 근거만 비추고 스스로 찾기 → 배경도 색이라는 것
    /// → 혼자서 끝까지 → 완성하면 이름 공개 안내. 진행 기록에는 남기지 않는다.
    ///
    /// 짚는 방식: 판 전체를 어둡게 하고 근거 칸의 3x3 만 밝게 비춘다(둘레 8칸에서 센다는 것이 한눈에 보이게).
    /// 칠할 칸은 두께가 일정한 진한 분홍 테두리가 숨 쉬고, 그 위에 화살표가 통통 튄다.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        const string DoneKey = "tutorial.done";
        const int GuidedSteps = 2;
        const int DimOrder = 56;
        const int MarkOrder = 60;

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

        /// <summary>칠할 칸마다의 분홍 테두리. 칠하면 그 칸 테두리만 지우고 화살표를 다음 칸으로 옮긴다.</summary>
        readonly Dictionary<int, Transform> targetMarks = new Dictionary<int, Transform>();

        Transform boardPointer;

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
            Talk("도트 그림 추리 퍼즐", "숫자를 단서로 칸을 칠해 숨은 그림을 완성해요.", ExplainPalette);
        }

        void ExplainPalette()
        {
            ClearMarks();
            MarkSwatch(play.Palette.Selected);
            Talk("색을 고르면 숫자가 바뀌어요", "열린 칸의 숫자 = 둘레 8칸 중 고른 색의 수", NextGuided);
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
                        AskColor(stage.BackgroundColor, "배경도 하나의 색이에요", "아래의 네모 칸(배경)을 골라 보세요.");
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
                AskColor(hint.Color, "이 색을 골라 보세요", "화살표가 가리키는 색이에요.");
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

        void AskColor(int color, string title, string detail)
        {
            targetColor = color;
            phase = Phase.WaitColor;
            MarkSwatch(color);
            bubble.Show(title, detail);
        }

        /// <summary>근거 칸의 3x3 을 비추고, 칠할 칸을 짚고, 실제 숫자로 짧게 이유를 말한다.</summary>
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
            Spotlight(hint.Clue);

            allowed.Clear();
            allowed.UnionWith(hint.Cells);
            phase = Phase.WaitPaint;
            if (guidedDone == 1 && !background)
            {
                // 두 번째는 근거만 비추고, 칠할 칸은 스스로 찾게 한다.
                bubble.Show("이번엔 직접 찾아보세요", "밝게 비춘 8칸에서 숫자만큼 칠할 칸을 찾아요.");
                return;
            }

            foreach (int cell in hint.Cells)
            {
                targetMarks[cell] = MarkTarget(cell);
            }

            MovePointer();
            string title = hidden == 1 ? "반짝이는 칸은 " + colorName + "!" : "반짝이는 칸 " + hidden + "개는 모두 " + colorName + "!";
            string detail = opened == 0
                ? "둘레 8칸에 " + colorName + "이 " + total + "칸, 가려진 칸도 " + hidden + "칸이에요."
                : "숫자 " + total + " 중 " + opened + "칸은 이미 열려 " + (total - opened) + "칸 남았고, 가려진 칸도 " + hidden + "칸이에요.";
            bubble.Show(title, detail);
        }

        void StartFreePlay()
        {
            ClearMarks();
            phase = Phase.Free;
            bubble.Show("이제 혼자 완성해요!", "틀리면 하트가 하나 줄어요.", () => bubble.Hide());
        }

        void Talk(string title, string detail, System.Action next)
        {
            phase = Phase.Talking;
            bubble.Show(title, detail, next);
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

            if (targetMarks.TryGetValue(cell, out Transform mark) && mark != null)
            {
                Tween.Kill(mark);
                Destroy(mark.gameObject);
            }

            targetMarks.Remove(cell);

            // 짚어 준 칸을 전부 칠해야 넘어간다(펼침으로 같이 열린 칸도 칠한 것으로 친다).
            foreach (int target in allowed)
            {
                if (!play.Session.IsRevealed(target))
                {
                    MovePointer();
                    return;
                }
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

        /// <summary>완성하면 기록을 남긴다. 결과 창은 플레이 화면이 띄운다.</summary>
        public void OnCleared()
        {
            phase = Phase.Done;
            ClearMarks();
            bubble.Hide();
            IsDone = true;
        }

        /// <summary>
        /// 판 전체를 어둡게 하고 근거 칸의 3x3 만 남긴다. 가운데(근거) 칸은 보라 테두리, 3x3 은 옅은 보라 테두리로 감싼다.
        /// </summary>
        void Spotlight(int clue)
        {
            Stage stage = play.Stage;
            Transform board = play.Board.transform;
            var lit = new HashSet<int>(stage.Neighbors(clue)) { clue };
            for (int cell = 0; cell < stage.CellCount; cell++)
            {
                if (lit.Contains(cell))
                {
                    continue;
                }

                SpriteRenderer shade = Draw.Sprite(board, "TutorialShade", SpriteFactory.Square(),
                    new Color(0.13f, 0.1f, 0.24f, 0f), DimOrder, play.Board.CellCenter(cell), Vector2.one * 1.01f);
                marks.Add(shade.transform);
                Tween.Run(shade, 0.25f, t => Draw.SetAlpha(shade, 0.5f * t));
            }

            Vector2 center = play.Board.CellCenter(clue);
            marks.Add(Draw.OutlinePanel(board, "TutorialArea", new Vector2(3.08f, 3.08f), Theme.WithAlpha(Theme.Accent, 0.55f),
                MarkOrder, center, 0.34f, 0.07f).transform);
            marks.Add(Draw.OutlinePanel(board, "TutorialClue", new Vector2(0.98f, 0.98f), Theme.Accent, MarkOrder + 1, center,
                0.24f, 0.11f).transform);
        }

        /// <summary>칠할 칸: 두께가 일정한 분홍 테두리가 크기만 바뀌며 숨 쉰다(선이 굵어졌다 얇아지지 않는다).</summary>
        Transform MarkTarget(int cell)
        {
            SpriteRenderer ring = Draw.OutlinePanel(play.Board.transform, "TutorialTarget", Vector2.one, Theme.Highlight,
                MarkOrder + 2, play.Board.CellCenter(cell), 0.24f, 0.12f);
            marks.Add(ring.transform);
            Breathe(ring, 0.98f, 1.14f);
            return ring.transform;
        }

        /// <summary>남은 칸 중 첫 칸 위로 화살표를 옮긴다.</summary>
        void MovePointer()
        {
            int next = -1;
            foreach (int cell in allowed)
            {
                if (!play.Session.IsRevealed(cell))
                {
                    next = cell;
                    break;
                }
            }

            if (next < 0 || targetMarks.Count == 0)
            {
                return;
            }

            if (boardPointer == null)
            {
                boardPointer = CreatePointer(play.Board.transform, MarkOrder + 3, 0.62f);
            }

            Bob(boardPointer, play.Board.CellCenter(next) + new Vector2(0f, 1.0f));
        }

        void MarkSwatch(int color)
        {
            Transform swatch = play.Palette.SwatchTransform(color);
            if (swatch == null)
            {
                return;
            }

            SpriteRenderer ring = Draw.Sprite(swatch, "TutorialSwatch", SpriteFactory.OutlineCircle(), Theme.Highlight, 99,
                Vector2.zero, new Vector2(1.75f, 1.75f));
            marks.Add(ring.transform);
            Tween.Run(ring, 0.9f, t => Draw.SetAlpha(ring, 0.45f + 0.55f * Ease.Pulse(t)), Ease.Linear, 0f,
                () => Glow(ring));

            Transform pointer = CreatePointer(swatch, 99, 0.6f);
            Bob(pointer, new Vector2(0f, 1.45f));
        }

        void Glow(SpriteRenderer ring)
        {
            if (ring == null)
            {
                return;
            }

            Tween.Run(ring, 0.9f, t => Draw.SetAlpha(ring, 0.45f + 0.55f * Ease.Pulse(t)), Ease.Linear, 0f, () => Glow(ring));
        }

        /// <summary>흰 테두리를 두른 분홍 화살표. 밝은 칸과 어두운 칸 어디서든 보이게 테두리를 한 번 더 그린다.</summary>
        Transform CreatePointer(Transform parent, int order, float size)
        {
            Transform root = Draw.Node(parent, "TutorialPointer");
            Sprite arrow = PixelGlyphs.Icon("pointer", PixelGlyphs.Pointer);
            Draw.Sprite(root, "Outline", arrow, Color.white, order, Vector2.zero, Vector2.one * size * 1.3f);
            Draw.Sprite(root, "Arrow", arrow, Theme.Highlight, order + 1, new Vector2(0f, 0.02f), Vector2.one * size);
            marks.Add(root);
            return root;
        }

        /// <summary>at 위에서 위아래로 통통 튄다.</summary>
        void Bob(Transform pointer, Vector2 at)
        {
            Tween.Kill(pointer);
            BobLoop(pointer, at);
        }

        void BobLoop(Transform pointer, Vector2 at)
        {
            if (pointer == null)
            {
                return;
            }

            Tween.Run(pointer, 0.6f, t => pointer.localPosition = at + new Vector2(0f, 0.18f * Ease.Pulse(t)), Ease.Linear, 0f,
                () => BobLoop(pointer, at));
        }

        /// <summary>테두리 크기만 from~to 로 오가게 한다(sliced 라 선 두께는 그대로).</summary>
        void Breathe(SpriteRenderer ring, float from, float to)
        {
            if (ring == null)
            {
                return;
            }

            Tween.Run(ring, 0.8f, t =>
            {
                float s = Mathf.Lerp(from, to, Ease.Pulse(t));
                ring.size = new Vector2(s, s);
            }, Ease.Linear, 0f, () => Breathe(ring, from, to));
        }

        void ClearMarks()
        {
            foreach (Transform mark in marks)
            {
                if (mark != null)
                {
                    Tween.Kill(mark);
                    foreach (SpriteRenderer r in mark.GetComponentsInChildren<SpriteRenderer>())
                    {
                        Tween.Kill(r);
                    }

                    Destroy(mark.gameObject);
                }
            }

            marks.Clear();
            targetMarks.Clear();
            allowed.Clear();
            boardPointer = null;
        }
    }
}
