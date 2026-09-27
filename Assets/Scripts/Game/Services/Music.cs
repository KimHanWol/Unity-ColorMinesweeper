using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 배경음악. 퍼즐에 집중을 방해하지 않도록 느리고 잔잔한 루프(84 BPM, C–Am–F–G 8마디)를 코드로 합성한다.
    /// 합성은 시작할 때 백그라운드 스레드에서 하고, 다 되면 조용히 페이드 인한다.
    /// 녹음한 곡으로 바꾸려면 <see cref="Compose"/> 대신 AudioClip 을 넣으면 된다.
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        const int SampleRate = 22050;
        const float Bpm = 84f;
        const int Bars = 8;
        const float BaseGain = 0.55f;

        static Music instance;
        static float volume = 0.5f;

        AudioSource source;
        Task<float[]> composing;
        float fade;
        bool ducked;

        /// <summary>배경음악 볼륨(0~1). 설정에서 바꾼다.</summary>
        public static float Volume
        {
            get => volume;
            set
            {
                volume = value;
                instance?.ApplyVolume();
            }
        }

        public static void Create(Transform parent)
        {
            var go = new GameObject("Music");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<Music>();
        }

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            composing = Task.Run(Compose);
        }

        void Update()
        {
            if (composing != null && composing.IsCompleted)
            {
                if (composing.Status == TaskStatus.RanToCompletion)
                {
                    float[] data = composing.Result;
                    AudioClip clip = AudioClip.Create("bgm", data.Length, 1, SampleRate, false);
                    clip.SetData(data, 0);
                    source.clip = clip;
                    source.Play();
                }
                else
                {
                    Debug.LogError("[Music] 배경음악을 만들지 못했습니다: " + composing.Exception);
                }

                composing = null;
            }

            if (source.isPlaying && fade < 1f)
            {
                fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / 2.5f);
                ApplyVolume();
            }
        }

        /// <summary>광고가 떠 있는 동안 배경음악을 끈다(광고 소리와 겹치지 않게).</summary>
        public static void Duck(bool on)
        {
            if (instance != null)
            {
                instance.ducked = on;
                instance.ApplyVolume();
            }
        }

        void ApplyVolume()
        {
            if (source != null)
            {
                source.volume = ducked ? 0f : volume * BaseGain * fade;
            }
        }

        /// <summary>
        /// 한 바퀴 길이의 버퍼에 음을 더한다. 끝을 넘친 꼬리는 버퍼 앞쪽에 겹쳐 더해서 이음새 없이 반복된다.
        /// </summary>
        static float[] Compose()
        {
            float beat = 60f / Bpm;
            float bar = beat * 4f;
            int length = Mathf.RoundToInt(bar * Bars * SampleRate);
            var buffer = new float[length];

            // 마디마다 화음(MIDI 번호). Cmaj7, Am7, Fmaj7, G6 를 두 번.
            var chords = new List<int[]>
            {
                new[] { 48, 55, 59, 64 }, new[] { 45, 52, 55, 60 }, new[] { 41, 48, 52, 57 }, new[] { 43, 50, 55, 59 },
                new[] { 48, 55, 59, 64 }, new[] { 45, 52, 55, 60 }, new[] { 41, 48, 52, 57 }, new[] { 43, 50, 55, 62 },
            };

            // 아르페지오 무늬: 8분음표 8개 중 쉬는 자리(-1)를 둬서 숨 쉴 틈을 만든다.
            int[] pattern = { 1, 2, 3, -1, 2, 3, 1, -1 };

            for (int b = 0; b < Bars; b++)
            {
                int[] chord = chords[b];
                float barStart = b * bar;

                foreach (int note in chord)
                {
                    float f = SoundSynth.NoteFrequency(note + 12);
                    Add(buffer, barStart, bar + 1.2f, t => SoundSynth.Pad(t, f, bar) * 0.05f);
                }

                float root = SoundSynth.NoteFrequency(chord[0] - 12);
                Add(buffer, barStart, 1.8f, t => SoundSynth.Marimba(t, root, 2.2f) * 0.22f);
                Add(buffer, barStart + beat * 2f, 1.8f, t => SoundSynth.Marimba(t, root, 2.6f) * 0.15f);

                for (int step = 0; step < pattern.Length; step++)
                {
                    if (pattern[step] < 0)
                    {
                        continue;
                    }

                    float f = SoundSynth.NoteFrequency(chord[pattern[step]] + 24);
                    float accent = step == 0 || step == 4 ? 1f : 0.75f;
                    Add(buffer, barStart + step * beat / 2f, 0.9f, t => SoundSynth.Marimba(t, f, 6f) * 0.07f * accent);
                }

                // 아주 작은 셰이커: 뒷박에만.
                for (int step = 1; step < 8; step += 2)
                {
                    int seed = b * 16 + step * 997;
                    Add(buffer, barStart + step * beat / 2f, 0.06f,
                        t => SoundSynth.Noise(seed + (int)(t * SampleRate)) * Mathf.Exp(-t * 70f) * 0.012f);
                }
            }

            float peak = 0f;
            foreach (float s in buffer)
            {
                peak = Mathf.Max(peak, Mathf.Abs(s));
            }

            if (peak > 0.0001f)
            {
                float gain = 0.8f / peak;
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] *= gain;
                }
            }

            return buffer;
        }

        static void Add(float[] buffer, float start, float seconds, System.Func<float, float> wave)
        {
            int offset = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(seconds * SampleRate);
            for (int i = 0; i < count; i++)
            {
                int index = (offset + i) % buffer.Length;
                buffer[index] += wave((float)i / SampleRate);
            }
        }
    }
}
