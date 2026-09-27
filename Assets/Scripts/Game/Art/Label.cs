using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 한글이 들어가는 글자(스테이지 이름, 버튼 문구)용. 둥글둥글한 동글(Dongle Bold, SIL OFL,
    /// Resources/Fonts/Dongle-OFL.txt)을 쓴다. 파일 크기 때문에 굵은 글씨 하나만 넣었다.
    /// TextMesh 의 크기 단위가 직관적이지 않아서, 글자 높이를 월드 유닛으로 받아 배율을 맞춘다.
    /// </summary>
    public sealed class Label : MonoBehaviour
    {
        const int FontPixelSize = 96;
        const string FontPath = "Fonts/Dongle-Bold";

        /// <summary>
        /// 동글은 한 줄 높이에 비해 글자가 작게 그려진다. 다른 폰트 기준으로 정한 크기가 비슷하게 보이도록 키운다.
        /// </summary>
        const float DongleGlyphScale = 1.55f;

        /// <summary>동글은 줄 간격이 넓어서 여러 줄 문구는 좁힌다.</summary>
        const float DongleLineSpacing = 0.62f;

        static Font font;
        static bool usingDongle;
        static float unitHeight;

        TextMesh mesh;

        static Font SharedFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<Font>(FontPath);
                    usingDongle = font != null;
                }

                if (font == null)
                {
                    // 폰트 파일이 없으면 기기 폰트로 그린다(한글이 깨지지 않게).
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
            // 동글은 굵은 글씨 파일 하나라 가짜 굵게(FontStyle.Bold)를 쓰지 않는다. 기기 폰트일 때만 굵게 한다.
            mesh.fontStyle = bold && !usingDongle ? FontStyle.Bold : FontStyle.Normal;
            mesh.lineSpacing = usingDongle ? DongleLineSpacing : 1f;
            mesh.color = color;
            mesh.text = text;
            renderer.sharedMaterial = SharedFont.material;
            renderer.sortingOrder = order;

            float scale = height / UnitHeight() * (usingDongle ? DongleGlyphScale : 1f);
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
