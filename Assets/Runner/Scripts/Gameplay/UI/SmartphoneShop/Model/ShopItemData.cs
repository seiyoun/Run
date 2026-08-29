/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タイムセールショップで取り扱うアイテム種別および商品データ定義。
 */

using System;

namespace Runner
{
    /// <summary>
    /// ショップで取り扱うアイテムの機能種別。
    /// </summary>
    public enum ShopItemType
    {
        Drone,        // 自動護衛ドローン
        Bodyguard,    // 屈強なボディガード
        EnergyDrink,  // スタミナ栄養ドリンク (HP回復)
        SpeedSneakers,// 超軽量ランニングシューズ (移動速度UP)
        PointMagnet,  // 強力ポイ活マグネット (コイン/ポイント吸引)
        BarrierShield // 身代わりシールド (被弾無効)
    }

    /// <summary>
    /// ショップに陳列される商品アイテムのデータクラス。
    /// </summary>
    [Serializable]
    public class ShopItemData
    {
        public string id;
        public string itemName;
        public string description;
        public string iconEmoji;
        public int price;
        public ShopItemType itemType;

        /// <summary>
        /// 商品データインスタンスを生成する。
        /// </summary>
        /// <param name="id">アイテム識別子</param>
        /// <param name="name">アイテム名</param>
        /// <param name="desc">説明文</param>
        /// <param name="emoji">表示用絵文字/アイコンタグ</param>
        /// <param name="price">必要ポイント数</param>
        /// <param name="type">アイテム機能種別</param>
        public ShopItemData(string id, string name, string desc, string emoji, int price, ShopItemType type)
        {
            this.id = id;
            this.itemName = name;
            this.description = desc;
            this.iconEmoji = emoji;
            this.price = price;
            this.itemType = type;
        }
    }
}
