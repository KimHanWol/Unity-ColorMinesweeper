using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 메인 화면의 "PIXEL CLUE" 로고. 글자를 판의 칸과 같은 둥근 타일로 쌓아 만들고, 버튼처럼 아래에 진한 면을 깔아
    /// 도톰하게 보이게 한다. 가끔 빛이 대각선으로 훑고 지나간다(완성한 그림과 같은 연출).
    /// </summary>
    public sealed class PixelLogo : MonoBehaviour
    {
        /// <summary>타일 한 칸의 크기(유닛)와 두 줄 사이 간격(칸 수).</summary>
        const float Unit = 0.3f;
        const float LineGap = 0.9f;
        const float ShineEvery = 4.5f;

        static readonly Dictionary<char, string[]> Letters = new Dictionary<char, string[]>
        {
            ['P'] = new[] { "###.", "#..#", "###.", "#...", "#..." },
            ['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" },
            ['X'] = new[] { "#...#", ".#.#.", "..#..", ".#.#.", "#...#" },
            ['E'] = new[] { "####", "#...", "###.", "#...", "####" },
            ['L'] = new[] { "#...", "#...", "#...", "#...", "####" },
            ['C'] = new[] { ".###", "#...", "#...", "#...", ".###" },
            ['U'] = new[] { "#..#", "#..#", "#..#", "#..#", ".##." },
        };

        sealed class Tile
        {
            public SpriteRenderer Face;
            public Color Color;

            /// <summary>빛이 닿는 순서(왼쪽 위 0 → 오른쪽 아래 1).</summary>
            public float Along;
        }

        readonly List<Tile> tiles = new List<Tile>();
        float nextShine;
        float shineStart = -10f;

        /// <summary>줄 수(글자 5칸 두 줄과 사이 간격)로 잰 전체 높이.</summary>
        public static float Height => (10f + LineGap) * Unit;

        /// <param name="maxWidth">이 폭을 넘으면 전체를 줄여 맞춘다.</param>
        public static PixelLogo Create(Transform parent, Vector2 position, float maxWidth, int order)
        {
            Transform root = Draw.Node(parent, "PixelLogo", position);
            var logo = root.gameObject.AddComponent<PixelLogo>();
            float lineOffset = (5f + LineGap) / 2f * Unit;
            float widest = Mathf.Max(
                logo.BuildWord(root, "PIXEL", Theme.Accent, lineOffset, order),
                logo.BuildWord(root, "CLUE", Theme.Ink, -lineOffset, order));
            float scale = Mathf.Min(1f, maxWidth / widest);
            root.localScale = new Vector3(scale, scale, 1f);
            logo.nextShine = Time.unscaledTime + 1.2f;
            return logo;
        }

        /// <summary>한 낱말을 가운데 정렬해 쌓고 그 폭(유닛)을 돌려준다.</summary>
        float BuildWord(Transform parent, string word, Color color, float centerY, int order)
        {
            int columns = word.Length - 1;
            foreach (char c in word)
            {
                columns += Letters[c][0].Length;
            }

            Color under = Color.Lerp(color, Color.black, 0.28f);
            float left = -columns * Unit / 2f;
            int column = 0;
            foreach (char c in word)
            {
                string[] rows = Letters[c];
                for (int y = 0; y < rows.Length; y++)
                {
                    for (int x = 0; x < rows[y].Length; x++)
                    {
                        if (rows[y][x] != '#')
                        {
                            continue;
                        }

                        var position = new Vector2(left + (column + x + 0.5f) * Unit, centerY + (2f - y) * Unit);
                        Vector2 size = Vector2.one * (Unit * 0.95f);
                        Draw.Sprite(parent, "Under", SpriteFactory.RoundedRect(0.25f), under, order,
                            position + Vector2.down * (Unit * 0.2f), size);
                        tiles.Add(new Tile
                        {
                            Face = Draw.Sprite(parent, "Tile", SpriteFactory.RoundedRect(0.25f), color, order + 1, position, size),
                            Color = color,
                            Along = 0f,
                        });
                        tiles[tiles.Count - 1].Along = position.x - position.y;
                    }
                }

                column += rows[0].Length + 1;
            }

            return columns * Unit;
        }

        void Start()
        {
            // 빛이 닿는 순서를 0~1 로 맞춘다.
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (Tile tile in tiles)
            {
                min = Mathf.Min(min, tile.Along);
                max = Mathf.Max(max, tile.Along);
            }

            foreach (Tile tile in tiles)
            {
                tile.Along = Mathf.InverseLerp(min, max, tile.Along);
            }
        }

        void Update()
        {
            const float sweep = 0.7f;
            const float flash = 0.28f;
            float now = Time.unscaledTime;
            if (now >= nextShine)
            {
                shineStart = now;
                nextShine = now + ShineEvery;
            }

            float elapsed = now - shineStart;
            if (elapsed > sweep + flash + 0.1f)
            {
                return;
            }

            foreach (Tile tile in tiles)
            {
                float local = (elapsed - tile.Along * sweep) / flash;
                float glow = local > 0f && local < 1f ? Ease.Pulse(local) : 0f;
                tile.Face.color = Color.Lerp(tile.Color, Color.white, glow * 0.6f);
            }
        }
    }
}
