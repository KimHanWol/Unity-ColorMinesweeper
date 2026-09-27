namespace ColorMinesweeper.Game
{
    /// <summary>스테이지별 최고 별점. 0 은 아직 못 깬 것. 스테이지는 앞 스테이지를 깨야 열린다. 저장은 <see cref="SaveStore"/>.</summary>
    public static class Progress
    {
        public static int Stars(string stageId)
        {
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
            SaveStore.RecordStars(stageId, stars);
        }
    }
}
