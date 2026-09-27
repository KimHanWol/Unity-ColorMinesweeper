using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 튜토리얼 말풍선. 굵은 한 줄(결론)과 옅은 설명 한두 줄로 짧게 보여 준다. 필요하면 "다음" 버튼을 둔다.
    /// 판 위쪽에 떠서 판 입력은 막지 않는다. 글자는 말풍선 폭에 맞춰 단어 단위로 줄을 바꾸고,
    /// 실제로 그려진 글자 영역을 재서 위아래 여백을 맞춘다(동글은 폰트 줄 높이가 글자보다 훨씬 크다).
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        const int Order = 180;
        const float Padding = 0.42f;
        const float LineGap = 0.16f;
        const float ButtonRow = 1.0f;
        const float MinHeight = 1.3f;

        /// <summary>말풍선이 가장 커졌을 때의 높이. 판을 이만큼 아래로 내려 가리지 않게 한다.</summary>
        public const float ReservedHeight = 2.6f;

        Label headline;
        Label body;
        Transform nextButton;
        SpriteRenderer face;
        SpriteRenderer shadow;
        Action onNext;
        float width;

        public static SpeechBubble Create(Transform parent, UiRoot ui)
        {
            Transform root = Draw.Node(parent, "SpeechBubble");
            var bubble = root.gameObject.AddComponent<SpeechBubble>();
            bubble.width = ui.Safe.width - 0.6f;
            bubble.shadow = Draw.Sprite(root, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, Order, Vector2.zero);
            bubble.face = Draw.Panel(root, "Face", new Vector2(bubble.width, MinHeight), Color.white, Order + 1, Vector2.zero, 0.5f);
            bubble.headline = Label.Create(root, "Headline", string.Empty, Theme.Ink, Order + 2, Vector2.zero, 0.52f,
                TextAnchor.UpperLeft, true);
            bubble.body = Label.Create(root, "Body", string.Empty, Theme.SubInk, Order + 2, Vector2.zero, 0.4f,
                TextAnchor.UpperLeft);
            bubble.nextButton = UiKit.Button(root, "Next", "다음", null, Theme.Accent, Color.white, new Vector2(2.2f, 0.8f),
                Vector2.zero, Order + 3, () => bubble.onNext?.Invoke()).transform;
            root.gameObject.SetActive(false);
            return bubble;
        }

        /// <summary>
        /// title 은 굵은 결론 한 줄, detail 은 옅은 설명(없어도 된다). next 가 있으면 "다음" 버튼을 띄우고,
        /// 없으면 플레이어의 행동을 기다린다.
        /// </summary>
        public void Show(string title, string detail = null, Action next = null)
        {
            gameObject.SetActive(true);
            onNext = next;
            nextButton.gameObject.SetActive(next != null);
            headline.transform.localPosition = Vector3.zero;
            body.transform.localPosition = Vector3.zero;
            headline.SetWrappedText(title, width - Padding * 2f);
            body.SetWrappedText(detail ?? string.Empty, width - Padding * 2f);
            body.gameObject.SetActive(!string.IsNullOrEmpty(detail));

            Relayout();
            // TextMesh 가 글자 영역을 다음 프레임에 갱신하는 경우가 있어 한 번 더 잰다.
            Tween.Delay(this, 0.02f, Relayout);

            Transform t = transform;
            Tween.Kill(t);
            t.localScale = new Vector3(0.94f, 0.94f, 1f);
            Tween.Run(t, 0.28f, k => t.localScale = Vector3.LerpUnclamped(new Vector3(0.94f, 0.94f, 1f), Vector3.one, k),
                Ease.OutBack);
        }

        void Relayout()
        {
            Transform t = transform;
            Vector3 scale = t.localScale;
            t.localScale = Vector3.one;

            float left = -width / 2f + Padding;
            float y = -Padding;
            y = Place(headline, left, y);
            if (body.gameObject.activeSelf)
            {
                y = Place(body, left, y - LineGap);
            }

            bool hasButton = nextButton.gameObject.activeSelf;
            float height = Mathf.Max(MinHeight, -y + Padding + (hasButton ? ButtonRow : 0f));
            face.size = new Vector2(width, height);
            face.transform.localPosition = new Vector3(0f, -height / 2f, 0f);
            shadow.transform.localPosition = new Vector3(0f, -height / 2f - 0.2f, 0f);
            shadow.transform.localScale = new Vector3(width * 1.15f, height * 1.3f, 1f);
            nextButton.localPosition = new Vector3(width / 2f - 1.4f, -height + Padding + 0.4f, 0f);
            t.localScale = scale;
        }

        /// <summary>글자 윗변이 top 에 오게 놓고, 아랫변 위치를 돌려준다.</summary>
        float Place(Label label, float left, float top)
        {
            Transform t = transform;
            label.transform.localPosition = new Vector3(left, 0f, 0f);
            Bounds bounds = label.RenderBounds;
            float glyphTop = t.InverseTransformPoint(bounds.max).y;
            float glyphBottom = t.InverseTransformPoint(bounds.min).y;
            if (glyphTop - glyphBottom <= 0f)
            {
                return top;
            }

            label.transform.localPosition = new Vector3(left, top - glyphTop, 0f);
            return top - (glyphTop - glyphBottom);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
