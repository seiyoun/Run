/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 移動を阻害することなくターゲットへ接近してダメージを与え、クールダウンを待機する Unity Behavior カスタムアクションノード。
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
    /// 移動コンポーネントを停止させることなく、ターゲットへ接近してダメージを与え、クールダウンを待機するカスタムアクションノード。
    /// Run In Parallel ノードと組み合わせることで、移動・追尾を行いながらの近接接触攻撃を実現します。
    /// ターゲットが未バインドの場合は PlayerController.Instance から自動取得を試みます。
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Attack Target 2D",
        description: "Attacks target without stopping movement and waits for cooldown.",
        story: "[Agent] attacks [Target] with [Damage] damage cooldown [Cooldown]s in range [AttackRange]m",
        category: "Action/Combat",
        id: "b4c8d2e6a19047358e7a65431234abcd")]
    public partial class AttackTarget2DAction : Action
    {
        private const int DefaultDamage = 10;
        private const float DefaultCooldown = 1.0f;
        private const float DefaultAttackRange = 1.0f;
        private const string LogPlayerDamagedFormat = "[AttackTarget2DAction] ターゲットに近接ダメージを与えました: -{0} (残HP: {1}/{2}, 距離: {3:F2}m)";

        [Tooltip("攻撃を行う自身（Agent の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Agent;

        [Tooltip("攻撃対象のターゲット（プレイヤー等の GameObject）")]
        [SerializeReference]
        public BlackboardVariable<GameObject> Target;

        [Tooltip("1回の攻撃で与えるダメージ量")]
        [SerializeReference]
        public BlackboardVariable<int> Damage = new BlackboardVariable<int>(DefaultDamage);

        [Tooltip("次回攻撃までのクールダウン時間（秒）")]
        [SerializeReference]
        public BlackboardVariable<float> Cooldown = new BlackboardVariable<float>(DefaultCooldown);

        [Tooltip("攻撃判定を行う射程距離（m）")]
        [SerializeReference]
        public BlackboardVariable<float> AttackRange = new BlackboardVariable<float>(DefaultAttackRange);

        private float cooldownTimer;

        /// <summary>
        /// アクション開始時にタイマーを初期化する。初回から即時攻撃判定が行えるようタイマーをクールダウン値で初期化します。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnStart()
        {
            var interval = Cooldown != null ? Cooldown.Value : DefaultCooldown;
            cooldownTimer = interval;
            return Status.Running;
        }

        /// <summary>
        /// 毎フレームターゲットとの距離を監視し、射程内かつクールダウン完了時にダメージを与え続ける。
        /// </summary>
        /// <returns>ノードの実行ステータス</returns>
        protected override Status OnUpdate()
        {
            cooldownTimer += Time.deltaTime;

            var targetGo = ResolveTargetGameObject();
            if (targetGo == null)
            {
                return Status.Running;
            }

            var agentGo = ResolveAgentGameObject();
            if (agentGo == null)
            {
                return Status.Running;
            }

            var range = AttackRange != null ? AttackRange.Value : DefaultAttackRange;
            var distance = Vector2.Distance(agentGo.transform.position, targetGo.transform.position);

            if (distance <= range)
            {
                var interval = Cooldown != null ? Cooldown.Value : DefaultCooldown;
                if (cooldownTimer >= interval)
                {
                    cooldownTimer = 0f;
                    ExecuteAttack(targetGo, distance);
                }
            }

            return Status.Running;
        }

        /// <summary>
        /// アクション終了時の処理。
        /// </summary>
        protected override void OnEnd()
        {
        }

        /// <summary>
        /// 攻撃対象の GameObject を解決して取得する。未バインド時は PlayerController.Instance からフォールバック取得します。
        /// </summary>
        /// <returns>攻撃対象の GameObject</returns>
        private GameObject ResolveTargetGameObject()
        {
            if (Target != null && Target.Value != null)
            {
                return Target.Value;
            }

            if (PlayerController.Instance != null)
            {
                return PlayerController.Instance.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 攻撃主体の GameObject を解決して取得する。未バインド時は GameObject プロパティからフォールバック取得します。
        /// </summary>
        /// <returns>攻撃主体の GameObject</returns>
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
        /// ターゲットに対してダメージを付与する。
        /// </summary>
        /// <param name="targetGo">攻撃対象の GameObject</param>
        /// <param name="distance">現在のターゲットとの距離</param>
        private void ExecuteAttack(GameObject targetGo, float distance)
        {
            var damageable = targetGo.GetComponent<IDamageable>()
                             ?? targetGo.GetComponentInChildren<IDamageable>()
                             ?? targetGo.GetComponentInParent<IDamageable>();

            if (damageable == null && PlayerController.Instance != null && PlayerController.Instance.gameObject == targetGo)
            {
                damageable = PlayerController.Instance;
            }

            if (damageable != null)
            {
                var dmg = Damage != null ? Damage.Value : DefaultDamage;
                damageable.TakeDamage(dmg);

                var status = targetGo.GetComponent<ICharacterStatus>()
                             ?? targetGo.GetComponentInChildren<ICharacterStatus>()
                             ?? targetGo.GetComponentInParent<ICharacterStatus>();

                var currentHp = status != null ? status.CurrentHp : 0;
                var maxHp = status != null ? status.MaxHp : 0;

                DebugLogger.Log(string.Format(LogPlayerDamagedFormat, dmg, currentHp, maxHp, distance));
            }
        }
    }
}
