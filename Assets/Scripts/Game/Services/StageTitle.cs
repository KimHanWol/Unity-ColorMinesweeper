using ColorMinesweeper.Core;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 스테이지 제목 표시 규칙. 이름이 곧 그림의 정답이라, 완성하기 전에는 "번호. ???" 로 가리고
    /// 완성한 뒤에만 이름을 보여 준다(목록 카드와 플레이 화면 HUD 가 같이 쓴다).
    /// </summary>
    public static class StageTitle
    {
        public const string HiddenName = "???";

        /// <summary>index 는 0 부터. -1 이면 에디터 시험 플레이라 번호 없이 쓴다.</summary>
        public static string Hidden(int index)
        {
            return index >= 0 ? (index + 1) + ". " + HiddenName : HiddenName;
        }

        public static string Revealed(int index, Stage stage)
        {
            return index >= 0 ? (index + 1) + ". " + Name(stage) : Name(stage);
        }

        /// <summary>현재 언어의 스테이지 이름.</summary>
        public static string Name(Stage stage)
        {
            return stage.NameIn(Loc.Code(Loc.Current));
        }

        /// <summary>지금 보여 줄 제목. 전에 깬 스테이지면 이름이 보인다.</summary>
        public static string For(int index, Stage stage)
        {
            return Progress.IsCleared(stage.Id) ? Revealed(index, stage) : Hidden(index);
        }
    }
}
