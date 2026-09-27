using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 효과음. 사운드 파일 없이 <see cref="SoundSynth"/> 로 시작할 때 만들어 두고, 녹음한 소리로 바꿔 끼우기 쉽게 한 곳에 모았다.
    ///
    /// 칸이 열리는 소리(<see cref="Reveal"/>)가 가장 자주 들리므로 가장 공을 들였다.
    /// - 부드러운 마림바 한 음이라 수백 번 들어도 거슬리지 않는다.
    /// - 5음 음계만 써서 연달아 겹쳐도 화음이 된다.
    /// - 빠르게 이어 맞히면 음이 한 칸씩 올라가고(콤보), 잠깐 쉬면 처음 음으로 돌아온다.
    /// - 펼침으로 여러 칸이 열리면 깊이만큼 음이 올라가 "도르르" 굴러간다.
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        const float ComboWindow = 1.4f;
        const int MaxCombo = 5;

        public static Sfx Instance { get; private set; }

        /// <summary>효과음 볼륨(0~1). 설정에서 바꾼다.</summary>
        public static float Volume = 0.8f;

        AudioSource source;
        AudioClip[] reveal;
        AudioClip[] select;
        AudioClip tap;
        AudioClip wrong;
        AudioClip heart;
        AudioClip clear;
        AudioClip[] star;
        AudioClip open;
        AudioClip close;
        AudioClip revive;
        AudioClip nameReveal;
        AudioClip unlock;
        AudioClip locked;

        /// <summary>빈 구역이 한꺼번에 열릴 때의 소리 파일(Resources/Sounds/area). 없으면 마림바가 굴러가는 소리로 대신한다.</summary>
        AudioClip area;

        /// <summary>넣어 둔 소리 파일이 있으면 true. 펼침 소리를 파일 한 번으로 바꾼다.</summary>
        public bool HasAreaClip => area != null;

        float lastReveal = -10f;
        int combo;

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
            area = Resources.Load<AudioClip>("Sounds/area");

            float[] scale = SoundSynth.Pentatonic;
            reveal = new AudioClip[scale.Length];
            for (int i = 0; i < scale.Length; i++)
            {
                float f = scale[i];
                reveal[i] = SoundSynth.Clip("reveal" + i, 0.45f, t => SoundSynth.Marimba(t, f, 9f) * 0.55f);
            }

            select = new AudioClip[6];
            for (int i = 0; i < select.Length; i++)
            {
                float f = scale[4 + i % (scale.Length - 4)] * 2f;
                select[i] = SoundSynth.Clip("select" + i, 0.12f, t => SoundSynth.Marimba(t, f, 40f) * 0.35f);
            }

            tap = SoundSynth.Clip("tap", 0.06f, t =>
                (Mathf.Sin(SoundSynth.TwoPi * 1250f * t) * 0.6f + SoundSynth.Noise((int)(t * 44100f)) * 0.15f)
                * Mathf.Exp(-t * 90f) * 0.45f);

            // 틀렸을 때: 날카로운 경고음 대신 둔탁한 "통" 소리. 음이 살짝 내려가며 아쉬운 느낌만 준다.
            wrong = SoundSynth.Clip("wrong", 0.32f, t =>
            {
                float f = Mathf.Lerp(196f, 147f, Mathf.Clamp01(t / 0.25f));
                return (Mathf.Sin(SoundSynth.TwoPi * f * t) + 0.35f * Mathf.Sin(SoundSynth.TwoPi * f * 2.01f * t))
                       * Mathf.Exp(-t * 11f) * Mathf.Clamp01(t / 0.004f) * 0.6f;
            });
            heart = SoundSynth.Clip("heart", 0.6f, t =>
                (SoundSynth.Marimba(t, 329.63f, 6f) + SoundSynth.Marimba(t - 0.13f, 261.63f, 5f)) * 0.4f);

            clear = SoundSynth.Clip("clear", 1.8f, t =>
            {
                float[] arp = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.51f };
                float sum = 0f;
                for (int n = 0; n < arp.Length; n++)
                {
                    sum += SoundSynth.Marimba(t - n * 0.085f, arp[n], n == arp.Length - 1 ? 3f : 7f);
                }

                float shimmer = SoundSynth.Bell(t - 0.42f, 2093f, 3.5f) * 0.25f;
                return (sum * 0.32f + shimmer) * 0.9f;
            });

            star = new AudioClip[3];
            float[] starNotes = { 1046.5f, 1318.51f, 1567.98f };
            for (int i = 0; i < star.Length; i++)
            {
                float f = starNotes[i];
                star[i] = SoundSynth.Clip("star" + i, 0.7f, t => SoundSynth.Bell(t, f, 5f) * 0.35f);
            }

            // 창이 열릴 때 낮은 음에서 위로 "뽁", 닫힐 때 위에서 아래로 "뿅". 낮은 음역의 사인파라 귀를 찌르지 않는다.
            open = SoundSynth.Clip("open", 0.3f, t => Bubble(t, 260f, 520f, 0.07f, 16f) * 0.3f);
            close = SoundSynth.Clip("close", 0.3f, t => Bubble(t, 480f, 240f, 0.08f, 18f) * 0.26f);
            revive = SoundSynth.Clip("revive", 0.7f, t =>
                (SoundSynth.Marimba(t, 523.25f, 8f) + SoundSynth.Marimba(t - 0.09f, 659.25f, 8f)
                 + SoundSynth.Marimba(t - 0.18f, 1046.5f, 5f)) * 0.35f);
            // 자물쇠가 풀릴 때: 금속성 "철컥"(짧은 잡음 두 번) 뒤에 위로 올라가는 종소리.
            unlock = SoundSynth.Clip("unlock", 0.9f, t =>
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
            // 잠긴 칸을 눌렀을 때: 짧고 둔한 "딱".
            locked = SoundSynth.Clip("locked", 0.12f, t =>
                (Mathf.Sin(SoundSynth.TwoPi * 330f * t) + SoundSynth.Noise((int)(t * 44100f)) * 0.3f) * Mathf.Exp(-t * 45f) * 0.45f);
            nameReveal = SoundSynth.Clip("name", 0.7f, t =>
            {
                float sum = 0f;
                float[] sparkle = { 1567.98f, 2093f, 1760f, 2637f };
                for (int n = 0; n < sparkle.Length; n++)
                {
                    sum += SoundSynth.Bell(t - n * 0.07f, sparkle[n], 9f);
                }

                return sum * 0.12f;
            });
        }

        /// <summary>
        /// 칸이 열리는 소리. step 은 펼침 깊이. 짧은 간격으로 이어 맞히면 기준음이 한 칸씩 올라간다.
        /// </summary>
        public void Reveal(int step)
        {
            if (step == 0)
            {
                combo = Time.unscaledTime - lastReveal < ComboWindow ? Mathf.Min(combo + 1, MaxCombo) : 0;
                lastReveal = Time.unscaledTime;
            }

            Play(reveal[Mathf.Clamp(combo + step, 0, reveal.Length - 1)], 1f);
        }

        /// <summary>빈 구역이 한꺼번에 열릴 때 한 번 튼다(소리 파일이 있을 때).</summary>
        public void AreaReveal() => Play(area, 1f);

        /// <summary>예전 이름. 펼침 소리.</summary>
        public void Pop(int step) => Reveal(step);

        public void Tap() => Play(tap, 0.6f);

        /// <summary>팔레트에서 색을 고를 때. 색마다 음이 조금씩 달라서 손에 익는다.</summary>
        public void Select(int colorIndex) => Play(select[Mathf.Abs(colorIndex) % select.Length], 0.8f);

        public void Wrong() => Play(wrong, 1f);
        public void LoseHeart() => Play(heart, 0.8f);
        public void Clear() => Play(clear, 1f);
        public void Star(int index) => Play(star[Mathf.Clamp(index, 0, star.Length - 1)], 1f);
        public void Open() => Play(open, 0.6f);
        public void Close() => Play(close, 0.5f);
        public void Revive() => Play(revive, 1f);
        public void NameReveal() => Play(nameReveal, 1f);
        public void Unlock() => Play(unlock, 1f);
        public void Locked() => Play(locked, 0.8f);

        void Play(AudioClip clip, float volume)
        {
            if (Volume > 0.001f && clip != null)
            {
                source.PlayOneShot(clip, volume * Volume);
            }
        }

        /// <summary>
        /// 음높이가 미끄러지는 둥근 "뽁" 한 음(사인파). fromHz 에서 toHz 로 glideSeconds 동안 옮겨 간다.
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
