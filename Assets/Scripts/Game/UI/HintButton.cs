using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 플레이 화면 위쪽의 힌트 버튼. 전구 아이콘에 남은 힌트 수를 작은 배지로 붙인다.
    /// 힌트가 없으면 배지 자리에 광고 아이콘을 보여 광고로 채울 수 있다는 것을 알린다.
    /// 누르고 칸을 고르는 동안(힌트 모드)에는 버튼 둘레가 숨 쉬듯 빛난다.
    /// </summary>
    public sealed class HintButton : MonoBehaviour
    {
        const float BadgeSize = 0.52f;

        SpriteRenderer ring;
        Transform badge;
        PixelText count;
        SpriteRenderer adIcon;
        bool active;

        public static HintButton Create(Transform parent, Vector2 position, int order, Action onClick)
        {
            UiButton button = UiKit.IconButton(parent, "Hint", Icons.Hint, position, order, onClick);
            var hint = button.gameObject.AddComponent<HintButton>();
            Transform root = button.transform;

            hint.ring = Draw.OutlinePanel(root, "ActiveRing", new Vector2(1.55f, 1.55f), Theme.Gold, order + 3, Vector2.zero, 0.72f,
                0.12f);
            hint.ring.enabled = false;

            hint.badge = Draw.Node(root, "Badge", new Vector2(0.5f, 0.48f));
            Draw.Sprite(hint.badge, "Back", SpriteFactory.Circle(), Theme.Accent, order + 4, Vector2.zero,
                new Vector2(BadgeSize, BadgeSize));
            hint.count = PixelText.Create(hint.badge, "Count", "0", Color.white, order + 5, Vector2.zero, 0.24f);
            hint.adIcon = Draw.Sprite(hint.badge, "Ad", Icons.Ad, Color.white, order + 5, Vector2.zero,
                new Vector2(0.3f, 0.3f));
            return hint;
        }

        public void SetCount(int value)
        {
            bool empty = value <= 0;
            count.gameObject.SetActive(!empty);
            adIcon.enabled = empty;
            if (!empty)
            {
                count.SetText(value > 99 ? "99" : value.ToString());
            }

            Tween.Kill(badge);
            Tween.Run(badge, 0.35f, t => badge.localScale = Vector3.one * Mathf.LerpUnclamped(1f, 1.35f, Ease.Pulse(t)),
                Ease.Linear);
        }

        /// <summary>칸을 고르는 중인지. 켜져 있는 동안 금색 테두리가 천천히 밝아졌다 흐려진다.</summary>
        public void SetActive(bool on)
        {
            active = on;
            ring.enabled = on;
        }

        void Update()
        {
            if (active)
            {
                Draw.SetAlpha(ring, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f));
            }
        }
    }
}
