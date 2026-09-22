/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 移動速度を一定時間向上させる移動速度バフクラス。IBuff を実装し、効果時間の管理と効果値を提供します。
 */

namespace Runner
{
    /// <summary>
    /// 移動速度を一時的に向上させる移動速度バフクラス。
    /// IBuff を実装し、効果時間のカウントダウンおよび速度上昇値の保持を行います。
    /// </summary>
    public sealed class SpeedBuff : IBuff
    {
        /// <summary>移動速度バフの識別番号</summary>
        public const int Id = 1;

        private readonly float duration;
        private readonly float value;
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

        /// <summary>移動速度の上昇量</summary>
        public float Value => value;

        /// <summary>
        /// 移動速度バフのインスタンスを生成する。
        /// </summary>
        /// <param name="duration">効果持続時間（秒）</param>
        /// <param name="value">移動速度上昇値</param>
        public SpeedBuff(float duration, float value)
        {
            this.duration = duration;
            this.remainingDuration = duration;
            this.value = value;
        }

        /// <summary>
        /// バフを有効化し、効果時間を初期化する。
        /// </summary>
        public void Apply()
        {
            if (isActive) return;

            isActive = true;
            remainingDuration = duration;
        }

        /// <summary>
        /// バフを無効化する。
        /// </summary>
        public void Remove()
        {
            if (!isActive) return;

            isActive = false;
            remainingDuration = 0f;
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
