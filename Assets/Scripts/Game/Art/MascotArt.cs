using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    public enum MascotMood
    {
        Idle,
        Blink,
        Happy,
        Shock,
        Sad,
    }

    /// <summary>
    /// 마스코트(치즈 고양이 얼굴) 도트 그림. 표정은 눈과 입이 있는 줄만 바꿔 그린다.
    /// K 외곽선, O 털, D 이마 무늬, W 흰 털과 눈 반짝임, P 귀·볼·코, E 눈, B 눈물.
    /// 다른 동물로 바꾸려면 여기 그림과 색만 바꾸면 된다(<see cref="Mascot"/> 의 움직임은 그대로 쓴다).
    /// </summary>
    public static class MascotArt
    {
        static readonly string[] Base =
        {
            ".K...........K.",
            "KOK.........KOK",
            "KOOK.......KOOK",
            "KOPOKKKKKKKOPOK",
            "KOOOOODODOOOOOK",
            "KOOOOOOOOOOOOOK",
            "KOOOWEOOOWEOOOK",
            "KOOOEEOOOEEOOOK",
            "KOPOOWWPWWOOPOK",
            "KOOOOWWWWWOOOOK",
            ".KOOOOOOOOOOOK.",
            "..KKOOOOOOOKK..",
            "....KKKKKKK....",
        };

        static readonly Dictionary<char, Color32> Colors = new Dictionary<char, Color32>
        {
            ['K'] = new Color32(0x4A, 0x3B, 0x52, 0xFF),
            ['O'] = new Color32(0xF6, 0xA9, 0x5B, 0xFF),
            ['D'] = new Color32(0xE0, 0x8A, 0x3C, 0xFF),
            ['W'] = new Color32(0xFF, 0xF6, 0xE9, 0xFF),
            ['P'] = new Color32(0xFF, 0x9F, 0xB2, 0xFF),
            ['E'] = new Color32(0x3A, 0x2E, 0x45, 0xFF),
            ['B'] = new Color32(0x6C, 0xC4, 0xF5, 0xFF),
        };

        /// <summary>표정마다 바꿔 그리는 줄(줄 번호 → 그림).</summary>
        static readonly Dictionary<MascotMood, Dictionary<int, string>> Changes =
            new Dictionary<MascotMood, Dictionary<int, string>>
            {
                [MascotMood.Idle] = new Dictionary<int, string>(),
                [MascotMood.Blink] = new Dictionary<int, string>
                {
                    [6] = "KOOOOOOOOOOOOOK",
                    [7] = "KOOEEEOOOEEEOOK",
                },
                [MascotMood.Happy] = new Dictionary<int, string>
                {
                    [6] = "KOOOEEOOOEEOOOK",
                    [7] = "KOOEOOEOEOOEOOK",
                    [9] = "KOOOOWWPWWOOOOK",
                },
                [MascotMood.Shock] = new Dictionary<int, string>
                {
                    [6] = "KOOOWWOOOWWOOOK",
                    [7] = "KOOOWEOOOEWOOOK",
                    [9] = "KOOOOWWKWWOOOOK",
                },
                [MascotMood.Sad] = new Dictionary<int, string>
                {
                    [6] = "KOOOOOOOOOOOOOK",
                    [7] = "KOOOEEOOOEEOOOK",
                    [8] = "KOPOBWWPWWOOPOK",
                    [9] = "KOOOBWWWWWOOOOK",
                },
            };

        /// <summary>그림의 가로/세로 비(가로가 조금 더 넓다).</summary>
        public static float Aspect => (float)Base[0].Length / Base.Length;

        /// <summary>높이 1 유닛짜리 얼굴 그림.</summary>
        public static Sprite Face(MascotMood mood)
        {
            var rows = (string[])Base.Clone();
            foreach (KeyValuePair<int, string> change in Changes[mood])
            {
                rows[change.Key] = change.Value;
            }

            return SpriteFactory.ColorBitmap("mascot" + mood, rows, Colors);
        }
    }
}
