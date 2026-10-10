using System.Collections.Generic;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 메인 화면의 "PIXEL CLUE" 로고. 글자 하나가 칸 하나다: PIXEL 은 색이 드러난 칸(<see cref="Theme.Pastels"/> 순서대로),
    /// CLUE 는 아직 안 연 칸. 버튼처럼 아래에 진한 면을 깔아 도톰하게 보이게 하고, 가끔 빛이 대각선으로 훑고 지나간다.
    /// 색은 Theme 에서만 가져오므로 앱 아이콘 등 다른 곳에서도 같은 색으로 다시 그릴 수 있다.
    /// </summary>
    public sealed class PixelLogo : MonoBehaviour
    {
        /// <summary>타일 한 칸의 크기, 칸 사이 간격, 아래로 보이는 단면의 두께(유닛).</summary>
        const float Tile = 1.2f;
        const float Gap = 0.14f;
        const float Depth = 0.13f;
        const float ShineEvery = 4.5f;

        sealed class Face
        {
            public SpriteRenderer Renderer;
            public Color Color;

            /// <summary>빛이 닿는 순서(왼쪽 위 0 → 오른쪽 아래 1).</summary>
            public float Along;
        }

        readonly List<Face> faces = new List<Face>();
        float nextShine;
        float shineStart = -10f;

        /// <summary>두 줄과 사이 간격, 아랫줄 단면까지 잰 전체 높이.</summary>
        public static float Height => Tile * 2f + Gap + Depth * 2f;

        /// <param name="maxWidth">이 폭을 넘으면 전체를 줄여 맞춘다.</param>
        public static PixelLogo Create(Transform parent, Vector2 position, float maxWidth, int order)
        {
            Transform root = Draw.Node(parent, "PixelLogo", position);
            var logo = root.gameObject.AddComponent<PixelLogo>();
            float lineOffset = (Tile + Gap + Depth) / 2f;
            float widest = Mathf.Max(
                logo.BuildWord(root, "PIXEL", true, lineOffset, order),
                logo.BuildWord(root, "CLUE", false, -lineOffset, order));
            float scale = Mathf.Min(1f, maxWidth / widest);
            root.localScale = new Vector3(scale, scale, 1f);
            logo.nextShine = Time.unscaledTime + 1.2f;
            return logo;
        }

        /// <summary>한 낱말을 가운데 정렬해 놓고 그 폭(유닛)을 돌려준다.</summary>
        float BuildWord(Transform parent, string word, bool revealed, float centerY, int order)
        {
            float width = word.Length * Tile + (word.Length - 1) * Gap;
            for (int i = 0; i < word.Length; i++)
            {
                Color color = revealed ? Theme.Pastels[i % Theme.Pastels.Length] : Theme.HiddenTile;
                Color ink = revealed ? Color.white : Theme.Ink;
                var position = new Vector2(-width / 2f + Tile / 2f + i * (Tile + Gap), centerY);
                Vector2 size = Vector2.one * Tile;
                Draw.Sprite(parent, "Under", SpriteFactory.RoundedRect(0.2f), Color.Lerp(color, Theme.Ink, 0.22f), order,
                    position + Vector2.down * Depth, size);
                faces.Add(new Face
                {
                    Renderer = Draw.Sprite(parent, "Tile", SpriteFactory.RoundedRect(0.2f), color, order + 1, position, size),
                    Color = color,
                    Along = position.x - position.y,
                });
                // 글꼴의 대문자가 줄 가운데보다 위에 앉아 있어서, 칸 가운데로 보이게 조금 내린다.
                Label.Create(parent, "Letter", word[i].ToString(), ink, order + 2, position + Vector2.down * (Tile * 0.13f),
                    Tile * 0.86f);
            }

            return width;
        }

        void Start()
        {
            // 빛이 닿는 순서를 0~1 로 맞춘다.
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (Face face in faces)
            {
                min = Mathf.Min(min, face.Along);
                max = Mathf.Max(max, face.Along);
            }

            foreach (Face face in faces)
            {
                face.Along = Mathf.InverseLerp(min, max, face.Along);
            }
        }

        void Update()
        {
            const float sweep = 0.5f;
            const float flash = 0.3f;
            float now = Time.unscaledTime;
            if (now >= nextShine)
            {
                shineStart = now;
                nextShine = now + ShineEvery;
            }

            float elapsed = now - shineStart;
            if (elapsed > sweep + flash + 0.1f)
            {
                return;
            }

            foreach (Face face in faces)
            {
                float local = (elapsed - face.Along * sweep) / flash;
                float glow = local > 0f && local < 1f ? Ease.Pulse(local) : 0f;
                face.Renderer.color = Color.Lerp(face.Color, Color.white, glow * 0.45f);
            }
        }
    }
}
