using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 한 칸. 닫혀 있을 때는 도톰한 중성색 타일, 열리면 정답 색 타일 위에 주변 색별 개수 칩을 올린다.
    /// 칩은 그 색 이웃이 다 열리면 흐려지고(기본), 설정에 따라 전체 개수 대신 남은 개수를 보여 준다.
    /// </summary>
    public sealed class CellView : MonoBehaviour
    {
        public const float TileSize = 0.9f;
        const float DoneAlpha = 0.25f;
        const float DoneScale = 0.82f;

        sealed class Chip
        {
            public int Color;
            public int Total;
            public int Remaining;
            public Transform Root;
            public SpriteRenderer Ring;
            public SpriteRenderer Fill;
            public PixelText Digit;
            public Vector3 RestScale;

            public bool Done => Remaining == 0;
        }

        readonly List<Chip> chips = new List<Chip>();
        Stage stage;
        int cell;
        SpriteRenderer tile;
        Transform chipRoot;
        Transform body;
        bool revealed;
        bool chipsShown;
        int focusColor = -1;

        public bool Revealed => revealed;

        public static CellView Create(Transform parent, Stage stage, int cell, Vector2 position)
        {
            Transform t = Draw.Node(parent, "Cell" + cell, position);
            var view = t.gameObject.AddComponent<CellView>();
            view.stage = stage;
            view.cell = cell;
            view.body = Draw.Node(t, "Body");
            view.tile = Draw.Sprite(view.body, "Tile", SpriteFactory.RaisedTile(), Theme.HiddenTile, 10, Vector2.zero,
                new Vector2(TileSize, TileSize));
            view.chipRoot = Draw.Node(view.body, "Chips");
            view.BuildChips();
            view.chipRoot.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// 주변 8칸의 색별 개수를 색마다 칩 하나로 만들어 가운데에 겹쳐 둔다(배경색 포함).
        /// 화면에는 팔레트에서 고른 색의 칩 하나만 크게 보인다. 한 칸에 여러 색 숫자가 몰리면 읽기 어렵기 때문이다.
        /// </summary>
        void BuildChips()
        {
            const float diameter = 0.6f;
            for (int k = 0; k < stage.ColorCount; k++)
            {
                int total = stage.Clue(cell, k);
                if (total == 0)
                {
                    continue;
                }

                int color = k;
                Rgb rgb = stage.Colors[color].Color;
                Transform root = Draw.Node(chipRoot, "Chip" + color, Vector2.zero);
                root.localScale = new Vector3(diameter, diameter, 1f);
                chips.Add(new Chip
                {
                    Color = color,
                    Total = total,
                    Remaining = total,
                    Root = root,
                    Ring = Draw.Sprite(root, "Ring", SpriteFactory.Circle(), new Color(1f, 1f, 1f, 0.95f), 20,
                        Vector2.zero, new Vector2(1.16f, 1.16f)),
                    Fill = Draw.Sprite(root, "Fill", SpriteFactory.Circle(), Theme.ToColor(rgb), 21),
                    Digit = PixelText.Create(root, "Digit", total.ToString(), Theme.InkOn(rgb), 22, Vector2.zero, 0.5f),
                    RestScale = root.localScale,
                });
            }
        }

        /// <summary>고른 색이 아니면 0(숨김). 고른 색이면 다 채워졌을 때 조금 작게.</summary>
        float TargetScale(Chip chip)
        {
            if (chip.Color != focusColor)
            {
                return 0f;
            }

            return chip.Done ? DoneScale : 1f;
        }

        float TargetAlpha(Chip chip)
        {
            return chip.Done ? DoneAlpha : 1f;
        }

        /// <summary>주변 칸이 열린 만큼 칩의 남은 개수를 갱신한다. animate 면 바뀐 칩이 부드럽게 흐려진다.</summary>
        public void UpdateRemaining(PuzzleSession session, bool animate)
        {
            foreach (Chip chip in chips)
            {
                int remaining = chip.Total;
                foreach (int n in stage.Neighbors(cell))
                {
                    if (session.IsRevealed(n) && stage.ColorAt(n) == chip.Color)
                    {
                        remaining--;
                    }
                }

                if (remaining == chip.Remaining)
                {
                    continue;
                }

                chip.Remaining = remaining;
                if (chipsShown)
                {
                    ApplyChip(chip, animate);
                }
            }
        }

        void ApplyChip(Chip chip, bool animate)
        {
            float alpha = TargetAlpha(chip);
            Vector3 to = chip.RestScale * TargetScale(chip);
            if (!animate)
            {
                Tween.Kill(chip.Root);
                SetChipAlpha(chip, alpha);
                chip.Root.localScale = to;
                return;
            }

            Tween.Kill(chip.Root);
            Vector3 from = chip.Root.localScale;
            float fromAlpha = chip.Fill.color.a;
            Tween.Run(chip.Root, 0.3f, t =>
            {
                chip.Root.localScale = Vector3.LerpUnclamped(from, to, t);
                SetChipAlpha(chip, Mathf.Lerp(fromAlpha, alpha, Mathf.Clamp01(t)));
            }, to == Vector3.zero ? Ease.InBack : Ease.OutBack);
        }

        static void SetChipAlpha(Chip chip, float alpha)
        {
            Draw.SetAlpha(chip.Fill, alpha);
            Draw.SetAlpha(chip.Ring, alpha * 0.92f);
            chip.Digit.SetAlpha(alpha);
        }

        public void ShowRevealed()
        {
            revealed = true;
            chipsShown = true;
            tile.sprite = SpriteFactory.RoundedRect(0.2f);
            tile.color = Theme.ToColor(stage.Colors[stage.ColorAt(cell)].Color);
            body.localScale = Vector3.one;
            chipRoot.gameObject.SetActive(true);
            foreach (Chip chip in chips)
            {
                ApplyChip(chip, false);
            }
        }

        /// <summary>delay 뒤에 색이 번지며 톡 튀어 열리고, 조금 늦게 칩이 올라온다.</summary>
        public void AnimateReveal(float delay)
        {
            revealed = true;
            Tween.Kill(this);
            Color from = tile.color;
            Color to = Theme.ToColor(stage.Colors[stage.ColorAt(cell)].Color);
            foreach (Chip chip in chips)
            {
                chip.Root.localScale = Vector3.zero;
            }

            Tween.Delay(this, delay, () =>
            {
                tile.sprite = SpriteFactory.RoundedRect(0.2f);
                Tween.Run(this, 0.22f, t => tile.color = Color.Lerp(from, to, t));
                Tween.Run(this, 0.42f, t => body.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, t),
                    Ease.OutElastic);
                chipRoot.gameObject.SetActive(true);
                for (int i = 0; i < chips.Count; i++)
                {
                    Chip chip = chips[i];
                    SetChipAlpha(chip, TargetAlpha(chip));
                    Tween.Run(chip.Root, 0.3f, t => chip.Root.localScale = chip.RestScale * (TargetScale(chip) * t),
                        Ease.OutBack, 0.1f + i * 0.04f, () => chipsShown = true);
                }

                if (chips.Count == 0)
                {
                    chipsShown = true;
                }
            });
        }

        /// <summary>고른 색이 칸 위에서 튀었다 사라지고, 칸은 붉게 번쩍이며 좌우로 흔들린다.</summary>
        public void AnimateWrong(Color chosen)
        {
            Tween.Kill(this);
            body.localPosition = Vector3.zero;
            SpriteRenderer splat = Draw.Sprite(transform, "Wrong", SpriteFactory.Circle(), chosen, 40, Vector2.zero,
                new Vector2(0.2f, 0.2f));
            Tween.Run(splat, 0.45f, t =>
            {
                float s = Mathf.Lerp(0.2f, 0.85f, Ease.OutBack(Mathf.Min(1f, t * 1.8f)));
                splat.transform.localScale = new Vector3(s, s, 1f);
                Draw.SetAlpha(splat, t < 0.55f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.55f) / 0.45f));
            }, Ease.Linear, 0f, () => Destroy(splat.gameObject));

            Color hidden = Theme.HiddenTile;
            Tween.Run(this, 0.45f, t =>
            {
                body.localPosition = new Vector3(Mathf.Sin(t * Mathf.PI * 6f) * 0.12f * (1f - t), 0f, 0f);
                tile.color = Color.Lerp(hidden, Theme.Danger, Ease.Pulse(t) * 0.7f);
            }, Ease.Linear, 0f, () =>
            {
                body.localPosition = Vector3.zero;
                tile.color = hidden;
            });
        }

        /// <summary>고른 색의 칩만 톡 튀어나오고 나머지는 들어간다. -1 이면 모두 숨긴다.</summary>
        public void SetFocus(int color, bool animate = true)
        {
            if (color == focusColor)
            {
                return;
            }

            focusColor = color;
            if (!chipsShown)
            {
                return;
            }

            foreach (Chip chip in chips)
            {
                ApplyChip(chip, animate);
            }
        }

        /// <summary>완성 연출 1단계: 칩이 쏙 들어간다.</summary>
        public void HideChips(float delay)
        {
            chipsShown = false;
            foreach (Chip chip in chips)
            {
                Chip c = chip;
                Tween.Kill(c.Root);
                Vector3 from = c.Root.localScale;
                Tween.Run(c.Root, 0.25f, t => c.Root.localScale = Vector3.LerpUnclamped(from, Vector3.zero, t),
                    Ease.InBack, delay);
            }
        }

        /// <summary>완성 연출 3단계: 칸이 잠깐 하얗게 빛난다. 칸마다 시간차를 주면 빛이 그림을 훑고 지나간다.</summary>
        public void Shine(float delay)
        {
            SpriteRenderer glow = Draw.Sprite(transform, "Shine", SpriteFactory.Square(), new Color(1f, 1f, 1f, 0f), 12,
                Vector2.zero, new Vector2(1.005f, 1.005f));
            Tween.Run(glow, 0.32f, t => Draw.SetAlpha(glow, Ease.Pulse(t) * 0.55f), Ease.Linear, delay,
                () => Destroy(glow.gameObject));
        }

        /// <summary>완성 연출 2단계: 틈이 메워지며 한 장의 도트 그림이 된다.</summary>
        public void BecomePixel(float delay)
        {
            Tween.Delay(tile, delay, () =>
            {
                tile.sprite = SpriteFactory.Square();
                Tween.Run(tile, 0.35f, t =>
                {
                    float s = Mathf.LerpUnclamped(TileSize, 1.005f, t);
                    tile.transform.localScale = new Vector3(s, s, 1f);
                }, Ease.OutBack);
            });
        }
    }
}
