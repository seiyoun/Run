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
        /// <summary>移動速度バフの識別番号</summary>
        public const int Id = 1;

        private readonly ICharacterStatus status;
        private readonly float additionalSpeed;
        private readonly float duration;
        private float remainingDuration;
        private bool isActive;

        /// <summary>バフ固有の識別番号</summary>
        public int BuffId => Id;

        /// <summary>現在バフが有効かどうか</summary>
        public bool IsActive => isActive;

        /// <summary>バフの残り効果時間（秒）</summary>
        public float RemainingDuration => remainingDuration;

        /// <summary>バフの総効果持続時間（秒）</summary>
        public float Duration => duration;

        /// <summary>バフによる速度上昇値</summary>
        public float AdditionalSpeed => additionalSpeed;

        /// <summary>
        /// 移動速度バフのインスタンスを生成する。
        /// </summary>
        /// <param name="status">対象の ICharacterStatus インターフェース</param>
        /// <param name="additionalSpeed">移動速度の上昇加算値</param>
        /// <param name="duration">効果持続時間（秒）</param>
        public SpeedBuff(ICharacterStatus status, float additionalSpeed, float duration)
        {
            this.status = status;
            this.additionalSpeed = additionalSpeed;
            this.duration = duration;
            this.remainingDuration = duration;
        }

        /// <summary>
        /// バフを付与し、移動速度の上昇値を適用する。
        /// </summary>
        public void Apply()
        {
            if (status == null || isActive) return;

            isActive = true;
            remainingDuration = duration;
            status.AdditionalMoveSpeed = additionalSpeed;
        }

        /// <summary>
        /// バフを解除し、移動速度の上昇値をリセットする。
        /// </summary>
        public void Remove()
        {
            if (status == null || !isActive) return;

            isActive = false;
            remainingDuration = 0f;
            status.AdditionalMoveSpeed = 0f;
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

