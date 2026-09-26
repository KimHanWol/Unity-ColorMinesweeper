using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 한글이 들어가는 글자(스테이지 이름, 버튼 문구)용. 도트 숫자와 달리 OS 폰트를 써서 한글을 그린다.
    /// TextMesh 의 크기 단위가 직관적이지 않아서, 글자 높이를 월드 유닛으로 받아 배율을 맞춘다.
    /// </summary>
    public sealed class Label : MonoBehaviour
    {
        const int FontPixelSize = 96;

        static Font font;
        static float unitHeight;

        TextMesh mesh;

        static Font SharedFont
        {
            get
            {
                if (font == null)
                {
                    font = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Noto Sans CJK KR", "NotoSansCJK-Regular", "Noto Sans KR", "SamsungOneKorean",
                        "Malgun Gothic", "Apple SD Gothic Neo", "Roboto", "Arial",
                    }, FontPixelSize);
                }

                return font;
            }
        }

        public static Label Create(Transform parent, string name, string text, Color color, int order,
            Vector2 localPosition, float height, TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false)
        {
            Transform t = Draw.Node(parent, name, localPosition);
            var renderer = t.gameObject.AddComponent<MeshRenderer>();
            var mesh = t.gameObject.AddComponent<TextMesh>();
            mesh.font = SharedFont;
            mesh.fontSize = FontPixelSize;
            mesh.characterSize = 1f;
            mesh.anchor = anchor;
            mesh.alignment = anchor == TextAnchor.MiddleLeft ? TextAlignment.Left
                : anchor == TextAnchor.MiddleRight ? TextAlignment.Right
                : TextAlignment.Center;
            mesh.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            mesh.color = color;
            mesh.text = text;
            renderer.sharedMaterial = SharedFont.material;
            renderer.sortingOrder = order;

            float scale = height / UnitHeight();
            t.localScale = new Vector3(scale, scale, 1f);

            var label = t.gameObject.AddComponent<Label>();
            label.mesh = mesh;
            return label;
        }

        /// <summary>characterSize 1 일 때 한 줄 높이(월드 유닛). TextMesh 는 fontSize/10 을 1 유닛으로 본다.</summary>
        static float UnitHeight()
        {
            if (unitHeight <= 0f)
            {
                unitHeight = FontPixelSize / 10f;
            }

            return unitHeight;
        }

        public string Text
        {
            get => mesh.text;
            set => mesh.text = value;
        }

        public void SetColor(Color color)
        {
            mesh.color = color;
        }
    }
}
