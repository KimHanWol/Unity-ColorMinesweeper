using System;
using ColorMinesweeper.Core;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 보상형 광고. 게임 코드는 이 인터페이스만 알고, 실제 SDK(AdMob 등)는 구현체를 바꿔 끼운다.
    /// 자리: 목숨을 다 잃었을 때 광고 보고 이어 하기.
    /// </summary>
    public interface IRewardedAds
    {
        bool IsReady { get; }

        /// <summary>광고를 끝까지 봤으면 true, 닫거나 실패하면 false 로 onFinished 를 부른다.</summary>
        void Show(Action<bool> onFinished);
    }

    /// <summary>전면 광고. 자리: 판을 완성하고 다음으로 넘어가는 순간(빈도는 <see cref="AdPacing"/>).</summary>
    public interface IInterstitialAds
    {
        bool IsReady { get; }

        /// <summary>광고가 닫히면(보여 주지 못했어도) onClosed 를 부른다.</summary>
        void Show(Action onClosed);
    }

    /// <summary>광고 정책과 기록. 광고 배치와 빈도의 근거는 docs/광고-수익화.md.</summary>
    public static class Ads
    {
        const string CompletionsKey = "ads.lifetimeCompletions";
        const string RemoveAdsKey = "iap.removeAds";

        public static IRewardedAds Rewarded { get; set; } = new PlaceholderRewardedAds();
        public static IInterstitialAds Interstitial { get; set; } = new PlaceholderInterstitialAds();

        static AdPacing pacing;

        static AdPacing Pacing => pacing ?? (pacing = new AdPacing(PlayerPrefs.GetInt(CompletionsKey, 0)));

        /// <summary>광고 제거 구매 여부. 인앱 결제를 붙이면 결제 완료 시 true 로 바꾼다. 보상형 광고는 그대로 남는다.</summary>
        public static bool AdsRemoved
        {
            get => PlayerPrefs.GetInt(RemoveAdsKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(RemoveAdsKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void OnStageCompleted()
        {
            Pacing.OnStageCompleted();
            PlayerPrefs.SetInt(CompletionsKey, Pacing.LifetimeCompletions);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 판을 마치고 "다음 그림"이나 "목록"을 눌렀을 때 부른다. 보여 줄 차례면 광고를 띄우고 닫힌 뒤 next 를,
        /// 아니면 바로 next 를 부른다. 플레이어가 누른 뒤에 뜨므로 연타로 광고를 잘못 누르는 일이 적다.
        /// </summary>
        public static void AfterStage(Action next)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!Pacing.ShouldShowInterstitial(now, AdsRemoved) || !Interstitial.IsReady)
            {
                next();
                return;
            }

            Pacing.OnInterstitialShown(now);
            Music.Duck(true);
            Interstitial.Show(() =>
            {
                Music.Duck(false);
                next();
            });
        }

        /// <summary>보상형 광고를 보여 주고, 끝까지 봤으면 빈도 기록에 남긴다.</summary>
        public static void ShowRewarded(Action<bool> onFinished)
        {
            Music.Duck(true);
            Rewarded.Show(rewarded =>
            {
                Music.Duck(false);
                if (rewarded)
                {
                    Pacing.OnRewardedShown(Time.realtimeSinceStartupAsDouble);
                }

                onFinished(rewarded);
            });
        }
    }

    /// <summary>SDK 를 붙이기 전까지 쓰는 보상형 광고 자리. 잠깐 기다린 뒤 보상을 준다. 출시 빌드에 남기지 않는다.</summary>
    public sealed class PlaceholderRewardedAds : IRewardedAds
    {
        public bool IsReady => true;

        public void Show(Action<bool> onFinished)
        {
            Debug.Log("[Ads] 보상형 광고 자리(아직 SDK 없음) — 보상 지급");
            PlaceholderAdScreen.Show("보상형 광고 자리", () => onFinished(true));
        }
    }

    /// <summary>SDK 를 붙이기 전까지 쓰는 전면 광고 자리. 흐름을 눈으로 확인할 수 있게 잠깐 화면을 덮는다.</summary>
    public sealed class PlaceholderInterstitialAds : IInterstitialAds
    {
        public bool IsReady => true;

        public void Show(Action onClosed)
        {
            Debug.Log("[Ads] 전면 광고 자리(아직 SDK 없음)");
            PlaceholderAdScreen.Show("전면 광고 자리", onClosed);
        }
    }

    /// <summary>광고 자리 표시 화면. 화면 전체를 어둡게 덮고 1.2초 뒤 닫힌다.</summary>
    static class PlaceholderAdScreen
    {
        public static void Show(string title, Action onClosed)
        {
            GameApp app = GameApp.Instance;
            if (app == null)
            {
                onClosed();
                return;
            }

            Transform root = UiRoot.NewLayerRoot("PlaceholderAd");
            root.SetParent(app.transform, false);
            UiKit.Modal modal = UiKit.Modal.Open(root, app.Ui, new Vector2(7f, 4f), 900);
            Label.Create(modal.Card, "Title", title, Theme.Ink, 910, new Vector2(0f, 0.5f), 0.6f, TextAnchor.MiddleCenter, true);
            Label.Create(modal.Card, "Body", "SDK 를 붙이기 전 테스트 화면이에요", Theme.SubInk, 910, new Vector2(0f, -0.5f), 0.36f);
            Tween.Delay(root, 1.2f, () => modal.Close(() =>
            {
                UnityEngine.Object.Destroy(root.gameObject);
                onClosed();
            }));
        }
    }
}
