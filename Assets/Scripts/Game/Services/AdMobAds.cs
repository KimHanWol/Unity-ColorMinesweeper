#if GOOGLE_MOBILE_ADS
using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// Google AdMob 연결. 시작할 때 동의(UMP)를 받고 SDK 를 초기화한 뒤, <see cref="Ads"/> 의 자리 표시 광고를
    /// 실제 광고로 바꿔 끼운다. 광고는 미리 불러 두고, 보여 준 뒤에는 바로 다음 광고를 불러 둔다.
    /// 플러그인(com.google.ads.mobile)이 있을 때만 컴파일된다(asmdef 의 versionDefines 가 GOOGLE_MOBILE_ADS 를 켠다).
    /// </summary>
    public static class AdMobAds
    {
        static bool started;

        public static void Initialize()
        {
            if (started)
            {
                return;
            }

            started = true;
            // 광고 콜백을 Unity 메인 스레드에서 받는다(화면을 바꾸는 코드가 콜백에서 돈다).
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            // 유럽 등 동의가 필요한 곳에서는 첫 실행 때 동의 창을 띄운다. 그 밖의 지역에서는 아무것도 뜨지 않는다.
            ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning("[AdMob] 동의 정보를 받지 못했습니다: " + updateError.Message);
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                {
                    if (formError != null)
                    {
                        Debug.LogWarning("[AdMob] 동의 창을 띄우지 못했습니다: " + formError.Message);
                    }

                    if (ConsentInformation.CanRequestAds())
                    {
                        StartSdk();
                    }
                });
            });

            // 지난번에 이미 동의를 받았으면 동의 정보를 새로 받는 동안 기다리지 않고 바로 시작한다.
            if (ConsentInformation.CanRequestAds())
            {
                StartSdk();
            }
        }

        static bool sdkStarted;

        static void StartSdk()
        {
            if (sdkStarted)
            {
                return;
            }

            sdkStarted = true;
            MobileAds.Initialize(_ =>
            {
                Debug.Log("[AdMob] 초기화 완료" + (AdMobConfig.UseTestAds ? " (테스트 광고)" : string.Empty));
                Ads.SetRewarded(RewardedPlacement.Revive, new AdMobRewardedAds(AdMobConfig.RewardedUnit(RewardedPlacement.Revive)));
                Ads.SetRewarded(RewardedPlacement.Hint, new AdMobRewardedAds(AdMobConfig.RewardedUnit(RewardedPlacement.Hint)));
                Ads.Interstitial = new AdMobInterstitialAds(AdMobConfig.InterstitialUnit);
            });
        }

        /// <summary>불러오기에 실패하면 5초, 10초, 20초… 최대 2분 간격으로 다시 시도한다.</summary>
        internal static float RetryDelay(int failures)
        {
            return Mathf.Min(120f, 5f * Mathf.Pow(2f, Mathf.Max(0, failures - 1)));
        }

        /// <summary>SDK 콜백이 아닌 곳에서 잠시 뒤에 할 일을 거는 데 쓰는 빈 오브젝트.</summary>
        internal static GameObject Runner
        {
            get
            {
                if (runner == null)
                {
                    runner = new GameObject("AdMob");
                    UnityEngine.Object.DontDestroyOnLoad(runner);
                }

                return runner;
            }
        }

        static GameObject runner;
    }

    /// <summary>AdMob 보상형 광고 한 자리(광고 단위 하나).</summary>
    public sealed class AdMobRewardedAds : IRewardedAds
    {
        readonly string unitId;
        RewardedAd ad;
        bool loading;
        int failures;

        public AdMobRewardedAds(string unitId)
        {
            this.unitId = unitId;
            Load();
        }

        public bool IsReady => ad != null && ad.CanShowAd();

        public void Show(Action<bool> onFinished)
        {
            if (!IsReady)
            {
                onFinished(false);
                Load();
                return;
            }

            RewardedAd showing = ad;
            ad = null;
            bool earned = false;
            bool finished = false;

            void Finish()
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                showing.Destroy();
                onFinished(earned);
                Load();
            }

            showing.OnAdFullScreenContentClosed += Finish;
            showing.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning("[AdMob] 보상형 광고를 보여 주지 못했습니다: " + error.GetMessage());
                Finish();
            };
            try
            {
                showing.Show(_ => earned = true);
            }
            catch (Exception e)
            {
                // 광고를 띄우다 예외가 나도 버튼이 눌린 채 멈추거나 음악이 꺼진 채 남지 않게 끝낸 것으로 친다.
                Debug.LogWarning("[AdMob] 보상형 광고를 띄우지 못했습니다: " + e.Message);
                Finish();
            }
        }

        void Load()
        {
            if (loading || ad != null)
            {
                return;
            }

            loading = true;
            RewardedAd.Load(unitId, new AdRequest(), (loaded, error) =>
            {
                loading = false;
                if (error != null || loaded == null)
                {
                    failures++;
                    Debug.LogWarning("[AdMob] 보상형 광고를 불러오지 못했습니다: " + error?.GetMessage());
                    Tween.Delay(AdMobAds.Runner, AdMobAds.RetryDelay(failures), Load);
                    return;
                }

                failures = 0;
                ad = loaded;
            });
        }
    }

    /// <summary>AdMob 전면 광고.</summary>
    public sealed class AdMobInterstitialAds : IInterstitialAds
    {
        readonly string unitId;
        InterstitialAd ad;
        bool loading;
        int failures;

        public AdMobInterstitialAds(string unitId)
        {
            this.unitId = unitId;
            Load();
        }

        public bool IsReady => ad != null && ad.CanShowAd();

        public void Show(Action onClosed)
        {
            if (!IsReady)
            {
                onClosed();
                Load();
                return;
            }

            InterstitialAd showing = ad;
            ad = null;
            bool finished = false;

            void Finish()
            {
                if (finished)
                {
                    return;
                }

                finished = true;
                showing.Destroy();
                onClosed();
                Load();
            }

            showing.OnAdFullScreenContentClosed += Finish;
            showing.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning("[AdMob] 전면 광고를 보여 주지 못했습니다: " + error.GetMessage());
                Finish();
            };
            try
            {
                showing.Show();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AdMob] 전면 광고를 띄우지 못했습니다: " + e.Message);
                Finish();
            }
        }

        void Load()
        {
            if (loading || ad != null)
            {
                return;
            }

            loading = true;
            InterstitialAd.Load(unitId, new AdRequest(), (loaded, error) =>
            {
                loading = false;
                if (error != null || loaded == null)
                {
                    failures++;
                    Debug.LogWarning("[AdMob] 전면 광고를 불러오지 못했습니다: " + error?.GetMessage());
                    Tween.Delay(AdMobAds.Runner, AdMobAds.RetryDelay(failures), Load);
                    return;
                }

                failures = 0;
                ad = loaded;
            });
        }
    }
}
#endif
