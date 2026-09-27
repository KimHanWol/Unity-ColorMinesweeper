using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 효과음·배경음악을 만드는 합성 도구. UnityEngine 객체를 만들지 않는 순수 계산이라 백그라운드 스레드에서도 쓸 수 있다.
    /// 소리의 결은 "나무 건반(마림바)"으로 통일했다. 배음이 적고 빨리 사라져서 여러 번 반복해 들어도 귀가 피곤하지 않다.
    /// </summary>
    public static class SoundSynth
    {
        public const float TwoPi = Mathf.PI * 2f;

        /// <summary>C 장조 5음 음계(도레미솔라)를 G4 부터 두 옥타브 남짓. 어떤 순서로 겹쳐도 불협화음이 나지 않는다.</summary>
        public static readonly float[] Pentatonic =
        {
            392.00f, 440.00f, 523.25f, 587.33f, 659.25f, 783.99f, 880.00f, 1046.50f, 1174.66f, 1318.51f, 1567.98f,
        };

        public static float NoteFrequency(int midi)
        {
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }

        /// <summary>
        /// 마림바 한 음. 기음에 약한 4배음(실제 마림바 막대의 배음 비율 3.93)과 아주 짧은 타격음을 섞는다.
        /// decay 가 클수록 짧게 끊긴다.
        /// </summary>
        public static float Marimba(float t, float frequency, float decay = 7f)
        {
            if (t < 0f)
            {
                return 0f;
            }

            float attack = Mathf.Clamp01(t / 0.003f);
            float body = Mathf.Sin(TwoPi * frequency * t) * Mathf.Exp(-t * decay)
                         + 0.25f * Mathf.Sin(TwoPi * frequency * 3.93f * t) * Mathf.Exp(-t * decay * 3.2f)
                         + 0.06f * Mathf.Sin(TwoPi * frequency * 9.2f * t) * Mathf.Exp(-t * decay * 7f);
            return body * attack;
        }

        /// <summary>종소리. 배음이 길게 남아 반짝이는 느낌(별, 완성)에 쓴다.</summary>
        public static float Bell(float t, float frequency, float decay = 4f)
        {
            if (t < 0f)
            {
                return 0f;
            }

            float attack = Mathf.Clamp01(t / 0.002f);
            return attack * Mathf.Exp(-t * decay) * (Mathf.Sin(TwoPi * frequency * t)
                                                    + 0.4f * Mathf.Sin(TwoPi * frequency * 2.76f * t) * Mathf.Exp(-t * 3f)
                                                    + 0.2f * Mathf.Sin(TwoPi * frequency * 5.4f * t) * Mathf.Exp(-t * 6f));
        }

        /// <summary>부드러운 패드. 천천히 올라오고 천천히 사라지는 사인+삼각파.</summary>
        public static float Pad(float t, float frequency, float length)
        {
            if (t < 0f || t > length + 1.2f)
            {
                return 0f;
            }

            float env = Mathf.Clamp01(t / 0.35f) * (t > length ? Mathf.Exp(-(t - length) * 4f) : 1f);
            float phase = frequency * t;
            float triangle = 1f - 4f * Mathf.Abs(phase - Mathf.Floor(phase + 0.5f));
            float vibrato = 1f + 0.002f * Mathf.Sin(TwoPi * 4.5f * t);
            return env * (0.7f * Mathf.Sin(TwoPi * frequency * vibrato * t) + 0.3f * triangle);
        }

        /// <summary>결정적인 난수 잡음(-1~1). 같은 소리가 매번 같게 나오도록 System.Random 대신 해시를 쓴다.</summary>
        public static float Noise(int i)
        {
            unchecked
            {
                uint x = (uint)i * 747796405u + 2891336453u;
                x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u;
                x = (x >> 22) ^ x;
                return x / (float)uint.MaxValue * 2f - 1f;
            }
        }

        /// <summary>seconds 길이의 소리를 만들어 AudioClip 으로. 끝 10ms 는 줄여서 뚝 끊기는 잡음이 없게 한다.</summary>
        public static AudioClip Clip(string name, float seconds, Func<float, float> wave, int sampleRate = 44100)
        {
            int length = Mathf.CeilToInt(seconds * sampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float fade = Mathf.Clamp01((seconds - t) / 0.01f);
                data[i] = Mathf.Clamp(wave(t) * fade, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
