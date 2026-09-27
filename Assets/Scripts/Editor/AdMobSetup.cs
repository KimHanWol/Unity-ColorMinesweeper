#if GOOGLE_MOBILE_ADS
using System;
using System.Reflection;
using ColorMinesweeper.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// AdMob 앱 ID를 <see cref="AdMobConfig"/> 한 곳에서만 고치도록, 플러그인 설정 파일
    /// (Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset)에 옮겨 적는다.
    /// 앱 ID가 비어 있으면 안드로이드 앱이 시작하자마자 꺼지므로, 실제 ID가 없으면 테스트 앱 ID를 넣는다.
    /// 에디터를 열 때와 빌드 직전에 확인한다.
    /// </summary>
    [InitializeOnLoad]
    public sealed class AdMobSetup : IPreprocessBuildWithReport
    {
        static AdMobSetup()
        {
            EditorApplication.delayCall += () => Apply(false);
        }

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            Apply(true);
        }

        static void Apply(bool building)
        {
            // 플러그인의 설정 클래스는 internal 이라 이름으로 찾는다.
            Type type = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor");
            MethodInfo load = type?.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (load == null)
            {
                Debug.LogWarning("[AdMob] 플러그인 설정을 찾지 못했습니다. Assets > Google Mobile Ads > Settings 에서 앱 ID를 직접 넣어 주세요.");
                return;
            }

            var settings = (ScriptableObject)load.Invoke(null, null);
            bool changed = Set(type, settings, "GoogleMobileAdsAndroidAppId",
                string.IsNullOrEmpty(AdMobConfig.AndroidAppId) ? AdMobConfig.TestAndroidAppId : AdMobConfig.AndroidAppId);
            changed |= Set(type, settings, "GoogleMobileAdsIOSAppId",
                string.IsNullOrEmpty(AdMobConfig.IosAppId) ? AdMobConfig.TestIosAppId : AdMobConfig.IosAppId);
            if (changed)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log("[AdMob] 앱 ID를 플러그인 설정에 옮겨 적었습니다.");
            }

            if (building && !EditorUserBuildSettings.development && string.IsNullOrEmpty(AdMobConfig.AndroidAppId))
            {
                Debug.LogWarning("[AdMob] 실제 앱 ID가 없어 테스트 광고로 빌드합니다. 출시 전에 AdMobConfig 에 실제 ID를 넣어 주세요.");
            }
        }

        static bool Set(Type type, ScriptableObject settings, string property, string value)
        {
            PropertyInfo info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (info == null || (string)info.GetValue(settings) == value)
            {
                return false;
            }

            info.SetValue(settings, value);
            return true;
        }
    }
}
#endif
