using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 처음 시작하기를 누르면 보여 주는 튜토리얼. 5x5 연습 판(1색 하트) 위에서 말풍선으로 규칙을 안내하고,
    /// 솔버의 <see cref="Solver.Hint"/> 로 "지금 칠할 수 있는 칸과 그 근거"를 실제 숫자로 짚어 준다.
    ///
    /// 흐름: 소개 → 팔레트 설명 → 숫자의 뜻 → 짚어 주는 추론 한 번 → 근거만 비추고 스스로 찾기 → 배경도 색이라는 것
    /// → 혼자서 끝까지 → 완성하면 이름 공개 안내. 진행 기록에는 남기지 않는다.
    ///
    /// 짚는 방식: 판 전체를 어둡게 하고 근거 칸의 3x3 만 밝게 비춘다(둘레 8칸에서 센다는 것이 한눈에 보이게).
    /// 말풍선은 비춘 3x3(또는 짚은 팔레트 색) 바로 옆에 붙어 꼬리로 가리킨다.
    /// 칠할 칸은 두께가 일정한 진한 분홍 테두리가 숨 쉬고, 고를 팔레트 색은 뒤에서 후광이 숨 쉰다.
    /// 말풍선 꼬리가 이미 가리키므로 화살표는 두지 않는다(겹쳐서 복잡해 보였다).
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        const string DoneKey = "tutorial.done";
        const int GuidedSteps = 2;
        const int DimOrder = 56;
        const int MarkOrder = 60;

        /// <summary>연습 판. 5x5 하트(4칸 폭은 하트로 보이지 않았다). 한 색만 보고 5단계에 풀리고, 첫 추론에서 빨간색 6칸이 열린다.</summary>
        const string StageJson = @"{
  ""id"": ""tutorial"",
  ""name"": ""하트"",
  ""names"": { ""en"": ""Heart"" },
  ""logic"": ""basic"",
  ""background"": ""."",
  ""palette"": [
    { ""key"": ""."", ""color"": ""#FFF1F3"" },
    { ""key"": ""R"", ""color"": ""#E8505B"" }
  ],
  ""pixels"": [
    ""RR.RR"",
    ""RRRRR"",
    ""RRRRR"",
    "".RRR."",
    ""..R..""
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
        bool numberExplained;
        readonly HashSet<int> allowed = new HashSet<int>();
        Deduction current;
        string currentColorName;
        readonly List<Transform> marks = new List<Transform>();

        /// <summary>칠할 칸마다의 분홍 테두리. 칠하면 그 칸 테두리만 지운다.</summary>
        readonly Dictionary<int, Transform> targetMarks = new Dictionary<int, Transform>();

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
            Talk(Loc.T("tut.intro.title"), Loc.T("tut.intro.detail"), NextGuided);
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
                        AskColor(stage.BackgroundColor, Loc.T("tut.bg.title"), Loc.T("tut.bg.detail"));
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
                AskColor(hint.Color, Loc.T("tut.pick.title"), Loc.T("tut.pick.detail"));
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
            AnchorSwatch(color);
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
            int around = stage.Neighbors(hint.Clue).Length;
            int remaining = total - opened;
            bool background = hint.Color == stage.BackgroundColor;
            string colorName = background ? Loc.T("color.background") : ColorNames.Describe(stage.Colors[hint.Color].Color);
            if (guidedDone == 0 && !background && !numberExplained)
            {
                // 맨 처음에는 결론보다 먼저 숫자가 무슨 뜻인지부터 알려 준다(가운데 숫자가 숨 쉬고, 칠할 칸은 아직 짚지 않는다).
                numberExplained = true;
                phase = Phase.Talking;
                Spotlight(hint.Clue, true);
                AnchorArea(hint.Clue);
                bubble.Show(Loc.F("tut.number.title", total), Loc.F("tut.number.detail", total, around, colorName),
                    () => Guide(hint));
                return;
            }

            Spotlight(hint.Clue, false);
            MarkFound(hint.Clue, hint.Color);

            allowed.Clear();
            allowed.UnionWith(hint.Cells);
            current = hint;
            currentColorName = colorName;
            phase = Phase.WaitPaint;
            AnchorArea(hint.Clue);
            if (guidedDone == 1 && !background)
            {
                // 두 번째는 근거만 비추고, 칠할 칸은 스스로 찾게 한다.
                string count = opened == 0
                    ? Loc.F("tut.self.none", around, colorName, total)
                    : Loc.F("tut.self.some", around, colorName, total, opened, remaining);
                bubble.Show(Loc.T("tut.self.title"), count, null, Loc.F("tut.self.todo", remaining));
                return;
            }

            foreach (int cell in hint.Cells)
            {
                targetMarks[cell] = MarkTarget(cell);
            }

            // 굵은 줄은 가운데 숫자에서 출발하는 근거, 옅은 줄은 거기서 나오는 결론, 보라 줄은 할 일.
            // 색 이름은 모두 받침이 있어 "이" 를 쓴다.
            string title = opened == 0
                ? Loc.F("tut.guide.title.none", total, hidden)
                : Loc.F("tut.guide.title.some", total, opened);
            string detail = opened == 0
                ? Loc.F("tut.guide.detail.none", colorName, total, hidden)
                : Loc.F("tut.guide.detail.some", colorName, remaining, hidden);
            bubble.Show(title, detail, null, Loc.T(hidden == 1 ? "tut.todo.one" : "tut.todo.many"));
        }

        void StartFreePlay()
        {
            ClearMarks();
            phase = Phase.Free;
            bubble.AnchorBottom();
            bubble.Show(Loc.T("tut.free.title"), Loc.T("tut.free.detail"), () => bubble.Hide());
        }

        void Talk(string title, string detail, System.Action next)
        {
            phase = Phase.Talking;
            bubble.AnchorBottom();
            bubble.Show(title, detail, next);
        }

        /// <summary>말풍선을 근거 칸의 3x3 옆에 붙인다.</summary>
        void AnchorArea(int clue)
        {
            bubble.AnchorTo(play.BoardAreaToUi(play.Board.CellCenter(clue), new Vector2(3f, 3f)));
        }

        /// <summary>말풍선을 팔레트 색 바로 위에 붙인다.</summary>
        void AnchorSwatch(int color)
        {
            Transform swatch = play.Palette.SwatchTransform(color);
            if (swatch == null)
            {
                bubble.AnchorBottom();
                return;
            }

            Vector2 at = play.UiPoint(swatch);
            bubble.AnchorTo(new Rect(at.x - 0.8f, at.y - 0.8f, 1.6f, 1.6f));
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

        /// <summary>
        /// 짚어 주는 중에는 짚은 칸을 짚은 색으로만, 혼자 하는 동안에는 어디든 칠할 수 있다.
        /// 막을 때는 조용히 무시하지 않고 무엇을 하면 되는지 다시 알려 준다(다른 색이면 그 색부터 고르게 한다).
        /// </summary>
        public bool AllowPaint(int cell, int color)
        {
            if (phase == Phase.Free)
            {
                return true;
            }

            if (phase == Phase.WaitColor)
            {
                // 색을 골라야 하는데 판을 눌렀다: 말풍선을 흔들어 색부터 고르라고 알려 준다.
                bubble.Nudge();
                return false;
            }

            if (phase != Phase.WaitPaint || current == null)
            {
                return false;
            }

            if (color != current.Color)
            {
                MarkSwatch(current.Color);
                bubble.SetTodo(Loc.F("tut.pickFirst", currentColorName));
                bubble.Nudge();
                return false;
            }

            if (!allowed.Contains(cell))
            {
                bubble.Nudge();
                return false;
            }

            return true;
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
            int left = 0;
            foreach (int target in allowed)
            {
                if (!play.Session.IsRevealed(target))
                {
                    left++;
                }
            }

            if (left > 0)
            {
                bubble.SetTodo(Loc.F("tut.left", left));
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

        /// <summary>완성하면 기록을 남긴다. 결과 창은 플레이 화면이 띄운다.</summary>
        public void OnCleared()
        {
            phase = Phase.Done;
            ClearMarks();
            bubble.Hide();
            IsDone = true;
        }

        /// <summary>
        /// 판 전체를 어둡게 하고 근거 칸의 3x3 만 밝게 남긴다. 가운데(근거) 칸만 보라 테두리로 짚는다.
        /// </summary>
        /// <param name="pulse">가운데 테두리가 숨 쉬게 한다(숫자를 보라고 할 때).</param>
        void Spotlight(int clue, bool pulse)
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
                    new Color(0.13f, 0.1f, 0.24f, 0f), DimOrder, play.Board.CellCenter(cell), Vector2.one * 1.04f);
                marks.Add(shade.transform);
                Tween.Run(shade, 0.25f, t => Draw.SetAlpha(shade, 0.5f * t));
            }

            // 3x3 을 감싸는 큰 테두리는 두지 않는다. 밝은 칸과 가린 칸의 경계에 걸쳐 쪽마다 굵기가 달라 보였다.
            // 가린 조각끼리 살짝 겹쳐 밝은 창의 가장자리가 곧은 선이 되게 하는 것으로 영역을 보여 준다.
            Vector2 center = play.Board.CellCenter(clue);
            SpriteRenderer ring = Draw.OutlinePanel(board, "TutorialClue", new Vector2(0.98f, 0.98f), Theme.Accent, MarkOrder + 1,
                center, 0.24f, 0.11f);
            marks.Add(ring.transform);
            if (pulse)
            {
                Breathe(ring, 0.98f, 1.16f);
            }
        }

        /// <summary>주변 8칸 중 이미 보이는 같은 색 칸에 체크 표시를 단다("이미 찾은 칸"을 셀 수 있게).</summary>
        void MarkFound(int clue, int color)
        {
            Stage stage = play.Stage;
            Sprite check = Icons.Check;
            foreach (int n in stage.Neighbors(clue))
            {
                if (!play.Session.IsRevealed(n) || stage.ColorAt(n) != color)
                {
                    continue;
                }

                Transform badge = Draw.Node(play.Board.transform, "TutorialFound", play.Board.CellCenter(n) + new Vector2(0.3f, 0.3f));
                Draw.Sprite(badge, "Back", SpriteFactory.Circle(), Color.white, MarkOrder + 3, Vector2.zero, Vector2.one * 0.46f);
                Draw.Sprite(badge, "Face", SpriteFactory.Circle(), Theme.Accent, MarkOrder + 4, Vector2.zero, Vector2.one * 0.38f);
                Draw.Sprite(badge, "Check", check, Color.white, MarkOrder + 5, Vector2.zero, Vector2.one * 0.34f);
                marks.Add(badge);
            }
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

        /// <summary>
        /// 팔레트 색 뒤에서 은은한 후광이 숨 쉬게 한다. 팔레트의 선택 테두리와 겹치지 않도록 테두리나 화살표는 올리지 않는다.
        /// </summary>
        void MarkSwatch(int color)
        {
            Transform swatch = play.Palette.SwatchTransform(color);
            if (swatch == null)
            {
                return;
            }

            SpriteRenderer halo = Draw.Sprite(swatch, "TutorialHalo", SpriteFactory.SoftCircle(),
                Theme.WithAlpha(Theme.Highlight, 0f), 92, Vector2.zero, new Vector2(2.6f, 2.6f));
            marks.Add(halo.transform);
            Glow(halo);
        }

        void Glow(SpriteRenderer halo)
        {
            if (halo == null)
            {
                return;
            }

            Tween.Run(halo, 1.1f, t => Draw.SetAlpha(halo, 0.2f + 0.45f * Ease.Pulse(t)), Ease.Linear, 0f, () => Glow(halo));
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
        }
    }
}
