using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 효과음을 코드로 합성한다. 사운드 에셋이 없어도 손맛이 나게 하고, 나중에 녹음한 소리로 바꿔 끼우기 쉽게 한 곳에 모은다.
    /// 펼침으로 여러 칸이 열리면 펜타토닉 음계를 따라 올라가서 듣기 좋은 "도르르" 소리가 난다.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int SampleRate = 44100;

        static readonly float[] Pentatonic = { 523.25f, 587.33f, 659.25f, 783.99f, 880f, 1046.5f, 1174.66f, 1318.51f, 1567.98f };

        public static Sfx Instance { get; private set; }
        public static bool Enabled = true;

        AudioSource source;
        AudioClip[] pops;
        AudioClip wrong;
        AudioClip tap;
        AudioClip clear;
        AudioClip heart;

        public static void Create(Transform parent)
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            Instance = go.AddComponent<Sfx>();
        }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            pops = new AudioClip[Pentatonic.Length];
            for (int i = 0; i < pops.Length; i++)
            {
                pops[i] = Synth("pop" + i, 0.16f, t => Bell(t, Pentatonic[i], 26f));
            }

            tap = Synth("tap", 0.05f, t => Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 90f) * 0.4f);
            wrong = Synth("wrong", 0.28f, t =>
            {
                float f = Mathf.Lerp(220f, 140f, t / 0.28f);
                float wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.35f + Mathf.Sin(2f * Mathf.PI * f * 1.01f * t) * 0.4f;
                return wave * Mathf.Exp(-t * 9f) * 0.5f;
            });
            heart = Synth("heart", 0.3f, t => Bell(t, 392f, 12f) * 0.8f);
            clear = Synth("clear", 1.1f, t =>
            {
                float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                float sum = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float start = n * 0.09f;
                    if (t >= start)
                    {
                        sum += Bell(t - start, notes[n], n == notes.Length - 1 ? 4f : 9f);
                    }
                }

                return sum * 0.45f;
            });
        }

        public void Pop(int step)
        {
            Play(pops[Mathf.Clamp(step, 0, pops.Length - 1)], 0.7f);
        }

        public void Tap() => Play(tap, 0.5f);
        public void Wrong() => Play(wrong, 0.9f);
        public void LoseHeart() => Play(heart, 0.6f);
        public void Clear() => Play(clear, 0.9f);

        void Play(AudioClip clip, float volume)
        {
            if (Enabled && clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }

        /// <summary>배음을 살짝 섞은 사인파에 빠른 어택과 지수 감쇠. 실로폰 비슷한 소리.</summary>
        static float Bell(float t, float frequency, float decay)
        {
            float attack = Mathf.Clamp01(t / 0.004f);
            float body = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) +
                         0.1f * Mathf.Sin(2f * Mathf.PI * frequency * 3.01f * t);
            return body * attack * Mathf.Exp(-t * decay) * 0.5f;
        }

        static AudioClip Synth(string name, float seconds, System.Func<float, float> wave)
        {
            int length = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / SampleRate;
                // 끝에서 뚝 끊기는 잡음이 없도록 마지막 10ms 를 줄인다.
                float fade = Mathf.Clamp01((seconds - t) / 0.01f);
                data[i] = Mathf.Clamp(wave(t) * fade, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
