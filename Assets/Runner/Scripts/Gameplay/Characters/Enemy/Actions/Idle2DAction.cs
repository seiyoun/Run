/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 移動を停止して待機状態を維持する Unity Behavior カスタムアクションノード。
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
    /// CharacterMovement2D による移動を停止させ、待機状態を維持するアクションノード。
    /// Behavior Graph 上でターゲット未検出時のフォールバック待機行動等に使用します。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Idle 2D",
        description: "Stops movement and idles using CharacterMovement2D.",
        story: "[Agent] idles",
        category: "Action/Movement",
        id: "c9f3b2e1a8044d728b9c6543210fedcb")]
    public partial class Idle2DAction : Action
    {
        private const float DefaultIdleDuration = 0.2f;

        [Tooltip("待機を行う自身（CharacterMovement2D を持つ GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        private CharacterMovement2D movementComponent;
        private float elapsedTime;

        /// <summary>
        /// アクション開始時に CharacterMovement2D を取得し、移動を停止してタイマーを初期化する。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null)
            {
                return Status.Failure;
            }

            movementComponent = Agent.Value.GetComponent<CharacterMovement2D>();
            movementComponent?.Stop();
            elapsedTime = 0f;

            return Status.Running;
        }

        /// <summary>
        /// 待機状態を維持し、一定時間経過後に完了（Success）して親ループの再評価を促す。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnUpdate()
        {
            movementComponent?.Stop();
            elapsedTime += Time.deltaTime;

            if (elapsedTime >= DefaultIdleDuration)
            {
                return Status.Success;
            }

            return Status.Running;
        }

        /// <summary>
        /// アクション終了時に確実に移動を停止する。
        /// </summary>
        protected override void OnEnd()
        {
            movementComponent?.Stop();
        }
    }
}

