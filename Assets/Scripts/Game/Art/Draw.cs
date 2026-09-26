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
