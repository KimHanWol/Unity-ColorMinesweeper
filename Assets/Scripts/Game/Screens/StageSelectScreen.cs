using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 스테이지 목록. 깬 스테이지는 완성된 도트 그림을, 아직 못 깬 스테이지는 가려진 판을 보여 준다.
    /// 앞 스테이지를 깨야 다음이 열린다.
    /// </summary>
    public sealed class StageSelectScreen : ScreenBase
    {
        const float HeaderHeight = 2.3f;
        const int Columns = 3;
        static readonly Vector2 CardSize = new Vector2(2.7f, 3.3f);
        const float Gap = 0.35f;

        Transform header;
        Transform content;

        /// <summary>
        /// 만든 카드(아직 안 만든 칸은 null). 300장을 한꺼번에 만들면 화면을 열 때 버벅여서, 보이는 줄 근처만 그때그때 만들고
        /// 멀어진 카드는 꺼 둔다(그리기와 버튼 검사에서도 빠진다).
        /// </summary>
        Transform[] cards;

        /// <summary>카드를 만들 때 튀어나오는 등장 연출을 할지. 화면을 처음 열 때 보이는 카드만 한다.</summary>
        bool animateEntrance;

        /// <summary>폭이 좁은 화면(20:9 등)에서 3열이 잘리지 않게 목록 전체를 줄이는 비율.</summary>
        float gridScale = 1f;

        /// <summary>보이는 줄 위아래로 미리 만들어 두는 줄 수.</summary>
        const int SpareRows = 2;
        float scroll;
        float maxScroll;
        float velocity;
        float lastDragTime;

        /// <summary>관성 스크롤의 감속. 클수록 빨리 멈춘다.</summary>
        const float ScrollFriction = 3.2f;
        int titleTaps;

        /// <summary>목록을 마지막으로 봤을 때 열려 있던 스테이지 수. 그보다 뒤에 새로 열린 카드는 자물쇠가 풀리는 연출을 한다.</summary>
        const string SeenUnlockedKey = "ui.seenUnlocked";
        int seenUnlocked;
        int newlyUnlockedShown;
        float lastTitleTap;

        protected override void Build()
        {
            header = Draw.Node(transform, "Header");
            SpriteRenderer strip = Draw.Sprite(header, "Strip", SpriteFactory.Square(), Theme.BackgroundTop, 150,
                new Vector2(0f, 1f), new Vector2(Ui.Width + 2f, HeaderHeight + 2f));
            UiButton.Attach(strip.transform, Vector2.one, 150, null).Pressable = false;
            // 뒤로 · 제목 · 설정을 한 줄에 둔다(게임 이름과 부제는 메인 화면에만 둔다).
            // 둥근 버튼은 아래 그림자까지 한 덩어리로 보여서, 제목은 버튼 면보다 그림자 절반만큼 내려 둔다.
            Label.Create(header, "Title", Loc.T("select.title"), Theme.Ink, 151, new Vector2(0f, 0.14f), 0.7f,
                TextAnchor.MiddleCenter, true);
            UiKit.IconButton(header, "Back", Icons.Back,
                new Vector2(-Ui.Safe.width / 2f + 1.05f, 0.2f), 152, OnBack);
            UiKit.IconButton(header, "Settings", Icons.Settings,
                new Vector2(Ui.Safe.width / 2f - 1.05f, 0.2f), 152, () => SettingsPanel.Open(transform, Ui, 300, null, true));

            // 개발용: 제목을 7번 연속 탭하면 모든 스테이지 잠금 해제를 켜고 끈다(에디터·Development Build 에서만).
            UiButton.Attach(Draw.Node(header, "TitleCheat", new Vector2(0f, 0.2f)), new Vector2(4.5f, 1.1f), 151,
                OnTitleTapped).Pressable = false;
            if (Cheats.UnlockAll)
            {
                Draw.Panel(header, "DevBadge", new Vector2(2.6f, 0.5f), Theme.Danger, 151, new Vector2(0f, -0.75f), 0.25f);
                Label.Create(header, "DevText", Loc.T("select.devUnlocked"), Color.white, 152, new Vector2(0f, -0.75f), 0.28f,
                    TextAnchor.MiddleCenter, true);
            }

            content = Draw.Node(transform, "Content");
            int unlockedNow = 0;
            for (int i = 0; i < StageCatalog.All.Count; i++)
            {
                if (StageCatalog.IsUnlocked(i))
                {
                    unlockedNow++;
                }
            }

            // 처음 보는 거라면 연출 없이 지금 상태를 기준으로 삼는다.
            seenUnlocked = PlayerPrefs.GetInt(SeenUnlockedKey, unlockedNow);
            if (unlockedNow - seenUnlocked > 3)
            {
                // 치트로 한꺼번에 열렸을 때처럼 너무 많으면 연출하지 않는다(소리가 겹쳐 시끄럽다).
                seenUnlocked = unlockedNow;
            }
            cards = new Transform[StageCatalog.All.Count];
            PlayerPrefs.SetInt(SeenUnlockedKey, unlockedNow);
            PlayerPrefs.Save();

            animateEntrance = true;
            Layout();
            animateEntrance = false;
        }

        /// <summary>
        /// 새로 열린 카드: 잠긴 모습(자물쇠)을 덮어 두었다가 자물쇠가 튀어 오르며 사라지고 철컥 소리가 난다.
        /// </summary>
        void PlayUnlock(Transform visual, Vector2 pictureSize, Vector2 pictureCenter, int order)
        {
            Transform cover = Draw.Node(visual, "UnlockCover", pictureCenter);
            SpriteRenderer back = Draw.Panel(cover, "Back", pictureSize, Theme.Locked, 14, Vector2.zero, 0.3f);
            SpriteRenderer lockIcon = Draw.Sprite(cover, "Lock", Icons.Lock, Color.white, 15, Vector2.zero, new Vector2(0.8f, 0.8f));
            float delay = 0.6f + order * 0.35f;
            Tween.Delay(cover, delay, () =>
            {
                Sfx.Instance?.Unlock();
                Transform icon = lockIcon.transform;
                Tween.Run(cover, 0.5f, t =>
                {
                    icon.localPosition = new Vector3(0f, t * 0.9f, 0f);
                    icon.localRotation = Quaternion.Euler(0f, 0f, t * 25f);
                    Draw.SetAlpha(lockIcon, 1f - t);
                    Draw.SetAlpha(back, 1f - t);
                }, Ease.InCubic, 0f, () => Destroy(cover.gameObject));
            });
        }

        public override void OnBack()
        {
            App.ShowTitle();
        }

        void OnTitleTapped()
        {
            if (!Cheats.Available)
            {
                return;
            }

            if (Time.unscaledTime - lastTitleTap > 1.5f)
            {
                titleTaps = 0;
            }

            titleTaps++;
            lastTitleTap = Time.unscaledTime;
            if (titleTaps >= 7)
            {
                Cheats.UnlockAll = !Cheats.UnlockAll;
                App.ShowSelect();
            }
        }

        public override void Layout()
        {
            Rect safe = Ui.Safe;
            header.localPosition = new Vector3(safe.center.x, safe.yMax - HeaderHeight / 2f, 0f);
            int rows = (StageCatalog.All.Count + Columns - 1) / Columns;
            float gridWidth = Columns * CardSize.x + (Columns - 1) * Gap;
            gridScale = Mathf.Min(1f, (safe.width - 0.6f) / gridWidth);
            content.localScale = new Vector3(gridScale, gridScale, 1f);
            float contentHeight = rows * (CardSize.y + Gap) * gridScale;
            float visible = safe.height - HeaderHeight;
            maxScroll = Mathf.Max(0f, contentHeight - visible + 0.8f);
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            content.localPosition = new Vector3(safe.center.x, safe.yMax - HeaderHeight - 0.3f + scroll, 0f);
            ShowCardsNear(visible);
        }

        /// <summary>지금 보이는 줄과 그 위아래 몇 줄의 카드를 만들어 켜고, 멀리 있는 카드는 끈다.</summary>
        void ShowCardsNear(float visibleHeight)
        {
            if (cards == null)
            {
                return;
            }

            float rowPitch = (CardSize.y + Gap) * gridScale;
            int firstRow = Mathf.FloorToInt(scroll / rowPitch) - SpareRows;
            int lastRow = Mathf.CeilToInt((scroll + visibleHeight) / rowPitch) + SpareRows;
            for (int i = 0; i < cards.Length; i++)
            {
                int row = i / Columns;
                bool near = row >= firstRow && row <= lastRow;
                if (near && cards[i] == null)
                {
                    cards[i] = CreateCard(i);
                }

                if (cards[i] != null && cards[i].gameObject.activeSelf != near)
                {
                    cards[i].gameObject.SetActive(near);
                }
            }
        }

        public override void OnDrag(Vector2 screenDelta)
        {
            float delta = screenDelta.y * UiRoot.Height / ScreenInfo.Height;
            ScrollBy(delta);

            // 손을 뗄 때의 속도로 계속 미끄러지게, 최근 끌기 속도를 부드럽게 따라간다.
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.001f);
            velocity = Mathf.Lerp(velocity, delta / dt, 0.4f);
            lastDragTime = Time.unscaledTime;
        }

        public override void OnZoom(Vector2 screenCenter, float ratio)
        {
            // 마우스 휠은 스크롤로 쓴다.
            velocity = 0f;
            ScrollBy((ratio - 1f) * -600f * UiRoot.Height / ScreenInfo.Height);
        }

        void ScrollBy(float delta)
        {
            scroll = Mathf.Clamp(scroll + delta, 0f, maxScroll);
            Layout();
        }

        /// <summary>관성 스크롤. 손을 떼면 마찰로 서서히 멈추고, 누르고 있으면(멈춰 잡으면) 바로 선다.</summary>
        void Update()
        {
            bool held = Input.touchCount > 0 || Input.GetMouseButton(0);
            if (held)
            {
                if (Time.unscaledTime - lastDragTime > 0.06f)
                {
                    velocity = 0f;
                }

                return;
            }

            if (Mathf.Abs(velocity) < 0.05f)
            {
                velocity = 0f;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            ScrollBy(velocity * dt);
            velocity *= Mathf.Exp(-ScrollFriction * dt);
            if (scroll <= 0f || scroll >= maxScroll)
            {
                velocity = 0f;
            }
        }

        Transform CreateCard(int index)
        {
            Stage stage = StageCatalog.All[index];
            bool unlocked = StageCatalog.IsUnlocked(index);
            int stars = Progress.Stars(stage.Id);
            int column = index % Columns;
            int row = index / Columns;
            var position = new Vector2(
                (column - (Columns - 1) / 2f) * (CardSize.x + Gap),
                -CardSize.y / 2f - row * (CardSize.y + Gap));

            Transform root = Draw.Node(content, "Card" + index, position);
            Transform visual = Draw.Node(root, "Visual");
            Draw.Panel(visual, "Under", CardSize, Theme.Locked, 10, new Vector2(0f, -0.12f), 0.45f);
            Draw.Panel(visual, "Face", CardSize, Theme.Card, 11, Vector2.zero, 0.45f);

            var pictureSize = new Vector2(CardSize.x - 0.5f, CardSize.x - 0.5f);
            var pictureCenter = new Vector2(0f, 0.3f);
            var infoLine = new Vector2(0f, -1.25f);
            if (stars > 0)
            {
                Draw.Panel(visual, "PictureBack", pictureSize, Theme.ToColor(stage.Colors[stage.BackgroundColor].Color), 12,
                    pictureCenter, 0.3f);
                float size = pictureSize.x * 0.84f;
                Draw.Sprite(visual, "Picture", SpriteFactory.StagePicture(stage), Color.white, 13, pictureCenter,
                    new Vector2(size, size));
                // 그림 위에 번호를 얹으면 그림 색에 묻혀서, 아래 줄에 번호(왼쪽)와 별(오른쪽)을 나란히 둔다.
                PixelText.Create(visual, "Number", (index + 1).ToString(), Theme.Ink, 14,
                    infoLine + new Vector2(-0.8f, 0f), 0.3f);
                Sprite star = Icons.Star;
                for (int i = 0; i < 3; i++)
                {
                    Draw.Sprite(visual, "Star" + i, star, i < stars ? Theme.Gold : Theme.Locked, 12,
                        infoLine + new Vector2(0.35f + (i - 1) * 0.44f, 0f), new Vector2(0.42f, 0.42f));
                }
            }
            else if (unlocked)
            {
                Draw.Panel(visual, "PictureBack", pictureSize, Theme.HiddenTile, 12, pictureCenter, 0.3f);
                PixelText.Create(visual, "Number", (index + 1).ToString(), Color.white, 13, pictureCenter, 0.7f);
                PixelText.Create(visual, "Size", stage.Width + "x" + stage.Height, Theme.SubInk, 12, infoLine, 0.26f);
            }
            else
            {
                Draw.Panel(visual, "PictureBack", pictureSize, Theme.Locked, 12, pictureCenter, 0.3f);
                Draw.Sprite(visual, "Lock", Icons.Lock, Color.white, 13, pictureCenter,
                    new Vector2(0.7f, 0.7f));
            }
            if (unlocked && stars == 0 && index >= seenUnlocked)
            {
                PlayUnlock(visual, pictureSize, pictureCenter, newlyUnlockedShown++);
            }

            UiButton button = UiButton.Attach(root, CardSize, 20, () =>
            {
                if (unlocked)
                {
                    Sfx.Instance?.Tap();
                    App.ShowPlay(index);
                }
                else
                {
                    Sfx.Instance?.Locked();
                    Tween.Kill(visual);
                    Tween.Run(visual, 0.4f, t => visual.localPosition = new Vector3(Mathf.Sin(t * Mathf.PI * 5f) * 0.12f * (1f - t), 0f, 0f),
                        Ease.Linear);
                }
            }, visual);
            button.ReleaseOnDrag = true;
            button.Silent = true;

            // 화면을 처음 열 때 보이는 카드만 차례로 튀어나온다. 스크롤하며 새로 만드는 카드는 바로 보인다.
            if (animateEntrance)
            {
                visual.localScale = Vector3.zero;
                Tween.Run(visual, 0.4f, t => visual.localScale = Vector3.one * t, Ease.OutBack, 0.05f + Mathf.Min(index, 15) * 0.04f);
            }

            button.SetRestScale(Vector3.one);
            return root;
        }
    }
}
