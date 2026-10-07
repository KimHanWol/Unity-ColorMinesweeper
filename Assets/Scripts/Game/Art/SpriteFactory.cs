using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 게임에 쓰는 스프라이트를 코드로 만든다. 모두 흰색 기반이라 SpriteRenderer.color 로 물들여 쓴다.
    /// 기본 도형은 1 유닛 크기(피벗 가운데)로 만든다.
    /// </summary>
    public static class SpriteFactory
    {
        const int ShapeSize = 96;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        /// <summary>둥근 사각형. radius 는 한 변 대비 비율.</summary>
        public static Sprite RoundedRect(float radius = 0.22f)
        {
            return Cached("rr" + radius, () => Shape((x, y) => RoundedBoxDistance(x, y, 0.5f, 0.5f, radius), 1f));
        }

        /// <summary>
        /// 아직 안 열린 칸. 위쪽이 밝고 아래가 살짝 어두운 도톰한 타일이라 누를 수 있는 것처럼 보인다.
        /// </summary>
        public static Sprite RaisedTile()
        {
            return Cached("raised", () =>
            {
                var tex = NewTexture(ShapeSize, ShapeSize, FilterMode.Bilinear);
                var pixels = new Color32[ShapeSize * ShapeSize];
                for (int py = 0; py < ShapeSize; py++)
                {
                    for (int px = 0; px < ShapeSize; px++)
                    {
                        float x = (px + 0.5f) / ShapeSize - 0.5f;
                        float y = (py + 0.5f) / ShapeSize - 0.5f;
                        float d = RoundedBoxDistance(x, y, 0.5f, 0.5f, 0.24f) * ShapeSize;
                        float alpha = Mathf.Clamp01(0.5f - d);
                        // 가장자리 안쪽 몇 픽셀은 아래쪽이 더 어두운 테두리, 위쪽은 밝은 하이라이트.
                        float edge = Mathf.Clamp01(-d / (ShapeSize * 0.07f));
                        float shade = Mathf.Lerp(0.9f, 1f, y + 0.5f);
                        float rim = (1f - edge) * (y < 0f ? -0.12f : 0.1f);
                        byte v = (byte)(Mathf.Clamp01(shade + rim) * 255f);
                        pixels[py * ShapeSize + px] = new Color32(v, v, v, (byte)(alpha * 255f));
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, ShapeSize, ShapeSize), new Vector2(0.5f, 0.5f), ShapeSize, 0,
                    SpriteMeshType.FullRect);
            });
        }

        /// <summary>
        /// 9-slice 둥근 패널. 늘려도 모서리 반지름(월드 유닛)이 그대로라 판, 카드, 버튼 바탕에 쓴다.
        /// SpriteRenderer.drawMode = Sliced 로 써야 한다(<see cref="Draw.Panel"/>).
        /// </summary>
        public static Sprite Panel(float cornerRadius)
        {
            return Cached("panel" + cornerRadius, () =>
            {
                const int size = 64;
                const int radius = 24;
                var tex = NewTexture(size, size, FilterMode.Bilinear);
                var pixels = new Color32[size * size];
                for (int py = 0; py < size; py++)
                {
                    for (int px = 0; px < size; px++)
                    {
                        float x = px + 0.5f - size / 2f;
                        float y = py + 0.5f - size / 2f;
                        float d = RoundedBoxDistance(x, y, size / 2f, size / 2f, radius);
                        pixels[py * size + px] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255f));
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                const float border = radius + 2;
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), radius / cornerRadius, 0,
                    SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            });
        }

        /// <summary>
        /// 9-slice 둥근 테두리(속이 빈). 크기를 바꿔도 모서리 반지름과 선 두께가 그대로라, 숨 쉬듯 커졌다 작아져도
        /// 선이 굵어지거나 얇아지지 않는다. SpriteRenderer.drawMode = Sliced 로 쓴다(<see cref="Draw.OutlinePanel"/>).
        /// </summary>
        public static Sprite OutlinePanel(float cornerRadius, float thickness)
        {
            return Cached("outlinePanel" + cornerRadius + "_" + thickness, () =>
            {
                const int size = 64;
                const int radius = 24;
                float ppu = radius / cornerRadius;
                float line = thickness * ppu;
                var tex = NewTexture(size, size, FilterMode.Bilinear);
                var pixels = new Color32[size * size];
                for (int py = 0; py < size; py++)
                {
                    for (int px = 0; px < size; px++)
                    {
                        float x = px + 0.5f - size / 2f;
                        float y = py + 0.5f - size / 2f;
                        float d = RoundedBoxDistance(x, y, size / 2f, size / 2f, radius);
                        float outer = Mathf.Clamp01(0.5f - d);
                        float inner = Mathf.Clamp01(0.5f - (-d - line));
                        pixels[py * size + px] = new Color32(255, 255, 255, (byte)(outer * inner * 255f));
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                const float border = radius + 2;
                return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu, 0,
                    SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            });
        }

        /// <summary>둥근 사각형 테두리(속이 빈). 튜토리얼에서 칸을 짚어 줄 때 쓴다.</summary>
        public static Sprite OutlineRect()
        {
            return Cached("outline", () => Shape((x, y) => Mathf.Abs(RoundedBoxDistance(x, y, 0.46f, 0.46f, 0.2f)) - 0.035f, 1f));
        }

        /// <summary>동그라미 테두리(속이 빈).</summary>
        public static Sprite OutlineCircle()
        {
            return Cached("outlineCircle", () => Shape((x, y) => Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.46f) - 0.035f, 1f));
        }

        /// <summary>가장자리가 부드럽게 번지는 원. 튜토리얼에서 팔레트 색 뒤의 후광으로 쓴다.</summary>
        public static Sprite SoftCircle()
        {
            return Cached("softCircle", () => Shape((x, y) => Mathf.Sqrt(x * x + y * y) - 0.3f, 0.2f));
        }

        public static Sprite Circle()
        {
            return Cached("circle", () => Shape((x, y) => Mathf.Sqrt(x * x + y * y) - 0.5f, 1f));
        }

        /// <summary>가장자리가 흐린 둥근 사각형. 카드와 판 밑 그림자에 쓴다.</summary>
        public static Sprite SoftShadow()
        {
            return Cached("shadow", () => Shape((x, y) => RoundedBoxDistance(x, y, 0.36f, 0.36f, 0.1f), 0.14f));
        }

        /// <summary>1 유닛짜리 흰 사각형.</summary>
        public static Sprite Square()
        {
            return Cached("square", () =>
            {
                var tex = NewTexture(4, 4, FilterMode.Point);
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, 255);
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4, 0, SpriteMeshType.FullRect);
            });
        }

        /// <summary>위 → 아래 세로 그라데이션. 배경에 쓴다.</summary>
        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            return Cached("grad" + top + bottom, () =>
            {
                const int h = 64;
                var tex = NewTexture(1, h, FilterMode.Bilinear);
                var pixels = new Color32[h];
                for (int y = 0; y < h; y++)
                {
                    pixels[y] = Color.Lerp(bottom, top, y / (h - 1f));
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), h, 0, SpriteMeshType.FullRect);
            });
        }

        /// <summary>
        /// '#' 은 칠하고 나머지는 비운 도트 비트맵. 높이가 1 유닛이 되도록 만든다.
        /// 가장자리가 번지지 않게 한 칸씩 투명 여백을 두고 Point 필터를 쓴다.
        /// </summary>
        public static Sprite Bitmap(string key, string[] rows)
        {
            return Cached("bmp" + key, () =>
            {
                int h = rows.Length;
                int w = rows[0].Length;
                var tex = NewTexture(w + 2, h + 2, FilterMode.Point);
                var pixels = new Color32[(w + 2) * (h + 2)];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (rows[y][x] == '#')
                        {
                            pixels[(h - y) * (w + 2) + x + 1] = new Color32(255, 255, 255, 255);
                        }
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, w + 2, h + 2), new Vector2(0.5f, 0.5f), h, 0,
                    SpriteMeshType.FullRect);
            });
        }

        /// <summary>스테이지 그림을 그대로 한 픽셀씩 옮긴 미리보기. 높이 1 유닛.</summary>
        public static Sprite StagePicture(Core.Stage stage)
        {
            return Cached("stage" + stage.Id + stage.GetHashCode(), () =>
            {
                var tex = NewTexture(stage.Width, stage.Height, FilterMode.Point);
                var pixels = new Color32[stage.CellCount];
                for (int y = 0; y < stage.Height; y++)
                {
                    for (int x = 0; x < stage.Width; x++)
                    {
                        pixels[(stage.Height - 1 - y) * stage.Width + x] = Theme.ToColor(stage.Colors[stage.ColorAt(x, y)].Color);
                    }
                }

                tex.SetPixels32(pixels);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, stage.Width, stage.Height), new Vector2(0.5f, 0.5f),
                    Mathf.Max(stage.Width, stage.Height), 0, SpriteMeshType.FullRect);
            });
        }

        delegate float DistanceField(float x, float y);

        /// <summary>부호 있는 거리장(한 변 1 기준)으로 안티에일리어싱된 도형을 만든다. softness 가 크면 흐려진다.</summary>
        static Sprite Shape(DistanceField field, float softness)
        {
            var tex = NewTexture(ShapeSize, ShapeSize, FilterMode.Bilinear);
            var pixels = new Color32[ShapeSize * ShapeSize];
            for (int py = 0; py < ShapeSize; py++)
            {
                for (int px = 0; px < ShapeSize; px++)
                {
                    float x = (px + 0.5f) / ShapeSize - 0.5f;
                    float y = (py + 0.5f) / ShapeSize - 0.5f;
                    float d = field(x, y);
                    float alpha = softness >= 1f
                        ? Mathf.Clamp01(0.5f - d * ShapeSize)
                        : Mathf.Clamp01(0.5f - d / softness);
                    pixels[py * ShapeSize + px] = new Color32(255, 255, 255, (byte)(alpha * alpha * (3f - 2f * alpha) * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, ShapeSize, ShapeSize), new Vector2(0.5f, 0.5f), ShapeSize, 0,
                SpriteMeshType.FullRect);
        }

        static float RoundedBoxDistance(float x, float y, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(x) - halfW + radius;
            float qy = Mathf.Abs(y) - halfH + radius;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static Texture2D NewTexture(int w, int h, FilterMode filter)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
        }

        static Sprite Cached(string key, System.Func<Sprite> make)
        {
            if (!cache.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                sprite = make();
                cache[key] = sprite;
            }

            return sprite;
        }
    }
}
