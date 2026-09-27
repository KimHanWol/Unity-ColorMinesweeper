using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 가로 슬라이더(0~1). 트랙 어디를 눌러도 그 자리로 가고, 누른 채 끌면 따라온다.
    /// 끄는 동안 onChange 를, 손을 떼면 onRelease 를 부른다.
    /// </summary>
    public sealed class UiSlider : MonoBehaviour
    {
        const float TrackHeight = 0.34f;
        const float KnobSize = 0.62f;

        float width;
        float value;
        SpriteRenderer fill;
        Transform knob;
        Action<float> onChange;

        public static UiSlider Create(Transform parent, string name, Vector2 position, float width, float value, int order,
            Action<float> onChange, Action onRelease)
        {
            Transform root = Draw.Node(parent, name, position);
            var slider = root.gameObject.AddComponent<UiSlider>();
            slider.width = width;
            slider.onChange = onChange;
            Draw.Panel(root, "Track", new Vector2(width, TrackHeight), Theme.Locked, order, Vector2.zero, TrackHeight / 2f);
            slider.fill = Draw.Panel(root, "Fill", new Vector2(width, TrackHeight), Theme.Accent, order + 1, Vector2.zero,
                TrackHeight / 2f);
            Transform knob = Draw.Node(root, "Knob");
            Draw.Sprite(knob, "Shadow", SpriteFactory.Circle(), new Color(0f, 0f, 0f, 0.15f), order + 2,
                new Vector2(0f, -0.05f), Vector2.one * (KnobSize + 0.06f));
            Draw.Sprite(knob, "Face", SpriteFactory.Circle(), Color.white, order + 3, Vector2.zero, Vector2.one * KnobSize);
            Draw.Sprite(knob, "Dot", SpriteFactory.Circle(), Theme.Accent, order + 4, Vector2.zero, Vector2.one * 0.22f);
            slider.knob = knob;
            slider.Show(Mathf.Clamp01(value));

            UiButton button = UiButton.Attach(root, new Vector2(width + KnobSize, 1f), order + 5, null);
            button.Pressable = false;
            button.OnDragTo = world => slider.DragTo(world);
            button.OnDragEnd = onRelease;
            return slider;
        }

        void DragTo(Vector2 world)
        {
            float local = transform.InverseTransformPoint(world).x;
            float v = Mathf.Clamp01(local / width + 0.5f);
            if (Mathf.Abs(v - value) < 0.001f)
            {
                return;
            }

            Show(v);
            onChange?.Invoke(v);
        }

        void Show(float v)
        {
            value = v;
            float filled = Mathf.Max(TrackHeight, width * v);
            fill.size = new Vector2(filled, TrackHeight);
            fill.transform.localPosition = new Vector3(-width / 2f + filled / 2f, 0f, 0f);
            fill.enabled = v > 0.001f;
            knob.localPosition = new Vector3(-width / 2f + width * v, 0f, 0f);
        }
    }
}
