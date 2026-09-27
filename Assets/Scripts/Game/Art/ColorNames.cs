using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 색에 사람이 부르는 이름을 붙인다(빨간색, 파란색…). 튜토리얼이 "이 색" 대신 실제 이름으로 말하게 하려고 쓴다.
    /// 스테이지 파일에는 색 이름이 없으므로 색상(hue)·채도·밝기로 가장 가까운 이름을 고른다.
    /// </summary>
    public static class ColorNames
    {
        /// <summary>모든 이름이 "색" 으로 끝나 받침이 있다. 조사는 이/을/으로/이에요 를 쓰면 된다.</summary>
        public static string Describe(Rgb rgb)
        {
            Color.RGBToHSV(new Color32(rgb.R, rgb.G, rgb.B, 255), out float h, out float s, out float v);
            if (v < 0.22f)
            {
                return "검은색";
            }

            if (s < 0.14f)
            {
                return v > 0.85f ? "흰색" : "회색";
            }

            float hue = h * 360f;
            if (hue < 15f || hue >= 345f)
            {
                return s < 0.5f && v > 0.8f ? "분홍색" : "빨간색";
            }

            if (hue < 40f)
            {
                return v < 0.6f ? "갈색" : "주황색";
            }

            if (hue < 70f)
            {
                return v < 0.55f ? "갈색" : "노란색";
            }

            if (hue < 165f)
            {
                return "초록색";
            }

            if (hue < 200f)
            {
                return "하늘색";
            }

            if (hue < 255f)
            {
                return "파란색";
            }

            if (hue < 300f)
            {
                return "보라색";
            }

            return "분홍색";
        }
    }
}
