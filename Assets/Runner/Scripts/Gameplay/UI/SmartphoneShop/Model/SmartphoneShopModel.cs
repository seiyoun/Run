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
            var result = new ShopItemData[count];
            var poolCopy = new List<ShopItemData>(availableItemPool);

            for (int i = 0; i < count; i++)
            {
                if (poolCopy.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, poolCopy.Count);
                    result[i] = poolCopy[randomIndex];
                    poolCopy.RemoveAt(randomIndex);
                }
                else if (availableItemPool.Count > 0)
                {
                    result[i] = availableItemPool[i % availableItemPool.Count];
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
        /// デフォルトのアイテムプールを構築する。
        /// </summary>
        private void InitializeDefaultItems()
        {
            availableItemPool.Clear();
            availableItemPool.Add(new ShopItemData("drone", "追従自律ドローン", "周囲のぶつかり屋を自動索敵して撃退する", "【機】", 300, ShopItemType.Drone));
            availableItemPool.Add(new ShopItemData("bodyguard", "専属ボディガード", "プレイヤーにピッタリ密着して敵をタックルで吹き飛ばす", "【護】", 500, ShopItemType.Bodyguard));
            availableItemPool.Add(new ShopItemData("energy_drink", "メガエナジードリンク", "体力を即座に全快にし、一定時間怒りゲージ上昇UP", "【薬】", 200, ShopItemType.EnergyDrink));
            availableItemPool.Add(new ShopItemData("sneakers", "エアジェットスニーカー", "移動速度が恒久的に25%アップし、回避しやすくなる", "【靴】", 350, ShopItemType.SpeedSneakers));
            availableItemPool.Add(new ShopItemData("magnet", "超電導ポイ活マグネット", "周囲に落ちているポイントやアイテムを一瞬で引き寄せる", "【磁】", 250, ShopItemType.PointMagnet));
            availableItemPool.Add(new ShopItemData("shield", "ワンタイムガード保険", "ぶつかり屋との衝突ダメージを1度だけ完全に無効化する", "【盾】", 400, ShopItemType.BarrierShield));
        }
    }
}
