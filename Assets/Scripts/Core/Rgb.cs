using System;
using System.Globalization;

namespace ColorMinesweeper.Core
{
    /// <summary>UnityEngine.Color32 에 의존하지 않는 8비트 RGB 색.</summary>
    public readonly struct Rgb : IEquatable<Rgb>
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public Rgb(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        /// <summary>"#RRGGBB" 또는 "RRGGBB" 를 읽는다.</summary>
        public static bool TryParseHex(string text, out Rgb rgb)
        {
            rgb = default;
            if (text == null)
            {
                return false;
            }

            string hex = text.StartsWith("#", StringComparison.Ordinal) ? text.Substring(1) : text;
            if (hex.Length != 6)
            {
                return false;
            }

            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }

            rgb = new Rgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
            return true;
        }

        public string ToHex()
        {
            return "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
        }

        /// <summary>WCAG 상대 휘도(0~1). 단서 숫자를 밝은 색/어두운 색 중 무엇으로 그릴지 정할 때 쓴다.</summary>
        public double Luminance()
        {
            return 0.2126 * Channel(R) + 0.7152 * Channel(G) + 0.0722 * Channel(B);
        }

        static double Channel(byte c)
        {
            double v = c / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        public bool Equals(Rgb other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is Rgb other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (R << 16) | (G << 8) | B;
        }

        public override string ToString()
        {
            return ToHex();
        }
    }
}
