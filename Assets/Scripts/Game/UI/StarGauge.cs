using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 완성 화면의 별 게이지. 막대 1/3, 2/3, 끝에 별이 박혀 있다.
    /// 막대가 끝까지 차오르며 별이 차례로 켜지고, 잃은 별만큼(실수, 광고로 이어 하기) 다시 깎이며 별이 꺼진다.
    /// 목숨(하트)과 별이 어떻게 이어지는지를 결과 화면에서 한눈에 보여 주려는 것이다.
    /// </summary>
    public static class StarGauge
    {
        const int StarCount = 3;
        const float BarHeight = 0.34f;
        const float StarSize = 0.95f;
        const float FillSeconds = 0.7f;
        const float DrainSeconds = 0.35f;
        const float DrainGap = 0.5f;

        /// <summary>게이지를 만들고 연출을 건다. 연출이 끝나기까지 걸리는 시간(초)을 돌려준다.</summary>
        public static float Create(Transform parent, Vector2 center, float width, int order, int stars, float delay)
        {
            Transform root = Draw.Node(parent, "StarGauge", center);
            float left = -width / 2f;
            Draw.Panel(root, "Track", new Vector2(width, BarHeight), Theme.Locked, order, Vector2.zero, BarHeight / 2f);
            SpriteRenderer fill = Draw.Panel(root, "Fill", new Vector2(BarHeight, BarHeight), Theme.Gold, order + 1,
                new Vector2(left + BarHeight / 2f, 0f), BarHeight / 2f);
            fill.enabled = false;

            void SetFill(float amount)
            {
                // 둥근 끝이 찌그러지지 않게 막대 높이보다 짧게는 줄이지 않는다.
                float w = Mathf.Max(BarHeight, width * Mathf.Clamp01(amount));
                fill.enabled = amount > 0.001f;
                fill.size = new Vector2(w, BarHeight);
                fill.transform.localPosition = new Vector3(left + w / 2f, 0f, 0f);
            }

            var icons = new SpriteRenderer[StarCount];
            for (int i = 0; i < StarCount; i++)
            {
                float x = left + width * (i + 1) / StarCount;
                // 막대 끝의 별은 막대 밖으로 튀어나가지 않게 조금 안쪽에 둔다.
                if (i == StarCount - 1)
                {
                    x -= StarSize * 0.25f;
                }

                icons[i] = Draw.Sprite(root, "Star" + i, Icons.Star, Theme.Locked, order + 2, new Vector2(x, 0f),
                    new Vector2(StarSize, StarSize));
            }

            // 1) 끝까지 차오르며 지나가는 별마다 켠다.
            Tween.Run(root, FillSeconds, SetFill, Ease.InOutSine, delay);
            for (int i = 0; i < StarCount; i++)
            {
                SpriteRenderer icon = icons[i];
                int index = i;
                Tween.Delay(icon, delay + FillSeconds * (i + 1) / StarCount, () =>
                {
                    icon.color = Theme.Gold;
                    Pop(icon, 1.45f);
                    Sfx.Instance?.Star(index);
                });
            }

            // 2) 잃은 별만큼 오른쪽부터 깎는다.
            float t = delay + FillSeconds + 0.35f;
            for (int k = 0; k < StarCount - stars; k++)
            {
                int lost = StarCount - 1 - k;
                SpriteRenderer icon = icons[lost];
                float from = (float)(lost + 1) / StarCount;
                float to = (float)lost / StarCount;
                Tween.Run(fill, DrainSeconds, a => SetFill(Mathf.Lerp(from, to, a)), Ease.OutCubic, t);
                Tween.Delay(icon, t, () =>
                {
                    icon.color = Theme.Locked;
                    Pop(icon, 0.7f);
                    Sfx.Instance?.LoseHeart();
                });
                t += DrainGap;
            }

            return t;
        }

        static void Pop(SpriteRenderer icon, float peak)
        {
            Transform tr = icon.transform;
            Tween.Kill(tr);
            Tween.Run(tr, 0.4f, a =>
            {
                float s = StarSize * Mathf.LerpUnclamped(1f, peak, Ease.Pulse(a));
                tr.localScale = new Vector3(s, s, 1f);
            }, Ease.Linear);
        }
    }
}
