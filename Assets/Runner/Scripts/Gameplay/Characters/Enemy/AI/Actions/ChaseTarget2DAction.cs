/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Unity Behavior Graph 内で使用する 2D ターゲット追尾カスタムアクションノード。
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
    /// ターゲット（プレイヤー等）に向かって追尾移動を行うアクションノード。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Chase Target 2D",
        description: "ターゲットへ接近・追尾します。",
        story: "[Agent] が停止距離 [StoppingDistance] m で [Target] を追尾",
        category: "Action/Movement",
        id: "a2b4f9c1e78044279b9a67e91234abcd")]
    public partial class ChaseTarget2DAction : Action
    {
        [Tooltip("移動を行う自身（CharacterMovement2D を持つ GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [Tooltip("追尾対象（プレイヤー等の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Target;

        [Tooltip("ターゲットに接近したと判定して完了（停止）する距離（メートル）")]
        [SerializeReference]
        public BlackboardVariable<float> StoppingDistance = new BlackboardVariable<float>(0.5f);

        private CharacterMovement2D movementComponent;

        /// <summary>
        /// アクション開始時にコンポーネントの検証と取得を行う。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null)
            {
                return Status.Failure;
            }

            movementComponent = Agent.Value.GetComponent<CharacterMovement2D>();
            if (movementComponent == null)
            {
                LogFailure("Agent に CharacterMovement2D がアタッチされていません。");
                return Status.Failure;
            }

            if (Target == null || Target.Value == null)
            {
                movementComponent.Stop();
                return Status.Failure;
            }

            return Status.Running;
        }

        /// <summary>
        /// 毎フレームターゲットへの方向を計算し、追尾移動を実行する。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnUpdate()
        {
            if (Agent == null || Agent.Value == null || Target == null || Target.Value == null || movementComponent == null)
            {
                movementComponent?.Stop();
                return Status.Failure;
            }

            var agentPos = (Vector2)Agent.Value.transform.position;
            var targetPos = (Vector2)Target.Value.transform.position;
            var toTarget = targetPos - agentPos;
            var distance = toTarget.magnitude;

            var stopDist = StoppingDistance != null ? StoppingDistance.Value : 0.5f;
            if (distance <= stopDist)
            {
                movementComponent.Stop();
                return Status.Success;
            }

            var direction = toTarget / distance;
            movementComponent.Move(direction);

            return Status.Running;
        }

        /// <summary>
        /// アクション終了時に移動を停止する。
        /// </summary>
        protected override void OnEnd()
        {
            movementComponent?.Stop();
        }
    }
}
