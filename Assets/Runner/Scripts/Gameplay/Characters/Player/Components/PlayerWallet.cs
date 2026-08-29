/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IMoneyCollector を実装し、プレイヤーの所持金・ポイントの収集・消費・残高管理を担うウォレットコンポーネント。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの所持金・ポイント残高の管理および加算・消費ロジックを制御するコンポーネント。
    /// IMoneyCollector インターフェースを実装し、Single Source of Truth として機能します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWallet : MonoBehaviour, IMoneyCollector
    {
        private long currentMoney;
        private long totalEarnedMoney;

        /// <summary>現在の所持金額/ポイント</summary>
        public long CurrentMoney => currentMoney;

        /// <summary>ゲーム開始からの累積獲得金額/ポイント</summary>
        public long TotalEarnedMoney => totalEarnedMoney;

        /// <summary>お金を獲得した際に発火するイベント (獲得金額)</summary>
        public event Action<long> OnMoneyCollected;

        /// <summary>所持金残高が変化した際に発火するイベント (現在の所持金額)</summary>
        public event Action<long> OnMoneyChanged;

        /// <summary>
        /// お金・ポイントを加算（収集）する。
        /// </summary>
        /// <param name="amount">加算額</param>
        public void CollectMoney(long amount)
        {
            if (amount <= 0) return;

            currentMoney += amount;
            totalEarnedMoney += amount;
            OnMoneyCollected?.Invoke(amount);
            OnMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// お金・ポイントを消費する。
        /// </summary>
        /// <param name="amount">消費額</param>
        /// <returns>消費に成功したかどうか（残高不足時はfalse）</returns>
        public bool TryConsumeMoney(long amount)
        {
            if (amount <= 0 || currentMoney < amount) return false;

            currentMoney -= amount;
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }

        /// <summary>
        /// 所持金残高を直接設定する。
        /// </summary>
        /// <param name="money">設定する所持金</param>
        public void SetMoney(long money)
        {
            currentMoney = Math.Max(0, money);
            OnMoneyChanged?.Invoke(currentMoney);
        }
    }
}

