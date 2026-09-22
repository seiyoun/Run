/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 一定時間HPを継続回復するリジェネバフクラス。IBuff を実装し、1秒ごとに指定量のHPを回復します。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 一定時間HPを継続的に回復（リジェネ）するバフクラス。
    /// IBuff を実装し、1秒経過ごとにHPを回復して持続時間終了時に自動解除します。
    /// </summary>
    public sealed class HpRegenBuff : IBuff
    {
        private const float HealInterval = 1.0f;

        private readonly CharacterStatus status;
        private readonly int healAmountPerSecond;
        private readonly float duration;
        private float remainingDuration;
        private float intervalTimer;
        private bool isActive;

        /// <summary>現在バフが有効かどうか</summary>
        public bool IsActive => isActive;

        /// <summary>バフの残り効果時間（秒）</summary>
        public float RemainingDuration => remainingDuration;

        /// <summary>バフの総効果持続時間（秒）</summary>
        public float Duration => duration;

        /// <summary>1秒あたりの回復量</summary>
        public int HealAmountPerSecond => healAmountPerSecond;

        /// <summary>
        /// HP継続回復バフのインスタンスを生成する。
        /// </summary>
        /// <param name="status">対象の CharacterStatus コンポーネント</param>
        /// <param name="healAmountPerSecond">1秒あたりの回復量（デフォルト: 5）</param>
        /// <param name="duration">効果持続時間（秒）</param>
        public HpRegenBuff(CharacterStatus status, int healAmountPerSecond, float duration)
        {
            this.status = status;
            this.healAmountPerSecond = healAmountPerSecond;
            this.duration = duration;
            this.remainingDuration = duration;
        }

        /// <summary>
        /// バフを付与し、回復タイマーを開始する。
        /// </summary>
        public void Apply()
        {
            if (status == null || isActive) return;

            isActive = true;
            remainingDuration = duration;
            intervalTimer = 0f;
        }

        /// <summary>
        /// バフを解除し、回復効果を終了する。
        /// </summary>
        public void Remove()
        {
            if (status == null || !isActive) return;

            isActive = false;
            remainingDuration = 0f;
            intervalTimer = 0f;
        }

        /// <summary>
        /// フレーム経過時間による効果時間の減衰およびインターバルごとの回復処理を行う。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            remainingDuration -= deltaTime;
            intervalTimer += deltaTime;

            while (intervalTimer >= HealInterval)
            {
                intervalTimer -= HealInterval;
                status.Heal(healAmountPerSecond);
            }

            if (remainingDuration <= 0f)
            {
                Remove();
            }
        }
    }
}

