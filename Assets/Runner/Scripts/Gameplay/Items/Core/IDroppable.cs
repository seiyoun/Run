/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 死亡時や破壊時にアイテムドロップを要求するエンティティの共通インターフェース。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 死亡・破壊時にアイテムドロップを要求するエンティティが実装するインターフェース。
    /// </summary>
    public interface IDroppable
    {
        /// <summary>
        /// ドロップ発生を要求するイベント（引数: ドロップワールド座標）。
        /// </summary>
        event Action<Vector3> OnDropRequested;
    }
}

