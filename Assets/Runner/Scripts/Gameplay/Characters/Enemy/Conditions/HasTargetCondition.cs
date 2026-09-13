/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ターゲットが設定されているかを評価する Unity Behavior カスタム条件ノード。
 */

using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 指定されたターゲット（GameObject）が有効に設定されているかを判定する条件ノード。
    /// Behavior Graph 上で Conditional Guard や Branching Condition として使用します。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Has Target",
        category: "Conditions",
        story: "[Target] is set",
        id: "d8e2a3c5f6704b12a9e874561234abcd")]
    public partial class HasTargetCondition : Condition
    {
        [Tooltip("判定対象のターゲット（GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Target;

        /// <summary>
        /// ターゲットが設定されているかを評価する。
        /// </summary>
        /// <returns>ターゲットが存在すれば true、null なら false</returns>
        public override bool IsTrue()
        {
            return Target != null && Target.Value != null;
        }
    }
}

