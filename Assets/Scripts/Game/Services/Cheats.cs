using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 개발용 치트. 에디터와 Development Build 에서만 동작하고, 출시 빌드에서는 켜져 있어도 무시된다.
    /// 에디터에서는 Tools > Pixel Clue > Cheats 메뉴로, 기기에서는 목록 화면 제목을 7번 연속 탭해서 쓴다.
    /// </summary>
    public static class Cheats
    {
        const string UnlockAllKey = "cheat.unlockAll";

        public static bool Available => Debug.isDebugBuild;

        /// <summary>
        /// 모든 스테이지를 연다. 진행 기록(별점)은 건드리지 않으므로 끄면 원래 잠금 상태로 돌아간다.
        /// </summary>
        public static bool UnlockAll
        {
            get => Available && PlayerPrefs.GetInt(UnlockAllKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(UnlockAllKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>모든 스테이지를 별 3개로 깬 것으로 기록한다. 목록에서 완성 그림을 한 번에 볼 때 쓴다.</summary>
        public static void ClearAll()
        {
            foreach (Core.Stage stage in StageCatalog.All)
            {
                Progress.Record(stage.Id, 3);
            }
        }

        /// <summary>진행 기록, 튜토리얼 완료, 치트 플래그를 모두 지운다. 설정(효과음·진동 등)은 남긴다.</summary>
        public static void ResetProgress()
        {
            foreach (Core.Stage stage in StageCatalog.All)
            {
                Progress.Clear(stage.Id);
            }

            UnlockAll = false;
            TutorialDirector.IsDone = false;
            SaveStore.Hints = SaveStore.StartingHints;
        }
    }
}
