/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ICharacterAnimator を実装した 2D アニメーション制御コンポーネント。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ICharacterAnimator を実装した 2D アニメーション制御クラス。
    /// Unity の Animator コンポーネントが存在すれば Mecanim パラメータを連動させ、
    /// 未設定の場合はコード駆動の自然なボビング・呼吸アニメーションを自動再生します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimator2D : MonoBehaviour, ICharacterAnimator
    {
        [Header("Animator Reference (Optional)")]
        [Tooltip("Unity Animator コンポーネント（設定されている場合は優先使用）")]
        [SerializeField]
        private Animator unityAnimator;

        [Header("Procedural Animation Settings")]
        [Tooltip("コード駆動アニメーションを有効にするか（Animator がない場合に自動動作）")]
        [SerializeField]
        private bool enableProceduralFallback = true;

        [SerializeField]
        private float idleBreathSpeed = 2.5f;

        [SerializeField]
        private float idleBreathIntensity = 0.04f;

        [SerializeField]
        private float runBobSpeed = 16f;

        [SerializeField]
        private float runBobIntensity = 0.1f;

        private CharacterAnimationState currentState = CharacterAnimationState.Idle;
        private Transform visualTransform;
        private Vector3 baseScale = Vector3.one;
        private float animTimer;
        private float currentSpeed;

        // Mecanim パラメータのハッシュ
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        private static readonly int DieTriggerHash = Animator.StringToHash("Die");

        public CharacterAnimationState CurrentState => currentState;

        private void Awake()
        {
            if (unityAnimator == null)
            {
                unityAnimator = GetComponentInChildren<Animator>();
            }

            var visual = GetComponentInChildren<SpriteRenderer>();
            visualTransform = visual != null ? visual.transform : transform;
            baseScale = visualTransform.localScale;
        }

        #region ICharacterAnimator Implementation

        /// <summary>
        /// 待機（Idle）アニメーションを再生する。
        /// </summary>
        public void PlayIdle()
        {
            if (currentState == CharacterAnimationState.Die) return;

            currentState = CharacterAnimationState.Idle;
            currentSpeed = 0f;

            if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
            {
                unityAnimator.SetBool(IsMovingHash, false);
                unityAnimator.SetFloat(MoveSpeedHash, 0f);
            }
        }

        /// <summary>
        /// 移動（Move）アニメーションを再生する。
        /// </summary>
        public void PlayMove(float normalizedSpeed)
        {
            if (currentState == CharacterAnimationState.Die) return;

            currentState = CharacterAnimationState.Move;
            currentSpeed = Mathf.Clamp01(normalizedSpeed);

            if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
            {
                unityAnimator.SetBool(IsMovingHash, true);
                unityAnimator.SetFloat(MoveSpeedHash, currentSpeed);
            }
        }

        /// <summary>
        /// 攻撃（Attack）アニメーションをトリガーする。
        /// </summary>
        public void TriggerAttack()
        {
            if (currentState == CharacterAnimationState.Die) return;

            if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
            {
                unityAnimator.SetTrigger(AttackTriggerHash);
            }
            else
            {
                // コード駆動の攻撃反動演出（パンチのように少し拡大）
                visualTransform.localScale = baseScale * 1.2f;
            }
        }

        /// <summary>
        /// 被弾（Hit）アニメーションをトリガーする。
        /// </summary>
        public void TriggerHit()
        {
            if (currentState == CharacterAnimationState.Die) return;

            if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
            {
                unityAnimator.SetTrigger(HitTriggerHash);
            }
            else
            {
                // コード駆動の被弾のけぞり演出
                visualTransform.localScale = new Vector3(baseScale.x * 1.15f, baseScale.y * 0.85f, baseScale.z);
            }
        }

        /// <summary>
        /// 死亡（Die）アニメーションを再生する。
        /// </summary>
        public void PlayDie()
        {
            currentState = CharacterAnimationState.Die;

            if (unityAnimator != null && unityAnimator.runtimeAnimatorController != null)
            {
                unityAnimator.SetTrigger(DieTriggerHash);
            }
            else
            {
                // コード駆動のぺしゃんこ演出
                visualTransform.localScale = new Vector3(baseScale.x * 1.3f, baseScale.y * 0.2f, baseScale.z);
            }
        }

        /// <summary>
        /// 指定されたアニメーション状態へ直接切り替える。
        /// </summary>
        public void SetState(CharacterAnimationState state)
        {
            switch (state)
            {
                case CharacterAnimationState.Idle: PlayIdle(); break;
                case CharacterAnimationState.Move: PlayMove(1f); break;
                case CharacterAnimationState.Attack: TriggerAttack(); break;
                case CharacterAnimationState.Hit: TriggerHit(); break;
                case CharacterAnimationState.Die: PlayDie(); break;
            }
        }

        #endregion

        private void Update()
        {
            // Animator がない場合のコード駆動アニメーション更新
            if (enableProceduralFallback && (unityAnimator == null || unityAnimator.runtimeAnimatorController == null))
            {
                UpdateProceduralAnimation();
            }
        }

        /// <summary>
        /// アセット未割り当て時でも自然に動くプロシージャルアニメーション（呼吸・歩行揺れ）。
        /// </summary>
        private void UpdateProceduralAnimation()
        {
            if (visualTransform == null || currentState == CharacterAnimationState.Die) return;

            if (currentState == CharacterAnimationState.Move && currentSpeed > 0.01f)
            {
                // 歩行時：リズミカルな縦横スクワッシュ（弾むような歩行）
                animTimer += Time.deltaTime * runBobSpeed * currentSpeed;
                var squishX = 1f + Mathf.Sin(animTimer) * runBobIntensity;
                var squishY = 1f - Mathf.Sin(animTimer) * runBobIntensity;
                visualTransform.localScale = new Vector3(baseScale.x * squishX, baseScale.y * squishY, baseScale.z);
            }
            else if (currentState == CharacterAnimationState.Idle)
            {
                // 待機時：ゆったりとした呼吸アニメーション
                animTimer += Time.deltaTime * idleBreathSpeed;
                var breathY = 1f + Mathf.Sin(animTimer) * idleBreathIntensity;
                var breathX = 1f - Mathf.Sin(animTimer) * (idleBreathIntensity * 0.5f);
                visualTransform.localScale = Vector3.Lerp(
                    visualTransform.localScale,
                    new Vector3(baseScale.x * breathX, baseScale.y * breathY, baseScale.z),
                    Time.deltaTime * 8f
                );
            }
        }
    }
}
