/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ダメージを受けるエンティティ（プレイヤー、敵、破壊可能オブジェクト等）を抽象化するインターフェース。
 */

using System;

namespace Runner
{
    /// <summary>
    /// ダメージを受けることが可能なエンティティの共通インターフェース。
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 死亡・破壊状態かどうか。
        /// </summary>
        bool IsDead { get; }

        /// <summary>
        /// ダメージを受けた際に発火するイベント (実際のダメージ量)。
        /// </summary>
        event Action<int> OnTakeDamage;

        /// <summary>
        /// HPが0になり死亡・破壊された際に発火するイベント。
        /// </summary>
        event Action OnDead;

        /// <summary>
        /// ダメージを受ける。
        /// </summary>
        /// <param name="amount">ダメージ量</param>
        void TakeDamage(int amount);
    }
}

