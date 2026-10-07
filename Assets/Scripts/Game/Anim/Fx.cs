using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 한 번 터지고 사라지는 작은 이펙트들. 파티클 시스템 없이 스프라이트 몇 장을 <see cref="Tween"/> 으로 움직인다.
    /// 부모가 사라지면 같이 사라지므로 화면을 넘길 때 따로 치울 필요가 없다.
    ///
    /// 게임의 모든 연출은 같은 재료를 쓴다: 조각은 칸과 같은 둥근 네모, 색은 그 자리의 색(칸 색, 하트의 빨강, 별의 금색),
    /// 움직임은 톡 튀었다가 가라앉는 곡선. 새 연출을 넣을 때도 여기 있는 것을 조합해 쓴다.
    /// </summary>
    public static class Fx
    {
        static readonly Color[] ConfettiColors =
        {
            Theme.Hex(0xFF8FAB), Theme.Hex(0xFFD166), Theme.Hex(0x8ECAE6), Theme.Hex(0x95D5B2), Theme.Hex(0xC77DFF),
            Theme.Hex(0xFF9F68),
        };

        /// <summary>색 조각이 사방으로 튀었다가 떨어지며 작아진다. 칸을 맞혔을 때 쓴다.</summary>
        public static void Burst(Transform parent, Vector2 position, Color color, int order, int count = 7,
            float reach = 0.85f, float size = 0.2f)
        {
            float turn = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = (turn + i * 360f / count + Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float distance = reach * Random.Range(0.6f, 1f);
                float pieceSize = size * Random.Range(0.7f, 1.2f);
                float spin = Random.Range(-260f, 260f);
                float fall = reach * Random.Range(0.35f, 0.7f);
                Color tint = Color.Lerp(color, Color.white, Random.Range(0f, 0.35f));
                SpriteRenderer piece = Draw.Sprite(parent, "Bit", SpriteFactory.RoundedRect(0.25f), tint, order, position,
                    new Vector2(pieceSize, pieceSize));
                Transform t = piece.transform;
                Tween.Run(piece, Random.Range(0.4f, 0.55f), k =>
                {
                    float outward = Ease.OutCubic(k);
                    t.localPosition = position + direction * (distance * outward) + Vector2.down * (fall * k * k);
                    float s = pieceSize * (1f - k * k);
                    t.localScale = new Vector3(s, s, 1f);
                    t.localRotation = Quaternion.Euler(0f, 0f, spin * k);
                }, Ease.Linear, 0f, () => Object.Destroy(piece.gameObject));
            }
        }

        /// <summary>동그란 테두리가 퍼지며 옅어진다. 누른 자리를 짚어 준다.</summary>
        public static void Ring(Transform parent, Vector2 position, Color color, int order, float toSize = 1.7f)
        {
            SpriteRenderer ring = Draw.Sprite(parent, "Ring", SpriteFactory.OutlineCircle(), color, order, position,
                new Vector2(0.5f, 0.5f));
            Tween.Run(ring, 0.38f, k =>
            {
                float s = Mathf.Lerp(0.5f, toSize, k);
                ring.transform.localScale = new Vector3(s, s, 1f);
                Draw.SetAlpha(ring, (1f - k) * 0.8f);
            }, Ease.OutCubic, 0f, () => Object.Destroy(ring.gameObject));
        }

        /// <summary>조각이 튀고 테두리가 퍼지는 한 묶음. 무언가 맞아떨어진 순간(칸, 별, 다 칠한 색)에 쓴다.</summary>
        public static void Pop(Transform parent, Vector2 position, Color color, int order, float scale = 1f)
        {
            Burst(parent, position, color, order, 7, 0.85f * scale, 0.2f * scale);
            Ring(parent, position, Color.Lerp(color, Color.white, 0.4f), order, 1.7f * scale);
        }

        /// <summary>
        /// 한 번에 여러 칸이 열렸을 때 "+12" 처럼 연 칸 수를 띄운다. 단서 칩과 같은 모양(흰 테두리 알약에 도트 숫자)이라
        /// 판 위의 다른 숫자와 한 식구로 보인다.
        /// </summary>
        public static void Count(Transform parent, Vector2 position, int count, Color color, Color ink, int order)
        {
            Transform root = Draw.Node(parent, "Count", position);
            string text = "+" + count;
            const float height = 0.42f;
            float width = text.Length * height * 6f / 7f + 0.4f;
            SpriteRenderer rim = Draw.Panel(root, "Rim", new Vector2(width + 0.12f, height + 0.42f), Color.white, order,
                Vector2.zero, (height + 0.42f) / 2f);
            SpriteRenderer fill = Draw.Panel(root, "Fill", new Vector2(width, height + 0.3f), color, order + 1, Vector2.zero,
                (height + 0.3f) / 2f);
            PixelText digits = PixelText.Create(root, "Digits", text, ink, order + 2, Vector2.zero, height);
            root.localScale = Vector3.zero;
            Tween.Run(root, 1.05f, k =>
            {
                float pop = Ease.OutBack(Mathf.Min(1f, k * 4.5f));
                root.localScale = Vector3.one * pop;
                root.localPosition = position + Vector2.up * (0.9f * Ease.OutCubic(k));
                float alpha = k < 0.65f ? 1f : 1f - (k - 0.65f) / 0.35f;
                Draw.SetAlpha(rim, alpha);
                Draw.SetAlpha(fill, alpha);
                digits.SetAlpha(alpha);
            }, Ease.Linear, 0f, () => Object.Destroy(root.gameObject));
        }

        /// <summary>작은 그림(하트, 반짝이)이 흔들리며 떠올라 사라진다.</summary>
        public static void FloatUp(Transform parent, Sprite sprite, Color color, Vector2 position, float size, int order,
            float delay = 0f)
        {
            SpriteRenderer icon = Draw.Sprite(parent, "Float", sprite, color, order, position, Vector2.zero);
            float sway = Random.Range(-0.25f, 0.25f);
            float rise = Random.Range(0.7f, 1.05f);
            Tween.Run(icon, 0.85f, k =>
            {
                float pop = Mathf.Min(1f, k * 5f);
                float s = size * Ease.OutBack(pop);
                icon.transform.localScale = new Vector3(s, s, 1f);
                icon.transform.localPosition = position + new Vector2(Mathf.Sin(k * Mathf.PI * 2f) * 0.08f + sway * k, rise * k);
                Draw.SetAlpha(icon, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            }, Ease.OutCubic, delay, () => Object.Destroy(icon.gameObject));
        }

        /// <summary>색종이가 area 위에서 쏟아져 내린다. 그림을 완성했을 때 쓴다.</summary>
        public static void Confetti(Transform parent, Rect area, int order, int count = 46)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(area.xMin, area.xMax);
                float startY = area.yMax + Random.Range(0.3f, 2.5f);
                float endY = area.yMin - 1f;
                float width = Random.Range(0.16f, 0.3f);
                float height = width * Random.Range(0.5f, 1f);
                float sway = Random.Range(0.3f, 0.9f);
                float swaySpeed = Random.Range(2f, 4.5f);
                float phase = Random.Range(0f, 6.28f);
                float spin = Random.Range(-420f, 420f);
                float flip = Random.Range(4f, 9f);
                SpriteRenderer piece = Draw.Sprite(parent, "Confetti", SpriteFactory.RoundedRect(0.2f),
                    ConfettiColors[i % ConfettiColors.Length], order, new Vector2(x, startY), new Vector2(width, height));
                Transform t = piece.transform;
                Tween.Run(piece, Random.Range(1.7f, 2.9f), k =>
                {
                    t.localPosition = new Vector3(x + Mathf.Sin(phase + k * swaySpeed * 2f) * sway, Mathf.Lerp(startY, endY, k), 0f);
                    t.localRotation = Quaternion.Euler(0f, 0f, spin * k);
                    // 뒤집히며 떨어지는 것처럼 세로로 납작해졌다 펴진다.
                    t.localScale = new Vector3(width, height * Mathf.Abs(Mathf.Cos(phase + k * flip * 2f)), 1f);
                }, Ease.Linear, Random.Range(0f, 0.5f), () => Object.Destroy(piece.gameObject));
            }
        }
    }
}
