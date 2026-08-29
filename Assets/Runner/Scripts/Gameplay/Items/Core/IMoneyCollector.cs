/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: お金・ポイントの収集・増加を行うエンティティ（プレイヤー等）のインターフェース。
 */

using System;

namespace Runner
{
    /// <summary>
    /// お金・ポイントを収集・獲得可能なエンティティのインターフェース。
    /// </summary>
    public interface IMoneyCollector
    {
        /// <summary>現在の所持金額/ポイント</summary>
        long CurrentMoney { get; }

        /// <summary>お金を獲得した際に発火するイベント (獲得金額)</summary>
        event Action<long> OnMoneyCollected;

        /// <summary>お金・ポイントを加算（収集）する</summary>
        /// <param name="amount">加算額</param>
        void CollectMoney(long amount);

        /// <summary>お金・ポイントを消費する</summary>
        /// <param name="amount">消費額</param>
        /// <returns>消費に成功したかどうか</returns>
        bool TryConsumeMoney(long amount);
    }
}

