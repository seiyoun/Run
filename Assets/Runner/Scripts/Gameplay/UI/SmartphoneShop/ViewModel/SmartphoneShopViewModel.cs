/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タイムセールショップのUI状態管理およびModel・View間の仲介を行うViewModelクラス。
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// タイムセールショップのUI表示状態・ユーザー操作・データバインディングを統括するViewModel。
    /// </summary>
    public sealed class SmartphoneShopViewModel
    {
        private const int OfferItemCount = 3;

        private readonly SmartphoneShopModel model;
        private readonly ShopItemData[] currentOfferedItems = new ShopItemData[OfferItemCount];
        private PointStepHUD pointStepHUD;
        private bool isShopOpen;

        /// <summary>ショップモーダルが開いているかどうか</summary>
        public bool IsOpen => isShopOpen;

        /// <summary>現在陳列されているオファー商品一覧</summary>
        public IReadOnlyList<ShopItemData> OfferedItems => currentOfferedItems;

        /// <summary>ユーザーの現在所持ポイント</summary>
        public long CurrentPoints => pointStepHUD != null ? pointStepHUD.CurrentPoint : 0;

        /// <summary>開閉状態が変化した際のイベント (isOpen)</summary>
        public event Action<bool> OnOpenStateChanged;

        /// <summary>陳列商品が更新された際のイベント (offers, currentPoints)</summary>
        public event Action<IReadOnlyList<ShopItemData>, long> OnOffersUpdated;

        /// <summary>所持ポイント表示が更新された際のイベント (currentPoints)</summary>
        public event Action<long> OnPointsUpdated;

        /// <summary>アイテム購入が成功した際のイベント (purchasedItem)</summary>
        public event Action<ShopItemData> OnItemPurchased;

        /// <summary>ショップが閉じられた際のイベント</summary>
        public event Action OnShopClosed;

        /// <summary>
        /// タイムセールショップViewModelインスタンスを生成する。
        /// </summary>
        /// <param name="shopModel">紐付けるModelインスタンス（nullの場合は新規生成）</param>
        public SmartphoneShopViewModel(SmartphoneShopModel shopModel = null)
        {
            model = shopModel ?? new SmartphoneShopModel();
        }

        /// <summary>
        /// 所持ポイント参照元となるPointStepHUDをバインドする。
        /// </summary>
        /// <param name="hud">バインド対象のPointStepHUD</param>
        public void BindPointHUD(PointStepHUD hud)
        {
            pointStepHUD = hud;
            OnPointsUpdated?.Invoke(CurrentPoints);
        }

        /// <summary>
        /// ショップを開き、商品を新しく陳列してUI更新イベントを発火する。
        /// </summary>
        public void OpenShop()
        {
            isShopOpen = true;

            var offers = model.PickRandomOffers(OfferItemCount);
            for (int i = 0; i < OfferItemCount; i++)
            {
                currentOfferedItems[i] = i < offers.Length ? offers[i] : null;
            }

            OnOpenStateChanged?.Invoke(true);
            OnPointsUpdated?.Invoke(CurrentPoints);
            OnOffersUpdated?.Invoke(currentOfferedItems, CurrentPoints);
        }

        /// <summary>
        /// ショップを閉じる。
        /// </summary>
        public void CloseShop()
        {
            if (!isShopOpen) return;

            isShopOpen = false;
            OnOpenStateChanged?.Invoke(false);
            OnShopClosed?.Invoke();
        }

        /// <summary>
        /// 指定されたスロット位置のアイテムを購入する。
        /// </summary>
        /// <param name="cardIndex">購入対象の商品スロット番号 (0〜2)</param>
        public void BuyItem(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= currentOfferedItems.Length) return;
            var item = currentOfferedItems[cardIndex];
            if (item == null) return;

            if (!model.CanAffordItem(item, CurrentPoints))
            {
                Debug.LogWarning($"[SmartphoneShopViewModel] ポイントが不足しています: 必要 {item.price} pt (所持: {CurrentPoints} pt)");
                return;
            }

            if (pointStepHUD != null && !pointStepHUD.TryConsumePoints(item.price))
            {
                Debug.LogWarning($"[SmartphoneShopViewModel] ポイント消費に失敗しました: {item.price} pt");
                return;
            }

            Debug.Log($"[SmartphoneShopViewModel] 商品購入成功: {item.itemName} (¥{item.price}pt)");
            OnItemPurchased?.Invoke(item);
            OnPointsUpdated?.Invoke(CurrentPoints);

            // 購入完了後に自動でショップを閉じる
            CloseShop();
        }

        /// <summary>
        /// 所持ポイントの表示更新を要求する。
        /// </summary>
        public void RefreshPoints()
        {
            OnPointsUpdated?.Invoke(CurrentPoints);
        }
    }
}
