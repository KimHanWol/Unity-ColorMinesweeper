using System;
using UnityEngine;
#if PIXEL_IAP
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;
#endif

namespace ColorMinesweeper.Game
{
    /// <summary>
    /// "광고 제거" 인앱 상품(한 번 사면 영구). 사면 <see cref="Ads.AdsRemoved"/> 를 켜서 전면 광고를 끈다.
    /// 보상형 광고(이어 하기, 힌트)는 스스로 고르는 것이라 그대로 둔다.
    ///
    /// 결제는 Unity IAP(com.unity.purchasing)로 하고, 패키지가 없으면 PIXEL_IAP 가 꺼져 상품이 보이지 않는다.
    /// 앱을 다시 깔거나 기기를 바꿔도 같은 스토어 계정이면 시작할 때 구매 기록을 받아 와 되살린다.
    /// </summary>
    public static class RemoveAdsStore
    {
        /// <summary>스토어(Play Console, App Store Connect)에 만든 상품 ID. 두 스토어에 같은 ID로 만든다.</summary>
        public const string ProductId = "remove_ads";

        /// <summary>연결, 가격, 구매 상태가 바뀌면 부른다(버튼 글자를 다시 쓰는 데 쓴다).</summary>
        public static event Action Changed;

        /// <summary>이 빌드에 결제 기능이 들어 있는지. 없으면 광고 제거 버튼을 두지 않는다.</summary>
        public static bool IsSupported
        {
            get
            {
#if PIXEL_IAP
                return true;
#else
                return false;
#endif
            }
        }

        public static bool Owned => Ads.AdsRemoved;

        /// <summary>스토어에 연결되어 상품 정보를 받아 왔는지(지금 살 수 있는지).</summary>
        public static bool CanBuy => IsSupported && !Owned && !string.IsNullOrEmpty(PriceText);

        /// <summary>스토어가 알려 준 그 나라 통화의 가격(예: ₩3,300). 받아 오기 전에는 비어 있다.</summary>
        public static string PriceText { get; private set; } = string.Empty;

        static void Notify()
        {
            Changed?.Invoke();
        }

        static void Grant()
        {
            if (Ads.AdsRemoved)
            {
                return;
            }

            Ads.AdsRemoved = true;
            Debug.Log("[IAP] 광고 제거를 적용했습니다.");
            Notify();
        }

#if PIXEL_IAP
        static StoreController store;
        static Action<bool> pendingBuy;

        public static async void Initialize()
        {
            if (store != null)
            {
                return;
            }

            store = UnityIAPServices.StoreController();
            store.OnStoreDisconnected += description =>
                Debug.LogWarning("[IAP] 스토어 연결이 끊겼습니다: " + description.message);
            store.OnProductsFetched += OnProductsFetched;
            store.OnProductsFetchFailed += failure =>
                Debug.LogWarning("[IAP] 상품 정보를 받지 못했습니다: " + failure.FailureReason);
            store.OnPurchasesFetched += OnPurchasesFetched;
            store.OnPurchasesFetchFailed += failure =>
                Debug.LogWarning("[IAP] 구매 기록을 받지 못했습니다: " + failure.message);
            store.OnPurchasePending += OnPurchasePending;
            store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            store.OnPurchaseFailed += OnPurchaseFailed;

            try
            {
                await store.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[IAP] 스토어에 연결하지 못했습니다: " + e.Message);
                return;
            }

            store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(ProductId, ProductType.NonConsumable) });
        }

        /// <param name="done">샀으면 true, 취소하거나 실패하면 false.</param>
        public static void Buy(Action<bool> done)
        {
            Product product = store?.GetProductById(ProductId);
            if (Owned || product == null || !product.availableToPurchase)
            {
                done?.Invoke(Owned);
                return;
            }

            pendingBuy = done;
            store.PurchaseProduct(product);
        }

        /// <summary>구매 복원. iOS 는 사용자가 직접 누르는 복원 버튼이 있어야 해서 둔다(Android 는 시작할 때 저절로 된다).</summary>
        public static void Restore(Action<bool> done)
        {
            if (store == null)
            {
                done?.Invoke(false);
                return;
            }

            store.RestoreTransactions((success, error) =>
            {
                if (!success)
                {
                    Debug.LogWarning("[IAP] 구매 복원에 실패했습니다: " + error);
                }

                store.FetchPurchases();
                done?.Invoke(success);
            });
        }

        static void OnProductsFetched(List<Product> products)
        {
            Product product = products.FirstOrDefault(p => p.definition.id == ProductId);
            PriceText = product?.metadata.localizedPriceString ?? string.Empty;
            Notify();
            // 예전에 산 기록이 있으면 되살린다(앱 재설치, 기기 변경).
            store.FetchPurchases();
        }

        static void OnPurchasesFetched(Orders orders)
        {
            if (orders.ConfirmedOrders.Any(Contains))
            {
                Grant();
            }
        }

        static void OnPurchasePending(PendingOrder order)
        {
            // 한 번 사면 끝나는 상품이라 서버 검증 없이 바로 적용하고 스토어에 완료를 알린다.
            if (Contains(order))
            {
                Grant();
            }

            store.ConfirmPurchase(order);
        }

        static void OnPurchaseConfirmed(Order order)
        {
            if (!Contains(order))
            {
                return;
            }

            if (order is FailedOrder failed)
            {
                Debug.LogWarning("[IAP] 구매 확정에 실패했습니다: " + failed.FailureReason + " " + failed.Details);
            }

            Finish(Owned);
        }

        static void OnPurchaseFailed(FailedOrder order)
        {
            if (!Contains(order))
            {
                return;
            }

            // 이미 산 상품을 다시 사려 하면 실패로 오지만, 실제로는 가지고 있는 것이다.
            if (order.FailureReason == PurchaseFailureReason.DuplicateTransaction)
            {
                Grant();
            }
            else
            {
                Debug.Log("[IAP] 구매하지 않았습니다: " + order.FailureReason + " " + order.Details);
            }

            Finish(Owned);
        }

        static void Finish(bool bought)
        {
            Action<bool> done = pendingBuy;
            pendingBuy = null;
            done?.Invoke(bought);
            Notify();
        }

        static bool Contains(Order order)
        {
            return order.CartOrdered.Items().Any(item => item.Product.definition.id == ProductId);
        }
#else
        public static void Initialize()
        {
        }

        public static void Buy(Action<bool> done)
        {
            done?.Invoke(Owned);
        }

        public static void Restore(Action<bool> done)
        {
            done?.Invoke(Owned);
        }
#endif
    }
}
