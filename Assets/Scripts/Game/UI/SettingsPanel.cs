using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 설정 창. 배경음악·효과음 볼륨 슬라이더와 진동, 단서 표시 방식을 바꾼다.
    /// 메인 화면, 스테이지 목록, 플레이 화면 모두 이 창을 연다.
    /// </summary>
    public static class SettingsPanel
    {
        const float RowHeight = 1.3f;
        const float SliderWidth = 3.4f;

        static float lastPreview;

        /// <param name="allowTutorial">튜토리얼 다시 보기 버튼을 둘지. 판 도중(플레이 화면)에는 진행이 사라지므로 두지 않는다.</param>
        public static UiKit.Modal Open(Transform parent, UiRoot ui, int order, Action onClosed, bool allowTutorial = false)
        {
            // 언어와 튜토리얼 다시 보기는 판 도중(플레이 화면)에는 두지 않는다. 화면을 다시 만들면 진행이 사라진다.
            // 위에서부터 차례로 쌓는다. 자리를 먼저 모두 계산해 카드 높이를 정하므로 줄이 늘어도 겹치지 않는다.
            int rows = allowTutorial ? 5 : 4;
            float titleY = -0.9f;
            float firstRowY = titleY - 1.35f;
            // 마지막 줄 아래 끝. 줄 한가운데에서 반 줄 내려간 곳이다.
            float bottom = firstRowY - (rows - 1) * RowHeight - RowHeight / 2f;
            float tutorialY = 0f;
            if (allowTutorial)
            {
                tutorialY = bottom - 0.3f - 0.525f;
                bottom = tutorialY - 0.525f;
            }

            float closeY = bottom - 0.3f - 0.6f;
            float height = 0.6f - (closeY - 0.6f);
            float top = height / 2f;

            UiKit.Modal modal = UiKit.Modal.Open(parent, ui, new Vector2(7.8f, height), order);
            Transform card = modal.Card;
            int o = order + 10;

            Label.Create(card, "Title", Loc.T("settings.title"), Theme.Ink, o, new Vector2(0f, top + titleY), 0.75f,
                TextAnchor.MiddleCenter, true);
            float y = top + firstRowY;

            Volume(card, Loc.T("settings.music"), new Vector2(0f, y), o, Settings.MusicVolume, v => Settings.MusicVolume = v, false);
            y -= RowHeight;
            Volume(card, Loc.T("settings.sfx"), new Vector2(0f, y), o, Settings.SfxVolume, v => Settings.SfxVolume = v, true);
            y -= RowHeight;
            Toggle(card, Loc.T("settings.vibration"), null, new Vector2(0f, y), o, () => Settings.Vibration, v => Settings.Vibration = v);
            y -= RowHeight;
            Toggle(card, Loc.T("settings.remaining"), Loc.T("settings.remaining.desc"), new Vector2(0f, y), o,
                () => Settings.ShowRemaining, v => Settings.ShowRemaining = v);
            if (allowTutorial)
            {
                y -= RowHeight;
                LanguageRow(card, new Vector2(0f, y), o);
            }

            if (allowTutorial)
            {
                UiKit.Button(card, "Tutorial", Loc.T("settings.tutorial"), null, Theme.HiddenTile, Theme.Ink, new Vector2(5.6f, 1.05f),
                    new Vector2(0f, top + tutorialY), o, () =>
                    {
                        Settings.Flush();
                        GameApp.Instance?.ShowTutorial();
                    });
            }

            UiKit.Button(card, "Close", Loc.T("common.close"), null, Theme.Accent, Color.white, new Vector2(5.6f, 1.2f),
                new Vector2(0f, top + closeY), o, () =>
                {
                    Settings.Flush();
                    modal.Close(onClosed);
                });
            return modal;
        }

        /// <summary>
        /// 언어 고르기. 언어 이름은 그 언어로 쓴다(한국어 / English). 고르면 지금 화면을 새 언어로 다시 그린다.
        /// </summary>
        static void LanguageRow(Transform parent, Vector2 position, int order)
        {
            Transform row = Draw.Node(parent, "Language", position);
            Label.Create(row, "Title", Loc.T("settings.language"), Theme.Ink, order, new Vector2(-3.2f, 0f), 0.46f,
                TextAnchor.MiddleLeft);
            Language[] languages = Loc.Available;
            const float width = 1.75f;
            for (int i = 0; i < languages.Length; i++)
            {
                Language language = languages[i];
                bool on = language == Loc.Current;
                float x = 2.95f - (languages.Length - 1 - i) * (width + 0.12f);
                UiKit.Button(row, Loc.NativeName(language), Loc.NativeName(language), null,
                    on ? Theme.Accent : Theme.HiddenTile, on ? Color.white : Theme.Ink, new Vector2(width, 0.8f),
                    new Vector2(x - width / 2f + 0.3f, 0f), order + 1, () =>
                    {
                        if (language == Loc.Current)
                        {
                            return;
                        }

                        Settings.Flush();
                        Loc.Current = language;
                        GameApp.Instance?.RefreshForLanguage();
                    });
            }
        }

        /// <summary>왼쪽에 이름, 오른쪽에 볼륨 슬라이더. 효과음은 끄는 동안 새 볼륨으로 짧게 들려준다.</summary>
        static void Volume(Transform parent, string title, Vector2 position, int order, float value, Action<float> set,
            bool preview)
        {
            Transform row = Draw.Node(parent, "Volume " + title, position);
            Label.Create(row, "Title", title, Theme.Ink, order, new Vector2(-3.2f, 0f), 0.46f, TextAnchor.MiddleLeft);
            UiSlider.Create(row, "Slider", new Vector2(1.35f, 0f), SliderWidth, value, order, v =>
            {
                set(v);
                if (preview && Time.unscaledTime - lastPreview > 0.12f)
                {
                    lastPreview = Time.unscaledTime;
                    Sfx.Instance?.Reveal(Mathf.RoundToInt(v * 6f));
                }
            }, Settings.Flush);
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
