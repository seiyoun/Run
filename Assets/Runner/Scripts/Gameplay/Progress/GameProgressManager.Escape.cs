/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: GameProgressManager の経過時間計測・脱出タイマー・非常口開放を担う partial クラス。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    public sealed partial class GameProgressManager
    {
        /// <summary>経過時間更新イベント（引数: 累計経過秒数）</summary>
        public event Action<float> OnTimeUpdated;

        /// <summary>脱出残り時間更新イベント（引数: 脱出までの残り秒数）</summary>
        public event Action<float> OnRemainingTimeUpdated;

        /// <summary>非常口開放イベント</summary>
        public event Action OnExitUnlocked;

        private float elapsedTime;
        private float escapeDuration = 180f;
        private bool isExitUnlocked;

        /// <summary>ゲーム開始からの累計経過時間（秒）</summary>
        public float ElapsedTime => elapsedTime;

        /// <summary>ステージで設定された脱出までの制限時間（秒）</summary>
        public float EscapeDuration => escapeDuration;

        /// <summary>脱出までの残り時間（秒）</summary>
        public float RemainingEscapeTime => Mathf.Max(0f, escapeDuration - elapsedTime);

        /// <summary>非常口が開放されているかどうか</summary>
        public bool IsExitUnlocked => isExitUnlocked;

        /// <summary>
        /// StageMasterData からステージ設定（脱出制限時間等）を一括反映する。
        /// </summary>
        /// <param name="stageData">適用するステージマスターデータ</param>
        public void ApplyStageData(StageMasterData stageData)
        {
            if (stageData == null) return;
            SetEscapeDuration(stageData.EscapeTime);
        }

        /// <summary>
        /// ステージ設定の脱出制限時間を反映する。
        /// </summary>
        /// <param name="duration">脱出制限時間（秒）</param>
        public void SetEscapeDuration(float duration)
        {
            escapeDuration = Mathf.Max(1f, duration);
        }

        /// <summary>
        /// 経過時間・脱出タイマーおよび非常口状態を初期化し、初期残り時間を通知する。
        /// </summary>
        private void StartEscapeProgress()
        {
            elapsedTime = 0f;
            isExitUnlocked = false;
            OnRemainingTimeUpdated?.Invoke(RemainingEscapeTime);
        }

        /// <summary>
        /// 経過時間を加算し、脱出残り時間の通知および非常口の開放判定を行う。
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        private void TickEscape(float deltaTime)
        {
            elapsedTime += deltaTime;
            OnTimeUpdated?.Invoke(elapsedTime);
            OnRemainingTimeUpdated?.Invoke(RemainingEscapeTime);

            if (!isExitUnlocked && elapsedTime >= escapeDuration)
            {
                isExitUnlocked = true;
                DebugLogger.Log("[GameProgressManager] 脱出制限時間に到達し、非常口が開放されました！");
                OnExitUnlocked?.Invoke();
            }
        }

        /// <summary>
        /// 時間・脱出関連イベントの購読を解除する。
        /// </summary>
        private void CleanupEscapeEvents()
        {
            OnTimeUpdated = null;
            OnRemainingTimeUpdated = null;
            OnExitUnlocked = null;
        }
    }
}
