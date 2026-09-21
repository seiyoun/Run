/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エージェントがノックバック中であるかを評価する Unity Behavior カスタム条件ノード。
 */

using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Blackboard に登録されている IsKnockedBack フラグが true であるかを判定する条件ノード。
    /// Behavior Graph 上でノックバック待機アクションへの割り込み分岐条件として使用します。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "Is Knocked Back",
        category: "Conditions",
        story: "[IsKnockedBack] が true",
        id: "f5e4d3c2b1a047128e7a65431234knock")]
    public partial class IsKnockedBackCondition : Condition
    {
        [Tooltip("ノックバック中フラグ（Blackboard の IsKnockedBack 変数）")]
        [SerializeReference]
        public BlackboardVariable<bool> IsKnockedBack;

        /// <summary>
        /// IsKnockedBack 変数が true であるかを評価する。
        /// </summary>
        /// <returns>ノックバック中なら true、それ以外なら false</returns>
        public override bool IsTrue()
        {
            return IsKnockedBack != null && IsKnockedBack.Value;
        }
    }
}
