using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 튜토리얼 말풍선. 안내 문구와, 필요하면 "다음" 버튼을 담는다. 판 위쪽에 떠서 판 입력은 막지 않는다.
    /// 문구는 말풍선 폭에 맞춰 단어 단위로 줄을 바꾸고, 줄 수만큼 말풍선이 아래로 늘어난다(윗변은 고정).
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        const int Order = 180;
        const float Padding = 0.45f;
        const float ButtonRow = 1.0f;
        const float MinHeight = 1.6f;

        Label text;
        Transform nextButton;
        SpriteRenderer face;
        SpriteRenderer shadow;
        Action onNext;
        float width;

        /// <summary>말풍선이 가장 커졌을 때의 높이. 판을 이만큼 아래로 내려 가리지 않게 한다.</summary>
        public const float ReservedHeight = 3.0f;

        public static SpeechBubble Create(Transform parent, UiRoot ui)
        {
            Transform root = Draw.Node(parent, "SpeechBubble");
            var bubble = root.gameObject.AddComponent<SpeechBubble>();
            bubble.width = ui.Safe.width - 0.6f;
            bubble.shadow = Draw.Sprite(root, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, Order, Vector2.zero);
            bubble.face = Draw.Panel(root, "Face", new Vector2(bubble.width, MinHeight), Color.white, Order + 1, Vector2.zero, 0.5f);
            bubble.text = Label.Create(root, "Text", string.Empty, Theme.Ink, Order + 2, Vector2.zero, 0.42f,
                TextAnchor.UpperLeft);
            bubble.nextButton = UiKit.Button(root, "Next", "다음", null, Theme.Accent, Color.white, new Vector2(2.2f, 0.8f),
                Vector2.zero, Order + 3, () => bubble.onNext?.Invoke()).transform;
            root.gameObject.SetActive(false);
            return bubble;
        }

        /// <summary>문구를 보여 준다. next 가 있으면 "다음" 버튼을 띄우고, 없으면 플레이어의 행동을 기다린다.</summary>
        public void Show(string message, Action next = null)
        {
            gameObject.SetActive(true);
            onNext = next;
            nextButton.gameObject.SetActive(next != null);

            float textWidth = width - Padding * 2f;
            int lines = text.SetWrappedText(message, textWidth);
            float textHeight = lines * text.LineAdvance;
            float height = Mathf.Max(MinHeight, Padding * 2f + textHeight + (next != null ? ButtonRow : 0f));

            // 윗변(로컬 y=0)을 고정하고 아래로 늘인다.
            face.size = new Vector2(width, height);
            face.transform.localPosition = new Vector3(0f, -height / 2f, 0f);
            shadow.transform.localPosition = new Vector3(0f, -height / 2f - 0.2f, 0f);
            shadow.transform.localScale = new Vector3(width * 1.15f, height * 1.3f, 1f);
            text.transform.localPosition = new Vector3(-width / 2f + Padding, -Padding, 0f);
            nextButton.localPosition = new Vector3(width / 2f - 1.4f, -height + 0.65f, 0f);

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
