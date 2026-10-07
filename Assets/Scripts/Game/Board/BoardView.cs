using System;
using System.Collections.Generic;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>판 전체. 칸 한 칸이 1 유닛이고, 판 가운데가 원점이다.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        const float RevealStepDelay = 0.045f;
        const float PlatePadding = 0.35f;

        /// <summary>한 번에 이만큼 넘게 열리면 연 칸 수를 띄운다.</summary>
        const int CountFrom = 5;

        Stage stage;
        CellView[] cells;
        Transform plate;
        int focus = -1;

        public Rect Bounds { get; private set; }

        public static BoardView Create(Transform parent, Stage stage)
        {
            Transform t = Draw.Node(parent, "Board");
            var view = t.gameObject.AddComponent<BoardView>();
            view.stage = stage;
            view.Build();
            return view;
        }

        void Build()
        {
            float w = stage.Width;
            float h = stage.Height;
            Bounds = new Rect(-w / 2f - PlatePadding, -h / 2f - PlatePadding, w + PlatePadding * 2f, h + PlatePadding * 2f);

            Draw.Sprite(transform, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, 0, new Vector2(0f, -0.25f),
                new Vector2(Bounds.width * 1.25f, Bounds.height * 1.25f));
            plate = Draw.Panel(transform, "Plate", Bounds.size, Theme.Plate, 1, Vector2.zero, 0.4f).transform;

            cells = new CellView[stage.CellCount];
            for (int i = 0; i < stage.CellCount; i++)
            {
                cells[i] = CellView.Create(transform, stage, i, CellCenter(i));
            }
        }

        public Vector2 CellCenter(int cell)
        {
            int x = cell % stage.Width;
            int y = cell / stage.Width;
            return new Vector2(x - stage.Width / 2f + 0.5f, stage.Height / 2f - y - 0.5f);
        }

        /// <summary>월드 좌표의 칸. 판 밖이면 -1.</summary>
        public int CellAt(Vector2 world)
        {
            Vector2 local = transform.InverseTransformPoint(world);
            int x = Mathf.FloorToInt(local.x + stage.Width / 2f);
            int y = Mathf.FloorToInt(stage.Height / 2f - local.y);
            if (x < 0 || y < 0 || x >= stage.Width || y >= stage.Height)
            {
                return -1;
            }

            return y * stage.Width + x;
        }

        /// <summary>처음 들어올 때 칸들이 대각선으로 톡톡 떨어지고, 시작 칸은 이어서 펼쳐진다.</summary>
        public float PlayIntro(PuzzleSession session)
        {
            float last = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                CellView cell = cells[i];
                int x = i % stage.Width;
                int y = i / stage.Width;
                float delay = (x + y) * 0.018f;
                last = Mathf.Max(last, delay);
                cell.transform.localScale = Vector3.zero;
                Tween.Run(cell.transform, 0.35f, t => cell.transform.localScale = Vector3.one * t, Ease.OutBack, delay);
            }

            float revealStart = last + 0.3f;
            var opened = new List<RevealedCell>();
            var seen = new bool[stage.CellCount];
            foreach (int g in session.Stage.Givens)
            {
                stage.RevealWithFlood(g, seen, opened);
            }

            return revealStart + PlayReveal(session, opened, revealStart);
        }

        /// <summary>
        /// 펼쳐진 칸을 거리 순서로 열고, 새로 열린 칸 주변 단서 칩의 남은 개수를 갱신한다.
        /// 연출이 끝나는 데 걸리는 시간을 돌려준다.
        /// </summary>
        public float PlayReveal(PuzzleSession session, List<RevealedCell> opened, float startDelay = 0f)
        {
            int maxDepth = 0;
            var affected = new HashSet<int>();
            foreach (RevealedCell r in opened)
            {
                cells[r.Cell].SetFocus(focus, false);
                cells[r.Cell].UpdateRemaining(session, false);
                cells[r.Cell].AnimateReveal(startDelay + r.Depth * RevealStepDelay);
                maxDepth = Mathf.Max(maxDepth, r.Depth);
                // 직접 누른 칸에서는 그 색 조각이 튀고 테두리가 퍼진다(처음 판이 열릴 때는 조용히 둔다).
                if (r.Depth == 0 && startDelay <= 0f)
                {
                    Rgb rgb = stage.Colors[stage.ColorAt(r.Cell)].Color;
                    Fx.Pop(transform, CellCenter(r.Cell), Theme.ToColor(rgb), 30);
                    if (opened.Count >= CountFrom)
                    {
                        Fx.Count(transform, CellCenter(r.Cell) + Vector2.up * 0.55f, opened.Count, Theme.ToColor(rgb),
                            Theme.InkOn(rgb), 40);
                    }
                }
                foreach (int n in stage.Neighbors(r.Cell))
                {
                    affected.Add(n);
                }
            }

            foreach (int n in affected)
            {
                if (session.IsRevealed(n))
                {
                    cells[n].UpdateRemaining(session, true);
                }
            }

            // 넣어 둔 소리 파일이 있으면 빈 구역이 펼쳐질 때 그 소리를 한 번 튼다.
            if (maxDepth > 0 && Sfx.Instance != null && Sfx.Instance.HasAreaClip)
            {
                Tween.Delay(this, startDelay, () => Sfx.Instance?.AreaReveal());
                return maxDepth * RevealStepDelay + 0.45f;
            }

            // 펼침 깊이마다 음이 한 칸씩 올라간다. 너무 많으면 시끄러우니 8단까지만.
            int steps = Mathf.Min(maxDepth, 8);
            for (int d = 0; d <= steps; d++)
            {
                int step = d;
                Tween.Delay(this, startDelay + d * RevealStepDelay * Mathf.Max(1f, maxDepth / 8f), () => Sfx.Instance?.Reveal(step));
            }

            return maxDepth * RevealStepDelay + 0.45f;
        }

        /// <summary>힌트로 연 칸을 금색 테두리로 한 번 짚어 준다.</summary>
        public void PlayHint(int cell)
        {
            Fx.Ring(transform, CellCenter(cell), Theme.Gold, 31, 2.4f);
        }

        public void PlayWrong(int cell, Color chosen)
        {
            cells[cell].AnimateWrong(chosen);
        }

        public void SetFocus(int color)
        {
            focus = color;
            foreach (CellView cell in cells)
            {
                cell.SetFocus(focus);
            }
        }

        /// <summary>칩이 들어가고, 틈이 대각선으로 메워지고, 판이 한 번 튄 뒤 반짝인다.</summary>
        public void PlayClear(Action done)
        {
            Vector2 center = new Vector2(stage.Width / 2f, stage.Height / 2f);
            float lastChip = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                Vector2 p = new Vector2(i % stage.Width, i / stage.Width);
                float delay = Vector2.Distance(p, center) * 0.02f;
                lastChip = Mathf.Max(lastChip, delay);
                cells[i].HideChips(delay);
            }

            float mergeStart = lastChip + 0.3f;
            float lastMerge = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                float delay = mergeStart + (i % stage.Width + i / stage.Width) * 0.022f;
                lastMerge = Mathf.Max(lastMerge, delay);
                cells[i].BecomePixel(delay);
            }

            float pulse = lastMerge + 0.4f;
            Tween.Delay(this, pulse, () =>
            {
                Sfx.Instance?.Clear();
                Haptics.Success();
                Tween.Run(this, 0.5f, t => transform.localScale = Vector3.one * (1f + Ease.Pulse(t) * 0.05f),
                    Ease.Linear);
                SpawnSparkles();
                // 완성된 그림 위로 빛이 대각선으로 한 번 훑고 지나간다.
                for (int i = 0; i < cells.Length; i++)
                {
                    cells[i].Shine((i % stage.Width + i / stage.Width) * 0.03f);
                }
            });
            Tween.Delay(this, pulse + 0.9f, () => done?.Invoke());
        }

        void SpawnSparkles()
        {
            Sprite sprite = PixelGlyphs.Icon("sparkle", PixelGlyphs.Sparkle);
            for (int i = 0; i < 14; i++)
            {
                var position = new Vector2(
                    UnityEngine.Random.Range(Bounds.xMin, Bounds.xMax),
                    UnityEngine.Random.Range(Bounds.yMin, Bounds.yMax));
                Color color = i % 3 == 0 ? Theme.Gold : Color.white;
                SpriteRenderer s = Draw.Sprite(transform, "Sparkle", sprite, color, 60, position);
                float size = UnityEngine.Random.Range(0.35f, 0.7f);
                float delay = UnityEngine.Random.Range(0f, 0.5f);
                s.transform.localScale = Vector3.zero;
                Tween.Run(s, 0.6f, t =>
                {
                    float k = Ease.Pulse(t) * size;
                    s.transform.localScale = new Vector3(k, k, 1f);
                    s.transform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                }, Ease.Linear, delay, () => Destroy(s.gameObject));
            }
        }
    }
}
