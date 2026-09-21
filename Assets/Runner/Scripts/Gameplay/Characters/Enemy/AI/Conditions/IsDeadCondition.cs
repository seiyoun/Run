/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エージェントのHPがゼロ以下（または死亡状態）であるかを評価する Unity Behavior カスタム条件ノード。
 */

using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 指定された Agent（GameObject）の体力がゼロ以下、または死亡状態であるかを判定する条件ノード。
    /// Behavior Graph 上で死亡アクションへの分岐やガード条件として使用します。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Dead",
        category: "Conditions",
        story: "[Agent] の HP が 0 以下",
        id: "e4d3c2b1a09847128e7a65431234dead")]
    public partial class IsDeadCondition : Condition
    {
        [Tooltip("判定対象の自身（Agent の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        /// <summary>
        /// Agent の HP がゼロ以下、または既に死亡状態であるかを評価する。
        /// </summary>
        /// <returns>HPが0以下または死亡状態なら true、生存中または未設定なら false</returns>
        public override bool IsTrue()
        {
            var agentGo = ResolveAgentGameObject();
            if (agentGo == null)
            {
                return false;
            }

            var status = agentGo.GetComponent<ICharacterStatus>()
                         ?? agentGo.GetComponentInChildren<ICharacterStatus>()
                         ?? agentGo.GetComponentInParent<ICharacterStatus>();

            if (status == null)
            {
                return false;
            }

            return status.CurrentHp <= 0 || status.IsDead;
        }

        /// <summary>
        /// 判定対象の Agent GameObject を解決して取得する。
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
    }
}
