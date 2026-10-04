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
    /// - 배치 모드: Unity.exe -batchmode -quit -projectPath . -executeMethod ColorMinesweeper.EditorTools.AndroidBuild.BuildRelease
    ///
    /// 업로드 키(keystore)와 비밀번호는 저장소에 두지 않는다. 사용자 폴더의 .pixelclue/signing.json 에서 읽는다.
    /// { "keystore": "D:/Keys/pixelclue-upload.keystore", "storePass": "...", "alias": "pixelclue", "keyPass": "..." }
    /// 빌드할 때마다 버전 코드(versionCode)를 1 올린다. Play Console 은 같은 버전 코드를 두 번 받지 않는다.
    /// 결과는 Builds/PixelClue-{버전}-{버전코드}.aab.
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

        static string SigningPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pixelclue",
            "signing.json");

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

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            // Google Play 요구: 64비트(ARM64), 그래서 IL2CPP. 개발 빌드가 아니어야 실제 광고 ID 를 쓴다.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = true;
            EditorUserBuildSettings.development = false;

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = signing.keystore;
            PlayerSettings.Android.keystorePass = signing.storePass;
            PlayerSettings.Android.keyaliasName = signing.alias;
            PlayerSettings.Android.keyaliasPass = signing.keyPass;

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
