using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 색에 사람이 부르는 이름을 붙인다(빨간색/red…, 문구는 <see cref="Loc"/>). 튜토리얼이 "이 색" 대신 실제 이름으로 말하게 하려고 쓴다.
    /// 스테이지 파일에는 색 이름이 없으므로 색상(hue)·채도·밝기로 가장 가까운 이름을 고른다.
    /// </summary>
    public static class ColorNames
    {
        /// <summary>현재 언어의 색 이름. 한국어는 모두 "색" 으로 끝나 받침이 있어 조사를 고정할 수 있다.</summary>
        public static string Describe(Rgb rgb)
        {
            Color.RGBToHSV(new Color32(rgb.R, rgb.G, rgb.B, 255), out float h, out float s, out float v);
            if (v < 0.22f)
            {
                return Loc.T("color.black");
            }

            if (s < 0.14f)
            {
                return v > 0.85f ? Loc.T("color.white") : Loc.T("color.gray");
            }

            float hue = h * 360f;
            if (hue < 15f || hue >= 345f)
            {
                return s < 0.5f && v > 0.8f ? Loc.T("color.pink") : Loc.T("color.red");
            }

            if (hue < 40f)
            {
                return v < 0.6f ? Loc.T("color.brown") : Loc.T("color.orange");
            }

            if (hue < 70f)
            {
                return v < 0.55f ? Loc.T("color.brown") : Loc.T("color.yellow");
            }

            if (hue < 165f)
            {
                return Loc.T("color.green");
            }

            if (hue < 200f)
            {
                return Loc.T("color.skyblue");
            }

            if (hue < 255f)
            {
                return Loc.T("color.blue");
            }

            if (hue < 300f)
            {
                return Loc.T("color.purple");
            }

            return Loc.T("color.pink");
        }
    }
}
