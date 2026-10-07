using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// 에디터를 열자마자 게임을 실행한다. 명령줄에서 바로 띄워 볼 때 쓴다.
    /// Unity.exe -projectPath . -executeMethod ColorMinesweeper.EditorTools.PlayNow.Run
    /// </summary>
    public static class PlayNow
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string PendingKey = "PixelClue.PlayNow.Pending";

        /// <summary>에디터가 막 뜬 직후에는 실행 요청이 무시되므로, 뜨고 나서 이만큼 기다렸다가 들어간다.</summary>
        const double SettleSeconds = 2.0;

        static double readyAt;

        [MenuItem("Tools/Pixel Clue/Play Now")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SessionState.SetBool(PendingKey, true);
            Debug.Log("[PlayNow] 실행 요청을 받았습니다.");
            Arm();
        }

        /// <summary>스크립트를 다시 읽어 들인 뒤에도(도메인 리로드) 걸어 둔 요청을 이어 간다.</summary>
        [InitializeOnLoadMethod]
        static void Resume()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
                {
                    SessionState.SetBool(PendingKey, false);
                    Debug.Log("[PlayNow] 게임이 실행 중입니다.");
                }
            };

            if (SessionState.GetBool(PendingKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Arm();
            }
        }

        static void Arm()
        {
            readyAt = EditorApplication.timeSinceStartup + SettleSeconds;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                readyAt = EditorApplication.timeSinceStartup + SettleSeconds;
                return;
            }

            if (EditorApplication.timeSinceStartup < readyAt)
            {
                return;
            }

            EditorApplication.update -= Tick;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("[PlayNow] 플레이 모드로 들어갑니다.");
            EditorApplication.EnterPlaymode();
        }
    }
}
