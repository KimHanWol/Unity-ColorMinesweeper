using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>플레이어 설정. 기기에 저장되고, 바뀌면 <see cref="Changed"/> 로 알린다.</summary>
    public static class Settings
    {
        const string MusicKey = "settings.musicVolume";
        const string SfxKey = "settings.sfxVolume";
        const string LegacySoundKey = "settings.sound";
        const string HapticsKey = "settings.haptics";
        const string RemainingKey = "settings.showRemaining";

        public static event Action Changed;

        /// <summary>배경음악 볼륨(0~1).</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.5f);
            set => SetFloat(MusicKey, value);
        }

        /// <summary>효과음 볼륨(0~1). 예전 켬/끔 설정을 끔으로 해 둔 사람은 0 에서 시작한다.</summary>
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, PlayerPrefs.GetInt(LegacySoundKey, 1) == 1 ? 0.8f : 0f);
            set => SetFloat(SfxKey, value);
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
            set => SetBool(HapticsKey, value);
        }

        /// <summary>
        /// 켜면 칩이 "주변 전체 개수" 대신 "아직 안 열린 칸 중 그 색의 개수"를 보여 주고, 0 이 되면 사라진다.
        /// 머릿속으로 빼는 수고가 없어져 쉬워지므로 기본은 꺼 둔다(대신 다 채워진 칩은 흐려진다).
        /// </summary>
        public static bool ShowRemaining
        {
            get => PlayerPrefs.GetInt(RemainingKey, 0) == 1;
            set => SetBool(RemainingKey, value);
        }

        /// <summary>저장된 값을 소리·진동에 반영한다. 시작할 때와 바뀔 때 부른다.</summary>
        public static void Apply()
        {
            Sfx.Volume = SfxVolume;
            Music.Volume = MusicVolume;
            Haptics.Enabled = Vibration;
        }

        static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            Apply();
            Changed?.Invoke();
        }

        static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>슬라이더를 놓았을 때처럼 한꺼번에 디스크에 쓸 때 부른다(끄는 동안 매 프레임 쓰지 않게).</summary>
        public static void Flush()
        {
            PlayerPrefs.Save();
        }
    }
}
