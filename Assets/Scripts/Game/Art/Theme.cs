using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>화면 전체가 공유하는 색. 스테이지 그림 색과 섞여도 튀지 않게 채도를 낮춘 중성색 위주로 둔다.</summary>
    public static class Theme
    {
        public static readonly Color BackgroundTop = Hex(0xF7F3EC);
        public static readonly Color BackgroundBottom = Hex(0xE9E4F2);
        public static readonly Color Plate = Hex(0xFFFFFF);
        public static readonly Color Shadow = new Color(0.24f, 0.2f, 0.38f, 0.16f);
        public static readonly Color HiddenTile = Hex(0xD8D3E6);
        public static readonly Color Ink = Hex(0x2E2A40);
        public static readonly Color SubInk = Hex(0x8A84A3);
        public static readonly Color Accent = Hex(0x9488F2);
        public static readonly Color Danger = Hex(0xFF5A6E);
        public static readonly Color Gold = Hex(0xFFC53D);

        /// <summary>튜토리얼에서 누를 칸을 짚는 색. 밝은 배경과 대부분의 그림 색 위에서 잘 보이는 진한 분홍.</summary>
        public static readonly Color Highlight = Hex(0xFF2E7E);
        public static readonly Color Dim = new Color(0.12f, 0.1f, 0.2f, 0.55f);
        public static readonly Color Card = Hex(0xFFFFFF);
        public static readonly Color Locked = Hex(0xCFCADD);

        /// <summary>
        /// 장식에 쓰는 파스텔 다섯 색(분홍, 노랑, 하늘, 연두, 보라). 배경에 떠다니는 타일, 색종이, 화면 전환 도트가 모두
        /// 이 색만 써서 화면마다 색감이 달라지지 않게 한다. 장식을 새로 넣을 때도 여기서 고른다.
        /// </summary>
        public static readonly Color[] Pastels =
        {
            Hex(0xFF8FAB), Hex(0xFFD166), Hex(0x8ECAE6), Hex(0x95D5B2), Hex(0xC77DFF),
        };

        /// <summary>그림 색을 얼마나 부드럽게 할지: 채도를 이만큼 덜고, 밝기를 이만큼 흰색 쪽으로 올린다.</summary>
        const float SoftenSaturation = 0.14f;
        const float SoftenValue = 0.14f;

        /// <summary>
        /// 스테이지 그림 색을 화면에 쓸 색으로 바꾼다. 스테이지 파일의 색은 진하고 선명해서 파스텔인 로고·버튼과 따로 놀기 때문에,
        /// 모든 그림 색을 여기서 한 번에 살짝 부드럽게 한다(색 사이 구분은 남을 만큼만).
        /// </summary>
        public static Color ToColor(Rgb rgb)
        {
            Color.RGBToHSV(new Color32(rgb.R, rgb.G, rgb.B, 255), out float h, out float s, out float v);
            return Color.HSVToRGB(h, s * (1f - SoftenSaturation), Mathf.Lerp(v, 1f, SoftenValue));
        }

        /// <summary>그 색 위에 올릴 글자색. 밝은 색 위엔 진한 잉크, 어두운 색 위엔 흰색.</summary>
        public static Color InkOn(Rgb rgb)
        {
            return rgb.Luminance() > 0.45 ? Ink : Color.white;
        }

        public static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
