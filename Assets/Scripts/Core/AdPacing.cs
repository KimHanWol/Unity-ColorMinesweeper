namespace ColorMinesweeper.Core
{
    /// <summary>
    /// 전면 광고를 언제 보여도 되는지 정하는 규칙. 근거는 docs/광고-수익화.md.
    ///
    /// - 처음 <see cref="GraceCompletions"/> 판은 광고 없이 게임에 빠져들게 둔다(초반 1색 구간).
    /// - 완성 <see cref="EveryNthCompletion"/> 판마다 한 번까지만.
    /// - 직전 전면 광고에서 <see cref="MinSecondsBetween"/> 초가 지나야 한다.
    /// - 보상형 광고(부활)를 본 직후 <see cref="MinSecondsAfterRewarded"/> 초 동안은 전면 광고를 쉰다.
    /// - 광고 제거를 산 사람에게는 전면 광고를 보이지 않는다(보상형은 스스로 고르는 것이라 남긴다).
    ///
    /// 시간은 호출하는 쪽이 넘긴다(초). 그래서 Unity 없이 테스트할 수 있다.
    /// </summary>
    public sealed class AdPacing
    {
        public const int GraceCompletions = 8;
        public const int EveryNthCompletion = 3;
        public const double MinSecondsBetween = 180;
        public const double MinSecondsAfterRewarded = 90;

        /// <summary>지금까지(기기에 남은) 완성한 판 수. 다시 푼 판도 센다.</summary>
        public int LifetimeCompletions { get; private set; }

        int completionsSinceInterstitial;
        double lastInterstitial = double.NegativeInfinity;
        double lastRewarded = double.NegativeInfinity;

        public AdPacing(int lifetimeCompletions)
        {
            LifetimeCompletions = lifetimeCompletions;
        }

        public void OnStageCompleted()
        {
            LifetimeCompletions++;
            completionsSinceInterstitial++;
        }

        /// <summary>판을 마치고 다음으로 넘어가는 순간에 부른다. true 면 지금 전면 광고를 보여도 된다.</summary>
        public bool ShouldShowInterstitial(double now, bool adsRemoved)
        {
            if (adsRemoved || LifetimeCompletions <= GraceCompletions)
            {
                return false;
            }

            if (completionsSinceInterstitial < EveryNthCompletion)
            {
                return false;
            }

            return now - lastInterstitial >= MinSecondsBetween && now - lastRewarded >= MinSecondsAfterRewarded;
        }

        public void OnInterstitialShown(double now)
        {
            lastInterstitial = now;
            completionsSinceInterstitial = 0;
        }

        public void OnRewardedShown(double now)
        {
            lastRewarded = now;
        }
    }
}
