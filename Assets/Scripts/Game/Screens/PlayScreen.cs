using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>한 판을 푸는 화면. 규칙은 <see cref="PuzzleSession"/> 에 있고, 여기서는 입력과 연출을 잇는다.</summary>
    public sealed class PlayScreen : ScreenBase
    {
        const int ModalOrder = 200;
        static readonly Vector2 ResultCardSize = new Vector2(7.6f, 7.4f);

        /// <summary>완성 카드 윗변(안전 영역 아래에서 잰 높이). 완성된 그림은 이 위에 보인다.</summary>
        static float ResultCardTop => 0.5f + ResultCardSize.y;

        Stage stage;
        int stageIndex;
        PuzzleSession session;
        BoardView board;
        Hud hud;
        PaletteBar palette;
        Transform top;
        Transform bottom;
        UiKit.Modal modal;

        /// <summary>뒤로 가기로 닫아도 되는 창(설정, 나가기 확인)인지. 게임 오버 창은 뒤로 가기로 닫지 않는다.</summary>
        bool modalClosable;
        bool busy;
        int initialRevealed;
        TutorialDirector tutorial;
        SpeechBubble bubble;

        public Stage Stage => stage;
        public PuzzleSession Session => session;
        public BoardView Board => board;
        public PaletteBar Palette => palette;

        /// <summary>판 좌표(보드 로컬) 영역을 UI 화면 좌표(이 화면 루트 기준)로. 튜토리얼 말풍선을 판 옆에 붙일 때 쓴다.</summary>
        public Rect BoardAreaToUi(Vector2 center, Vector2 size)
        {
            Vector2 a = BoardPointToUi(center - size / 2f);
            Vector2 b = BoardPointToUi(center + size / 2f);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        Vector2 BoardPointToUi(Vector2 local)
        {
            Vector3 world = board.transform.TransformPoint(local);
            Vector3 screen = App.BoardCamera.Camera.WorldToScreenPoint(world);
            Vector3 ui = Ui.Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
            return transform.InverseTransformPoint(ui);
        }

        /// <summary>UI 오브젝트의 위치를 이 화면 루트 기준 좌표로.</summary>
        public Vector2 UiPoint(Transform target)
        {
            return transform.InverseTransformPoint(target.position);
        }

        /// <summary>index 가 -1 이면 에디터에서 띄운 시험 플레이라 진행을 저장하지 않는다.</summary>
        public void Setup(Stage stageToPlay, int index, bool asTutorial = false)
        {
            stage = stageToPlay;
            stageIndex = index;
            if (asTutorial)
            {
                tutorial = gameObject.AddComponent<TutorialDirector>();
            }
        }

        protected override void Build()
        {
            session = new PuzzleSession(stage, StageCatalog.GivensFor(stage));
            initialRevealed = session.RevealedCount;
            board = BoardView.Create(World, stage);

            top = Draw.Node(transform, "Top");
            bottom = Draw.Node(transform, "Bottom");
            hud = Hud.Create(top, Ui, tutorial != null ? "튜토리얼" : StageTitle.For(stageIndex, stage), OnBack, OpenSettings);
            if (tutorial != null)
            {
                bubble = SpeechBubble.Create(transform, Ui);
            }
            palette = PaletteBar.Create(bottom, Ui, stage, OnSelectColor);
            Layout();

            hud.SetLives(session.Lives, false);
            palette.Refresh(session);
            palette.Select(FirstColorToPaint(), true);

            busy = true;
            float intro = board.PlayIntro(session);
            Tween.Delay(this, intro * 0.7f, () =>
            {
                busy = false;
                tutorial?.Begin(this, bubble);
            });
            Settings.Changed += OnSettingsChanged;
        }

        protected override void OnDestroy()
        {
            Settings.Changed -= OnSettingsChanged;
            base.OnDestroy();
        }

        public override void OnBack()
        {
            if (tutorial != null && !session.IsCleared)
            {
                // 튜토리얼을 나가도 완료로 치지 않는다. 끝까지 마쳐야 다음 시작하기부터 스테이지로 들어간다.
                App.ShowTitle();
                return;
            }

            if (session.IsCleared)
            {
                App.ShowSelect();
                return;
            }

            if (modal != null)
            {
                if (modalClosable)
                {
                    CloseModal();
                }

                return;
            }

            bool played = session.RevealedCount > initialRevealed || session.Mistakes > 0;
            if (!played)
            {
                App.ShowSelect();
                return;
            }

            ConfirmLeave();
        }

        /// <summary>판 도중에는 저장하지 않으므로, 나가기 전에 진행 상황이 사라진다는 것을 알린다.</summary>
        void ConfirmLeave()
        {
            modal = UiKit.Modal.Open(transform, Ui, new Vector2(7.6f, 6.2f), ModalOrder);
            modalClosable = true;
            Transform card = modal.Card;
            int order = ModalOrder + 10;
            Label.Create(card, "Title", "그만할까요?", Theme.Ink, order, new Vector2(0f, 2.1f), 0.72f, TextAnchor.MiddleCenter, true);
            Label.Create(card, "Body", "지금 나가면 이 그림의 진행 상황은\n저장되지 않아요. 다음에 처음부터 풀어요.", Theme.SubInk,
                order, new Vector2(0f, 0.85f), 0.4f);
            UiKit.Button(card, "Stay", "계속하기", null, Theme.Accent, Color.white, new Vector2(6f, 1.3f), new Vector2(0f, -0.75f),
                order, CloseModal);
            UiKit.Button(card, "Leave", "나가기", null, Theme.HiddenTile, Theme.Ink, new Vector2(6f, 1.1f),
                new Vector2(0f, -2.15f), order, () => App.ShowSelect());
        }

        void OnSettingsChanged()
        {
            board.RefreshClueDigits();
        }

        void OpenSettings()
        {
            if (modal != null || session.IsCleared || session.IsGameOver)
            {
                return;
            }

            modal = SettingsPanel.Open(transform, Ui, ModalOrder, () => modal = null);
            modalClosable = true;
        }

        /// <summary>남은 칸이 있는 색 중 배경이 아닌 첫 색. 그림 색부터 칠하는 게 더 재미있다.</summary>
        int FirstColorToPaint()
        {
            for (int k = 0; k < stage.ColorCount; k++)
            {
                if (k != stage.BackgroundColor && session.Remaining(k) > 0)
                {
                    return k;
                }
            }

            return session.Remaining(stage.BackgroundColor) > 0 ? stage.BackgroundColor : -1;
        }

        public override void Layout()
        {
            Rect safe = Ui.Safe;
            top.localPosition = new Vector3(safe.center.x, safe.yMax - Hud.BarHeight / 2f, 0f);
            bottom.localPosition = new Vector3(safe.center.x, safe.yMin + PaletteBar.BarHeight / 2f, 0f);
            App.BoardCamera.Frame(board.Bounds, Viewport(session.IsCleared), false);
        }

        /// <summary>
        /// 판을 보여 줄 화면 영역(픽셀). 완성 뒤에는 팔레트 자리에 결과 카드가 올라오므로 그 위쪽만 쓴다.
        /// </summary>
        Rect Viewport(bool cleared)
        {
            Rect safe = Ui.Safe;
            float bottomInset = tutorial != null ? SpeechBubble.DockHeight + 0.3f : 0f;
            float yMin = Ui.ToPixelY(safe.yMin + (cleared ? ResultCardTop + 0.3f : PaletteBar.BarHeight + bottomInset));
            float yMax = Ui.ToPixelY(safe.yMax - Hud.BarHeight);
            float xMin = Ui.ToPixelX(safe.xMin);
            float xMax = Ui.ToPixelX(safe.xMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        void OnSelectColor(int color)
        {
            board.SetFocus(color);
            tutorial?.OnColorSelected(color);
        }

        public override void OnTap(Vector2 screen)
        {
            if (busy || modal != null || session.IsCleared || session.IsGameOver)
            {
                return;
            }

            int cell = board.CellAt(App.BoardCamera.ScreenToWorld(screen));
            int color = palette.Selected;
            if (cell < 0 || color < 0 || session.IsRevealed(cell))
            {
                return;
            }

            if (tutorial != null && !tutorial.AllowPaint(cell, color))
            {
                return;
            }

            PaintResult result = session.Paint(cell, color);
            if (result.Outcome == PaintOutcome.Correct)
            {
                Haptics.Light();
                float duration = board.PlayReveal(session, result.Revealed);
                palette.Refresh(session);
                tutorial?.OnPainted(cell, color);
                if (result.Cleared)
                {
                    busy = true;
                    Tween.Delay(this, duration, OnCleared);
                }
                else if (session.Remaining(color) == 0)
                {
                    palette.SelectNextAvailable();
                }
            }
            else if (result.Outcome == PaintOutcome.Wrong)
            {
                board.PlayWrong(cell, Theme.ToColor(stage.Colors[color].Color));
                Sfx.Instance?.Wrong();
                Haptics.Heavy();
                App.BoardCamera.Shake(0.12f);
                hud.SetLives(session.Lives, true);
                Tween.Delay(this, 0.18f, () => Sfx.Instance?.LoseHeart());
                if (result.GameOver && tutorial != null)
                {
                    // 튜토리얼에서는 목숨을 다 잃어도 멈추지 않는다.
                    session.Revive();
                    hud.SetLives(session.Lives, true);
                }
                else if (result.GameOver)
                {
                    busy = true;
                    Tween.Delay(this, 0.7f, ShowGameOver);
                }
            }
        }

        public override void OnDrag(Vector2 screenDelta)
        {
            App.BoardCamera.Pan(screenDelta);
        }

        public override void OnZoom(Vector2 screenCenter, float ratio)
        {
            App.BoardCamera.Zoom(screenCenter, ratio);
        }

        void OnCleared()
        {
            if (stageIndex >= 0)
            {
                Progress.Record(stage.Id, session.Stars);
                Ads.OnStageCompleted();
            }

            hud.RevealTitle(StageTitle.Revealed(stageIndex, stage));
            board.SetFocus(-1);
            palette.Hide();
            App.BoardCamera.Frame(board.Bounds, Viewport(true), true);
            board.PlayClear(ShowClear);
        }

        void ShowClear()
        {
            if (tutorial != null)
            {
                ShowTutorialClear();
                return;
            }

            modal = UiKit.Modal.Open(transform, Ui, ResultCardSize, ModalOrder, false);
            Transform card = modal.Card;
            int order = ModalOrder + 10;
            modal.Card.localPosition = new Vector3(Ui.Safe.center.x, Ui.Safe.yMin + ResultCardTop - ResultCardSize.y / 2f, 0f);

            Label.Create(card, "Title", stage.Name + " 완성!", Theme.Ink, order, new Vector2(0f, 2.6f), 0.8f,
                TextAnchor.MiddleCenter, true);

            Sprite star = Icons.Star;
            int stars = session.Stars;
            for (int i = 0; i < 3; i++)
            {
                bool earned = i < stars;
                SpriteRenderer s = Draw.Sprite(card, "Star" + i, star, earned ? Theme.Gold : Theme.Locked, order,
                    new Vector2((i - 1) * 1.35f, 1.1f + (i == 1 ? 0.25f : 0f)), Vector2.zero);
                float size = i == 1 ? 1.2f : 1f;
                int index = i;
                Tween.Run(s, 0.45f, t => s.transform.localScale = new Vector3(size * t, size * t, 1f), Ease.OutBack,
                    0.25f + i * 0.15f);
                if (earned)
                {
                    Tween.Delay(s, 0.25f + i * 0.15f, () => Sfx.Instance?.Star(index));
                }
            }

            string mistakes = session.Mistakes == 0 ? "실수 없이 풀었어요" : "실수 " + session.Mistakes + "번";
            Label.Create(card, "Mistakes", mistakes, Theme.SubInk, order, new Vector2(0f, -0.2f), 0.45f);

            bool hasNext = stageIndex >= 0 && stageIndex + 1 < StageCatalog.All.Count;
            if (hasNext)
            {
                UiKit.Button(card, "Next", "다음 그림", Icons.Play, Theme.Accent,
                    Color.white, new Vector2(5.6f, 1.35f), new Vector2(0f, -1.6f), order, () => Ads.AfterStage(() => App.ShowPlay(stageIndex + 1)));
            }

            UiKit.Button(card, "List", "목록", null, Theme.HiddenTile, Theme.Ink, new Vector2(5.6f, 1.1f),
                new Vector2(0f, hasNext ? -3.0f : -1.6f), order, () => Ads.AfterStage(App.ShowSelect));
        }

        /// <summary>튜토리얼을 끝내면 이름 공개를 한 번 더 짚어 주고 메인 화면으로 보낸다.</summary>
        void ShowTutorialClear()
        {
            tutorial.OnCleared();
            modal = UiKit.Modal.Open(transform, Ui, ResultCardSize, ModalOrder, false);
            modalClosable = false;
            Transform card = modal.Card;
            int order = ModalOrder + 10;
            modal.Card.localPosition = new Vector3(Ui.Safe.center.x, Ui.Safe.yMin + ResultCardTop - ResultCardSize.y / 2f, 0f);
            Label.Create(card, "Title", "튜토리얼 완료!", Theme.Ink, order, new Vector2(0f, 2.5f), 0.8f, TextAnchor.MiddleCenter, true);
            Label.Create(card, "Body", "그림을 완성하면 이름이 공개돼요.\n색을 고르고, 숫자를 보고, 칠하면 끝!", Theme.SubInk, order,
                new Vector2(0f, 0.6f), 0.42f);
            UiKit.Button(card, "Start", "시작하기", Icons.Play, Theme.Accent, Color.white,
                new Vector2(5.6f, 1.35f), new Vector2(0f, -2.2f), order, () => App.ShowPlay(0));
        }

        void ShowGameOver()
        {
            modal = UiKit.Modal.Open(transform, Ui, new Vector2(7.6f, 7f), ModalOrder);
            modalClosable = false;
            Transform card = modal.Card;
            int order = ModalOrder + 10;

            Draw.Sprite(card, "Heart", Icons.Heart, Theme.Locked, order,
                new Vector2(0f, 2.3f), new Vector2(1.1f, 1.1f));
            Label.Create(card, "Title", "목숨을 다 썼어요", Theme.Ink, order, new Vector2(0f, 1.1f), 0.72f,
                TextAnchor.MiddleCenter, true);
            Label.Create(card, "Body", "광고를 보면 목숨 " + PuzzleSession.ReviveLives + "개로 이어서 할 수 있어요",
                Theme.SubInk, order, new Vector2(0f, 0.2f), 0.4f);

            UiButton revive = null;
            revive = UiKit.Button(card, "Revive", "광고 보고 이어 하기", Icons.Ad,
                Theme.Accent, Color.white, new Vector2(6f, 1.35f), new Vector2(0f, -1.2f), order, () =>
                {
                    revive.Interactable = false;
                    Ads.ShowRewarded(rewarded =>
                    {
                        if (this == null)
                        {
                            return;
                        }

                        if (!rewarded)
                        {
                            revive.Interactable = true;
                            return;
                        }

                        session.Revive();
                        Sfx.Instance?.Revive();
                        hud.SetLives(session.Lives, true);
                        CloseModal();
                    });
                });
            revive.Interactable = Ads.Rewarded.IsReady;

            UiKit.Button(card, "Retry", "처음부터", Icons.Retry, Theme.HiddenTile,
                Theme.Ink, new Vector2(6f, 1.1f), new Vector2(0f, -2.6f), order, () => App.ShowPlay(stage, stageIndex));
        }

        void CloseModal()
        {
            UiKit.Modal closing = modal;
            modal = null;
            closing.Close(() => busy = false);
        }
    }
}
