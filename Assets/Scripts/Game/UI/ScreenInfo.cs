using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 화면 크기와 안전 영역. 평소에는 Screen 값을 그대로 쓰고, 스토어 스크린샷처럼 실제 창과 다른 크기로
    /// 그릴 때만 <see cref="Override"/> 로 바꾼다(배치 모드에는 폰 화면이 없다). 레이아웃은 이 값만 본다.
    /// </summary>
    public static class ScreenInfo
    {
        /// <summary>null 이 아니면 이 크기(픽셀)의 화면으로 친다. 안전 영역은 화면 전체다.</summary>
        public static Vector2Int? Override;

        public static int Width => Override?.x ?? Screen.width;
        public static int Height => Override?.y ?? Screen.height;
        public static float Aspect => (float)Width / Height;

        public static Rect SafeArea => Override.HasValue
            ? new Rect(0f, 0f, Override.Value.x, Override.Value.y)
            : Screen.safeArea;

        /// <summary>카메라의 가로세로 비를 이 화면에 맞춘다(바꿔 쓴 크기가 없으면 실제 창을 따른다).</summary>
        public static void ApplyAspect(Camera camera)
        {
            if (Override.HasValue)
            {
                camera.aspect = Aspect;
            }
            else
            {
                camera.ResetAspect();
            }
        }
    }
}
