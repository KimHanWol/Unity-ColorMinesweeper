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
        const float HeaderHeight = 3.4f;
        const int Columns = 3;
        static readonly Vector2 CardSize = new Vector2(2.7f, 3.8f);
        const float Gap = 0.35f;

        Transform header;
        Transform content;
        float scroll;
        float maxScroll;
        int titleTaps;
        float lastTitleTap;

        protected override void Build()
        {
            header = Draw.Node(transform, "Header");
            SpriteRenderer strip = Draw.Sprite(header, "Strip", SpriteFactory.Square(), Theme.BackgroundTop, 150,
                new Vector2(0f, 1f), new Vector2(Ui.Width + 2f, HeaderHeight + 2f));
            UiButton.Attach(strip.transform, Vector2.one, 150, null).Pressable = false;
            Label.Create(header, "Title", "Color Minesweeper", Theme.Ink, 151, new Vector2(0f, 0.45f), 0.95f,
                TextAnchor.MiddleCenter, true);
            Label.Create(header, "Subtitle", "단서를 보고 칠하면 그림이 완성돼요", Theme.SubInk, 151, new Vector2(0f, -0.55f),
                0.42f);
            UiKit.IconButton(header, "Settings", PixelGlyphs.Icon("gear", PixelGlyphs.Gear),
                new Vector2(Ui.Safe.width / 2f - 1.05f, 1.2f), 152, () => SettingsPanel.Open(transform, Ui, 300, null));

            // 개발용: 제목을 7번 연속 탭하면 모든 스테이지 잠금 해제를 켜고 끈다(에디터·Development Build 에서만).
            UiButton.Attach(Draw.Node(header, "TitleCheat", new Vector2(0f, 0.45f)), new Vector2(7f, 1.2f), 151,
                OnTitleTapped).Pressable = false;
            if (Cheats.UnlockAll)
            {
                Draw.Panel(header, "DevBadge", new Vector2(2.6f, 0.55f), Theme.Danger, 151, new Vector2(0f, -1.25f), 0.27f);
                Label.Create(header, "DevText", "DEV 전체 잠금 해제", Color.white, 152, new Vector2(0f, -1.25f), 0.3f,
                    TextAnchor.MiddleCenter, true);
            }

            content = Draw.Node(transform, "Content");
            for (int i = 0; i < StageCatalog.All.Count; i++)
            {
                CreateCard(i);
            }

            Layout();
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
            float contentHeight = rows * (CardSize.y + Gap);
            float visible = safe.height - HeaderHeight;
            maxScroll = Mathf.Max(0f, contentHeight - visible + 0.8f);
            scroll = Mathf.Clamp(scroll, 0f, maxScroll);
            content.localPosition = new Vector3(safe.center.x, safe.yMax - HeaderHeight - 0.3f + scroll, 0f);
        }

        public override void OnDrag(Vector2 screenDelta)
        {
            scroll = Mathf.Clamp(scroll + screenDelta.y * UiRoot.Height / Screen.height, 0f, maxScroll);
            Layout();
        }

        public override void OnZoom(Vector2 screenCenter, float ratio)
        {
            // 마우스 휠은 스크롤로 쓴다.
            OnDrag(new Vector2(0f, (ratio - 1f) * -600f));
        }

        /// <summary>카드 아래 "번호. 이름" 줄. 긴 이름은 카드 폭을 넘지 않게 글자를 줄인다.</summary>
        static void Caption(Transform parent, string text, Color color, Vector2 position)
        {
            float height = text.Length <= 7 ? 0.3f : text.Length <= 10 ? 0.25f : 0.21f;
            Label.Create(parent, "Caption", text, color, 12, position, height, TextAnchor.MiddleCenter, true);
        }

        void CreateCard(int index)
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
            var pictureCenter = new Vector2(0f, 0.5f);
            var infoLine = new Vector2(0f, -1.0f);
            var captionLine = new Vector2(0f, -1.47f);
            if (stars > 0)
            {
                Draw.Panel(visual, "PictureBack", pictureSize, Theme.ToColor(stage.Colors[stage.BackgroundColor].Color), 12,
                    pictureCenter, 0.3f);
                float size = pictureSize.x * 0.84f;
                Draw.Sprite(visual, "Picture", SpriteFactory.StagePicture(stage), Color.white, 13, pictureCenter,
                    new Vector2(size, size));
                Sprite star = PixelGlyphs.Icon("star", PixelGlyphs.Star);
                for (int i = 0; i < 3; i++)
                {
                    Draw.Sprite(visual, "Star" + i, star, i < stars ? Theme.Gold : Theme.Locked, 12,
                        infoLine + new Vector2((i - 1) * 0.45f, 0f), new Vector2(0.34f, 0.34f));
                }

                Caption(visual, StageTitle.Revealed(index, stage), Theme.Ink, captionLine);
            }
            else if (unlocked)
            {
                Draw.Panel(visual, "PictureBack", pictureSize, Theme.HiddenTile, 12, pictureCenter, 0.3f);
                PixelText.Create(visual, "Number", (index + 1).ToString(), Color.white, 13, pictureCenter, 0.7f);
                PixelText.Create(visual, "Size", stage.Width + "x" + stage.Height, Theme.SubInk, 12, infoLine, 0.26f);
                Caption(visual, StageTitle.Hidden(index), Theme.SubInk, captionLine);
            }
            else
            {
                Draw.Panel(visual, "PictureBack", pictureSize, Theme.Locked, 12, pictureCenter, 0.3f);
                Draw.Sprite(visual, "Lock", PixelGlyphs.Icon("lock", PixelGlyphs.Lock), Color.white, 13, pictureCenter,
                    new Vector2(0.7f, 0.7f));
                Caption(visual, StageTitle.Hidden(index), Theme.Locked, captionLine);
            }
            UiButton button = UiButton.Attach(root, CardSize, 20, () =>
            {
                if (unlocked)
                {
                    App.ShowPlay(index);
                }
                else
                {
                    Tween.Kill(visual);
                    Tween.Run(visual, 0.4f, t => visual.localPosition = new Vector3(Mathf.Sin(t * Mathf.PI * 5f) * 0.12f * (1f - t), 0f, 0f),
                        Ease.Linear);
                }
            }, visual);
            button.ReleaseOnDrag = true;

            visual.localScale = Vector3.zero;
            // 처음 보이는 몇 줄만 차례로 튀어나오게 하고, 나머지는 거의 동시에 뜬다(300개면 마지막 카드가 한참 늦게 나온다).
            Tween.Run(visual, 0.4f, t => visual.localScale = Vector3.one * t, Ease.OutBack, 0.05f + Mathf.Min(index, 15) * 0.04f);
            button.SetRestScale(Vector3.one);
        }
    }
}
