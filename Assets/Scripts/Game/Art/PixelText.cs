using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>도트 숫자 줄. 높이 1 유닛, 가운데 정렬. transform 크기로 키운다.</summary>
    public sealed class PixelText : MonoBehaviour
    {
        const float Advance = 6f / 7f; // 글자 폭 5 + 간격 1, 높이 7 기준

        readonly List<SpriteRenderer> glyphs = new List<SpriteRenderer>();
        string text = string.Empty;
        Color color = Color.white;
        int order;

        public static PixelText Create(Transform parent, string name, string text, Color color, int order,
            Vector2 localPosition, float height)
        {
            Transform t = Draw.Node(parent, name, localPosition);
            t.localScale = new Vector3(height, height, 1f);
            var pixelText = t.gameObject.AddComponent<PixelText>();
            pixelText.color = color;
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
            }

            float x = -Width / 2f + 5f / 14f;
            for (int i = 0; i < glyphs.Count; i++)
            {
                bool used = i < text.Length && PixelGlyphs.HasGlyph(text[i]);
                glyphs[i].gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                glyphs[i].sprite = PixelGlyphs.Digit(text[i]);
                glyphs[i].transform.localPosition = new Vector3(x + i * Advance, 0f, 0f);
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
        }
    }
}
