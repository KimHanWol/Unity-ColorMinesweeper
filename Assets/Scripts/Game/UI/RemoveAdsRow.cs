using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 설정 창의 "광고 제거" 줄. 왼쪽에 이름, 오른쪽에 가격 버튼(샀으면 "구매함").
    /// 스토어 연결이나 구매 상태가 바뀌면 버튼을 다시 그린다.
    /// </summary>
    public sealed class RemoveAdsRow : MonoBehaviour
    {
        static readonly Vector2 ButtonSize = new Vector2(2.3f, 0.85f);

        /// <summary>오른쪽 끝을 다른 줄(스위치, 언어 버튼)과 같은 x=3.2 에 맞춘다.</summary>
        static readonly Vector2 ButtonCenter = new Vector2(3.2f - 1.15f, 0f);

        int order;
        Transform button;

        public static void Create(Transform parent, Vector2 position, int order)
        {
            Transform root = Draw.Node(parent, "RemoveAds", position);
            Label.Create(root, "Title", Loc.T("settings.removeAds"), Theme.Ink, order, new Vector2(-3.2f, 0f), 0.46f,
                TextAnchor.MiddleLeft)
                .FitWidth(3.8f);
            var row = root.gameObject.AddComponent<RemoveAdsRow>();
            row.order = order;
            row.Rebuild();
            RemoveAdsStore.Changed += row.Rebuild;
        }

        void OnDestroy()
        {
            RemoveAdsStore.Changed -= Rebuild;
        }

        void Rebuild()
        {
            if (this == null)
            {
                return;
            }

            if (button != null)
            {
                Destroy(button.gameObject);
            }

            string text;
            if (RemoveAdsStore.Owned)
            {
                text = Loc.T("settings.removeAds.owned");
            }
            else if (RemoveAdsStore.CanBuy)
            {
                text = RemoveAdsStore.PriceText;
            }
            else
            {
                text = Loc.T("settings.removeAds.loading");
            }

            bool buyable = RemoveAdsStore.CanBuy;
            UiButton created = UiKit.Button(transform, "Buy", text, null, buyable ? Theme.Accent : Theme.HiddenTile,
                buyable ? Color.white : Theme.SubInk, ButtonSize, ButtonCenter, order + 1, () =>
                {
                    if (RemoveAdsStore.CanBuy)
                    {
                        RemoveAdsStore.Buy(null);
                    }
                });
            created.Interactable = buyable;
            button = created.transform;
        }
    }
}
