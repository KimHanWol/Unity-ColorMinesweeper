using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>UI 가 받지 않은 입력을 받는 쪽(판, 스테이지 목록 스크롤).</summary>
    public interface IPointerHandler
    {
        void OnTap(Vector2 screen);
        void OnDrag(Vector2 screenDelta);

        /// <summary>ratio 가 1 보다 크면 확대.</summary>
        void OnZoom(Vector2 screenCenter, float ratio);
    }

    /// <summary>
    /// 터치와 마우스를 탭·드래그·핀치 하나로 모은다. UI 버튼 위에서 시작한 입력은 버튼이 끝까지 가져간다.
    /// 조금이라도 끌었거나 손가락이 두 개였던 입력은 탭으로 보지 않아서, 화면을 움직이다 칸이 칠해지는 일이 없다.
    /// </summary>
    public sealed class PointerInput : MonoBehaviour
    {
        public Camera UiCamera;
        public IPointerHandler Handler;

        bool active;
        bool dragging;
        bool multi;
        Vector2 start;
        Vector2 last;
        float lastPinchDistance;
        Vector2 lastPinchCenter;
        UiButton captured;

        float DragThreshold => (Screen.dpi > 0f ? Screen.dpi : 160f) * 0.08f;

        void Update()
        {
            if (Input.touchCount > 0)
            {
                HandleTouches();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Down(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && active)
            {
                Move(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && active)
            {
                Up(Input.mousePosition, false);
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f && Handler != null && UiButton.HitTest(ToUiWorld(Input.mousePosition)) == null)
            {
                Handler.OnZoom(Input.mousePosition, Mathf.Pow(1.1f, scroll));
            }
        }

        void HandleTouches()
        {
            if (Input.touchCount >= 2)
            {
                Touch a = Input.GetTouch(0);
                Touch b = Input.GetTouch(1);
                Vector2 center = (a.position + b.position) / 2f;
                float distance = Vector2.Distance(a.position, b.position);
                if (!multi || lastPinchDistance <= 0f)
                {
                    // 핀치 시작(또는 한 손가락으로 줄었다가 다시 두 손가락): 기준만 잡고 이번 프레임은 움직이지 않는다.
                    multi = true;
                    ReleaseCaptured();
                }
                else if (Handler != null && active)
                {
                    Handler.OnZoom(center, distance / lastPinchDistance);
                    Handler.OnDrag(center - lastPinchCenter);
                }

                lastPinchDistance = distance;
                lastPinchCenter = center;
                return;
            }

            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    Down(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (multi)
                    {
                        // 두 손가락에서 한 손가락으로 돌아오면 남은 손가락 위치부터 다시 잰다(화면이 튀지 않게).
                        last = touch.position;
                        lastPinchDistance = 0f;
                    }

                    Move(touch.position);
                    break;
                case TouchPhase.Ended:
                    Up(touch.position, false);
                    break;
                case TouchPhase.Canceled:
                    Up(touch.position, true);
                    break;
            }
        }

        void Down(Vector2 screen)
        {
            active = true;
            dragging = false;
            multi = false;
            start = screen;
            last = screen;
            captured = UiButton.HitTest(ToUiWorld(screen));
            captured?.SetPressed(true);
            captured?.OnDragTo?.Invoke(ToUiWorld(screen));
        }

        void Move(Vector2 screen)
        {
            if (!active)
            {
                return;
            }

            if (captured != null)
            {
                if (captured.OnDragTo != null)
                {
                    captured.OnDragTo(ToUiWorld(screen));
                    return;
                }

                if (!captured.ReleaseOnDrag || (screen - start).magnitude <= DragThreshold)
                {
                    return;
                }

                ReleaseCaptured();
            }

            if (!dragging && (screen - start).magnitude > DragThreshold)
            {
                dragging = true;
            }

            if (dragging && !multi)
            {
                Handler?.OnDrag(screen - last);
            }

            last = screen;
        }

        void Up(Vector2 screen, bool canceled)
        {
            if (!active)
            {
                return;
            }

            active = false;
            if (captured != null)
            {
                UiButton button = captured;
                captured = null;
                button.SetPressed(false);
                if (button.OnDragTo != null)
                {
                    button.OnDragEnd?.Invoke();
                    return;
                }
                if (!canceled && button.Contains(ToUiWorld(screen)))
                {
                    button.Click();
                }

                return;
            }

            if (!canceled && !dragging && !multi)
            {
                Handler?.OnTap(screen);
            }

            multi = false;
        }

        void ReleaseCaptured()
        {
            if (captured != null)
            {
                captured.SetPressed(false);
                captured = null;
            }
        }

        Vector2 ToUiWorld(Vector2 screen)
        {
            return UiCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
        }
    }
}
