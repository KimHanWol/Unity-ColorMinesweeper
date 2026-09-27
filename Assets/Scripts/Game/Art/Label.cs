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

        /// <summary>가운데 정렬 라벨을 글자 높이의 이 비율만큼 내린다(눈대중 보정).</summary>
        const float DongleOpticalDrop = 0.1f;

        static Font font;
        static bool usingDongle;
        static float unitHeight;

        TextMesh mesh;
        Vector3 basePosition;
        bool centerVertically;

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
            mesh.alignment = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.LowerLeft
                ? TextAlignment.Left
                : anchor == TextAnchor.UpperRight || anchor == TextAnchor.MiddleRight || anchor == TextAnchor.LowerRight
                    ? TextAlignment.Right
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
            label.basePosition = t.localPosition;
            label.centerVertically = anchor == TextAnchor.MiddleCenter || anchor == TextAnchor.MiddleLeft ||
                                     anchor == TextAnchor.MiddleRight;
            label.Recenter();
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
            set
            {
                mesh.text = value;
                Recenter();
            }
        }

        /// <summary>
        /// 가운데 정렬 라벨의 실제 글자 영역 세로 중심을 놓으려던 자리에 맞춘다. 동글은 글자가 줄 높이의 위쪽에 치우쳐
        /// 그려져서, TextMesh 기준점만 맞추면 글자가 떠 보인다. 글자 영역이 다음 프레임에 갱신될 때를 위해 한 번 더 한다.
        /// </summary>
        void Recenter()
        {
            if (!centerVertically)
            {
                return;
            }

            ApplyCenter();
            Tween.Delay(this, 0.02f, ApplyCenter);
        }

        void ApplyCenter()
        {
            Transform t = transform;
            t.localPosition = basePosition;
            Bounds bounds = RenderBounds;
            if (bounds.size.y <= 0f || t.parent == null)
            {
                return;
            }

            float glyphCenter = t.parent.InverseTransformPoint(bounds.center).y;
            // 한글은 초성·모음이 위에 몰려 글자 상자 가운데에 두어도 떠 보인다. 글자 높이의 일부만큼 살짝 내려 눈으로 가운데를 맞춘다.
            float optical = usingDongle ? bounds.size.y / Mathf.Max(t.parent.lossyScale.y, 0.0001f) * DongleOpticalDrop : 0f;
            t.localPosition = basePosition + new Vector3(0f, basePosition.y - glyphCenter - optical, 0f);
        }

        /// <summary>놓을 자리(부모 기준)를 바꾼다. 가운데 정렬 라벨은 글자 중심을 다시 맞춘다.</summary>
        public void MoveTo(Vector2 position)
        {
            basePosition = new Vector3(position.x, position.y, 0f);
            transform.localPosition = basePosition;
            Recenter();
        }

        public void SetColor(Color color)
        {
            mesh.color = color;
        }

        /// <summary>실제로 그려진 글자 영역(월드 좌표). 폰트의 줄 높이가 아니라 글자 모양 기준이라 여백을 맞출 때 쓴다.</summary>
        public Bounds RenderBounds => GetComponent<MeshRenderer>().bounds;

        /// <summary>한 줄이 차지하는 높이(부모 기준 유닛). 여러 줄 문구의 칸 높이를 잡을 때 쓴다.</summary>
        public float LineAdvance => mesh.font.lineHeight * mesh.lineSpacing / 10f * transform.localScale.y;

        /// <summary>text 를 한 줄로 그렸을 때의 폭(부모 기준 유닛). TextMesh 는 글꼴 픽셀 10 을 1 유닛으로 본다.</summary>
        public float MeasureWidth(string text)
        {
            Font f = mesh.font;
            f.RequestCharactersInTexture(text, FontPixelSize, mesh.fontStyle);
            float pixels = 0f;
            foreach (char c in text)
            {
                if (f.GetCharacterInfo(c, out CharacterInfo info, FontPixelSize, mesh.fontStyle))
                {
                    pixels += info.advance;
                }
            }

            return pixels / 10f * transform.localScale.x;
        }

        /// <summary>
        /// maxWidth 안에 들어가게 단어(띄어쓰기) 단위로 줄을 바꿔 넣는다. 한 단어가 너무 길면 글자 단위로 자른다.
        /// 문구 안의 줄바꿈(\n)은 문단 구분으로 그대로 둔다. 줄 수를 돌려준다.
        /// </summary>
        public int SetWrappedText(string text, float maxWidth)
        {
            var lines = new System.Collections.Generic.List<string>();
            foreach (string paragraph in text.Split('\n'))
            {
                string line = string.Empty;
                foreach (string word in paragraph.Split(' '))
                {
                    string candidate = line.Length == 0 ? word : line + " " + word;
                    if (MeasureWidth(candidate) <= maxWidth)
                    {
                        line = candidate;
                        continue;
                    }

                    if (line.Length > 0)
                    {
                        lines.Add(line);
                    }

                    line = word;
                    while (MeasureWidth(line) > maxWidth && line.Length > 1)
                    {
                        int cut = line.Length - 1;
                        while (cut > 1 && MeasureWidth(line.Substring(0, cut)) > maxWidth)
                        {
                            cut--;
                        }

                        lines.Add(line.Substring(0, cut));
                        line = line.Substring(cut);
                    }
                }

                lines.Add(line);
            }

            mesh.text = string.Join("\n", lines);
            return lines.Count;
        }
    }
}
