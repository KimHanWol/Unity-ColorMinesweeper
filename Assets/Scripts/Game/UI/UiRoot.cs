using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// UI 전용 직교 카메라. 화면 높이를 항상 <see cref="Height"/> 유닛으로 두고, 노치·홈 바를 피한 안전 영역을 월드 좌표로 준다.
    /// </summary>
    public sealed class UiRoot : MonoBehaviour
    {
        public const float Height = 20f;

        public Camera Camera { get; private set; }

        /// <summary>안전 영역(월드 유닛). 화면 가운데가 원점.</summary>
        public Rect Safe { get; private set; }

        public float Width => Height * Camera.aspect;

        public static UiRoot Create(Transform parent)
        {
            var go = new GameObject("UiCamera");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.cullingMask = 1 << GameApp.UiLayer;
            camera.depth = 10;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var root = go.AddComponent<UiRoot>();
            root.Camera = camera;
            root.Refresh();
            return root;
        }

        public void Refresh()
        {
            Rect area = Screen.safeArea;
            float unitsPerPixel = Height / Screen.height;
            Safe = new Rect(
                (area.x - Screen.width / 2f) * unitsPerPixel,
                (area.y - Screen.height / 2f) * unitsPerPixel,
                area.width * unitsPerPixel,
                area.height * unitsPerPixel);
        }

        /// <summary>UI 월드 y 를 화면 픽셀 y 로. 판을 보여 줄 영역을 BoardCamera 에 넘길 때 쓴다.</summary>
        public float ToPixelY(float worldY)
        {
            return (worldY / Height + 0.5f) * Screen.height;
        }

        public float ToPixelX(float worldX)
        {
            return (worldX / Width + 0.5f) * Screen.width;
        }

        /// <summary>UI 레이어에 속한 빈 노드. 화면마다 여기 아래에 UI 를 만든다.</summary>
        public static Transform NewLayerRoot(string name)
        {
            var go = new GameObject(name) { layer = GameApp.UiLayer };
            return go.transform;
        }
    }
}
