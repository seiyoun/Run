/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ターゲット（エネミー等）の索敵処理を抽象化するインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ターゲット（エネミー等）の索敵処理を抽象化するインターフェース。
    /// 攻撃コンポーネントや追尾システムが索敵ロジックを統一的に扱うために使用します。
    /// </summary>
    public interface IFindTarget
    {
        /// <summary>
        /// 索敵を行う最大半径。
        /// </summary>
        float SearchRadius { get; set; }

        /// <summary>
        /// 指定された基準位置から最も適したターゲットを検索して返す。
        /// </summary>
        /// <param name="origin">索敵の中心ワールド座標</param>
        /// <returns>検知されたターゲットの Transform（未検知時は null）</returns>
        Transform FindTarget(Vector3 origin);
    }
}

