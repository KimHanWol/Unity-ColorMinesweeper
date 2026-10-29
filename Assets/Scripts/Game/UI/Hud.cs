using System;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>플레이 화면 위쪽: 왼쪽 뒤로 가기, 가운데 스테이지 이름과 목숨, 오른쪽 설정.</summary>
    public sealed class Hud : MonoBehaviour
    {
        public const float BarHeight = 1.9f;
        const float HeartSize = 0.46f;

        SpriteRenderer[] hearts;
        Label title;

        /// <summary>위쪽 버튼(뒤로, 힌트, 설정)의 원래 크기, 화면 가장자리와의 여백, 버튼 사이 간격.</summary>
        const float ButtonSize = 1.3f;
        const float EdgeMargin = 0.4f;
        const float ButtonGap = 0.15f;

        /// <summary>
        /// 위쪽 버튼의 크기 비율. 보통 폰(9:20 이상 넓은 화면)에서는 1이고, 더 좁으면 가운데 제목 자리(2.4)가 남을 만큼 줄인다.
        /// </summary>
        public static float ButtonScale(UiRoot ui)
        {
            float half = ui.Safe.width / 2f;
            return Mathf.Clamp((half - EdgeMargin - ButtonGap - 0.15f - 1.2f) / 2f / ButtonSize, 0.75f, 1f);
        }

        /// <summary>오른쪽 끝에서 slot 번째(0 설정, 1 힌트) 버튼 가운데의 x. 뒤로 버튼은 -ButtonX(ui, 0) 이다.</summary>
        public static float ButtonX(UiRoot ui, int slot)
        {
            float size = ButtonSize * ButtonScale(ui);
            return ui.Safe.width / 2f - EdgeMargin - size / 2f - slot * (size + ButtonGap);
        }

        /// <summary>title 은 <see cref="StageTitle"/> 규칙대로 만든 제목(완성 전에는 이름 대신 ???).</summary>
        public static Hud Create(Transform parent, UiRoot ui, string title, Action onBack, Action onSettings)
        {
            Transform root = Draw.Node(parent, "Hud");
            var hud = root.gameObject.AddComponent<Hud>();
            float half = ui.Safe.width / 2f;

            // 버튼은 화면이 좁으면(플립 같은 21:9 폰) 조금 작게 해서, 제목과 하트가 늘 화면 정가운데에 오게 한다.
            float scale = ButtonScale(ui);
            UiKit.IconButton(root, "Back", Icons.Back, new Vector2(-ButtonX(ui, 0), 0f), 100, onBack)
                .transform.localScale = new Vector3(scale, scale, 1f);
            UiKit.IconButton(root, "Settings", Icons.Settings, new Vector2(ButtonX(ui, 0), 0f), 100, onSettings)
                .transform.localScale = new Vector3(scale, scale, 1f);

            float buttonSize = ButtonSize * scale;
            float room = 2f * (half - EdgeMargin - 2f * buttonSize - ButtonGap - 0.15f);
            hud.title = Label.Create(root, "Name", title, Theme.Ink, 100, new Vector2(0f, 0.32f), 0.58f,
                TextAnchor.MiddleCenter, true)
                .FitWidth(Mathf.Max(2f, room));

            Sprite heart = Icons.Heart;
            hud.hearts = new SpriteRenderer[PuzzleSession.MaxLives];
            for (int i = 0; i < hud.hearts.Length; i++)
            {
                hud.hearts[i] = Draw.Sprite(root, "Heart" + i, heart, Theme.Danger, 100,
                    new Vector2((i - (hud.hearts.Length - 1) / 2f) * 0.62f, -0.45f), new Vector2(HeartSize, HeartSize));
            }

            return hud;
        }

        /// <summary>완성했을 때 ??? 자리에 이름이 톡 튀어나오며 바뀐다.</summary>
        public void RevealTitle(string text)
        {
            if (title.Text == text)
            {
                return;
            }

            Sfx.Instance?.NameReveal();
            Transform t = title.transform;
            Vector3 rest = t.localScale;
            Tween.Kill(t);
            Tween.Run(t, 0.18f, k => t.localScale = Vector3.LerpUnclamped(rest, new Vector3(rest.x, 0f, 1f), k), Ease.InCubic,
                0f, () =>
                {
                    title.Text = text;
                    title.SetColor(Theme.Accent);
                    // 새 이름 길이에 맞춰 줄어든 크기로 펼친다(이름이 길면 위쪽 버튼과 겹치지 않게).
                    Vector3 fitted = t.localScale;
                    Tween.Run(t, 0.45f, k => t.localScale = Vector3.LerpUnclamped(new Vector3(fitted.x, 0f, 1f), fitted, k),
                        Ease.OutBack);
                });
        }

        /// <summary>잃은 목숨은 회색으로 줄어들고, 남은 목숨은 빨갛게 유지된다. animate 면 바뀐 하트가 튄다.</summary>
        public void SetLives(int lives, bool animate)
        {
            for (int i = 0; i < hearts.Length; i++)
            {
                SpriteRenderer heart = hearts[i];
                bool alive = i < lives;
                Color target = alive ? Theme.Danger : Theme.Locked;
                bool changed = heart.color != target;
                heart.color = target;
                if (!animate || !changed)
                {
                    continue;
                }

                // 잃은 하트에서는 빨간 조각이 떨어져 나가고, 되찾은 하트에서는 조각이 튀며 테두리가 퍼진다.
                if (alive)
                {
                    Fx.Pop(transform, heart.transform.localPosition, Theme.Danger, 101, 0.6f);
                }
                else
                {
                    Fx.Burst(transform, heart.transform.localPosition, Theme.Danger, 101, 6, 0.6f, 0.13f);
                }

                Tween.Kill(heart.transform);
                float peak = alive ? 1.5f : 0.6f;
                Tween.Run(heart.transform, 0.45f, t =>
                {
                    float s = HeartSize * Mathf.LerpUnclamped(1f, peak, Ease.Pulse(t));
                    heart.transform.localScale = new Vector3(s, s, 1f);
                }, Ease.Linear);
            }
        }
    }
}
