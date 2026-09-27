using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// AdMob 앱 ID와 광고 단위 ID. AdMob 에서 만든 실제 ID를 아래 빈칸에 넣는다.
    ///
    /// 실제 광고는 출시 빌드(개발 빌드가 아님)에서 실제 ID가 들어 있을 때만 쓴다. 에디터와 개발 빌드는 늘
    /// 구글 공식 테스트 ID를 써서, 개발하다 자기 광고를 눌러 계정이 정지되는 일을 막는다.
    /// 앱 ID는 빌드에 들어가는 설정 파일(Assets/GoogleMobileAds/Resources)에 에디터 도구가 옮겨 적는다.
    /// </summary>
    public static class AdMobConfig
    {
        // 실제 ID (AdMob → 앱 → 광고 단위). 비어 있으면 테스트 ID로 돈다.
        public const string AndroidAppId = "ca-app-pub-7890402400874906~1143647502";
        const string AndroidRevive = "ca-app-pub-7890402400874906/5303393529";
        const string AndroidHint = "ca-app-pub-7890402400874906/1088816343";
        const string AndroidInterstitial = "ca-app-pub-7890402400874906/1555720204";

        public const string IosAppId = "";
        const string IosRevive = "";
        const string IosHint = "";
        const string IosInterstitial = "";

        // 구글 공식 테스트 ID (https://developers.google.com/admob/unity/test-ads).
        public const string TestAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
        const string TestAndroidRewarded = "ca-app-pub-3940256099942544/5224354917";
        const string TestAndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";

        public const string TestIosAppId = "ca-app-pub-3940256099942544~1458002511";
        const string TestIosRewarded = "ca-app-pub-3940256099942544/1712485313";
        const string TestIosInterstitial = "ca-app-pub-3940256099942544/4411468910";

        static bool Ios => Application.platform == RuntimePlatform.IPhonePlayer;

        /// <summary>테스트 광고를 쓰는지. 에디터와 개발 빌드는 늘 테스트 광고다.</summary>
        public static bool UseTestAds => Application.isEditor || Debug.isDebugBuild;

        public static string RewardedUnit(RewardedPlacement placement)
        {
            string real = placement == RewardedPlacement.Hint
                ? (Ios ? IosHint : AndroidHint)
                : (Ios ? IosRevive : AndroidRevive);
            return Pick(real, Ios ? TestIosRewarded : TestAndroidRewarded);
        }

        public static string InterstitialUnit => Pick(Ios ? IosInterstitial : AndroidInterstitial,
            Ios ? TestIosInterstitial : TestAndroidInterstitial);

        static string Pick(string real, string test)
        {
            return UseTestAds || string.IsNullOrEmpty(real) ? test : real;
        }
    }
}
