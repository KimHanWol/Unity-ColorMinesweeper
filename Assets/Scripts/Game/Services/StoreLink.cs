using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>Play 스토어의 이 앱 페이지. 리뷰 쓰기와 업데이트 안내가 같이 쓴다.</summary>
    public static class StoreLink
    {
        public const string PackageId = "com.kimhanwol.pixelclue";

        /// <summary>
        /// 폰에서는 Play 스토어 앱으로 바로 연다(market://). 에디터나 스토어 앱이 없는 곳에서는 웹 페이지를 연다.
        /// </summary>
        public static void Open()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL("market://details?id=" + PackageId);
#else
            Application.OpenURL("https://play.google.com/store/apps/details?id=" + PackageId);
#endif
        }

        /// <summary>설치된 앱의 버전 코드. 에디터처럼 알 수 없으면 0.</summary>
        public static int InstalledVersionCode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject manager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (AndroidJavaObject info = manager.Call<AndroidJavaObject>("getPackageInfo",
                           activity.Call<string>("getPackageName"), 0))
                {
                    return info.Get<int>("versionCode");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Store] 버전 코드를 읽지 못했습니다: " + e.Message);
                return 0;
            }
#else
            return 0;
#endif
        }
    }
}
