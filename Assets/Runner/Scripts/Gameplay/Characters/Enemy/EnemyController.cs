/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Unity Behavior（BehaviorGraphAgent）と連携してエネミーの行動を制御するコントローラー。
 */

using System;
using Shiyuan.Foundation.Core;
using Unity.Behavior;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Unity Behavior（BehaviorGraphAgent）を用いて行動を制御するエネミーコントローラー。
    /// CharacterMovement2D による移動、CharacterStatus による体力管理、および Blackboard 変数（Target 等）の連携を統括します。
    /// </summary>
    [RequireComponent(typeof(CharacterMovement2D))]
    [RequireComponent(typeof(CharacterStatus))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(BehaviorGraphAgent))]
    [DisallowMultipleComponent]
    public sealed class EnemyController : MonoBehaviour
    {
        private const float DefaultMoveSpeed = 3.0f;
        private const string TargetVariableName = "Target";
        private const string SelfVariableName = "Self";
        private const string AttackPowerVariableName = "AttackPower";
        private const string AttackIntervalVariableName = "AttackInterval";
        private const string AttackRangeVariableName = "AttackRange";

        [Header("AI Settings")]
        [Tooltip("Blackboard に登録するターゲット変数名")]
        [SerializeField]
        private string targetBlackboardKey = TargetVariableName;

        [Tooltip("Blackboard に登録する自身のエージェント変数名（Unity Behavior デフォルトは Self）")]
        [SerializeField]
        private string agentBlackboardKey = SelfVariableName;

        private CharacterMovement2D movementComponent;
        private CharacterStatus statusComponent;
        private CircleCollider2D colliderComponent;
        private BehaviorGraphAgent behaviorAgent;
        private Transform targetTransform;
        private EnemyData currentEnemyData;

        /// <summary>追尾対象の Transform</summary>
        public Transform Target => targetTransform;

        /// <summary>移動制御コンポーネント</summary>
        public CharacterMovement2D Movement => movementComponent;

        /// <summary>キャラクターステータスコンポーネント</summary>
        public CharacterStatus Status => statusComponent;

        /// <summary>当たり判定コンポーネント</summary>
        public CircleCollider2D Collider => colliderComponent;

        /// <summary>現在適用中のエネミー設定データ</summary>
        public EnemyData CurrentEnemyData => currentEnemyData;

        /// <summary>BehaviorGraphAgent コンポーネント</summary>
        public BehaviorGraphAgent BehaviorAgent => behaviorAgent;

        /// <summary>移動速度</summary>
        public float MoveSpeed
        {
            get => movementComponent != null ? movementComponent.MoveSpeed : DefaultMoveSpeed;
            set
            {
                if (movementComponent != null)
                {
                    movementComponent.MoveSpeed = value;
                }
            }
        }

        /// <summary>攻撃力</summary>
        public int AttackPower { get; private set; } = 10;

        /// <summary>攻撃間隔（秒）</summary>
        public float AttackInterval { get; private set; } = 1.0f;

        /// <summary>攻撃射程（m）</summary>
        public float AttackRange { get; private set; } = 1.0f;

        /// <summary>
        /// 必要なコンポーネントの参照取得と初期データのロードを行う。
        /// </summary>
        private void Awake()
        {
            movementComponent = GetComponent<CharacterMovement2D>();
            statusComponent = GetComponent<CharacterStatus>();
            colliderComponent = GetComponent<CircleCollider2D>();
            behaviorAgent = GetComponent<BehaviorGraphAgent>();

            if (statusComponent != null)
            {
                statusComponent.OnDead += HandleDead;
            }

            LoadEnemyData();
        }

        /// <summary>
        /// 初回フレームでターゲットが未指定の場合に自動検索し、Blackboard 変数を初期バインドする。
        /// </summary>
        private void Start()
        {
            if (targetTransform == null)
            {
                AutoFindPlayerTarget();
            }

            SyncBlackboardVariables();
        }

        /// <summary>
        /// 毎フレームの更新処理（ターゲット再検索等）を行う。
        /// </summary>
        private void Update()
        {
            if (statusComponent != null && statusComponent.IsDead)
            {
                movementComponent?.Stop();
                return;
            }

            if (targetTransform == null)
            {
                AutoFindPlayerTarget();
                if (targetTransform != null)
                {
                    SyncBlackboardVariables();
                }
            }
        }

        /// <summary>
        /// オブジェクト破棄時の参照解放とイベント購読解除を行う。
        /// </summary>
        private void OnDestroy()
        {
            if (statusComponent != null)
            {
                statusComponent.OnDead -= HandleDead;
            }

            targetTransform = null;
            behaviorAgent = null;
        }

        /// <summary>
        /// エネミーを初期化し、追尾対象と移動速度を設定する。
        /// </summary>
        /// <param name="target">追尾ターゲット（プレイヤー等）</param>
        /// <param name="speed">移動速度（0以下の場合は既存値を維持）</param>
        public void Initialize(Transform target, float speed = 0f)
        {
            SetTarget(target);

            if (speed > 0f)
            {
                MoveSpeed = speed;
            }
        }

        /// <summary>
        /// 追尾ターゲットを設定し、BehaviorGraphAgent の Blackboard に反映する。
        /// </summary>
        /// <param name="target">設定するターゲット Transform</param>
        public void SetTarget(Transform target)
        {
            targetTransform = target;
            SyncBlackboardVariables();
        }

        /// <summary>
        /// Resources から EnemyData をロードして各コンポーネントへ適用する。
        /// </summary>
        public void LoadEnemyData()
        {
            var jsonAsset = Resources.Load<TextAsset>("Data/EnemyData");
            var data = (jsonAsset != null && !string.IsNullOrWhiteSpace(jsonAsset.text))
                ? EnemyData.FromJson(jsonAsset.text)
                : new EnemyData();

            ApplyData(data);
        }

        /// <summary>
        /// EnemyData の各設定値を対応するコンポーネントへ適用する。
        /// </summary>
        /// <param name="data">適用するエネミーデータ</param>
        public void ApplyData(EnemyData data)
        {
            if (data == null) return;

            currentEnemyData = data;

            if (statusComponent == null) statusComponent = GetComponent<CharacterStatus>();
            if (movementComponent == null) movementComponent = GetComponent<CharacterMovement2D>();
            if (colliderComponent == null) colliderComponent = GetComponent<CircleCollider2D>();

            if (statusComponent != null)
            {
                statusComponent.SetMaxHp(data.maxHp, true);
            }

            if (movementComponent != null)
            {
                movementComponent.MoveSpeed = data.moveSpeed;
            }

            if (colliderComponent != null)
            {
                colliderComponent.radius = data.colliderRadius;
            }

            AttackPower = data.attackPower;
            AttackInterval = data.attackInterval;
            AttackRange = data.attackRange;
        }

        /// <summary>
        /// シーン上のプレイヤーを検索してターゲットに設定する（静的インスタンス参照による O(1) 軽量アクセス）。
        /// </summary>
        private void AutoFindPlayerTarget()
        {
            if (PlayerController.Instance != null)
            {
                targetTransform = PlayerController.Instance.transform;
            }
        }

        /// <summary>
        /// BehaviorGraphAgent の Blackboard 変数（Self, Target, 攻撃設定等）へ最新の参照を同期する。
        /// </summary>
        private void SyncBlackboardVariables()
        {
            if (behaviorAgent == null) return;

            if (!string.IsNullOrEmpty(agentBlackboardKey))
            {
                behaviorAgent.SetVariableValue(agentBlackboardKey, gameObject);
            }

            if (agentBlackboardKey != SelfVariableName)
            {
                behaviorAgent.SetVariableValue(SelfVariableName, gameObject);
            }

            if (!string.IsNullOrEmpty(targetBlackboardKey) && targetTransform != null)
            {
                behaviorAgent.SetVariableValue(targetBlackboardKey, targetTransform.gameObject);
            }

            behaviorAgent.SetVariableValue(AttackPowerVariableName, AttackPower);
            behaviorAgent.SetVariableValue(AttackIntervalVariableName, AttackInterval);
            behaviorAgent.SetVariableValue(AttackRangeVariableName, AttackRange);
        }

        /// <summary>
        /// 死亡時に移動停止および BehaviorGraphAgent の無効化を行う。
        /// </summary>
        private void HandleDead()
        {
            movementComponent?.Stop();
            if (behaviorAgent != null)
            {
                behaviorAgent.enabled = false;
            }
        }
    }
}
