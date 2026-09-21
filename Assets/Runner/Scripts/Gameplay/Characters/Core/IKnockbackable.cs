/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ノックバック外力を受けるエンティティを抽象化するインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ノックバック外力を受けることが可能なエンティティの共通インターフェース。
    /// </summary>
    public interface IKnockbackable
    {
        /// <summary>
        /// 指定された方向と力でノックバック外力を適用する。
        /// </summary>
        /// <param name="direction">ノックバック方向ベクトル</param>
        /// <param name="force">ノックバックの強さ</param>
        void ApplyKnockback(Vector2 direction, float force);
    }
}
