/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクターのアニメーション制御を抽象化するインターフェース。
 */

namespace Runner
{
    /// <summary>
    /// キャラクターの共通アニメーションステート定義。
    /// </summary>
    public enum CharacterAnimationState
    {
        Idle,
        Move,
        Attack,
        Hit,
        Die
    }

    /// <summary>
    /// キャラクターのアニメーション再生・遷移を制御するインターフェース。
    /// Mecanim Animator、スプライト連番アニメーション、コード駆動アニメーションなど様々な実装に対応します。
    /// </summary>
    public interface ICharacterAnimator
    {
        /// <summary>
        /// 現在のアニメーション状態。
        /// </summary>
        CharacterAnimationState CurrentState { get; }

        /// <summary>
        /// 待機（Idle）アニメーションを再生する。
        /// </summary>
        void PlayIdle();

        /// <summary>
        /// 移動（Move）アニメーションを再生する。
        /// </summary>
        /// <param name="normalizedSpeed">正規化された移動速度（0.0 〜 1.0）</param>
        void PlayMove(float normalizedSpeed);

        /// <summary>
        /// 攻撃（Attack）アニメーションをトリガーする。
        /// </summary>
        void TriggerAttack();

        /// <summary>
        /// 被弾（Hit）アニメーションをトリガーする。
        /// </summary>
        void TriggerHit();

        /// <summary>
        /// 死亡（Die）アニメーションを再生する。
        /// </summary>
        void PlayDie();

        /// <summary>
        /// 指定されたアニメーション状態へ直接切り替える。
        /// </summary>
        /// <param name="state">切り替え先のアニメーション状態</param>
        void SetState(CharacterAnimationState state);
    }
}
