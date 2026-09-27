using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    public enum Language
    {
        Korean = 0,
        English = 1,
    }

    /// <summary>
    /// 화면 문구 번역. 모든 UI 문구는 여기 표의 키로 부른다(화면 코드에 문장을 직접 쓰지 않는다).
    /// 숫자·이름이 들어가는 문장은 언어마다 어순이 달라 {0}, {1} 자리 표시자를 쓴다.
    /// 언어를 늘릴 때는 <see cref="Language"/> 에 항목을 더하고 표의 각 줄에 번역을 한 칸씩 더한다.
    /// 한국어 문장의 조사(이/가, 은/는)는 들어갈 말(색 이름, 배경)이 모두 받침으로 끝나서 고정해 둘 수 있다.
    /// 일본어·중국어는 지금 폰트(동글)에 글자가 없어 폰트를 함께 추가해야 한다.
    /// </summary>
    public static class Loc
    {
        const string Key = "settings.language";

        public static event Action Changed;

        static Language? current;

        public static readonly Language[] Available = { Language.Korean, Language.English };

        public static Language Current
        {
            get
            {
                if (current == null)
                {
                    int saved = PlayerPrefs.GetInt(Key, -1);
                    current = saved >= 0 ? (Language)saved
                        : Application.systemLanguage == SystemLanguage.Korean ? Language.Korean : Language.English;
                }

                return current.Value;
            }
            set
            {
                if (current == value)
                {
                    return;
                }

                current = value;
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>언어 선택 버튼에 쓰는 이름(그 언어로 쓴 이름).</summary>
        public static string NativeName(Language language)
        {
            return language == Language.Korean ? "한국어" : "English";
        }

        /// <summary>스테이지 파일의 names 에 쓰는 언어 코드.</summary>
        public static string Code(Language language)
        {
            return language == Language.Korean ? "ko" : "en";
        }

        public static string T(string key)
        {
            if (!Table.TryGetValue(key, out string[] row))
            {
                Debug.LogWarning("[Loc] 번역 키가 없습니다: " + key);
                return key;
            }

            int index = (int)Current;
            return index < row.Length && !string.IsNullOrEmpty(row[index]) ? row[index] : row[0];
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        /// <summary>한국어, 영어 순서.</summary>
        static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // 공통
            ["common.next"] = new[] { "다음", "Next" },
            ["common.close"] = new[] { "닫기", "Close" },

            // 메인
            ["title.tagline"] = new[] { "단서를 읽고 칠해서 도트 그림을 완성하세요", "Read the clues, paint the cells, reveal the pixel art" },
            ["title.start"] = new[] { "시작하기", "Start" },
            ["title.continue"] = new[] { "이어하기 · {0}", "Continue · {0}" },
            ["title.stages"] = new[] { "스테이지 선택", "Stages" },
            ["title.progress"] = new[] { "완성한 그림 {0} / {1}", "Pictures completed {0} / {1}" },

            // 스테이지 선택
            ["select.title"] = new[] { "스테이지 선택", "Select a stage" },
            ["select.devUnlocked"] = new[] { "DEV 전체 잠금 해제", "DEV: all unlocked" },

            // 플레이
            ["play.tutorial"] = new[] { "튜토리얼", "Tutorial" },
            ["play.background"] = new[] { "배경", "Background" },
            ["leave.title"] = new[] { "그만할까요?", "Leave this puzzle?" },
            ["leave.body"] = new[] { "지금 나가면 이 그림의 진행 상황은\n저장되지 않아요. 다음에 처음부터 풀어요.", "Your progress on this picture won't be saved.\nYou'll start over next time." },
            ["leave.stay"] = new[] { "계속하기", "Keep playing" },
            ["leave.leave"] = new[] { "나가기", "Leave" },
            ["clear.title"] = new[] { "{0} 완성!", "{0} complete!" },
            ["clear.perfect"] = new[] { "실수 없이 풀었어요", "Solved without a mistake" },
            ["clear.mistakes"] = new[] { "실수 {0}번", "Mistakes: {0}" },
            ["clear.unlocked"] = new[] { "다음 그림이 열렸어요!", "Next picture unlocked!" },
            ["clear.next"] = new[] { "다음 그림", "Next picture" },
            ["clear.list"] = new[] { "목록", "Stages" },
            ["clear.share"] = new[] { "공유", "Share" },
            ["clear.shareText"] = new[] { "Color Minesweeper {0}번 그림 「{1}」 완성! {2}", "I finished picture #{0} \"{1}\" in Color Minesweeper! {2}" },
            ["over.title"] = new[] { "목숨을 다 썼어요", "Out of hearts" },
            ["over.body"] = new[] { "광고를 보면 목숨 {0}개로 이어서 할 수 있어요", "Watch an ad to keep going with {0} heart" },
            ["over.revive"] = new[] { "광고 보고 이어 하기", "Watch ad to continue" },
            ["over.retry"] = new[] { "처음부터", "Start over" },
            ["tutorialClear.title"] = new[] { "튜토리얼 완료!", "Tutorial complete!" },
            ["tutorialClear.body"] = new[] { "그림을 완성하면 이름이 공개돼요.\n색을 고르고, 숫자를 보고, 칠하면 끝!", "Finish a picture to reveal its name.\nPick a color, read the numbers, paint!" },
            ["tutorialClear.start"] = new[] { "시작하기", "Start" },

            // 튜토리얼. {색} 자리에는 색 이름이나 "배경"이 들어간다.
            ["tut.intro.title"] = new[] { "숨은 그림을 찾아요", "Find the hidden picture" },
            ["tut.intro.detail"] = new[] { "숫자를 보고 칸을 칠하면 그림이 완성돼요.", "Paint cells using the numbers to complete the picture." },
            ["tut.bg.title"] = new[] { "배경도 색이에요", "The background is a color too" },
            ["tut.bg.detail"] = new[] { "아래 네모 칸(배경)을 눌러 보세요.", "Tap the square swatch (background) below." },
            ["tut.pick.title"] = new[] { "이 색을 골라요", "Pick this color" },
            ["tut.pick.detail"] = new[] { "아래에서 반짝이는 색을 눌러 보세요.", "Tap the glowing color below." },
            ["tut.self.title"] = new[] { "이번엔 직접 찾아봐요", "Now find it yourself" },
            // {0}=주변 칸 수, {1}=색, {2}=숫자, {3}=체크한 칸, {4}=남은 칸
            ["tut.self.none"] = new[] { "주변 {0}칸 중 {1}은 {2}칸이에요. 아직 하나도 안 보이니 {2}칸을 찾아야 해요.", "{2} of the {0} cells around it are {1}. None are showing yet, so find all {2}." },
            ["tut.self.some"] = new[] { "주변 {0}칸 중 {1}은 {2}칸이에요. 체크한 {3}칸을 빼면 {4}칸이 남았죠.", "{2} of the {0} cells around it are {1}. Minus the {3} checked, {4} are left." },
            ["tut.self.todo"] = new[] { "나머지 {0}칸은 어디일까요? 눌러서 칠해 보세요", "Where are the other {0}? Tap to paint them" },
            ["tut.here.one"] = new[] { "여기가 {0}이에요!", "This one is {0}!" },
            ["tut.here.many"] = new[] { "여기 {0}칸은 모두 {1}이에요!", "These {0} are all {1}!" },
            // {0}=주변 칸 수, {1}=색, {2}=숫자
            ["tut.need"] = new[] { "주변 {0}칸 중 {1}은 {2}칸이에요. ", "{2} of the {0} cells around it are {1}. " },
            ["tut.reason.none"] = new[] { "아직 하나도 안 보이는데, 가려진 칸이 딱 {0}칸이죠!", "None are showing yet, and exactly {0} are still hidden!" },
            ["tut.reason.some"] = new[] { "체크한 {0}칸을 빼면 {1}칸이 남았는데, 가려진 칸이 딱 {2}칸이죠!", "Minus the {0} checked, {1} are left, and exactly {2} are hidden!" },
            ["tut.todo.one"] = new[] { "반짝이는 칸을 눌러서 칠해 보세요", "Tap the glowing cell to paint it" },
            ["tut.todo.many"] = new[] { "반짝이는 칸을 모두 눌러서 칠해 보세요", "Tap every glowing cell to paint them" },
            ["tut.free.title"] = new[] { "이제 혼자 해 봐요!", "Now try it on your own!" },
            ["tut.free.detail"] = new[] { "틀리면 하트가 하나 줄어요.", "A wrong tap costs one heart." },
            ["tut.pickFirst"] = new[] { "먼저 아래에서 {0}을 골라요", "First pick {0} below" },
            ["tut.left"] = new[] { "좋아요! 남은 {0}칸도 칠해 보세요", "Nice! Paint the other {0} too" },

            // 색 이름(튜토리얼). 한국어는 모두 받침으로 끝난다.
            ["color.black"] = new[] { "검은색", "black" },
            ["color.white"] = new[] { "흰색", "white" },
            ["color.gray"] = new[] { "회색", "gray" },
            ["color.pink"] = new[] { "분홍색", "pink" },
            ["color.red"] = new[] { "빨간색", "red" },
            ["color.brown"] = new[] { "갈색", "brown" },
            ["color.orange"] = new[] { "주황색", "orange" },
            ["color.yellow"] = new[] { "노란색", "yellow" },
            ["color.green"] = new[] { "초록색", "green" },
            ["color.skyblue"] = new[] { "하늘색", "sky blue" },
            ["color.blue"] = new[] { "파란색", "blue" },
            ["color.purple"] = new[] { "보라색", "purple" },
            ["color.background"] = new[] { "배경", "background" },

            // 설정
            ["settings.title"] = new[] { "설정", "Settings" },
            ["settings.music"] = new[] { "배경음악", "Music" },
            ["settings.sfx"] = new[] { "효과음", "Sound effects" },
            ["settings.vibration"] = new[] { "진동", "Vibration" },
            ["settings.remaining"] = new[] { "남은 개수로 보기", "Show remaining counts" },
            ["settings.remaining.desc"] = new[] { "단서가 아직 안 열린 칸 수만 보여 줘요", "Clues count only unopened cells" },
            ["settings.language"] = new[] { "언어", "Language" },
            ["settings.tutorial"] = new[] { "튜토리얼 다시 보기", "Replay tutorial" },

            // 광고 자리(개발용)
            ["ad.rewarded"] = new[] { "보상형 광고 자리", "Rewarded ad slot" },
            ["ad.interstitial"] = new[] { "전면 광고 자리", "Interstitial ad slot" },
            ["ad.body"] = new[] { "SDK 를 붙이기 전 테스트 화면이에요", "Test screen until the ad SDK is added" },
        };
    }
}
