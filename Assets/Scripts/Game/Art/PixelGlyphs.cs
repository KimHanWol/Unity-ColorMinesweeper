using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 숫자와 아이콘을 도트 비트맵으로 둔다. 도트 그림 게임의 톤과 맞고, 폰트 에셋 없이 어느 배율에서도 또렷하다.
    /// </summary>
    public static class PixelGlyphs
    {
        static readonly Dictionary<char, string[]> digits = new Dictionary<char, string[]>
        {
            ['0'] = new[] { ".###.", "##.##", "##.##", "##.##", "##.##", "##.##", ".###." },
            ['1'] = new[] { "..##.", ".###.", "..##.", "..##.", "..##.", "..##.", ".####" },
            ['2'] = new[] { ".###.", "##.##", "...##", "..##.", ".##..", "##...", "#####" },
            ['3'] = new[] { "####.", "...##", "...##", ".###.", "...##", "...##", "####." },
            ['4'] = new[] { "...##", "..###", ".#.##", "##.##", "#####", "...##", "...##" },
            ['5'] = new[] { "#####", "##...", "####.", "...##", "...##", "##.##", ".###." },
            ['6'] = new[] { ".###.", "##...", "####.", "##.##", "##.##", "##.##", ".###." },
            ['7'] = new[] { "#####", "...##", "..##.", "..##.", ".##..", ".##..", ".##.." },
            ['8'] = new[] { ".###.", "##.##", "##.##", ".###.", "##.##", "##.##", ".###." },
            ['9'] = new[] { ".###.", "##.##", "##.##", ".####", "...##", "...##", ".###." },
            ['/'] = new[] { "....#", "...##", "..##.", ".##..", "##...", "#....", "....." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            ['x'] = new[] { ".....", ".....", "##.##", ".###.", "..#..", ".###.", "##.##" },
        };

        public static readonly string[] Heart =
        {
            ".##.##.",
            "#######",
            "#######",
            "#######",
            ".#####.",
            "..###..",
            "...#...",
        };

        public static readonly string[] Star =
        {
            "....#....",
            "...###...",
            "...###...",
            "#########",
            ".#######.",
            "..#####..",
            "..#####..",
            ".###.###.",
            ".##...##.",
        };

        public static readonly string[] Back =
        {
            "...##...",
            "..##....",
            ".##.....",
            "########",
            "########",
            ".##.....",
            "..##....",
            "...##...",
        };

        public static readonly string[] Retry =
        {
            "..####.#",
            ".##..###",
            "##...###",
            "##......",
            "##......",
            "##....##",
            ".##..##.",
            "..####..",
        };

        public static readonly string[] Play =
        {
            "##......",
            "####....",
            "######..",
            "########",
            "########",
            "######..",
            "####....",
            "##......",
        };

        public static readonly string[] Lock =
        {
            "..###..",
            ".#...#.",
            ".#...#.",
            "#######",
            "###.###",
            "###.###",
            "#######",
        };

        public static readonly string[] Film =
        {
            "#########",
            "#.#####.#",
            "#########",
            "##.....##",
            "##.....##",
            "#########",
            "#.#####.#",
            "#########",
        };

        /// <summary>설정 톱니바퀴. 이빨 8개와 가운데 구멍이 또렷하게 보이도록 13x13 으로 그렸다.</summary>
        public static readonly string[] Gear =
        {
            ".....###.....",
            ".##..###..##.",
            ".###########.",
            "..#########..",
            ".####...####.",
            "####.....####",
            "###.......###",
            "####.....####",
            ".####...####.",
            "..#########..",
            ".###########.",
            ".##..###..##.",
            ".....###.....",
        };

        public static readonly string[] Check =
        {
            ".......##",
            "......##.",
            "##...##..",
            ".##.##...",
            "..###....",
            "...#.....",
        };

        /// <summary>공유(점 셋을 잇는 모양). 아이콘 파일이 없을 때 쓴다.</summary>
        public static readonly string[] Share =
        {
            ".....##",
            "...##.#",
            "##.#.##",
            "##.....",
            "...#.##",
            ".....##",
        };

        /// <summary>아래를 가리키는 화살표. 튜토리얼에서 누를 곳을 짚는다.</summary>
        public static readonly string[] Pointer =
        {
            "..###..",
            "..###..",
            "..###..",
            "#######",
            ".#####.",
            "..###..",
            "...#...",
        };

        public static readonly string[] Sparkle =
        {
            "..#..",
            "..#..",
            "##.##",
            "..#..",
            "..#..",
        };

        public static Sprite Digit(char c)
        {
            return SpriteFactory.Bitmap("glyph" + c, digits[c]);
        }

        /// <summary>
        /// 글자 둘레를 한 도트씩 두른 테두리 그림. 글자와 같은 눈금에 맞춰 글자 뒤에 깔면, 칸 색과 같은 색 숫자도 읽힌다.
        /// </summary>
        public static Sprite Outline(char c)
        {
            string[] rows = digits[c];
            int h = rows.Length;
            int w = rows[0].Length;
            var grown = new string[h + 2];
            for (int y = 0; y < h + 2; y++)
            {
                var line = new char[w + 2];
                for (int x = 0; x < w + 2; x++)
                {
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                    {
                        for (int dx = -1; dx <= 1 && !near; dx++)
                        {
                            int sy = y - 1 + dy;
                            int sx = x - 1 + dx;
                            near = sy >= 0 && sy < h && sx >= 0 && sx < w && rows[sy][sx] == '#';
                        }
                    }

                    line[x] = near ? '#' : '.';
                }

                grown[y] = new string(line);
            }

            return SpriteFactory.Bitmap("outline" + c, grown, h);
        }


        public static bool HasGlyph(char c)
        {
            return digits.ContainsKey(c);
        }

        public static Sprite Icon(string name, string[] rows)
        {
            return SpriteFactory.Bitmap("icon" + name, rows);
        }
    }
}
