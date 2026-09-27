using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 받아 온 아이콘(Assets/Resources/Icons). Google Material Icons(Apache 2.0, Icons/MaterialIcons-LICENSE.txt).
    /// 흰색으로 가져오므로(IconImporter) 색을 입혀 쓴다. 파일이 없으면 도트 아이콘으로 대신한다.
    /// </summary>
    public static class Icons
    {
        static readonly System.Collections.Generic.Dictionary<string, Sprite> cache =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite Settings => Load("settings", PixelGlyphs.Gear);
        public static Sprite Back => Load("back", PixelGlyphs.Back);
        public static Sprite Play => Load("play", PixelGlyphs.Play);
        public static Sprite Retry => Load("retry", PixelGlyphs.Retry);

        /// <summary>광고 보기 버튼(ondemand_video).</summary>
        public static Sprite Ad => Load("ad", PixelGlyphs.Film);
        public static Sprite Lock => Load("lock", PixelGlyphs.Lock);
        public static Sprite Check => Load("check", PixelGlyphs.Check);
        public static Sprite Heart => Load("heart", PixelGlyphs.Heart);
        public static Sprite Star => Load("star", PixelGlyphs.Star);

        /// <summary>결과 공유(share).</summary>
        public static Sprite Share => Load("share", PixelGlyphs.Share);

        static Sprite Load(string name, string[] fallback)
        {
            if (!cache.TryGetValue(name, out Sprite sprite) || sprite == null)
            {
                sprite = Resources.Load<Sprite>("Icons/" + name);
                cache[name] = sprite;
            }

            return sprite != null ? sprite : PixelGlyphs.Icon(name, fallback);
        }
    }
}
