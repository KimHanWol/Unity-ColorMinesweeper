namespace ColorMinesweeper.Core
{
    /// <summary>새 버전 안내를 어떻게 띄울지.</summary>
    public enum UpdateNeed
    {
        /// <summary>최신이거나, 버전 정보를 모른다.</summary>
        None,

        /// <summary>새 버전이 있다. "나중에"를 고를 수 있다.</summary>
        Optional,

        /// <summary>이 버전은 더 이상 쓸 수 없다. 업데이트해야 계속할 수 있다.</summary>
        Required,
    }

    /// <summary>
    /// 지금 버전 코드와, 웹에 올려 둔 버전 정보(최신 버전, 최소 버전)를 비교한다.
    /// 값을 모르면(0 이하) 안내하지 않는다. 인터넷이 없거나 파일을 못 읽었을 때 괜히 막지 않기 위해서다.
    /// </summary>
    public static class UpdateCheck
    {
        public static UpdateNeed Compare(int current, int latest, int minimum)
        {
            if (current <= 0)
            {
                return UpdateNeed.None;
            }

            if (minimum > 0 && current < minimum)
            {
                return UpdateNeed.Required;
            }

            return latest > 0 && current < latest ? UpdateNeed.Optional : UpdateNeed.None;
        }
    }
}
