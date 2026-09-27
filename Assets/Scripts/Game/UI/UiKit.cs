using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>화면들이 같이 쓰는 버튼과 창. 모든 버튼은 아래에 진한 그림자를 깔아 눌리는 느낌을 준다.</summary>
    public static class UiKit
    {
        /// <summary>글자(와 선택적 아이콘)가 들어간 둥근 버튼.</summary>
        public static UiButton Button(Transform parent, string name, string text, Sprite icon, Color fill, Color ink,
            Vector2 size, Vector2 position, int order, Action onClick)
        {
            Transform root = Draw.Node(parent, name, position);
            Transform visual = Draw.Node(root, "Visual");
            float radius = Mathf.Min(size.y / 2f, 0.6f);
            Draw.Panel(visual, "Under", size, Color.Lerp(fill, Color.black, 0.22f), order, new Vector2(0f, -0.12f), radius);
            Draw.Panel(visual, "Face", size, fill, order + 1, Vector2.zero, radius);

            float textHeight = size.y * 0.42f;
            if (icon != null && !string.IsNullOrEmpty(text))
            {
                float iconSize = size.y * 0.42f;
                float estimatedTextWidth = text.Length * textHeight * 0.62f;
                float total = iconSize + 0.25f + estimatedTextWidth;
                Draw.Sprite(visual, "Icon", icon, ink, order + 2, new Vector2(-total / 2f + iconSize / 2f, 0f),
                    new Vector2(iconSize, iconSize));
                Label.Create(visual, "Text", text, ink, order + 2,
                    new Vector2(-total / 2f + iconSize + 0.25f, 0f), textHeight, TextAnchor.MiddleLeft, true);
            }
            else if (icon != null)
            {
                float iconSize = size.y * 0.46f;
                Draw.Sprite(visual, "Icon", icon, ink, order + 2, Vector2.zero, new Vector2(iconSize, iconSize));
            }
            else
            {
                Label.Create(visual, "Text", text, ink, order + 2, Vector2.zero, textHeight, TextAnchor.MiddleCenter, true);
            }

            return UiButton.Attach(root, size, order, onClick, visual);
        }

        public static UiButton IconButton(Transform parent, string name, Sprite icon, Vector2 position, int order,
            Action onClick)
        {
            return Button(parent, name, null, icon, Color.white, Theme.Ink, new Vector2(1.3f, 1.3f), position, order,
                onClick);
        }

        /// <summary>뒤를 어둡게 가리고 가운데 카드를 띄우는 창. 뒤쪽 입력은 막힌다.</summary>
        public sealed class Modal
        {
            public Transform Root;
            public Transform Card;
            public Vector2 CardSize;
            SpriteRenderer dim;
            float dimAlpha;

            public void Close(Action done = null)
            {
                Transform card = Card;
                SpriteRenderer shade = dim;
                Transform root = Root;
                Sfx.Instance?.Close();
                Tween.Run(root, 0.2f, t =>
                {
                    card.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t);
                    Draw.SetAlpha(shade, Mathf.Lerp(dimAlpha, 0f, t));
                    foreach (SpriteRenderer r in card.GetComponentsInChildren<SpriteRenderer>())
                    {
                        Draw.SetAlpha(r, 1f - t);
                    }

                    foreach (TextMesh m in card.GetComponentsInChildren<TextMesh>())
                    {
                        Color c = m.color;
                        c.a = 1f - t;
                        m.color = c;
                    }
                }, Ease.InCubic, 0f, () =>
                {
                    UnityEngine.Object.Destroy(root.gameObject);
                    done?.Invoke();
                });
            }

            /// <param name="dimBackground">false 면 뒤를 어둡게 하지 않는다(완성된 그림을 가리지 않게). 입력은 그래도 막는다.</param>
            public static Modal Open(Transform parent, UiRoot ui, Vector2 cardSize, int order, bool dimBackground = true)
            {
                var modal = new Modal { CardSize = cardSize, dimAlpha = dimBackground ? Theme.Dim.a : 0f };
                Sfx.Instance?.Open();
                modal.Root = Draw.Node(parent, "Modal");
                modal.dim = Draw.Sprite(modal.Root, "Dim", SpriteFactory.Square(), Theme.WithAlpha(Theme.Dim, 0f), order,
                    Vector2.zero, new Vector2(ui.Width + 2f, UiRoot.Height + 2f));
                UiButton blocker = UiButton.Attach(modal.dim.transform, Vector2.one, order, null);
                blocker.Pressable = false;

                modal.Card = Draw.Node(modal.Root, "Card");
                Draw.Sprite(modal.Card, "Shadow", SpriteFactory.SoftShadow(), Theme.Shadow, order + 1,
                    new Vector2(0f, -0.3f), cardSize * 1.3f);
                Draw.Panel(modal.Card, "Face", cardSize, Theme.Card, order + 2, Vector2.zero, 0.6f);

                SpriteRenderer shade = modal.dim;
                Transform card = modal.Card;
                card.localScale = Vector3.one * 0.8f;
                float targetAlpha = modal.dimAlpha;
                Tween.Run(shade, 0.25f, t => Draw.SetAlpha(shade, targetAlpha * t));
                Tween.Run(card, 0.4f, t => card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, t), Ease.OutBack);
                return modal;
            }
        }
    }
}
