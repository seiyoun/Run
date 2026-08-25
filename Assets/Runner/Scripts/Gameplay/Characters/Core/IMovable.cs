/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクター等の移動処理を抽象化するインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 移動可能なエンティティ（プレイヤー、敵モンスター、NPC等）の共通移動インターフェース。
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// 移動速度。
        /// </summary>
        float MoveSpeed { get; set; }

        /// <summary>
        /// 現在の移動入力ベクトル（正規化済み）。
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// 現在向いている方向（Vector2.right / Vector2.left 等）。
        /// </summary>
        Vector2 FacingDirection { get; }

        /// <summary>
        /// 指定された方向ベクトルに従って移動を行う。
        /// </summary>
        /// <param name="direction">移動方向ベクトル</param>
        void Move(Vector2 direction);

        /// <summary>
        /// 移動を停止する。
        /// </summary>
        void Stop();
    }
}
