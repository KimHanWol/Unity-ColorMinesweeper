using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 튜토리얼 말풍선. 굵은 한 줄(결론)과 옅은 설명 한두 줄로 짧게 보여 준다. 필요하면 "다음" 버튼을 둔다.
    /// 판과 팔레트 사이의 비워 둔 자리에 떠서, 꼬리로 짚는 곳(판의 3x3 또는 팔레트 색)을 가리킨다.
    /// 판 위에 뜨면 짚는 칸을 가렸고, 화면 맨 위에 두면 위아래를 번갈아 봐야 했다. 글자는 말풍선 폭에 맞춰 단어 단위로 줄을 바꾸고,
    /// 실제로 그려진 글자 영역을 재서 위아래 여백을 맞춘다(동글은 폰트 줄 높이가 글자보다 훨씬 크다).
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        const int Order = 180;
        const float Padding = 0.42f;
        const float LineGap = 0.16f;
        const float ButtonRow = 1.0f;
        const float MinHeight = 1.3f;

        /// <summary>판 아래에 말풍선용으로 비워 두는 높이. 튜토리얼 문구가 이 안에 들어가게 짧게 쓴다.</summary>
        public const float DockHeight = 3.4f;

        const float TailSize = 0.36f;

        Label headline;
        Label body;
        Label action;
        Transform nextButton;
        SpriteRenderer face;
        SpriteRenderer shadow;
        Action onNext;
        float width;
        float height;
        Rect safe;
        Transform tail;
        bool anchored;
        Rect anchor;

        public static SpeechBubble Create(Transform parent, UiRoot ui)
        {
            Transform root = Draw.Node(parent, "SpeechBubble");
            var bubble = root.gameObject.AddComponent<SpeechBubble>();
            bubble.width = ui.Safe.width - 0.6f;
            bubble.safe = ui.Safe;
            bubble.shadow = Draw.Sprite(root, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, Order, Vector2.zero);
            bubble.face = Draw.Panel(root, "Face", new Vector2(bubble.width, MinHeight), Color.white, Order + 1, Vector2.zero, 0.5f);
            bubble.tail = Draw.Sprite(root, "Tail", SpriteFactory.RoundedRect(0.18f), Color.white, Order + 1, Vector2.zero,
                Vector2.one * TailSize).transform;
            bubble.tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            bubble.headline = Label.Create(root, "Headline", string.Empty, Theme.Ink, Order + 2, Vector2.zero, 0.52f,
                TextAnchor.UpperLeft, true);
            bubble.body = Label.Create(root, "Body", string.Empty, Theme.SubInk, Order + 2, Vector2.zero, 0.4f,
                TextAnchor.UpperLeft);
            bubble.action = Label.Create(root, "Action", string.Empty, Theme.Accent, Order + 2, Vector2.zero, 0.42f,
                TextAnchor.UpperLeft, true);
            bubble.nextButton = UiKit.Button(root, "Next", "다음", null, Theme.Accent, Color.white, new Vector2(2.2f, 0.8f),
                Vector2.zero, Order + 3, () => bubble.onNext?.Invoke()).transform;
            root.gameObject.SetActive(false);
            return bubble;
        }

        /// <summary>
        /// title 은 굵은 결론 한 줄, detail 은 옅은 설명(없어도 된다). next 가 있으면 "다음" 버튼을 띄우고,
        /// 없으면 플레이어의 행동을 기다린다.
        /// </summary>
        /// <param name="todo">할 일 한 줄(보라색). 판을 눌러야 넘어가는 단계에서 무엇을 하면 되는지 따로 보여 준다.</param>
        public void Show(string title, string detail = null, Action next = null, string todo = null)
        {
            gameObject.SetActive(true);
            onNext = next;
            nextButton.gameObject.SetActive(next != null);
            headline.transform.localPosition = Vector3.zero;
            body.transform.localPosition = Vector3.zero;
            headline.SetWrappedText(title, width - Padding * 2f);
            body.SetWrappedText(detail ?? string.Empty, width - Padding * 2f);
            body.gameObject.SetActive(!string.IsNullOrEmpty(detail));
            action.transform.localPosition = Vector3.zero;
            action.SetWrappedText(todo ?? string.Empty, width - Padding * 2f);
            action.gameObject.SetActive(!string.IsNullOrEmpty(todo));

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

            if (action.gameObject.activeSelf)
            {
                y = Place(action, left, y - LineGap * 2f);
            }

            bool hasButton = nextButton.gameObject.activeSelf;
            height = Mathf.Max(MinHeight, -y + Padding + (hasButton ? ButtonRow : 0f));
            face.size = new Vector2(width, height);
            face.transform.localPosition = new Vector3(0f, -height / 2f, 0f);
            shadow.transform.localPosition = new Vector3(0f, -height / 2f - 0.2f, 0f);
            shadow.transform.localScale = new Vector3(width * 1.15f, height * 1.3f, 1f);
            nextButton.localPosition = new Vector3(width / 2f - 1.4f, -height + Padding + 0.4f, 0f);
            t.localScale = scale;
            Place();
        }

        /// <summary>area(부모 좌표) 바로 아래에, 자리가 없으면 바로 위에 뜨고 꼬리로 가리킨다.</summary>
        public void AnchorTo(Rect area)
        {
            anchored = true;
            anchor = area;
            Place();
        }

        /// <summary>짚는 곳 없이 팔레트 바로 위에 뜬다(손가락과 가까운 쪽).</summary>
        public void AnchorBottom()
        {
            anchored = false;
            Place();
        }

        /// <summary>
        /// 말풍선은 판 바로 아래, 팔레트 바로 위의 고정된 자리(<see cref="DockHeight"/>)에 둔다. 판은 이 자리를 비워 두고
        /// 그려지므로 말풍선이 짚는 칸을 가리지 않는다. 꼬리는 짚는 곳 쪽(위: 판, 아래: 팔레트)을 향한다.
        /// 루트의 로컬 y=0 이 말풍선 윗변이다.
        /// </summary>
        void Place()
        {
            float floor = safe.yMin + PaletteBar.BarHeight + 0.2f;
            float top = floor + DockHeight;
            float x = safe.center.x;
            transform.localPosition = new Vector3(x, top, 0f);

            if (!anchored)
            {
                tail.gameObject.SetActive(false);
                return;
            }

            bool up = anchor.center.y > top - height / 2f;
            tail.gameObject.SetActive(true);
            float tailX = Mathf.Clamp(anchor.center.x - x, -width / 2f + 0.7f, width / 2f - 0.7f);
            tail.localPosition = new Vector3(tailX, up ? 0f : -height, 0f);
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

        /// <summary>할 일 줄만 바꾼다(여러 칸 중 일부를 칠했을 때 남은 수를 알려 줄 때).</summary>
        public void SetTodo(string todo)
        {
            action.SetWrappedText(todo, width - Padding * 2f);
            action.gameObject.SetActive(true);
            Relayout();
            Tween.Delay(this, 0.02f, Relayout);
        }

        /// <summary>엉뚱한 곳을 눌렀을 때: 말풍선이 살짝 흔들리고 할 일 줄이 톡 커진다.</summary>
        public void Nudge()
        {
            Transform t = transform;
            Vector3 rest = t.localPosition;
            Tween.Run(t, 0.35f, k => t.localPosition = rest + new Vector3(Mathf.Sin(k * Mathf.PI * 5f) * 0.12f * (1f - k), 0f, 0f),
                Ease.Linear, 0f, () => t.localPosition = rest);
            if (action.gameObject.activeSelf)
            {
                Transform a = action.transform;
                Vector3 scale = a.localScale;
                Tween.Kill(a);
                Tween.Run(a, 0.4f, k => a.localScale = scale * (1f + 0.12f * Ease.Pulse(k)), Ease.Linear, 0f,
                    () => a.localScale = scale);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
