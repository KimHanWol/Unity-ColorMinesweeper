using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 둥근 숫자(게임 글꼴 Dongle 의 0~9 와 +)와 그 테두리. Resources/Digits/RoundDigits.png 한 장에 위 줄은 숫자, 아래 줄은
    /// 테두리(숫자를 두껍게 부풀린 모양)가 150x200 칸마다 들어 있다. 흰색이라 그릴 때 색을 입힌다.
    /// 판의 숫자처럼 크게 보이는 곳에 쓴다. 도트 숫자는 딱딱해 보인다는 의견이 있었다.
    /// </summary>
    public static class RoundDigits
    {
        const int CellWidth = 150;
        const int CellHeight = 200;

        /// <summary>숫자 높이가 1 유닛이 되게 하는 배율(숫자는 칸 높이 200 중 약 120 도트).</summary>
        const float PixelsPerUnit = 120f;

        /// <summary>글자 사이 간격(유닛). 테두리끼리는 겹쳐도 숫자 뒤에 깔리므로 괜찮다.</summary>
        public const float Advance = 0.7f;

        static Texture2D atlas;
        static bool loaded;
        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();

        public static bool Available
        {
            get
            {
                if (!loaded)
                {
                    loaded = true;
                    atlas = Resources.Load<Texture2D>("Digits/RoundDigits");
                }

                return atlas != null;
            }
        }

        public static bool HasGlyph(char c)
        {
            return (c >= '0' && c <= '9') || c == '+';
        }

        public static Sprite Digit(char c)
        {
            return Slice(Index(c), true);
        }

        public static Sprite Outline(char c)
        {
            return Slice(Index(c), false);
        }

        /// <summary>그림 파일에서 몇 번째 칸인지: 0~9 다음 칸이 + 다.</summary>
        static int Index(char c)
        {
            return c == '+' ? 10 : c - '0';
        }

        static Sprite Slice(int digit, bool fill)
        {
            int key = digit * 2 + (fill ? 1 : 0);
            if (cache.TryGetValue(key, out Sprite sprite) && sprite != null)
            {
                return sprite;
            }

            // 그림 파일의 위 줄이 숫자, 아래 줄이 테두리다. 텍스처 좌표는 아래에서 위로 센다.
            var rect = new Rect(digit * CellWidth, fill ? CellHeight : 0, CellWidth, CellHeight);
            sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
            cache[key] = sprite;
            return sprite;
        }
    }
}
