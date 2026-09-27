using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>튜토리얼 말풍선. 안내 문구와, 필요하면 "다음" 버튼을 담는다. 판 위쪽에 떠서 판 입력은 막지 않는다.</summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        const int Order = 180;
        const float Height = 2.6f;

        Label text;
        Transform nextButton;
        Action onNext;
        Vector2 size;

        public static SpeechBubble Create(Transform parent, UiRoot ui)
        {
            Transform root = Draw.Node(parent, "SpeechBubble");
            var bubble = root.gameObject.AddComponent<SpeechBubble>();
            bubble.size = new Vector2(ui.Safe.width - 0.6f, Height);
            Draw.Sprite(root, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, Order, new Vector2(0f, -0.2f),
                bubble.size * 1.2f);
            Draw.Panel(root, "Face", bubble.size, Color.white, Order + 1, Vector2.zero, 0.5f);
            Draw.Panel(root, "Accent", new Vector2(0.18f, bubble.size.y - 0.6f), Theme.Accent, Order + 2,
                new Vector2(-bubble.size.x / 2f + 0.35f, 0f), 0.09f);
            bubble.text = Label.Create(root, "Text", string.Empty, Theme.Ink, Order + 2,
                new Vector2(-bubble.size.x / 2f + 0.7f, 0.25f), 0.42f, TextAnchor.MiddleLeft);
            bubble.nextButton = UiKit.Button(root, "Next", "다음", null, Theme.Accent, Color.white, new Vector2(2.2f, 0.85f),
                new Vector2(bubble.size.x / 2f - 1.4f, -bubble.size.y / 2f + 0.65f), Order + 3, () => bubble.onNext?.Invoke()).transform;
            root.gameObject.SetActive(false);
            return bubble;
        }

        /// <summary>문구를 보여 준다. onNext 가 있으면 "다음" 버튼을 띄우고, 없으면 플레이어의 행동을 기다린다.</summary>
        public void Show(string message, Action next = null)
        {
            gameObject.SetActive(true);
            text.Text = message;
            onNext = next;
            nextButton.gameObject.SetActive(next != null);

            Transform t = transform;
            Tween.Kill(t);
            t.localScale = new Vector3(0.9f, 0.9f, 1f);
            Tween.Run(t, 0.3f, k => t.localScale = Vector3.LerpUnclamped(new Vector3(0.9f, 0.9f, 1f), Vector3.one, k),
                Ease.OutBack);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
