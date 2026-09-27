using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 받아 온 아이콘(Assets/Resources/Icons). Google Material Icons(Apache 2.0, Icons/MaterialIcons-LICENSE.txt).
    /// 흰색으로 가져오므로(IconImporter) 색을 입혀 쓴다. 파일이 없으면 도트 아이콘으로 대신한다.
    /// </summary>
    public static class Icons
    {
        static Sprite settings;

        public static Sprite Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = Resources.Load<Sprite>("Icons/settings");
                }

                return settings != null ? settings : PixelGlyphs.Icon("gear", PixelGlyphs.Gear);
            }
        }
    }
}
