/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 一定時間継続回復を行うリジェネバフクラス。IBuff を実装し、1秒ごとに回復イベントを通知します。
 */

using System;

namespace Runner
{
    /// <summary>
    /// 一定時間継続的に効果を発揮するバフクラス。
    /// IBuff を実装し、1秒経過ごとに回復イベントを発火して持続時間終了時に自動解除します。
    /// </summary>
    public sealed class HpRegenBuff : IBuff
    {
        /// <summary>HP継続回復バフの識別番号</summary>
        public const int Id = 2;

        private const float HealInterval = 1.0f;

        private readonly float duration;
        private readonly int healAmountPerSecond;
        private float remainingDuration;
        private float intervalTimer;
        private bool isActive;

        /// <summary>1秒ごとの回復タイミングで発火するイベント (回復量)</summary>
        public event Action<int> OnHealTick;

        /// <summary>バフ固有の識別番号</summary>
        public int BuffId => Id;

        /// <summary>現在バフが有効かどうか</summary>
        public bool IsActive => isActive;

        /// <summary>バフの残り効果時間（秒）</summary>
        public float RemainingDuration => remainingDuration;

        /// <summary>バフの総効果持続時間（秒）</summary>
        public float Duration => duration;

        /// <summary>1秒あたりの回復量</summary>
        public int Value => healAmountPerSecond;

        /// <summary>
        /// HP継続回復バフのインスタンスを生成する。
        /// </summary>
        /// <param name="duration">効果持続時間（秒）</param>
        /// <param name="healAmountPerSecond">1秒あたりの回復量</param>
        public HpRegenBuff(float duration, int healAmountPerSecond)
        {
            this.duration = duration;
            this.remainingDuration = duration;
            this.healAmountPerSecond = healAmountPerSecond;
        }

        /// <summary>
        /// バフを有効化し、タイマーを開始する。
        /// </summary>
        public void Apply()
        {
            if (isActive) return;

            isActive = true;
            remainingDuration = duration;
            intervalTimer = 0f;
        }

        /// <summary>
        /// バフを無効化し、効果を終了する。
        /// </summary>
        public void Remove()
        {
            if (!isActive) return;

            isActive = false;
            remainingDuration = 0f;
            intervalTimer = 0f;
        }

        /// <summary>
        /// フレーム経過時間による効果時間の減衰およびインターバルごとの回復イベント発火を行う。
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
                OnHealTick?.Invoke(healAmountPerSecond);
            }

            if (remainingDuration <= 0f)
            {
                Remove();
            }
        }
    }
}
