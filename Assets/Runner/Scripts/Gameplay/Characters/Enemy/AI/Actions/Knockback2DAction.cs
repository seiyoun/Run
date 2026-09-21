/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Blackboard 変数からノックバック方向と力を受け取り、Rigidbody2D を直接操作して物理移動と減衰を行う Unity Behavior カスタムアクションノード。
 */

using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Status = Unity.Behavior.Node.Status;

namespace Runner
{
    /// <summary>
    /// Blackboard 変数（Direction, Force, IsKnockedBack）を用いてノックバック物理移動を実行するアクションノード。
    /// 指定持続時間をかけて速度をイージング減衰させ、完了時に IsKnockedBack を false にリセットします。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Knockback 2D",
        description: "Applies knockback physics using Blackboard variables and clears knockback state on completion.",
        story: "[Agent] is knocked back direction [Direction] force [Force] duration [Duration]s",
        category: "Action/Movement",
        id: "d3c2b1a09847128e7a65431234abcdknock")]
    public partial class Knockback2DAction : Action
    {
        private const float DefaultDuration = 0.2f;

        [Tooltip("ノックバック移動を行う自身（Agent の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [Tooltip("ノックバック方向ベクトル（Blackboard の KnockbackDirection 変数）")]
        [SerializeReference]
        public BlackboardVariable<Vector2> Direction;

        [Tooltip("ノックバック初速の大きさ（Blackboard の KnockbackForce 変数）")]
        [SerializeReference]
        public BlackboardVariable<float> Force;

        [Tooltip("ノックバック状態フラグ（Blackboard の IsKnockedBack 変数）")]
        [SerializeReference]
        public BlackboardVariable<bool> IsKnockedBack;

        [Tooltip("ノックバックの持続時間（秒）")]
        [SerializeReference]
        public BlackboardVariable<float> Duration = new BlackboardVariable<float>(DefaultDuration);

        private Rigidbody2D rb;
        private CharacterMovement2D movementComponent;
        private Vector2 initialVelocity;
        private float elapsedTime;

        /// <summary>
        /// アクション開始時にコンポーネントを取得し、自発移動の停止とノックバック初速の設定を行う。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnStart()
        {
            var agentGo = ResolveAgentGameObject();
            if (agentGo == null)
            {
                return Status.Failure;
            }

            rb = agentGo.GetComponent<Rigidbody2D>()
                 ?? agentGo.GetComponentInChildren<Rigidbody2D>()
                 ?? agentGo.GetComponentInParent<Rigidbody2D>();

            movementComponent = agentGo.GetComponent<CharacterMovement2D>()
                                ?? agentGo.GetComponentInChildren<CharacterMovement2D>()
                                ?? agentGo.GetComponentInParent<CharacterMovement2D>();

            if (rb == null)
            {
                ClearKnockbackFlag();
                return Status.Success;
            }

            if (movementComponent != null)
            {
                movementComponent.Stop();
                movementComponent.enabled = false;
            }

            elapsedTime = 0f;

            var dir = Direction != null ? Direction.Value : Vector2.zero;
            var forceVal = Force != null ? Force.Value : 0f;
            initialVelocity = dir * forceVal;
            rb.linearVelocity = initialVelocity;

            return Status.Running;
        }

        /// <summary>
        /// 毎フレームノックバック速度をイージング減衰させ、時間経過後に完了処理を行う。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnUpdate()
        {
            if (rb == null)
            {
                if (movementComponent != null)
                {
                    movementComponent.enabled = true;
                }
                ClearKnockbackFlag();
                return Status.Success;
            }

            elapsedTime += Time.deltaTime;
            var dur = Duration != null ? Duration.Value : DefaultDuration;
            var t = Mathf.Clamp01(elapsedTime / dur);

            rb.linearVelocity = Vector2.Lerp(initialVelocity, Vector2.zero, t);

            if (elapsedTime >= dur)
            {
                rb.linearVelocity = Vector2.zero;
                if (movementComponent != null)
                {
                    movementComponent.enabled = true;
                }
                ClearKnockbackFlag();
                return Status.Success;
            }

            return Status.Running;
        }

        /// <summary>
        /// アクション終了時に物理速度をリセットし、移動制御を復帰してノックバック状態を解除する。
        /// </summary>
        protected override void OnEnd()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (movementComponent != null)
            {
                movementComponent.enabled = true;
            }

            ClearKnockbackFlag();

            rb = null;
            movementComponent = null;
        }

        /// <summary>
        /// 対象の Agent GameObject を解決して取得する。
        /// </summary>
        /// <returns>Agent の GameObject</returns>
        private GameObject ResolveAgentGameObject()
        {
            if (Agent != null && Agent.Value != null)
            {
                return Agent.Value;
            }

            if (GameObject != null)
            {
                return GameObject;
            }

            return null;
        }

        /// <summary>
        /// Blackboard の IsKnockedBack フラグを false に更新する。
        /// </summary>
        private void ClearKnockbackFlag()
        {
            if (IsKnockedBack != null)
            {
                IsKnockedBack.Value = false;
            }
        }
    }
}
