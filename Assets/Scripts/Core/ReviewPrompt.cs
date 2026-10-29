namespace ColorMinesweeper.Core
{
    /// <summary>리뷰 요청을 어디까지 진행했는지.</summary>
    public enum ReviewState
    {
        /// <summary>아직 묻지 않았다.</summary>
        NotAsked,

        /// <summary>"나중에"를 눌렀다. 판을 더 깬 뒤 한 번 더 묻는다.</summary>
        Postponed,

        /// <summary>리뷰 쓰기로 갔거나, 재미없다고 답했다. 다시 묻지 않는다.</summary>
        Finished,
    }

    /// <summary>
    /// "재미있게 하고 계신가요?" 리뷰 요청을 언제 띄울지 정하는 규칙.
    ///
    /// - 그림을 <see cref="MinCleared"/> 개 이상 완성했고,
    /// - 처음 켠 날로부터 <see cref="MinDays"/> 일이 지났거나 <see cref="MinLaunches"/> 번 이상 켰을 때 처음 묻는다.
    /// - "나중에"를 누르면 그 뒤로 <see cref="PostponeCleared"/> 개를 더 완성했을 때 한 번 더 묻는다.
    /// - 재미없다고 답했거나 리뷰 쓰기로 간 뒤에는 다시 묻지 않는다.
    ///
    /// 날짜와 횟수는 호출하는 쪽이 넘긴다. 그래서 Unity 없이 테스트할 수 있다.
    /// </summary>
    public static class ReviewPrompt
    {
        public const int MinCleared = 8;
        public const double MinDays = 2;
        public const int MinLaunches = 4;
        public const int PostponeCleared = 15;

        /// <param name="cleared">지금까지 완성한 그림 수.</param>
        /// <param name="daysSinceFirstLaunch">처음 켠 뒤 지난 날 수.</param>
        /// <param name="launches">앱을 켠 횟수.</param>
        /// <param name="clearedWhenPostponed">"나중에"를 눌렀을 때 완성해 둔 그림 수.</param>
        public static bool ShouldAsk(ReviewState state, int cleared, double daysSinceFirstLaunch, int launches,
            int clearedWhenPostponed)
        {
            switch (state)
            {
                case ReviewState.NotAsked:
                    return cleared >= MinCleared && (daysSinceFirstLaunch >= MinDays || launches >= MinLaunches);
                case ReviewState.Postponed:
                    return cleared >= clearedWhenPostponed + PostponeCleared;
                default:
                    return false;
            }
        }
    }
}
