using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 한 화면. UI 레이어 루트에 붙고, 판처럼 UI 카메라 밖에 그리는 것은 <see cref="World"/> 아래에 둔다.
    /// 화면을 닫으면 둘 다 사라진다.
    /// </summary>
    public abstract class ScreenBase : MonoBehaviour, IPointerHandler
    {
        protected GameApp App;
        protected UiRoot Ui;
        protected Transform World;

        public void Init(GameApp app)
        {
            App = app;
            Ui = app.Ui;
            World = new GameObject(GetType().Name + "World").transform;
            Build();
        }

        protected abstract void Build();

        /// <summary>화면 크기나 안전 영역이 바뀌었을 때.</summary>
        public virtual void Layout()
        {
        }

        public virtual void OnTap(Vector2 screen)
        {
        }

        public virtual void OnDrag(Vector2 screenDelta)
        {
        }

        public virtual void OnZoom(Vector2 screenCenter, float ratio)
        {
        }

        protected virtual void OnDestroy()
        {
            if (World != null)
            {
                Destroy(World.gameObject);
            }
        }
    }
}
