using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// 저장소를 처음 열었을 때 필요한 것들을 만든다. 씬은 비어 있고(GameApp 이 스스로 뜬다) 빌드 목록에만 올린다.
    /// 이미 되어 있으면 아무것도 하지 않는다.
    /// </summary>
    [InitializeOnLoad]
    static class ProjectBootstrap
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string ApplicationId = "com.kimhanwol.pixelclue";

        static ProjectBootstrap()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EnsureMainScene();
            EnsurePlayerSettings();
            EnsureStoreBuild();
            IconImporter.ReimportIfNeeded();
        }

        static void EnsureMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[Bootstrap] " + ScenePath + " 를 만들었습니다.");
            }

            if (EditorBuildSettings.scenes.Length == 0)
            {
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            }
        }

        /// <summary>
        /// Google Play 는 64비트(ARM64) 빌드를 요구하고, ARM64 는 IL2CPP 로만 만들 수 있다. 기본값에 맡기지 않고 못박아 둔다.
        /// </summary>
        static void EnsureStoreBuild()
        {
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP
                && (PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != 0)
            {
                return;
            }

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            AssetDatabase.SaveAssets();
            Debug.Log("[Bootstrap] Android 빌드를 IL2CPP + ARM64 로 맞췄습니다(Google Play 64비트 요구).");
        }

        static void EnsurePlayerSettings()
        {
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == ApplicationId)
            {
                return;
            }

            PlayerSettings.companyName = "KimHanWol";
            PlayerSettings.productName = "Pixel Clue";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[Bootstrap] Player Settings(패키지 이름, 세로 고정)를 설정했습니다.");
        }
    }
}
