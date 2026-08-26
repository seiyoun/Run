/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクターや武器などの攻撃挙動・パラメータを抽象化するインターフェース。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 攻撃を行うエンティティ（プレイヤー、敵モンスター、武器等）の共通攻撃インターフェース。
    /// </summary>
    public interface IAttacker
    {
        /// <summary>
        /// 攻撃力（基本ダメージ量）。
        /// </summary>
        int AttackPower { get; set; }

        /// <summary>
        /// 攻撃間隔（秒）。クールダウン時間。
        /// </summary>
        float AttackInterval { get; set; }

        /// <summary>
        /// 現在攻撃可能かどうか（クールダウン完了状態かつ行動可能状態か）。
        /// </summary>
        bool CanAttack { get; }

        /// <summary>
        /// 攻撃を実行した際に発火するイベント。
        /// </summary>
        event Action OnAttack;

        /// <summary>
        /// 攻撃を実行する（デフォルト方向または正面）。
        /// </summary>
        void Attack();

        /// <summary>
        /// 指定された方向に向けて攻撃を実行する。
        /// </summary>
        /// <param name="direction">攻撃方向ベクトル</param>
        void Attack(Vector2 direction);
    }
}
