using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 모든 화면 뒤에서 천천히 떠오르는 파스텔 타일. 메인 화면에만 있던 것을 판 카메라에 붙여 어느 화면에서나 같은 배경이
    /// 이어지게 했다. 판을 확대하거나 옮겨도 화면에 고정되어 보이고, 판과 UI 보다 뒤에 그려진다.
    /// </summary>
    public sealed class Backdrop : MonoBehaviour
    {
        /// <summary>화면 높이를 이 유닛으로 놓고 타일을 배치한다(UI 와 같은 눈금).</summary>
        const float Height = 20f;
        const int Count = 22;

        static readonly Color[] Colors =
        {
            Theme.Hex(0xFF8FAB), Theme.Hex(0xFFD166), Theme.Hex(0x8ECAE6), Theme.Hex(0x95D5B2), Theme.Hex(0xC77DFF),
        };

        sealed class Floater
        {
            public Transform Transform;
            public float Speed;
            public float Spin;
            public float Sway;
            public float Phase;
        }

        readonly List<Floater> floaters = new List<Floater>();
        Camera view;

        public static Backdrop Create(Camera camera)
        {
            Transform root = Draw.Node(camera.transform, "Backdrop");
            root.localPosition = new Vector3(0f, 0f, 40f);
            var backdrop = root.gameObject.AddComponent<Backdrop>();
            backdrop.view = camera;
            backdrop.Build();
            return backdrop;
        }

        float Width => Height * view.aspect;

        void Build()
        {
            for (int i = 0; i < Count; i++)
            {
                float size = Random.Range(0.25f, 0.7f);
                SpriteRenderer tile = Draw.Sprite(transform, "Tile", SpriteFactory.RoundedRect(0.25f),
                    Theme.WithAlpha(Colors[i % Colors.Length], Random.Range(0.22f, 0.42f)), -900,
                    new Vector2(Random.Range(-Width / 2f, Width / 2f), Random.Range(-Height / 2f, Height / 2f)),
                    new Vector2(size, size));
                floaters.Add(new Floater
                {
                    Transform = tile.transform,
                    Speed = Random.Range(0.25f, 0.7f),
                    Spin = Random.Range(-40f, 40f),
                    Sway = Random.Range(0.1f, 0.4f),
                    Phase = Random.Range(0f, 10f),
                });
            }
        }

        void LateUpdate()
        {
            // 카메라가 판 크기에 맞춰 확대·축소되어도 타일은 화면에서 늘 같은 크기로 보이게 한다.
            float scale = view.orthographicSize * 2f / Height;
            transform.localScale = new Vector3(scale, scale, 1f);

            float dt = Time.unscaledDeltaTime;
            float top = Height / 2f + 1f;
            foreach (Floater f in floaters)
            {
                Vector3 p = f.Transform.localPosition;
                p.y += f.Speed * dt;
                p.x += Mathf.Sin((Time.unscaledTime + f.Phase) * 0.8f) * f.Sway * dt;
                if (p.y > top)
                {
                    p.y = -top;
                    p.x = Random.Range(-Width / 2f, Width / 2f);
                }

                f.Transform.localPosition = p;
                f.Transform.Rotate(0f, 0f, f.Spin * dt);
            }
        }
    }
}
