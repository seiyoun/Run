/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: HP回復を受けるエンティティ（プレイヤー、味方キャラクター等）を抽象化するインターフェース。
 */

using System;

namespace Runner
{
    /// <summary>
    /// HP回復を受けることが可能なエンティティの共通インターフェース。
    /// </summary>
    public interface IHealable
    {
        /// <summary>
        /// HPが回復した際に発火するイベント (実際の回復量)。
        /// </summary>
        event Action<int> OnHeal;

        /// <summary>
        /// HPを回復する。
        /// </summary>
        /// <param name="amount">回復量</param>
        void Heal(int amount);
    }
}

