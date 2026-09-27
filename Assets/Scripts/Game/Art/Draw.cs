using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>화면을 코드로 조립할 때 쓰는 짧은 도우미.</summary>
    public static class Draw
    {
        public static Transform Node(Transform parent, string name, Vector2 localPosition = default)
        {
            var go = new GameObject(name);
            go.layer = parent != null ? parent.gameObject.layer : 0;
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }

        public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Color color, int order,
            Vector2 localPosition = default, Vector2? size = null)
        {
            Transform t = Node(parent, name, localPosition);
            var renderer = t.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            if (size.HasValue)
            {
                t.localScale = new Vector3(size.Value.x, size.Value.y, 1f);
            }

            return renderer;
        }

        /// <summary>모서리가 둥근 사각 패널. size 는 월드 유닛.</summary>
        public static SpriteRenderer Panel(Transform parent, string name, Vector2 size, Color color, int order,
            Vector2 localPosition = default, float cornerRadius = 0.35f)
        {
            SpriteRenderer renderer = Sprite(parent, name, SpriteFactory.Panel(cornerRadius), color, order, localPosition);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            return renderer;
        }

        /// <summary>선 두께가 일정한 둥근 테두리. size 를 바꿔도 두께가 변하지 않는다.</summary>
        public static SpriteRenderer OutlinePanel(Transform parent, string name, Vector2 size, Color color, int order,
            Vector2 localPosition = default, float cornerRadius = 0.25f, float thickness = 0.09f)
        {
            SpriteRenderer renderer = Sprite(parent, name, SpriteFactory.OutlinePanel(cornerRadius, thickness), color, order,
                localPosition);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            return renderer;
        }

        public static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            Color c = renderer.color;
            c.a = alpha;
            renderer.color = c;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
