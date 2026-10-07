using System;
using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 플레이 화면 아래쪽 색 팔레트. 고른 색은 떠오르고 테두리가 생기며, 남은 칸 수를 도트 숫자로 보여 준다.
    /// 다 칠한 색은 작아지고 흐려져서 더 고를 수 없다.
    /// </summary>
    public sealed class PaletteBar : MonoBehaviour
    {
        public const float BarHeight = 3.2f;
        const float SwatchSize = 1.2f;

        sealed class Swatch
        {
            public int Color;
            public Transform Root;
            public Vector3 Base;
            public Transform Visual;
            public SpriteRenderer Ring;
            public SpriteRenderer Fill;
            public PixelText Count;
            public UiButton Button;
            public bool Done;
        }

        readonly List<Swatch> swatches = new List<Swatch>();
        Action<int> onSelect;
        int selected = -1;

        public int Selected => selected;

        /// <summary>색 color 의 스와치(튜토리얼이 짚어 줄 때 쓴다). 없으면 null.</summary>
        public Transform SwatchTransform(int color)
        {
            foreach (Swatch swatch in swatches)
            {
                if (swatch.Color == color)
                {
                    return swatch.Visual;
                }
            }

            return null;
        }

        public static PaletteBar Create(Transform parent, UiRoot ui, Stage stage, Action<int> onSelect)
        {
            Transform root = Draw.Node(parent, "Palette");
            var bar = root.gameObject.AddComponent<PaletteBar>();
            bar.onSelect = onSelect;

            float width = ui.Width + 1f;
            Draw.Sprite(root, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, 90, new Vector2(0f, 0.1f),
                new Vector2(width * 1.1f, BarHeight * 1.3f));
            Draw.Panel(root, "Panel", new Vector2(width, BarHeight + 1.5f), Color.white, 91, new Vector2(0f, -0.75f), 0.7f);

            int n = stage.ColorCount;
            int perRow = n <= 7 ? n : (n + 1) / 2;
            int rows = (n + perRow - 1) / perRow;
            float spacing = Mathf.Min(1.55f, (ui.Safe.width - 0.8f) / perRow);
            float scale = Mathf.Min(1f, spacing / 1.55f) * (rows > 1 ? 0.8f : 1f);
            for (int i = 0; i < n; i++)
            {
                int row = i / perRow;
                int inRow = Mathf.Min(perRow, n - row * perRow);
                int column = i % perRow;
                var position = new Vector2((column - (inRow - 1) / 2f) * spacing,
                    rows == 1 ? 0.1f : 0.65f - row * 1.3f);
                bar.swatches.Add(bar.CreateSwatch(root, stage, i, position, scale));
            }

            return bar;
        }

        Swatch CreateSwatch(Transform parent, Stage stage, int color, Vector2 position, float scale)
        {
            Rgb rgb = stage.Colors[color].Color;
            bool background = color == stage.BackgroundColor;
            Transform root = Draw.Node(parent, "Swatch" + color, position);
            Transform visual = Draw.Node(root, "Visual");
            visual.localScale = Vector3.one * scale;
            var swatch = new Swatch
            {
                Color = color,
                Root = root,
                Base = root.localPosition,
                Visual = visual,
                Ring = Shape(visual, "Ring", background, Theme.Accent, 92, SwatchSize + 0.24f),
                Fill = Shape(visual, "Fill", background, Theme.ToColor(rgb), 94, SwatchSize),
            };
            // 밝은 색(흰색, 배경 크림색)은 흰 바탕에 묻히므로 얇은 회색 테두리를 둔다.
            Shape(visual, "Edge", background, new Color(0f, 0f, 0f, 0.1f), 93, SwatchSize + 0.06f);
            swatch.Count = PixelText.Create(visual, "Count", "0", Theme.InkOn(rgb), 95, Vector2.zero, 0.34f);
            if (background)
            {
                // 배경색은 단서 칩에 나오지 않는다. 모양(네모)과 이름으로 다른 색과 구분한다.
                Label.Create(visual, "BackgroundTag", Loc.T("play.background"), Theme.SubInk, 95, new Vector2(0f, -SwatchSize / 2f - 0.32f),
                    0.3f, TextAnchor.MiddleCenter, true)
                .FitWidth(SwatchSize * 1.5f);
            }
            swatch.Ring.enabled = false;
            swatch.Button = UiButton.Attach(root, Vector2.one * 1.5f * scale, 96, () =>
            {
                Sfx.Instance?.Select(color);
                Select(color, true);
            }, visual);
            swatch.Button.Silent = true;
            return swatch;
        }

        /// <summary>그림 색은 동그라미, 배경색은 둥근 네모.</summary>
        static SpriteRenderer Shape(Transform parent, string name, bool square, Color color, int order, float size)
        {
            return square
                ? Draw.Panel(parent, name, Vector2.one * size, color, order, Vector2.zero, size * 0.28f)
                : Draw.Sprite(parent, name, SpriteFactory.Circle(), color, order, Vector2.zero, Vector2.one * size);
        }

        public void Refresh(PuzzleSession session)
        {
            foreach (Swatch swatch in swatches)
            {
                int remaining = session.Remaining(swatch.Color);
                swatch.Count.SetText(remaining.ToString());
                if (remaining == 0 && !swatch.Done)
                {
                    swatch.Done = true;
                    swatch.Button.Interactable = false;
                    Swatch s = swatch;
                    Vector3 from = s.Visual.localScale;
                    // 한 색을 다 칠하면 그 색 조각이 튀며 마무리된다.
                    Fx.Pop(transform, s.Root.localPosition, s.Fill.color, 97);
                    Tween.Run(s.Visual, 0.35f, t =>
                    {
                        s.Visual.localScale = Vector3.LerpUnclamped(from, from * 0.72f, t);
                        Draw.SetAlpha(s.Fill, Mathf.Lerp(1f, 0.35f, t));
                        s.Count.SetAlpha(1f - t);
                    }, Ease.OutBack);
                }
            }
        }

        /// <summary>아직 칠할 칸이 남은 색 중 지금 색 다음 것을 고른다. 다 칠했으면 -1.</summary>
        public void SelectNextAvailable()
        {
            int n = swatches.Count;
            int start = Mathf.Max(0, selected);
            for (int i = 1; i <= n; i++)
            {
                Swatch s = swatches[(start + i) % n];
                if (!s.Done)
                {
                    Select(s.Color, true);
                    return;
                }
            }

            Select(-1, true);
        }

        public void Select(int color, bool notify)
        {
            if (color == selected)
            {
                return;
            }

            foreach (Swatch swatch in swatches)
            {
                bool on = swatch.Color == color;
                bool was = swatch.Color == selected;
                if (!on && !was)
                {
                    continue;
                }

                Swatch s = swatch;
                s.Ring.enabled = on;
                Vector3 from = s.Root.localPosition;
                Vector3 to = s.Base + (on ? new Vector3(0f, 0.22f, 0f) : Vector3.zero);
                Tween.Kill(s.Root);
                Tween.Run(s.Root, 0.3f, t => s.Root.localPosition = Vector3.LerpUnclamped(from, to, t), Ease.OutBack);
            }

            selected = color;
            if (notify)
            {
                onSelect?.Invoke(color);
            }
        }

        public void Hide()
        {
            Transform t = transform;
            Vector3 from = t.localPosition;
            Tween.Run(t, 0.4f, k => t.localPosition = from + Vector3.down * (BarHeight + 1f) * k, Ease.InBack);
        }
    }
}
