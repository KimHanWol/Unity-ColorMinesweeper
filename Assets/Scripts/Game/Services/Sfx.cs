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
        AudioClip hint;

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

            // 크기는 SfxSounds 의 역할별 목표 음량으로 맞춰져 나오므로 재생할 때는 따로 줄이지 않는다.
            reveal = Clips(SfxSounds.Reveal());
            select = Clips(SfxSounds.Select());
            tap = SoundSynth.Clip(SfxSounds.Tap());
            wrong = SoundSynth.Clip(SfxSounds.Wrong());
            heart = SoundSynth.Clip(SfxSounds.LoseHeart());
            clear = SoundSynth.Clip(SfxSounds.Clear());
            star = Clips(SfxSounds.Star());
            open = SoundSynth.Clip(SfxSounds.Open());
            close = SoundSynth.Clip(SfxSounds.Close());
            revive = SoundSynth.Clip(SfxSounds.Revive());
            unlock = SoundSynth.Clip(SfxSounds.Unlock());
            locked = SoundSynth.Clip(SfxSounds.Locked());
            nameReveal = SoundSynth.Clip(SfxSounds.NameReveal());
            hint = SoundSynth.Clip(SfxSounds.Hint());
        }

        static AudioClip[] Clips(SfxSound[] sounds)
        {
            var clips = new AudioClip[sounds.Length];
            for (int i = 0; i < sounds.Length; i++)
            {
                clips[i] = SoundSynth.Clip(sounds[i]);
            }

            return clips;
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

        public void Tap() => Play(tap, 1f);

        /// <summary>팔레트에서 색을 고를 때. 색마다 음이 조금씩 달라서 손에 익는다.</summary>
        public void Select(int colorIndex) => Play(select[Mathf.Abs(colorIndex) % select.Length], 1f);

        public void Wrong() => Play(wrong, 1f);
        public void LoseHeart() => Play(heart, 1f);
        public void Clear() => Play(clear, 1f);
        public void Star(int index) => Play(star[Mathf.Clamp(index, 0, star.Length - 1)], 1f);
        public void Open() => Play(open, 1f);
        public void Close() => Play(close, 1f);
        public void Revive() => Play(revive, 1f);
        public void NameReveal() => Play(nameReveal, 1f);
        public void Unlock() => Play(unlock, 1f);
        public void Locked() => Play(locked, 1f);
        public void Hint() => Play(hint, 1f);

        void Play(AudioClip clip, float volume)
        {
            if (Volume > 0.001f && clip != null)
            {
                source.PlayOneShot(clip, volume * Volume);
            }
        }
    }
}
