/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タイムセールショップのアイテムマスター管理および購入・抽選ロジックを担うModelクラス。
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// タイムセールショップのビジネスロジックおよびデータ管理を担うModel。
    /// </summary>
    public sealed class SmartphoneShopModel
    {
        private readonly List<ShopItemData> availableItemPool = new List<ShopItemData>();

        /// <summary>利用可能な全アイテムプール</summary>
        public IReadOnlyList<ShopItemData> AvailableItemPool => availableItemPool;

        /// <summary>
        /// タイムセールショップModelの初期化を行い、デフォルトのアイテムプールを登録する。
        /// </summary>
        public SmartphoneShopModel()
        {
            InitializeDefaultItems();
        }

        /// <summary>
        /// 指定された個数の商品を重複なくランダムに選出する。
        /// </summary>
        /// <param name="count">選出するアイテム数</param>
        /// <returns>選出されたアイテム配列</returns>
        public ShopItemData[] PickRandomOffers(int count)
        {
            int offerCount = Mathf.Min(count, availableItemPool.Count);
            var result = new ShopItemData[offerCount];
            var poolCopy = new List<ShopItemData>(availableItemPool);

            for (int i = 0; i < offerCount; i++)
            {
                if (poolCopy.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, poolCopy.Count);
                    result[i] = poolCopy[randomIndex];
                    poolCopy.RemoveAt(randomIndex);
                }
            }

            return result;
        }

        /// <summary>
        /// アイテム購入が可能かどうかを判定する。
        /// </summary>
        /// <param name="item">購入対象アイテム</param>
        /// <param name="currentPoints">ユーザーの所持ポイント</param>
        /// <returns>購入可能な場合true</returns>
        public bool CanAffordItem(ShopItemData item, long currentPoints)
        {
            if (item == null) return false;
            return currentPoints >= item.price;
        }

        /// <summary>
        /// アイテムプールに新規アイテムを追加する。
        /// </summary>
        /// <param name="item">追加するアイテムデータ</param>
        public void AddItemToPool(ShopItemData item)
        {
            if (item != null && !availableItemPool.Contains(item))
            {
                availableItemPool.Add(item);
            }
        }

        /// <summary>
        /// マスターデータからアイテムプールを初期化・構築する（現在は追従自律ドローンおよび移動速度アップを登録）。
        /// </summary>
        private void InitializeDefaultItems()
        {
            availableItemPool.Clear();

            var shopItems = MasterDataManager.AllShopItemData;
            if (shopItems != null && shopItems.Count > 0)
            {
                for (int i = 0; i < shopItems.Count; i++)
                {
                    if (shopItems[i].ItemId == ShopItemId.Drone || shopItems[i].ItemId == ShopItemId.SpeedUp)
                    {
                        availableItemPool.Add(shopItems[i]);
                    }
                }
            }
        }
    }
}
