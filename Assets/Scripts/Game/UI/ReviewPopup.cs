using System;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// "재미있게 하고 계신가요?" 리뷰 요청. 언제 묻는지는 <see cref="ReviewPrompt"/> 가 정하고, 여기서는 기록과 창만 다룬다.
    ///
    /// - 재밌다 → 스토어 리뷰를 부탁한다(리뷰 쓰기 / 나중에).
    /// - 아쉽다 → 리뷰 대신 의견을 메일로 받는다. 스토어로 보내지 않는다.
    /// 스테이지 목록에 들어올 때(보통 그림을 하나 완성한 뒤) 확인하고, 한 번 켠 동안에는 한 번만 띄운다.
    /// </summary>
    public static class ReviewPopup
    {
        const string FirstLaunchKey = "review.firstLaunch";
        const string LaunchesKey = "review.launches";
        const string StateKey = "review.state";
        const string PostponedAtKey = "review.postponedAt";
        const string FeedbackMail = "linkingmain@gmail.com";

        static bool shownThisRun;

        /// <summary>앱을 켤 때 한 번 부른다. 처음 켠 날과 켠 횟수를 남긴다.</summary>
        public static void RecordLaunch()
        {
            if (!PlayerPrefs.HasKey(FirstLaunchKey))
            {
                PlayerPrefs.SetString(FirstLaunchKey, DateTime.UtcNow.Ticks.ToString());
            }

            PlayerPrefs.SetInt(LaunchesKey, PlayerPrefs.GetInt(LaunchesKey, 0) + 1);
            PlayerPrefs.Save();
        }

        static double DaysSinceFirstLaunch()
        {
            if (!long.TryParse(PlayerPrefs.GetString(FirstLaunchKey, string.Empty), out long ticks))
            {
                return 0;
            }

            return (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc)).TotalDays;
        }

        static ReviewState State
        {
            get => (ReviewState)PlayerPrefs.GetInt(StateKey, (int)ReviewState.NotAsked);
            set
            {
                PlayerPrefs.SetInt(StateKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>물을 때가 됐으면 창을 띄운다.</summary>
        public static void TryShow(Transform parent, UiRoot ui, int order)
        {
            int cleared = StageCatalog.ClearedCount();
            if (shownThisRun || !ReviewPrompt.ShouldAsk(State, cleared, DaysSinceFirstLaunch(),
                    PlayerPrefs.GetInt(LaunchesKey, 0), PlayerPrefs.GetInt(PostponedAtKey, 0)))
            {
                return;
            }

            shownThisRun = true;
            UiKit.Dialog(parent, ui, order, Loc.T("review.ask.title"), null,
                Loc.T("review.ask.yes"), () => AskForReview(parent, ui, order, cleared),
                Loc.T("review.ask.no"), () => AskForFeedback(parent, ui, order));
        }

        static void AskForReview(Transform parent, UiRoot ui, int order, int cleared)
        {
            UiKit.Dialog(parent, ui, order, Loc.T("review.rate.title"), Loc.T("review.rate.body"),
                Loc.T("review.rate.yes"), () =>
                {
                    State = ReviewState.Finished;
                    StoreLink.Open();
                },
                Loc.T("review.rate.later"), () =>
                {
                    State = ReviewState.Postponed;
                    PlayerPrefs.SetInt(PostponedAtKey, cleared);
                    PlayerPrefs.Save();
                });
        }

        static void AskForFeedback(Transform parent, UiRoot ui, int order)
        {
            State = ReviewState.Finished;
            UiKit.Dialog(parent, ui, order, Loc.T("review.feedback.title"), Loc.T("review.feedback.body"),
                Loc.T("review.feedback.send"), () =>
                {
                    string subject = Uri.EscapeDataString("Pixel Clue " + Loc.T("review.feedback.subject"));
                    Application.OpenURL("mailto:" + FeedbackMail + "?subject=" + subject);
                },
                Loc.T("common.close"), null);
        }
    }
}
