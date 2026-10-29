using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 도트 숫자 줄. 높이 1 유닛, 가운데 정렬. transform 크기로 키운다.
    /// 테두리 색을 주면 글자마다 한 도트 두께의 테두리를 뒤에 깐다(칸 색과 같은 색 숫자도 읽히게).
    /// </summary>
    public sealed class PixelText : MonoBehaviour
    {
        const float Advance = 6f / 7f; // 글자 폭 5 + 간격 1, 높이 7 기준

        readonly List<SpriteRenderer> glyphs = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> outlines = new List<SpriteRenderer>();
        string text = string.Empty;
        Color color = Color.white;
        Color? outlineColor;
        int order;

        public static PixelText Create(Transform parent, string name, string text, Color color, int order,
            Vector2 localPosition, float height)
        {
            return Create(parent, name, text, color, order, localPosition, height, null);
        }

        /// <param name="outline">테두리 색. 테두리는 order - 1 에 그린다.</param>
        public static PixelText Create(Transform parent, string name, string text, Color color, int order,
            Vector2 localPosition, float height, Color? outline)
        {
            Transform t = Draw.Node(parent, name, localPosition);
            t.localScale = new Vector3(height, height, 1f);
            var pixelText = t.gameObject.AddComponent<PixelText>();
            pixelText.color = color;
            pixelText.outlineColor = outline;
            pixelText.order = order;
            pixelText.SetText(text);
            return pixelText;
        }

        public float Width => text.Length * Advance - (text.Length > 0 ? 1f / 7f : 0f);

        public void SetText(string value)
        {
            value = value ?? string.Empty;
            if (value == text && glyphs.Count == value.Length)
            {
                return;
            }

            text = value;
            while (glyphs.Count < text.Length)
            {
                glyphs.Add(Draw.Sprite(transform, "g", null, color, order));
                if (outlineColor.HasValue)
                {
                    outlines.Add(Draw.Sprite(transform, "o", null, outlineColor.Value, order - 1));
                }
            }

            float x = -Width / 2f + 5f / 14f;
            for (int i = 0; i < glyphs.Count; i++)
            {
                bool used = i < text.Length && PixelGlyphs.HasGlyph(text[i]);
                glyphs[i].gameObject.SetActive(used);
                if (i < outlines.Count)
                {
                    outlines[i].gameObject.SetActive(used);
                }

                if (!used)
                {
                    continue;
                }

                var position = new Vector3(x + i * Advance, 0f, 0f);
                glyphs[i].sprite = PixelGlyphs.Digit(text[i]);
                glyphs[i].transform.localPosition = position;
                if (i < outlines.Count)
                {
                    outlines[i].sprite = PixelGlyphs.Outline(text[i]);
                    outlines[i].transform.localPosition = position;
                }
            }
        }

        public void SetColor(Color value)
        {
            color = value;
            foreach (SpriteRenderer g in glyphs)
            {
                g.color = value;
            }
        }

        public void SetAlpha(float alpha)
        {
            foreach (SpriteRenderer g in glyphs)
            {
                Draw.SetAlpha(g, alpha);
            }

            foreach (SpriteRenderer o in outlines)
            {
                Draw.SetAlpha(o, alpha);
            }
        }
    }
}
