using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 판을 HUD 와 팔레트 사이 영역에 맞춰 보여 주고, 확대·이동을 판 밖으로 벗어나지 않게 제한한다.
    /// 배경 그라데이션도 이 카메라에 붙여 화면을 항상 덮게 한다.
    /// </summary>
    public sealed class BoardCamera : MonoBehaviour
    {
        const float MaxZoomCellsAcross = 6f;

        public Camera Camera { get; private set; }

        Rect board;
        Rect viewport;
        float fitSize;
        float minSize;
        Vector2 fitPosition;
        Transform background;
        Vector2 backgroundSize;
        Vector2 basePosition;
        float shake;

        public static BoardCamera Create(Transform parent)
        {
            var go = new GameObject("BoardCamera");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Theme.BackgroundBottom;
            camera.cullingMask = ~(1 << GameApp.UiLayer);
            camera.depth = 0;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var boardCamera = go.AddComponent<BoardCamera>();
            boardCamera.Camera = camera;
            boardCamera.background = Draw.Sprite(go.transform, "Background",
                SpriteFactory.VerticalGradient(Theme.BackgroundTop, Theme.BackgroundBottom), Color.white, -1000,
                Vector2.zero).transform;
            boardCamera.background.localPosition = new Vector3(0f, 0f, 50f);
            boardCamera.backgroundSize = boardCamera.background.GetComponent<SpriteRenderer>().sprite.bounds.size;
            return boardCamera;
        }

        /// <summary>boardRect 는 월드 좌표, viewportPixels 는 판을 보여 줄 화면 영역(픽셀).</summary>
        public void Frame(Rect boardRect, Rect viewportPixels, bool animate)
        {
            board = boardRect;
            viewport = viewportPixels;
            ScreenInfo.ApplyAspect(Camera);

            float margin = 0.6f;
            float heightFraction = viewport.height / ScreenInfo.Height;
            float widthFraction = viewport.width / ScreenInfo.Width;
            float aspect = (float)ScreenInfo.Width / ScreenInfo.Height;
            fitSize = Mathf.Max(
                (board.height + margin) / (2f * heightFraction),
                (board.width + margin) / (2f * aspect * widthFraction));
            minSize = Mathf.Min(fitSize, MaxZoomCellsAcross / (2f * aspect * widthFraction));

            // 영역 가운데가 화면 가운데가 아니므로, 판 가운데가 영역 가운데에 오도록 카메라를 옮긴다.
            float offsetY = (viewport.center.y / ScreenInfo.Height - 0.5f) * 2f * fitSize;
            float offsetX = (viewport.center.x / ScreenInfo.Width - 0.5f) * 2f * fitSize * aspect;
            fitPosition = board.center - new Vector2(offsetX, offsetY);

            if (!animate)
            {
                Camera.orthographicSize = fitSize;
                SetPosition(fitPosition);
                return;
            }

            float fromSize = Camera.orthographicSize;
            Vector2 fromPosition = basePosition;
            Tween.Kill(this);
            Tween.Run(this, 0.6f, t =>
            {
                Camera.orthographicSize = Mathf.Lerp(fromSize, fitSize, t);
                SetPosition(Vector2.Lerp(fromPosition, fitPosition, t));
            }, Ease.InOutSine);
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            return Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
        }

        public void Pan(Vector2 screenDelta)
        {
            float unitsPerPixel = 2f * Camera.orthographicSize / ScreenInfo.Height;
            SetPosition(basePosition - screenDelta * unitsPerPixel);
        }

        public void Zoom(Vector2 screenCenter, float ratio)
        {
            Vector2 before = ScreenToWorld(screenCenter);
            Camera.orthographicSize = Mathf.Clamp(Camera.orthographicSize / ratio, minSize, fitSize);
            Vector2 after = ScreenToWorld(screenCenter);
            SetPosition(basePosition + before - after);
        }

        public void Shake(float strength)
        {
            shake = Mathf.Max(shake, strength);
        }

        /// <summary>확대한 만큼만 움직일 수 있다. 다 줄이면 제자리에 고정된다.</summary>
        void SetPosition(Vector2 position)
        {
            float zoom = 1f - Camera.orthographicSize / Mathf.Max(fitSize, 0.001f);
            float rangeX = board.width / 2f * zoom * 1.4f;
            float rangeY = board.height / 2f * zoom * 1.4f;
            position.x = Mathf.Clamp(position.x, fitPosition.x - rangeX, fitPosition.x + rangeX);
            position.y = Mathf.Clamp(position.y, fitPosition.y - rangeY, fitPosition.y + rangeY);
            basePosition = position;
            transform.position = new Vector3(position.x, position.y, -10f);
        }

        void LateUpdate()
        {
            float height = Camera.orthographicSize * 2f;
            // 그라데이션 그림은 1x64 픽셀이라 폭이 높이의 1/64 이다. 그림 크기로 나눠야 화면 전체를 덮는다.
            background.localScale = new Vector3(height * Camera.aspect * 1.2f / backgroundSize.x, height * 1.2f / backgroundSize.y, 1f);

            // 흔들림은 기준 위치에 매 프레임 새 오프셋을 더하는 식이라 쌓이지 않는다. 배경은 반대로 밀어 흔들리지 않게 한다.
            Vector2 offset = shake > 0.001f ? Random.insideUnitCircle * shake : Vector2.zero;
            shake = shake > 0.001f ? shake * 0.82f : 0f;
            transform.position = new Vector3(basePosition.x + offset.x, basePosition.y + offset.y, -10f);
            background.localPosition = new Vector3(-offset.x, -offset.y, 50f);
        }
    }
}
