/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: フィールド上にドロップ・配置される全アイテムの基底インターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// アイテム種別定義
    /// </summary>
    public enum DropItemType
    {
        Money = 0,      // お金・ポイント
        EnergyDrink = 1, // 回復薬
        SpeedBooster = 2, // 加速アイテム
        Magnet = 3,     // マグネット（全アイテム即時吸引）
    }

    /// <summary>
    /// ドロップアイテムの共通インターフェース。
    /// </summary>
    public interface IItem
    {
        /// <summary>アイテムの種別</summary>
        DropItemType ItemType { get; }

        /// <summary>
        /// 収集者（プレイヤー等）によってアイテムが回収された際の処理。
        /// </summary>
        /// <param name="collector">回収した GameObject</param>
        void Collect(GameObject collector);
    }
}

