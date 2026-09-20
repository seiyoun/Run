/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ターゲットへの追従・浮遊移動処理を抽象化するインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ターゲットへの追従・浮遊移動処理を抽象化するインターフェース。
    /// </summary>
    public interface IFollowTarget
    {
        /// <summary>現在の追従対象 Transform</summary>
        Transform Target { get; }

        /// <summary>ターゲットを基準とした基本追従オフセット位置</summary>
        Vector3 FollowOffset { get; set; }

        /// <summary>
        /// 追従対象の Transform を設定する。
        /// </summary>
        /// <param name="newTarget">新しい追従対象</param>
        void SetTarget(Transform newTarget);
    }
}

