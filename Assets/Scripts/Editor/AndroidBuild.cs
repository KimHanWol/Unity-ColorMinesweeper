using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// Google Play 에 올릴 Android 앱 번들(.aab)을 만든다.
    ///
    /// - 에디터: Tools > Pixel Clue > Build Android Release
    /// - 배치 모드: Unity.exe -batchmode -quit -projectPath . -buildTarget Android -executeMethod ColorMinesweeper.EditorTools.AndroidBuild.BuildRelease
    ///
    /// 업로드 키(keystore)와 비밀번호는 저장소에 두지 않는다. 서명 설정 파일(signing.json)에서 읽는다(위치는 <see cref="SigningPath"/>).
    /// { "keystore": "D:/Keys/pixelclue-upload.keystore", "storePass": "...", "alias": "pixelclue", "keyPass": "..." }
    /// 빌드할 때마다 버전 코드(versionCode)를 1 올린다. Play Console 은 같은 버전 코드를 두 번 받지 않는다.
    /// 결과는 Builds/PixelClue-{버전}-{버전코드}.aab 와, Play Console 에 함께 올릴 디버그 기호 파일(…symbols.zip).
    /// </summary>
    public static class AndroidBuild
    {
        [Serializable]
        sealed class Signing
        {
            public string keystore;
            public string storePass;
            public string alias;
            public string keyPass;
        }

        /// <summary>
        /// 서명 설정 파일 위치. 이 기기에서만 쓰는 UserSettings/PixelClueSigningPath.txt(저장소에 올라가지 않음)에 적힌 경로를 쓰고,
        /// 없으면 사용자 폴더의 .pixelclue/signing.json 을 본다.
        /// </summary>
        static string SigningPath
        {
            get
            {
                string pointer = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "UserSettings", "PixelClueSigningPath.txt"));
                if (File.Exists(pointer))
                {
                    string path = File.ReadAllText(pointer).Trim();
                    if (path.Length > 0)
                    {
                        return path;
                    }
                }

                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pixelclue", "signing.json");
            }
        }

        [MenuItem("Tools/Pixel Clue/Build Android Release")]
        public static void BuildRelease()
        {
            bool ok = false;
            try
            {
                ok = Build();
            }
            catch (Exception e)
            {
                Debug.LogError("[Build] " + e);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        static bool Build()
        {
            Signing signing = LoadSigning();
            if (signing == null)
            {
                return false;
            }

            // 플러그인의 Android 전용 빌드 처리(AdMob 앱 ID 를 매니페스트에 넣는 일 등)는 #if UNITY_ANDROID 로 감싸여 있어
            // 에디터가 처음부터 Android 대상으로 켜져 있어야 컴파일된다. 빌드 도중에 대상을 바꾸면 그 처리가 빠진다.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                Debug.LogError("[Build] 빌드 대상을 Android 로 바꿨습니다. 다시 실행해 주세요(배치 모드는 -buildTarget Android 를 붙인다).");
                return false;
            }

            // Google Play 요구: 64비트(ARM64), 그래서 IL2CPP. 개발 빌드가 아니어야 실제 광고 ID 를 쓴다.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;
            // 출시 후 꺼짐(크래시) 원인을 Play Console 에서 읽을 수 있게 네이티브 디버그 기호(symbols.zip)를 함께 만든다.
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = signing.keystore;
            PlayerSettings.Android.keystorePass = signing.storePass;
            PlayerSettings.Android.keyaliasName = signing.alias;
            PlayerSettings.Android.keyaliasPass = signing.keyPass;

            ApplyIcons();

            PlayerSettings.Android.bundleVersionCode++;
            AssetDatabase.SaveAssets();

            ResolveAndroidDependencies();

            string version = PlayerSettings.bundleVersion;
            int code = PlayerSettings.Android.bundleVersionCode;
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds",
                "PixelClue-" + version + "-" + code + ".aab"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                // 실패한 빌드에 버전 코드를 쓰지 않도록 되돌린다.
                PlayerSettings.Android.bundleVersionCode--;
                AssetDatabase.SaveAssets();
                Debug.LogError("[Build] 실패: " + report.summary.result + ", 오류 " + report.summary.totalErrors + "개");
                return false;
            }

            Debug.Log("[Build] 완료: " + output + " (" + report.summary.totalSize / (1024 * 1024) + " MB, 버전 " + version +
                      ", 버전 코드 " + code + ")");
            return true;
        }

        /// <summary>
        /// 앱 아이콘을 Assets/AppIcon 의 그림으로 맞춘다. 적응형 아이콘은 배경과 전경을 따로 넣고(런처가 모양을 잘라 낸다),
        /// 오래된 기기용(legacy, round)은 한 장으로 합친 그림을 쓴다. 스토어용 512 아이콘은 Store/icon-512.png.
        /// </summary>
        static void ApplyIcons()
        {
            const string folder = "Assets/AppIcon/";
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "background.png");
            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "foreground.png");
            var legacy = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "legacy.png");
            if (background == null || foreground == null || legacy == null)
            {
                Debug.LogWarning("[Build] Assets/AppIcon 에 아이콘 그림이 없어 기본 아이콘으로 빌드합니다.");
                return;
            }

            SetIcons(UnityEditor.Android.AndroidPlatformIconKind.Adaptive, background, foreground);
            SetIcons(UnityEditor.Android.AndroidPlatformIconKind.Legacy, legacy);
            SetIcons(UnityEditor.Android.AndroidPlatformIconKind.Round, legacy);
        }

        static void SetIcons(PlatformIconKind kind, params Texture2D[] layers)
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTextures(layers);
            }

            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        static Signing LoadSigning()
        {
            if (!File.Exists(SigningPath))
            {
                Debug.LogError("[Build] 서명 설정이 없습니다: " + SigningPath);
                return null;
            }

            var signing = JsonUtility.FromJson<Signing>(File.ReadAllText(SigningPath));
            if (signing == null || string.IsNullOrEmpty(signing.keystore) || !File.Exists(signing.keystore))
            {
                Debug.LogError("[Build] 서명 설정의 keystore 파일을 찾지 못했습니다: " + SigningPath);
                return null;
            }

            return signing;
        }

        /// <summary>AdMob 등 플러그인이 쓰는 Android 라이브러리를 External Dependency Manager 로 받아 둔다.</summary>
        static void ResolveAndroidDependencies()
        {
            Type resolver = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
            if (resolver == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    resolver = assembly.GetType("GooglePlayServices.PlayServicesResolver");
                    if (resolver != null)
                    {
                        break;
                    }
                }
            }

            MethodInfo resolve = resolver?.GetMethod("ResolveSync", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(bool) }, null);
            if (resolve == null)
            {
                Debug.LogWarning("[Build] Android 의존성 도구를 찾지 못해 건너뜁니다.");
                return;
            }

            bool resolved = (bool)resolve.Invoke(null, new object[] { true });
            Debug.Log("[Build] Android 의존성 " + (resolved ? "준비 완료" : "준비 실패(빌드는 계속 시도)"));
        }
    }
}
