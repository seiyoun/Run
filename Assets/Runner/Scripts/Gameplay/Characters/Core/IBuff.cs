/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクター等に適用されるバフ効果のライフサイクル（付与・解除・更新）を規定するインターフェース。
 */

namespace Runner
{
    /// <summary>
    /// キャラクター等に付与されるバフ効果の共通インターフェース。
    /// バフの適用（付与）、無効化（解除）、および時間経過更新を管理します。
    /// </summary>
    public interface IBuff
    {
        /// <summary>バフ固有の識別番号</summary>
        int BuffId { get; }

        /// <summary>現在バフが有効かどうか</summary>
        bool IsActive { get; }

        /// <summary>バフの残り効果時間（秒）</summary>
        float RemainingDuration { get; }

        /// <summary>バフの総効果持続時間（秒）</summary>
        float Duration { get; }

        /// <summary>
        /// バフを付与し、効果を適用する。
        /// </summary>
        void Apply();

        /// <summary>
        /// バフを解除し、効果を無効化する。
        /// </summary>
        void Remove();

        /// <summary>
        /// フレーム経過時間による効果時間の更新を行う。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        void Tick(float deltaTime);
    }
}

