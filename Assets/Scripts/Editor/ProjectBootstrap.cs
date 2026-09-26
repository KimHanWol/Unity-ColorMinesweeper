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
        const string ApplicationId = "com.kimhanwol.colorminesweeper";

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

        static void EnsurePlayerSettings()
        {
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == ApplicationId)
            {
                return;
            }

            PlayerSettings.companyName = "KimHanWol";
            PlayerSettings.productName = "Color Minesweeper";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[Bootstrap] Player Settings(패키지 이름, 세로 고정)를 설정했습니다.");
        }
    }
}
