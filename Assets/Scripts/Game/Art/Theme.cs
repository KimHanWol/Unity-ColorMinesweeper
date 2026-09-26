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
        public static readonly Color Accent = Hex(0x6C5CE7);
        public static readonly Color Danger = Hex(0xFF5A6E);
        public static readonly Color Gold = Hex(0xFFC53D);
        public static readonly Color Dim = new Color(0.12f, 0.1f, 0.2f, 0.55f);
        public static readonly Color Card = Hex(0xFFFFFF);
        public static readonly Color Locked = Hex(0xCFCADD);

        public static Color ToColor(Rgb rgb)
        {
            return new Color32(rgb.R, rgb.G, rgb.B, 255);
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
