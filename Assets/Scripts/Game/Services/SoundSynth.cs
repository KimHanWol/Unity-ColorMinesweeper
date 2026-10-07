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

        /// <summary>
        /// 동물 울음소리 같은 한 마디. 음높이가 startHz → peakHz(peakAt 초) → endHz 로 미끄러지고, 처음에는 밝게(배음이 많게)
        /// 시작해 끝으로 갈수록 둥글어진다. 올라갔다 내려오면 "야옹", 올라가기만 하면 "먕?" 처럼 들린다.
        /// trill 을 주면 소리가 잘게 떨려 "므르릉" 하는 느낌이 난다.
        /// </summary>
        public static float Voice(float t, float seconds, float startHz, float peakHz, float endHz, float peakAt,
            float brightStart, float brightEnd, float trill = 0f)
        {
            if (t < 0f || t > seconds)
            {
                return 0f;
            }

            // 구간마다 주파수가 직선으로 변하므로 위상은 그 적분(2차식)이다. 주파수만 바꿔 곱하면 파형이 튄다.
            float phase;
            if (t < peakAt)
            {
                phase = startHz * t + (peakHz - startHz) * t * t / (2f * peakAt);
            }
            else
            {
                float u = t - peakAt;
                phase = (startHz + peakHz) * peakAt / 2f + peakHz * u + (endHz - peakHz) * u * u / (2f * (seconds - peakAt));
            }

            float k = t / seconds;
            float bright = Mathf.Lerp(brightStart, brightEnd, k);
            float angle = TwoPi * phase;
            float tone = Mathf.Sin(angle) + bright * (0.6f * Mathf.Sin(2f * angle) + 0.35f * Mathf.Sin(3f * angle)
                                                       + 0.15f * Mathf.Sin(4f * angle));
            float attack = Mathf.Clamp01(t / 0.04f);
            float release = k > 0.6f ? 0.5f + 0.5f * Mathf.Cos((k - 0.6f) / 0.4f * Mathf.PI) : 1f;
            float flutter = 1f - trill * (0.5f + 0.5f * Mathf.Sin(TwoPi * 28f * t));
            return tone * attack * release * flutter;
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

        /// <summary>seconds 길이로 파형을 그린다. 끝 10ms 는 줄여서 뚝 끊기는 잡음이 없게 한다.</summary>
        public static float[] Render(float seconds, Func<float, float> wave, int sampleRate = 44100)
        {
            int length = Mathf.CeilToInt(seconds * sampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / sampleRate;
                float fade = Mathf.Clamp01((seconds - t) / 0.01f);
                data[i] = wave(t) * fade;
            }

            return data;
        }

        /// <summary>
        /// 귀에 들리는 크기(dB). 방송 음량 기준(ITU BS.1770)의 K-가중 필터로 사람 귀가 둔한 저음은 덜,
        /// 예민한 고음은 더 세게 친 뒤, 100ms 구간마다 평균 세기를 재서 가장 큰 구간을 돌려준다.
        /// 효과음은 짧아서 곡 전체 평균 대신 가장 큰 순간을 본다.
        /// </summary>
        public static float Loudness(float[] data, int sampleRate = 44100)
        {
            double[] weighted = KWeight(data, sampleRate);
            int window = sampleRate / 10;
            int hop = sampleRate / 100;
            double best = 1e-12;
            for (int start = 0; start == 0 || start + window <= weighted.Length; start += hop)
            {
                double sum = 0;
                for (int i = start; i < start + window; i++)
                {
                    double v = i < weighted.Length ? weighted[i] : 0.0;
                    sum += v * v;
                }

                best = Math.Max(best, sum / window);
            }

            return (float)(-0.691 + 10.0 * Math.Log10(best));
        }

        /// <summary>목표 크기(dB)에 맞춰 키우거나 줄인다. 찢어지지 않게 가장 큰 값이 0.98 을 넘지 않는 선에서 멈춘다.</summary>
        public static float[] Normalize(float[] data, float targetLoudness, int sampleRate = 44100)
        {
            float gain = Mathf.Pow(10f, (targetLoudness - Loudness(data, sampleRate)) / 20f);
            float peak = 0f;
            foreach (float v in data)
            {
                peak = Mathf.Max(peak, Mathf.Abs(v));
            }

            if (peak * gain > 0.98f)
            {
                gain = 0.98f / peak;
            }

            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= gain;
            }

            return data;
        }

        static double[] KWeight(float[] data, int sampleRate)
        {
            // 1단: 고음을 약 4dB 올리는 셸빙 필터(머리가 소리를 받는 효과). 2단: 아주 낮은 저음을 거르는 필터.
            double w0 = 2 * Math.PI * 1681.974450955532 / sampleRate;
            double a = Math.Pow(10, 3.99984385397 / 40);
            double alpha = Math.Sin(w0) / (2 * 0.7071752369554193);
            double cos = Math.Cos(w0);
            double sqrtA = Math.Sqrt(a);
            double[] shelf = Biquad(
                a * ((a + 1) + (a - 1) * cos + 2 * sqrtA * alpha),
                -2 * a * ((a - 1) + (a + 1) * cos),
                a * ((a + 1) + (a - 1) * cos - 2 * sqrtA * alpha),
                (a + 1) - (a - 1) * cos + 2 * sqrtA * alpha,
                2 * ((a - 1) - (a + 1) * cos),
                (a + 1) - (a - 1) * cos - 2 * sqrtA * alpha);

            w0 = 2 * Math.PI * 38.13547087613982 / sampleRate;
            alpha = Math.Sin(w0) / (2 * 0.5003270373253953);
            cos = Math.Cos(w0);
            double[] highPass = Biquad((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);

            var output = new double[data.Length];
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0, z1 = 0, z2 = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double x = data[i];
                double y = shelf[0] * x + shelf[1] * x1 + shelf[2] * x2 - shelf[3] * y1 - shelf[4] * y2;
                x2 = x1;
                x1 = x;
                double z = highPass[0] * y + highPass[1] * y1 + highPass[2] * y2 - highPass[3] * z1 - highPass[4] * z2;
                y2 = y1;
                y1 = y;
                z2 = z1;
                z1 = z;
                output[i] = z;
            }

            return output;
        }

        /// <summary>a0 로 나눈 계수(b0, b1, b2, a1, a2).</summary>
        static double[] Biquad(double b0, double b1, double b2, double a0, double a1, double a2)
        {
            return new[] { b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0 };
        }

        /// <summary>효과음을 역할별 목표 크기에 맞춰 AudioClip 으로 만든다.</summary>
        public static AudioClip Clip(SfxSound sound, int sampleRate = 44100)
        {
            float[] data = Normalize(Render(sound.Seconds, sound.Wave, sampleRate), SfxSounds.TargetLoudness(sound.Role),
                sampleRate);
            AudioClip clip = AudioClip.Create(sound.Name, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
