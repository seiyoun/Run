/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 指定されたタグと索敵範囲に基づいてターゲットを検索し、Blackboard 変数へ設定する Unity Behavior カスタムアクションノード。
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
    /// 指定されたタグと索敵範囲（半径）に基づいて最も近いターゲットを検索し、Blackboard 変数（Target）に代入するアクションノード。
    /// ターゲットが見つかった場合は Success、見つからなかった場合は Failure を返します。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Find Target 2D",
        description: "Finds the nearest target by tag within detection range and assigns it to Target.",
        story: "[Agent] finds [Target] with tag [TargetTag] in range [DetectionRange]m",
        category: "Action/Perception",
        id: "e7b1a2c3d4e5f6789012345678abcdef")]
    public partial class FindTarget2DAction : Action
    {
        private const float DefaultDetectionRange = 15.0f;
        private const string DefaultTargetTag = "Player";
        private const string LogTargetFoundFormat = "[FindTarget2DAction] {0} がターゲットを検出しました: {1} (距離: {2:F2}m)";

        [Tooltip("探索を行う自身（Agent の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [Tooltip("検出したターゲットの代入先（Blackboard 変数 Target）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Target;

        [Tooltip("検索対象のタグ")]
        [SerializeReference]
        public BlackboardVariable<string> TargetTag = new BlackboardVariable<string>(DefaultTargetTag);

        [Tooltip("索敵範囲（半径メートル、0以下の場合は無制限）")]
        [SerializeReference]
        public BlackboardVariable<float> DetectionRange = new BlackboardVariable<float>(DefaultDetectionRange);

        /// <summary>
        /// アクション開始時に索敵を実行し、最も近いターゲットを検出して Blackboard に代入する。
        /// </summary>
        /// <returns>ターゲット検出時は Success、未検出時は Failure</returns>
        protected override Status OnStart()
        {
            var agentGo = ResolveAgentGameObject();
            if (agentGo == null)
            {
                return Status.Failure;
            }

            var targetGo = SearchNearestTarget(agentGo);
            if (targetGo != null)
            {
                if (Target != null)
                {
                    Target.Value = targetGo;
                }

                return Status.Success;
            }

            return Status.Failure;
        }

        /// <summary>
        /// アクション終了時の処理。
        /// </summary>
        protected override void OnEnd()
        {
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
        /// 指定されたタグと範囲から最も近い対象 GameObject を検索する。
        /// </summary>
        /// <param name="agentGo">探索主体の GameObject</param>
        /// <returns>最も近いターゲット（未検出時は null）</returns>
        private GameObject SearchNearestTarget(GameObject agentGo)
        {
            var tag = (TargetTag != null && !string.IsNullOrEmpty(TargetTag.Value))
                ? TargetTag.Value
                : DefaultTargetTag;

            var maxRange = DetectionRange != null ? DetectionRange.Value : DefaultDetectionRange;
            var origin = (Vector2)agentGo.transform.position;

            GameObject nearestTarget = null;
            var nearestSqrDist = float.MaxValue;
            var maxSqrRange = maxRange > 0f ? maxRange * maxRange : float.MaxValue;

            var candidates = GameObject.FindGameObjectsWithTag(tag);
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || candidate == agentGo) continue;

                var sqrDist = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
                if (sqrDist <= maxSqrRange && sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearestTarget = candidate;
                }
            }

            // タグ付き候補が見つからず、PlayerController が存在する場合の安全なフォールバック
            if (nearestTarget == null && PlayerController.Instance != null && PlayerController.Instance.gameObject != agentGo)
            {
                var playerGo = PlayerController.Instance.gameObject;
                var sqrDist = ((Vector2)playerGo.transform.position - origin).sqrMagnitude;
                if (sqrDist <= maxSqrRange)
                {
                    nearestTarget = playerGo;
                    nearestSqrDist = sqrDist;
                }
            }

            if (nearestTarget != null)
            {
                var dist = Mathf.Sqrt(nearestSqrDist);
                DebugLogger.Log(string.Format(LogTargetFoundFormat, agentGo.name, nearestTarget.name, dist));
            }

            return nearestTarget;
        }
    }
}

