using ColorMinesweeper.Game;
using UnityEditor;
using UnityEngine;

namespace ColorMinesweeper.EditorTools
{
    /// <summary>
    /// 에디터 메뉴의 개발용 치트. 에디터와 플레이 모드가 같은 PlayerPrefs 를 쓰므로 플레이 중에도 바로 반영되고,
    /// 목록 화면을 보고 있으면 다시 그린다.
    /// </summary>
    static class CheatMenu
    {
        const string Root = "Tools/Color Minesweeper/Cheats/";
        const string UnlockAllPath = Root + "모든 스테이지 잠금 해제";

        [MenuItem(UnlockAllPath, priority = 100)]
        static void ToggleUnlockAll()
        {
            Cheats.UnlockAll = !Cheats.UnlockAll;
            Debug.Log("[Cheat] 모든 스테이지 잠금 해제: " + (Cheats.UnlockAll ? "켬" : "끔"));
            Refresh();
        }

        [MenuItem(UnlockAllPath, true)]
        static bool ToggleUnlockAllValidate()
        {
            Menu.SetChecked(UnlockAllPath, Cheats.UnlockAll);
            return true;
        }

        [MenuItem(Root + "모든 스테이지 별 3개로 클리어", priority = 101)]
        static void ClearAll()
        {
            Cheats.ClearAll();
            Debug.Log("[Cheat] 모든 스테이지를 별 3개로 기록했습니다.");
            Refresh();
        }

        [MenuItem(Root + "진행 기록 초기화", priority = 120)]
        static void ResetProgress()
        {
            if (!EditorUtility.DisplayDialog("진행 기록 초기화", "모든 스테이지의 별점과 치트 설정을 지웁니다.", "지우기", "취소"))
            {
                return;
            }

            Cheats.ResetProgress();
            Debug.Log("[Cheat] 진행 기록을 지웠습니다.");
            Refresh();
        }

        static void Refresh()
        {
            if (Application.isPlaying)
            {
                Object.FindAnyObjectByType<GameApp>()?.RefreshStageList();
            }
        }
    }
}
