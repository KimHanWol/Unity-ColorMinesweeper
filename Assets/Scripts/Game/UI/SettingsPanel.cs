using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>효과음, 진동, 단서 표시 방식을 켜고 끄는 창. 목록 화면과 플레이 화면 양쪽에서 연다.</summary>
    public static class SettingsPanel
    {
        const float RowHeight = 1.25f;

        public static UiKit.Modal Open(Transform parent, UiRoot ui, int order, Action onClosed)
        {
            UiKit.Modal modal = UiKit.Modal.Open(parent, ui, new Vector2(7.6f, 8.2f), order);
            Transform card = modal.Card;
            int o = order + 10;

            Label.Create(card, "Title", "설정", Theme.Ink, o, new Vector2(0f, 3.3f), 0.75f, TextAnchor.MiddleCenter, true);

            Toggle(card, "효과음", null, new Vector2(0f, 2.0f), o, () => Settings.Sound, v => Settings.Sound = v);
            Toggle(card, "진동", null, new Vector2(0f, 2.0f - RowHeight), o, () => Settings.Vibration,
                v => Settings.Vibration = v);
            Toggle(card, "남은 개수로 보기", "단서가 아직 안 열린 칸 수만 보여 줘요",
                new Vector2(0f, 2.0f - RowHeight * 2f - 0.1f), o, () => Settings.ShowRemaining,
                v => Settings.ShowRemaining = v);

            Label.Create(card, "Tip", "배경색은 단서에 나오지 않아요.\n팔레트의 네모 칸이 이 그림의 배경색이에요.",
                Theme.SubInk, o, new Vector2(0f, -1.55f), 0.34f);

            UiKit.Button(card, "Close", "닫기", null, Theme.Accent, Color.white, new Vector2(5.6f, 1.2f),
                new Vector2(0f, -3.1f), o, () => modal.Close(onClosed));
            return modal;
        }

        /// <summary>왼쪽에 이름(과 설명), 오른쪽에 켜짐/꺼짐 스위치. 줄 전체를 눌러 바꾼다.</summary>
        static void Toggle(Transform parent, string title, string description, Vector2 position, int order,
            Func<bool> get, Action<bool> set)
        {
            Transform row = Draw.Node(parent, "Toggle " + title, position);
            float left = -3.2f;
            if (description == null)
            {
                Label.Create(row, "Title", title, Theme.Ink, order, new Vector2(left, 0f), 0.46f, TextAnchor.MiddleLeft);
            }
            else
            {
                Label.Create(row, "Title", title, Theme.Ink, order, new Vector2(left, 0.2f), 0.46f, TextAnchor.MiddleLeft);
                Label.Create(row, "Description", description, Theme.SubInk, order, new Vector2(left, -0.3f), 0.3f,
                    TextAnchor.MiddleLeft);
            }

            var trackSize = new Vector2(1.4f, 0.76f);
            var trackCenter = new Vector2(2.5f, 0f);
            SpriteRenderer track = Draw.Panel(row, "Track", trackSize, Theme.Locked, order, trackCenter, 0.38f);
            SpriteRenderer knob = Draw.Sprite(row, "Knob", SpriteFactory.Circle(), Color.white, order + 1, trackCenter,
                new Vector2(0.6f, 0.6f));

            void Show(bool on, bool animate)
            {
                Color toColor = on ? Theme.Accent : Theme.Locked;
                Vector3 toPosition = new Vector3(trackCenter.x + (on ? 0.32f : -0.32f), 0f, 0f);
                if (!animate)
                {
                    track.color = toColor;
                    knob.transform.localPosition = toPosition;
                    return;
                }

                Color fromColor = track.color;
                Vector3 fromPosition = knob.transform.localPosition;
                Tween.Kill(knob);
                Tween.Run(knob, 0.25f, t =>
                {
                    track.color = Color.Lerp(fromColor, toColor, Mathf.Clamp01(t));
                    knob.transform.localPosition = Vector3.LerpUnclamped(fromPosition, toPosition, t);
                }, Ease.OutBack);
            }

            Show(get(), false);
            UiButton.Attach(row, new Vector2(7f, RowHeight), order + 2, () =>
            {
                bool value = !get();
                set(value);
                Show(value, true);
            }).Pressable = false;
        }
    }
}
