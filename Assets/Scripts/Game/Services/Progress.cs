namespace ColorMinesweeper.Game
{
    /// <summary>스테이지별 최고 별점. 0 은 아직 못 깬 것. 스테이지는 앞 스테이지를 깨야 열린다. 저장은 <see cref="SaveStore"/>.</summary>
    public static class Progress
    {
#if UNITY_EDITOR
        /// <summary>스토어 스크린샷용: 저장 파일을 건드리지 않고 별 기록을 대신 보여 준다.</summary>
        public static System.Func<string, int> DebugStars;
#endif

        public static int Stars(string stageId)
        {
#if UNITY_EDITOR
            if (DebugStars != null)
            {
                return DebugStars(stageId);
            }
#endif
            return SaveStore.Stars(stageId);
        }

        public static bool IsCleared(string stageId)
        {
            return Stars(stageId) > 0;
        }

        public static void Clear(string stageId)
        {
            SaveStore.Remove(stageId);
        }

        public static void Record(string stageId, int stars)
        {
#if UNITY_EDITOR
            if (DebugStars != null)
            {
                return;
            }
#endif
            SaveStore.RecordStars(stageId, stars);
        }
    }
}
