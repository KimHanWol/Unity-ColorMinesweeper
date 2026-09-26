# Color Minesweeper Agent Notes

- 게임 규칙, 스테이지 형식, 설계 이유는 `README.md` 에 있다. 설계를 되돌리기 전에 먼저 읽는다.
- `Assets/Scripts/Core` 는 UnityEngine 을 참조하지 않는다(asmdef `noEngineReferences`). Unity 기능이 필요하면
  `Assets/Scripts/Game` 쪽에 둔다.
- Unity 6 가 지원하는 C# 9 까지만 쓴다(file-scoped namespace, global using, record struct 금지).
- 코어를 고치면 `dotnet test Tools/CoreTests` 를, 스테이지를 고치면 `dotnet run --project Tools/StageTool -- validate` 를 돌린다.
- 화면과 UI 는 코드로 조립한다. 씬/프리팹에 수동으로 배치한 오브젝트에 기능을 의존시키지 않는다.
