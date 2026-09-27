using System;
using UnityEngine;

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// 전면 광고가 끝난 직후에 "광고 없이 즐기기"를 권하는 창. 광고를 막 본 순간이라 구매 의향이 가장 높다.
    /// 거슬리지 않게 하루에 한 번만 띄우고, 이미 샀거나 스토어에 연결되지 않았으면 띄우지 않는다.
    /// </summary>
    public static class RemoveAdsPromo
    {
        const string LastShownDayKey = "iap.promoDay";

        static string Today => DateTime.Now.ToString("yyyyMMdd");

        public static bool ShouldShow => RemoveAdsStore.CanBuy && PlayerPrefs.GetString(LastShownDayKey, string.Empty) != Today;

        /// <summary>창을 띄우고, 닫히면(샀든 안 샀든) done 을 부른다.</summary>
        public static void Show(Action done)
        {
            GameApp app = GameApp.Instance;
            if (app == null || !ShouldShow)
            {
                done();
                return;
            }

            PlayerPrefs.SetString(LastShownDayKey, Today);
            PlayerPrefs.Save();

            Transform root = UiRoot.NewLayerRoot("RemoveAdsPromo");
            root.SetParent(app.transform, false);
            UiKit.Modal modal = UiKit.Modal.Open(root, app.Ui, new Vector2(7.8f, 7.4f), 900);
            Transform card = modal.Card;
            const int order = 910;

            void Close()
            {
                modal.Close(() =>
                {
                    UnityEngine.Object.Destroy(root.gameObject);
                    done();
                });
            }

            Draw.Sprite(card, "Icon", Icons.Star, Theme.Gold, order, new Vector2(0f, 2.55f), new Vector2(1.1f, 1.1f));
            Label.Create(card, "Title", Loc.T("promo.title"), Theme.Ink, order, new Vector2(0f, 1.45f), 0.72f,
                TextAnchor.MiddleCenter, true);
            Label body = Label.Create(card, "Body", string.Empty, Theme.SubInk, order, new Vector2(0f, 0.45f), 0.36f);
            body.SetWrappedText(Loc.T("promo.body"), 6.8f);
            body.MoveTo(new Vector2(0f, 0.45f));

            UiButton buy = null;
            buy = UiKit.Button(card, "Buy", Loc.F("promo.buy", RemoveAdsStore.PriceText), null, Theme.Accent, Color.white,
                UiKit.ModalButtonSize, new Vector2(0f, -1.1f), order, () =>
                {
                    buy.Interactable = false;
                    RemoveAdsStore.Buy(bought =>
                    {
                        if (bought)
                        {
                            Close();
                        }
                        else if (buy != null)
                        {
                            buy.Interactable = true;
                        }
                    });
                });
            UiKit.Button(card, "Later", Loc.T("promo.later"), null, Theme.HiddenTile, Theme.Ink, UiKit.ModalButtonSize,
                new Vector2(0f, -2.6f), order, Close);
        }
    }
}
