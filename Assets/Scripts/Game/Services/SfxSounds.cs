using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>효과음의 역할. 역할마다 목표 크기가 달라서, 자주 들리는 소리는 작게, 보상 소리는 조금 크게 맞춘다.</summary>
    public enum SfxRole
    {
        /// <summary>버튼, 색 고르기, 창 열고 닫기처럼 계속 들리는 소리.</summary>
        Soft,

        /// <summary>칸이 열리는 소리. 가장 자주 들리는 게임의 중심 소리.</summary>
        Reveal,

        /// <summary>틀림, 목숨 잃음처럼 알아채야 하는 소리.</summary>
        Feedback,

        /// <summary>완성, 별, 잠금 해제처럼 가끔 나오는 보상 소리.</summary>
        Reward,
    }

    /// <summary>효과음 하나의 정의. 파형만 담은 순수 계산이라 Unity 밖(도구, 테스트)에서도 그려 볼 수 있다.</summary>
    public sealed class SfxSound
    {
        public string Name;
        public float Seconds;
        public Func<float, float> Wave;
        public SfxRole Role;

        public SfxSound(string name, float seconds, SfxRole role, Func<float, float> wave)
        {
            Name = name;
            Seconds = seconds;
            Role = role;
            Wave = wave;
        }
    }

    /// <summary>
    /// 모든 효과음의 파형. 크기는 여기서 대충만 잡고, 실제 재생 크기는 <see cref="SoundSynth.Normalize"/> 가
    /// 역할별 목표 음량(<see cref="TargetLoudness"/>)에 맞춘다. 그래서 소리를 새로 바꿔도 밸런스가 흐트러지지 않는다.
    /// </summary>
    public static class SfxSounds
    {
        /// <summary>
        /// 역할별 목표 음량(dB, 100ms 구간 K-가중 음량의 최댓값). 칸 여는 소리를 기준으로
        /// 계속 들리는 소리는 8dB 작게, 알림은 1dB 크게, 보상은 3dB 크게 둔다.
        /// </summary>
        public static float TargetLoudness(SfxRole role)
        {
            switch (role)
            {
                case SfxRole.Soft:
                    return -21f;
                case SfxRole.Reveal:
                    return -13f;
                case SfxRole.Feedback:
                    return -12f;
                default:
                    return -10f;
            }
        }

        /// <summary>칸이 열리는 마림바 음. 5음 음계 순서대로.</summary>
        public static SfxSound[] Reveal()
        {
            float[] scale = SoundSynth.Pentatonic;
            var sounds = new SfxSound[scale.Length];
            for (int i = 0; i < scale.Length; i++)
            {
                float f = scale[i];
                sounds[i] = new SfxSound("reveal" + i, 0.45f, SfxRole.Reveal, t => SoundSynth.Marimba(t, f, 9f));
            }

            return sounds;
        }

        /// <summary>팔레트에서 색을 고를 때. 색마다 음이 조금씩 달라서 손에 익는다.</summary>
        public static SfxSound[] Select()
        {
            float[] scale = SoundSynth.Pentatonic;
            var sounds = new SfxSound[6];
            for (int i = 0; i < sounds.Length; i++)
            {
                float f = scale[4 + i % (scale.Length - 4)] * 2f;
                sounds[i] = new SfxSound("select" + i, 0.12f, SfxRole.Soft, t => SoundSynth.Marimba(t, f, 40f));
            }

            return sounds;
        }

        public static SfxSound Tap() => new SfxSound("tap", 0.06f, SfxRole.Soft, t =>
            (Mathf.Sin(SoundSynth.TwoPi * 1250f * t) * 0.6f + SoundSynth.Noise((int)(t * 44100f)) * 0.15f)
            * Mathf.Exp(-t * 90f));

        /// <summary>틀렸을 때: 날카로운 경고음 대신 둔탁한 "통" 소리. 음이 살짝 내려가며 아쉬운 느낌만 준다.</summary>
        public static SfxSound Wrong() => new SfxSound("wrong", 0.32f, SfxRole.Feedback, t =>
        {
            float f = Mathf.Lerp(196f, 147f, Mathf.Clamp01(t / 0.25f));
            return (Mathf.Sin(SoundSynth.TwoPi * f * t) + 0.35f * Mathf.Sin(SoundSynth.TwoPi * f * 2.01f * t))
                   * Mathf.Exp(-t * 11f) * Mathf.Clamp01(t / 0.004f);
        });

        public static SfxSound LoseHeart() => new SfxSound("heart", 0.6f, SfxRole.Feedback, t =>
            SoundSynth.Marimba(t, 329.63f, 6f) + SoundSynth.Marimba(t - 0.13f, 261.63f, 5f));

        public static SfxSound Clear() => new SfxSound("clear", 1.8f, SfxRole.Reward, t =>
        {
            float[] arp = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.51f };
            float sum = 0f;
            for (int n = 0; n < arp.Length; n++)
            {
                sum += SoundSynth.Marimba(t - n * 0.085f, arp[n], n == arp.Length - 1 ? 3f : 7f);
            }

            return sum * 0.32f + SoundSynth.Bell(t - 0.42f, 2093f, 3.5f) * 0.25f;
        });

        /// <summary>별이 하나씩 켜질 때. 도 → 미 → 솔로 올라간다.</summary>
        public static SfxSound[] Star()
        {
            float[] notes = { 1046.5f, 1318.51f, 1567.98f };
            var sounds = new SfxSound[notes.Length];
            for (int i = 0; i < notes.Length; i++)
            {
                float f = notes[i];
                sounds[i] = new SfxSound("star" + i, 0.7f, SfxRole.Reward, t => SoundSynth.Bell(t, f, 5f));
            }

            return sounds;
        }

        /// <summary>창이 열릴 때 낮은 음에서 위로 "뽁". 낮은 음역의 사인파라 귀를 찌르지 않는다.</summary>
        public static SfxSound Open() => new SfxSound("open", 0.3f, SfxRole.Soft, t => Bubble(t, 260f, 520f, 0.07f, 16f));

        /// <summary>창이 닫힐 때 위에서 아래로 "뿅".</summary>
        public static SfxSound Close() => new SfxSound("close", 0.3f, SfxRole.Soft, t => Bubble(t, 480f, 240f, 0.08f, 18f));

        public static SfxSound Revive() => new SfxSound("revive", 0.7f, SfxRole.Reward, t =>
            SoundSynth.Marimba(t, 523.25f, 8f) + SoundSynth.Marimba(t - 0.09f, 659.25f, 8f)
                                                + SoundSynth.Marimba(t - 0.18f, 1046.5f, 5f));

        /// <summary>자물쇠가 풀릴 때: 금속성 "철컥"(짧은 잡음 두 번) 뒤에 위로 올라가는 종소리.</summary>
        public static SfxSound Unlock() => new SfxSound("unlock", 0.9f, SfxRole.Reward, t =>
        {
            float click = 0f;
            foreach (float at in new[] { 0f, 0.07f })
            {
                float k = t - at;
                if (k >= 0f && k < 0.03f)
                {
                    click += SoundSynth.Noise((int)(t * 44100f)) * Mathf.Exp(-k * 160f) * 0.5f
                             + Mathf.Sin(SoundSynth.TwoPi * 2600f * k) * Mathf.Exp(-k * 120f) * 0.3f;
                }
            }

            float chime = SoundSynth.Bell(t - 0.14f, 1046.5f, 6f) + SoundSynth.Bell(t - 0.24f, 1567.98f, 4f);
            return click + chime * 0.28f;
        });

        /// <summary>잠긴 칸을 눌렀을 때: 짧고 둔한 "딱".</summary>
        public static SfxSound Locked() => new SfxSound("locked", 0.12f, SfxRole.Soft, t =>
            (Mathf.Sin(SoundSynth.TwoPi * 330f * t) + SoundSynth.Noise((int)(t * 44100f)) * 0.3f) * Mathf.Exp(-t * 45f));

        /// <summary>완성해서 이름이 드러날 때 반짝이는 소리.</summary>
        public static SfxSound NameReveal() => new SfxSound("name", 0.7f, SfxRole.Reward, t =>
        {
            float sum = 0f;
            float[] sparkle = { 1567.98f, 2093f, 1760f, 2637f };
            for (int n = 0; n < sparkle.Length; n++)
            {
                sum += SoundSynth.Bell(t - n * 0.07f, sparkle[n], 9f);
            }

            return sum;
        });

        /// <summary>
        /// 음높이가 미끄러지는 둥근 한 음(사인파). fromHz 에서 toHz 로 glideSeconds 동안 옮겨 간다.
        /// 위로 미끄러지면 열리는 느낌, 아래로 미끄러지면 닫히는 느낌이라 두 소리가 헷갈리지 않는다.
        /// </summary>
        static float Bubble(float t, float fromHz, float toHz, float glideSeconds, float decay)
        {
            if (t < 0f)
            {
                return 0f;
            }

            // 지수로 미끄러지는 주파수를 적분한 위상. 주파수만 바꾸면 파형이 튀므로 위상을 이어 준다.
            float ratio = toHz / fromHz;
            float logRatio = Mathf.Log(ratio);
            float phase = t < glideSeconds
                ? fromHz * glideSeconds / logRatio * (Mathf.Pow(ratio, t / glideSeconds) - 1f)
                : fromHz * glideSeconds / logRatio * (ratio - 1f) + toHz * (t - glideSeconds);
            float attack = Mathf.Clamp01(t / 0.006f);
            return attack * Mathf.Exp(-t * decay) * Mathf.Sin(SoundSynth.TwoPi * phase);
        }
    }
}
