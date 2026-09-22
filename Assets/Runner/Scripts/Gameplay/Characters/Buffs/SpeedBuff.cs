/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 移動速度を一定時間向上させる移動速度バフクラス。IBuff を実装し、効果適用・解除・時間更新を自己完結で管理します。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 移動速度を一時的に向上させる移動速度バフクラス。
    /// IBuff を実装し、効果時間中の速度乗算および解除時の通常復帰を管理します。
    /// </summary>
    public sealed class SpeedBuff : IBuff
    {
        private readonly CharacterMovement2D movement;
        private readonly float multiplier;
        private readonly float duration;
        private float remainingDuration;
        private bool isActive;

        /// <summary>現在バフが有効かどうか</summary>
        public bool IsActive => isActive;

        /// <summary>バフの残り効果時間（秒）</summary>
        public float RemainingDuration => remainingDuration;

        /// <summary>バフの総効果持続時間（秒）</summary>
        public float Duration => duration;

        /// <summary>バフによる速度倍率</summary>
        public float Multiplier => multiplier;

        /// <summary>
        /// 移動速度バフのインスタンスを生成する。
        /// </summary>
        /// <param name="movement">対象の CharacterMovement2D コンポーネント</param>
        /// <param name="multiplier">速度倍率（1.0を超える値）</param>
        /// <param name="duration">効果持続時間（秒）</param>
        public SpeedBuff(CharacterMovement2D movement, float multiplier, float duration)
        {
            this.movement = movement;
            this.multiplier = multiplier;
            this.duration = duration;
            this.remainingDuration = duration;
        }

        /// <summary>
        /// バフを付与し、移動速度の補正倍率を適用する。
        /// </summary>
        public void Apply()
        {
            if (movement == null || isActive) return;

            isActive = true;
            remainingDuration = duration;
            movement.SpeedMultiplier = multiplier;
        }

        /// <summary>
        /// バフを解除し、移動速度の補正倍率を通常値に戻す。
        /// </summary>
        public void Remove()
        {
            if (movement == null || !isActive) return;

            isActive = false;
            remainingDuration = 0f;
            movement.SpeedMultiplier = 1.0f;
        }

        /// <summary>
        /// フレーム経過時間による効果時間の減衰を行い、持続時間終了時に自動解除する。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        public void Tick(float deltaTime)
        {
            if (!isActive) return;

            remainingDuration -= deltaTime;
            if (remainingDuration <= 0f)
            {
                Remove();
            }
        }
    }
}

