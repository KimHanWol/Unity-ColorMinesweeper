using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>스테이지별 최고 별점. 0 은 아직 못 깬 것. 스테이지는 앞 스테이지를 깨야 열린다.</summary>
    public static class Progress
    {
        static string Key(string stageId) => "stars." + stageId;

        public static int Stars(string stageId)
        {
            return PlayerPrefs.GetInt(Key(stageId), 0);
        }

        public static bool IsCleared(string stageId)
        {
            return Stars(stageId) > 0;
        }

        public static void Record(string stageId, int stars)
        {
            if (stars > Stars(stageId))
            {
                PlayerPrefs.SetInt(Key(stageId), stars);
                PlayerPrefs.Save();
            }
        }
    }
}
