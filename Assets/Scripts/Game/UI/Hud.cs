using System;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>플레이 화면 위쪽: 뒤로 가기, 스테이지 이름, 목숨.</summary>
    public sealed class Hud : MonoBehaviour
    {
        public const float BarHeight = 1.9f;

        SpriteRenderer[] hearts;

        public static Hud Create(Transform parent, UiRoot ui, int stageNumber, string stageName, Action onBack)
        {
            Transform root = Draw.Node(parent, "Hud");
            var hud = root.gameObject.AddComponent<Hud>();
            float half = ui.Safe.width / 2f;

            UiKit.IconButton(root, "Back", PixelGlyphs.Icon("back", PixelGlyphs.Back), new Vector2(-half + 1.05f, 0f), 100,
                onBack);

            Label.Create(root, "Name", stageName, Theme.Ink, 100, new Vector2(0f, 0.18f), 0.62f, TextAnchor.MiddleCenter, true);
            PixelText.Create(root, "Number", stageNumber.ToString(), Theme.SubInk, 100, new Vector2(0f, -0.42f), 0.3f);

            Sprite heart = PixelGlyphs.Icon("heart", PixelGlyphs.Heart);
            hud.hearts = new SpriteRenderer[PuzzleSession.MaxLives];
            for (int i = 0; i < hud.hearts.Length; i++)
            {
                hud.hearts[i] = Draw.Sprite(root, "Heart" + i, heart, Theme.Danger, 100,
                    new Vector2(half - 0.55f - (hud.hearts.Length - 1 - i) * 0.72f, 0f), new Vector2(0.56f, 0.56f));
            }

            return hud;
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

                Tween.Kill(heart.transform);
                float peak = alive ? 1.5f : 0.6f;
                Tween.Run(heart.transform, 0.45f, t =>
                {
                    float s = 0.56f * Mathf.LerpUnclamped(1f, peak, Ease.Pulse(t));
                    heart.transform.localScale = new Vector3(s, s, 1f);
                }, Ease.Linear);
            }
        }
    }
}
