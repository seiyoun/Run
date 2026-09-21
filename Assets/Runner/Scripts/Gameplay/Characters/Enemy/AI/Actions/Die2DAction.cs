/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 移動停止、当たり判定無効化、死亡アニメーション再生および消滅処理を行う Unity Behavior カスタムアクションノード。
 */

using System;
using Shiyuan.Foundation.Core;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Status = Unity.Behavior.Node.Status;

namespace Runner
{
    /// <summary>
    /// 死亡アニメーションを再生し、一定遅延後にオブジェクトを破棄するアクションノード。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Die 2D",
        description: "死亡アニメーションを再生し、一定時間後に自身を破棄します。",
        story: "[Agent] が遅延 [DestroyDelay] 秒で死亡・破棄",
        category: "Action/Combat",
        id: "f3a7c8e9b0124d568e7a65431234dead")]
    public partial class Die2DAction : Action
    {
        private const float DefaultDestroyDelay = 0.5f;
        private const string LogAgentDeadFormat = "[Die2DAction] {0} の死亡アクションを実行しました。(遅延: {1:F2}秒)";

        private CharacterMovement2D movementComponent;
        private Collider2D colliderComponent;
        private ICharacterAnimator characterAnimator;
        private float elapsedTime;
        private bool isDeathInitiated;

        [Tooltip("死亡処理を行う自身（Agent の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [Tooltip("死亡アニメーション再生からオブジェクト破棄までの遅延時間（秒）")]
        [SerializeReference]
        public BlackboardVariable<float> DestroyDelay = new BlackboardVariable<float>(DefaultDestroyDelay);

        [Tooltip("遅延完了時に GameObject を破棄（Destroy）するか")]
        [SerializeReference]
        public BlackboardVariable<bool> DestroyOnComplete = new BlackboardVariable<bool>(true);

        /// <summary>
        /// 死亡アクションを開始し、移動停止・コライダー無効化・アニメーション再生をトリガーする。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnStart()
        {
            var agentGo = ResolveAgentGameObject();
            if (agentGo == null)
            {
                return Status.Failure;
            }

            elapsedTime = 0f;
            isDeathInitiated = false;

            InitiateDeath(agentGo);

            return Status.Running;
        }

        /// <summary>
        /// 毎フレーム経過時間を計測し、遅延完了後にオブジェクトを破棄して Success を返す。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnUpdate()
        {
            var agentGo = ResolveAgentGameObject();
            if (agentGo == null)
            {
                return Status.Success;
            }

            elapsedTime += Time.deltaTime;
            var delay = DestroyDelay != null ? DestroyDelay.Value : DefaultDestroyDelay;

            if (elapsedTime >= delay)
            {
                var shouldDestroy = DestroyOnComplete == null || DestroyOnComplete.Value;
                if (shouldDestroy)
                {
                    var enemy = agentGo.GetComponent<EnemyController>();
                    if (EnemySpawner.HasInstance && enemy != null)
                    {
                        EnemySpawner.Instance.ReturnEnemy(enemy);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(agentGo);
                    }
                }

                return Status.Success;
            }

            return Status.Running;
        }

        /// <summary>
        /// アクション終了時に移動を停止する。
        /// </summary>
        protected override void OnEnd()
        {
            movementComponent?.Stop();
        }

        /// <summary>
        /// 自身（Agent）の GameObject を解決して取得する。
        /// </summary>
        /// <returns>自身を表す GameObject</returns>
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
        /// 死亡の初回処理（移動停止、当たり判定無効化、死亡アニメーション再生）を一括適用する。
        /// </summary>
        /// <param name="agentGo">対象の GameObject</param>
        private void InitiateDeath(GameObject agentGo)
        {
            if (isDeathInitiated) return;
            isDeathInitiated = true;

            movementComponent = agentGo.GetComponent<CharacterMovement2D>();
            movementComponent?.Stop();

            colliderComponent = agentGo.GetComponent<Collider2D>();
            if (colliderComponent != null)
            {
                colliderComponent.enabled = false;
            }

            characterAnimator = agentGo.GetComponent<ICharacterAnimator>()
                                ?? agentGo.GetComponentInChildren<ICharacterAnimator>();
            characterAnimator?.PlayDie();

            var enemyController = agentGo.GetComponent<EnemyController>();
            enemyController?.NotifyDeathActionExecuted();

            var delay = DestroyDelay != null ? DestroyDelay.Value : DefaultDestroyDelay;
            DebugLogger.Log(string.Format(LogAgentDeadFormat, agentGo.name, delay));
        }
    }
}
