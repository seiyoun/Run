/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: GameProgressManager のショップ入荷進捗計算およびセール直接オープン判定を担う partial クラス。
 */

using System;
using Shiyuan.Foundation.Core;

namespace Runner
{
    public sealed partial class GameProgressManager
    {
        private const long SaleTriggerPointInterval = 300;

        /// <summary>入荷進捗更新イベント（引数: 残り必要ポイント, 進捗率0~1, 初回フラグ）</summary>
        public event Action<long, float, bool> OnRestockProgressUpdated;

        /// <summary>累積ポイント到達によるショップ直接オープン要求イベント</summary>
        public event Action OnShopTriggered;

        private long nextSaleTriggerPoint = SaleTriggerPointInterval;

        /// <summary>
        /// ショップ入荷進捗を初期化し、初期ゲージ状態を通知する。
        /// </summary>
        private void StartRestockProgress()
        {
            nextSaleTriggerPoint = SaleTriggerPointInterval;
            long initialEarned = GameRecordTracker.HasInstance ? GameRecordTracker.Instance.EarnedMoney : 0;
            long initialCycleEarned = initialEarned % SaleTriggerPointInterval;
            long initialRemaining = SaleTriggerPointInterval - initialCycleEarned;
            float initialProgress = (float)initialCycleEarned / SaleTriggerPointInterval;
            OnRestockProgressUpdated?.Invoke(initialRemaining, initialProgress, true);
        }

        /// <summary>
        /// 累積獲得ポイントに基づく入荷進捗率の計算およびセール（ショップオープン）発生判定を行う。
        /// </summary>
        private void TickRestock()
        {
            long totalEarned = GameRecordTracker.HasInstance ? GameRecordTracker.Instance.EarnedMoney : 0;
            long cycleEarned = totalEarned % SaleTriggerPointInterval;
            long remainingPoints = SaleTriggerPointInterval - cycleEarned;
            float progress = (float)cycleEarned / SaleTriggerPointInterval;

            if (totalEarned >= nextSaleTriggerPoint)
            {
                nextSaleTriggerPoint = ((totalEarned / SaleTriggerPointInterval) + 1) * SaleTriggerPointInterval;
                OnShopTriggered?.Invoke();
                OnRestockProgressUpdated?.Invoke(remainingPoints, progress, false);
            }
            else
            {
                OnRestockProgressUpdated?.Invoke(remainingPoints, progress, false);
            }
        }

        /// <summary>
        /// 入荷・ショップ関連イベントの購読を解除する。
        /// </summary>
        private void CleanupRestockEvents()
        {
            OnRestockProgressUpdated = null;
            OnShopTriggered = null;
        }
    }
}
