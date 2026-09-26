using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>플레이어 설정. 기기에 저장되고, 바뀌면 <see cref="Changed"/> 로 알린다.</summary>
    public static class Settings
    {
        const string SoundKey = "settings.sound";
        const string HapticsKey = "settings.haptics";
        const string RemainingKey = "settings.showRemaining";

        public static event Action Changed;

        public static bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set => Set(SoundKey, value);
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set => Set(HapticsKey, value);
        }

        /// <summary>
        /// 켜면 칩이 "주변 전체 개수" 대신 "아직 안 열린 칸 중 그 색의 개수"를 보여 주고, 0 이 되면 사라진다.
        /// 머릿속으로 빼는 수고가 없어져 쉬워지므로 기본은 꺼 둔다(대신 다 채워진 칩은 흐려진다).
        /// </summary>
        public static bool ShowRemaining
        {
            get => PlayerPrefs.GetInt(RemainingKey, 0) == 1;
            set => Set(RemainingKey, value);
        }

        /// <summary>저장된 값을 효과음·진동에 반영한다. 시작할 때와 바뀔 때 부른다.</summary>
        public static void Apply()
        {
            Sfx.Enabled = Sound;
            Haptics.Enabled = Vibration;
        }

        static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }
    }
}
