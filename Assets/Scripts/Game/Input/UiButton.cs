using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// UI 카메라 위의 누를 수 있는 영역. 누르는 동안 살짝 들어가고 떼면 튕기며 돌아온다.
    /// OnClick 이 없으면 아래로 입력이 새지 않게 막는 가림막으로만 쓴다.
    /// </summary>
    public sealed class UiButton : MonoBehaviour
    {
        static readonly List<UiButton> all = new List<UiButton>();

        public Vector2 Size;

        /// <summary>겹칠 때 큰 쪽이 먼저 입력을 받는다. 보통 sortingOrder 와 같게 둔다.</summary>
        public int Priority;

        public Action OnClick;
        public bool Interactable = true;
        public bool Pressable = true;

        /// <summary>누를 때 기본 딸깍 소리를 내지 않는다(자기 소리를 따로 내는 버튼).</summary>
        public bool Silent;

        /// <summary>스크롤 목록 안의 버튼. 누른 채로 끌면 버튼을 놓고 스크롤로 넘긴다.</summary>
        public bool ReleaseOnDrag;

        /// <summary>슬라이더처럼 누른 채 끄는 동안 위치(UI 월드 좌표)를 받는다. 누르는 순간에도 한 번 부른다.</summary>
        public Action<Vector2> OnDragTo;

        /// <summary>끌기를 마쳤을 때(손을 뗐을 때).</summary>
        public Action OnDragEnd;

        Transform visual;
        Vector3 restScale = Vector3.one;

        public static UiButton Attach(Transform target, Vector2 size, int priority, Action onClick, Transform visual = null)
        {
            var button = target.gameObject.AddComponent<UiButton>();
            button.Size = size;
            button.Priority = priority;
            button.OnClick = onClick;
            button.visual = visual != null ? visual : target;
            button.restScale = button.visual.localScale;
            return button;
        }

        void OnEnable()
        {
            all.Add(this);
        }

        void OnDisable()
        {
            all.Remove(this);
        }

        public bool Contains(Vector2 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            return Mathf.Abs(local.x) <= Size.x / 2f && Mathf.Abs(local.y) <= Size.y / 2f;
        }

        public static UiButton HitTest(Vector2 world)
        {
            UiButton best = null;
            foreach (UiButton button in all)
            {
                if (button.Contains(world) && (best == null || button.Priority > best.Priority))
                {
                    best = button;
                }
            }

            return best;
        }

        public void SetPressed(bool pressed)
        {
            if (!Pressable || !Interactable || OnClick == null)
            {
                return;
            }

            Tween.Kill(visual);
            Vector3 from = visual.localScale;
            Vector3 to = pressed ? restScale * 0.93f : restScale;
            Tween.Run(visual, pressed ? 0.08f : 0.3f, t => visual.localScale = Vector3.LerpUnclamped(from, to, t),
                pressed ? Ease.OutCubic : Ease.OutBack);
        }

        /// <summary>visual 의 크기를 바꾼 뒤 눌림 연출의 기준 크기를 다시 잡는다.</summary>
        public void SetRestScale(Vector3 scale)
        {
            restScale = scale;
        }

        public void Click()
        {
            if (!Interactable || OnClick == null)
            {
                return;
            }

            if (!Silent)
            {
                Sfx.Instance?.Tap();
            }

            Haptics.Tick();
            OnClick();
        }
    }
}
