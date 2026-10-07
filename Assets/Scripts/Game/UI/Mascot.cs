using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 화면 한쪽에서 플레이에 반응하는 마스코트. 가만히 있을 때는 숨 쉬듯 살짝 움직이며 가끔 눈을 깜빡이고,
    /// 맞히면 폴짝 뛰고, 틀리면 깜짝 놀라고, 완성하면 신나서 뛴다. 그림은 <see cref="MascotArt"/> 에 있다.
    /// </summary>
    public sealed class Mascot : MonoBehaviour
    {
        Transform body;
        SpriteRenderer face;
        float height;
        int order;

        /// <summary>반응이 끝나면 돌아갈 표정.</summary>
        MascotMood rest = MascotMood.Idle;
        MascotMood shown = MascotMood.Idle;
        float moodUntil;
        float nextBlink;
        float blinkUntil;

        /// <summary>뛰거나 떠는 중에는 숨 쉬는 움직임을 멈춘다.</summary>
        float busyUntil;

        /// <param name="height">얼굴 높이(부모 기준 유닛).</param>
        public static Mascot Create(Transform parent, string name, Vector2 position, float height, int order)
        {
            Transform root = Draw.Node(parent, name, position);
            var mascot = root.gameObject.AddComponent<Mascot>();
            mascot.height = height;
            mascot.order = order;
            mascot.body = Draw.Node(root, "Body");
            mascot.face = Draw.Sprite(mascot.body, "Face", MascotArt.Face(MascotMood.Idle), Color.white, order, Vector2.zero,
                new Vector2(height, height));
            mascot.nextBlink = Time.unscaledTime + Random.Range(1.5f, 3.5f);
            return mascot;
        }

        /// <summary>아래에서 쏙 올라오며 나타난다.</summary>
        public void PopIn(float delay = 0f)
        {
            Transform b = body;
            b.localScale = Vector3.zero;
            busyUntil = Time.unscaledTime + delay + 0.45f;
            Tween.Run(b, 0.45f, t => b.localScale = Vector3.one * t, Ease.OutBack, delay);
        }

        /// <summary>평소 표정을 바꾼다(게임 오버면 울상, 완성하면 웃는 얼굴).</summary>
        public void SetRest(MascotMood mood)
        {
            rest = mood;
            moodUntil = 0f;
            Show(mood);
        }

        /// <summary>맞혔을 때. big 이면 더 높이 뛰고 하트를 날린다.</summary>
        public void Cheer(bool big)
        {
            Hold(MascotMood.Happy, big ? 1.1f : 0.6f);
            Hop(big ? 0.34f : 0.16f, big ? 0.42f : 0.3f);
            if (big)
            {
                EmitHearts(3);
            }
        }

        /// <summary>틀렸을 때. 깜짝 놀라 부르르 떤다.</summary>
        public void Ouch()
        {
            Hold(MascotMood.Shock, 0.9f);
            Transform b = body;
            Tween.Kill(b);
            busyUntil = Time.unscaledTime + 0.5f;
            Tween.Run(b, 0.5f, t =>
            {
                b.localPosition = new Vector3(Mathf.Sin(t * Mathf.PI * 7f) * 0.09f * height * (1f - t), 0f, 0f);
                float squash = Ease.Pulse(Mathf.Min(1f, t * 2f)) * 0.14f;
                b.localScale = new Vector3(1f + squash, 1f - squash, 1f);
            }, Ease.Linear, 0f, () =>
            {
                b.localPosition = Vector3.zero;
                b.localScale = Vector3.one;
            });
        }

        /// <summary>완성했을 때. 웃는 얼굴로 세 번 뛰며 하트와 반짝이를 날린다.</summary>
        public void Celebrate()
        {
            SetRest(MascotMood.Happy);
            for (int i = 0; i < 3; i++)
            {
                Tween.Delay(this, i * 0.42f, () => Hop(0.36f, 0.4f));
            }

            EmitHearts(5);
        }

        void Hold(MascotMood mood, float seconds)
        {
            Show(mood);
            moodUntil = Time.unscaledTime + seconds;
        }

        void Show(MascotMood mood)
        {
            if (shown == mood)
            {
                return;
            }

            shown = mood;
            face.sprite = MascotArt.Face(mood);
        }

        /// <summary>웅크렸다가 길쭉해지며 뛰어오르고, 내려앉으며 납작해진다.</summary>
        void Hop(float jump, float seconds)
        {
            Transform b = body;
            Tween.Kill(b);
            busyUntil = Time.unscaledTime + seconds;
            float lift = jump * height;
            Tween.Run(b, seconds, t =>
            {
                float air = Ease.Pulse(t);
                b.localPosition = new Vector3(0f, air * lift, 0f);
                // 바닥에 닿는 처음과 끝에는 납작, 공중에서는 길쭉.
                float stretch = Mathf.Lerp(-0.14f, 0.12f, air);
                b.localScale = new Vector3(1f - stretch, 1f + stretch, 1f);
            }, Ease.Linear, 0f, () =>
            {
                b.localPosition = Vector3.zero;
                b.localScale = Vector3.one;
            });
        }

        void EmitHearts(int count)
        {
            Sprite heart = PixelGlyphs.Icon("heart", PixelGlyphs.Heart);
            Sprite sparkle = PixelGlyphs.Icon("sparkle", PixelGlyphs.Sparkle);
            for (int i = 0; i < count; i++)
            {
                bool isHeart = i % 2 == 0;
                var from = new Vector2(Random.Range(-0.45f, 0.45f) * height, height * 0.45f);
                Fx.FloatUp(transform, isHeart ? heart : sparkle, isHeart ? Theme.Hex(0xFF6B8B) : Theme.Gold, from,
                    height * Random.Range(0.26f, 0.36f), order + 1, i * 0.09f);
            }
        }

        void Update()
        {
            float now = Time.unscaledTime;
            if (moodUntil > 0f && now >= moodUntil)
            {
                moodUntil = 0f;
                Show(rest);
            }

            // 평소 표정일 때만 눈을 깜빡인다.
            if (moodUntil <= 0f && rest == MascotMood.Idle)
            {
                if (blinkUntil > 0f && now >= blinkUntil)
                {
                    blinkUntil = 0f;
                    Show(MascotMood.Idle);
                    nextBlink = now + Random.Range(2.2f, 4.8f);
                }
                else if (blinkUntil <= 0f && now >= nextBlink)
                {
                    blinkUntil = now + 0.13f;
                    Show(MascotMood.Blink);
                }
            }

            if (now >= busyUntil)
            {
                float breath = Mathf.Sin(now * 2.4f) * 0.025f;
                body.localScale = new Vector3(1f - breath, 1f + breath, 1f);
            }
        }
    }
}
