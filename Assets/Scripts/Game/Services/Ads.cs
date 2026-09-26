using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 보상형 광고. 게임 코드는 이 인터페이스만 알고, 실제 SDK(AdMob 등)는 구현체를 바꿔 끼운다.
    /// 지금 광고 자리는 "목숨을 다 잃었을 때 광고 보고 이어 하기" 하나다.
    /// </summary>
    public interface IRewardedAds
    {
        bool IsReady { get; }

        /// <summary>광고를 끝까지 봤으면 true, 닫거나 실패하면 false 로 onFinished 를 부른다.</summary>
        void Show(Action<bool> onFinished);
    }

    public static class Ads
    {
        public static IRewardedAds Rewarded { get; set; } = new PlaceholderRewardedAds();
    }

    /// <summary>
    /// SDK 를 붙이기 전까지 쓰는 자리 표시용 구현. 잠깐 기다린 뒤 보상을 준다. 출시 빌드에 남기지 않는다.
    /// </summary>
    public sealed class PlaceholderRewardedAds : IRewardedAds
    {
        public bool IsReady => true;

        public void Show(Action<bool> onFinished)
        {
            Debug.Log("[Ads] 보상형 광고 자리(아직 SDK 없음) — 보상 지급");
            Tween.Delay(this, 0.8f, () => onFinished(true));
        }
    }
}
