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
        private SpriteRenderer spriteRenderer;
        private ICharacterVisual visualComponent;
        private EnemyMasterData currentEnemyData;
        private bool isDeathHandled;

        /// <summary>死亡状態であるか</summary>
        public bool IsDead => isDeathHandled || (statusComponent != null && statusComponent.IsDead);

        /// <summary>Blackboard に設定されている追尾対象の Transform</summary>
        public Transform Target
        {
            get
            {
                if (behaviorAgent != null &&
                    !string.IsNullOrEmpty(targetBlackboardKey) &&
                    behaviorAgent.BlackboardReference.GetVariableValue(targetBlackboardKey, out GameObject targetGo) &&
                    targetGo != null)
                {
                    return targetGo.transform;
                }

                return null;
            }
        }

        /// <summary>移動制御コンポーネント</summary>
        public CharacterMovement2D Movement => movementComponent;

        /// <summary>キャラクターステータスコンポーネント</summary>
        public CharacterStatus Status => statusComponent;

        /// <summary>当たり判定コンポーネント</summary>
        public CircleCollider2D Collider => colliderComponent;

        /// <summary>現在適用中のエネミー設定マスターデータ</summary>
        public EnemyMasterData CurrentEnemyData => currentEnemyData;

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
            spriteRenderer = GetComponent<SpriteRenderer>();
            visualComponent = GetComponent<ICharacterVisual>();

            LoadEnemyData();
        }

        /// <summary>
        /// 初回フレームで Blackboard 変数を初期バインドする。
        /// </summary>
        private void Start()
        {
            SyncBlackboardVariables();
        }

        /// <summary>
        /// 毎フレームの更新処理を行い、移動方向に応じたスプライトの左右反転を制御する。
        /// </summary>
        private void Update()
        {
            if (IsDead) return;

            if (movementComponent != null)
            {
                if (visualComponent != null)
                {
                    visualComponent.SetFacingDirection(movementComponent.FacingDirection);
                }
                else if (spriteRenderer != null)
                {
                    if (movementComponent.FacingDirection.x < 0f)
                    {
                        spriteRenderer.flipX = true;
                    }
                    else if (movementComponent.FacingDirection.x > 0f)
                    {
                        spriteRenderer.flipX = false;
                    }
                }
            }
        }

        /// <summary>
        /// オブジェクト破棄時の参照解放を行う。
        /// </summary>
        private void OnDestroy()
        {
            behaviorAgent = null;
        }

        /// <summary>
        /// エネミーを初期化し、移動速度を設定する。
        /// </summary>
        /// <param name="speed">移動速度（0以下の場合は既存値を維持）</param>
        public void Initialize(float speed = 0f)
        {
            if (speed > 0f)
            {
                MoveSpeed = speed;
            }
        }

        /// <summary>
        /// エネミーを初期化し、追尾対象と移動速度を設定する（後方互換用）。
        /// </summary>
        /// <param name="target">追尾ターゲット（プレイヤー等）</param>
        /// <param name="speed">移動速度（0以下の場合は既存値を維持）</param>
        public void Initialize(Transform target, float speed = 0f)
        {
            SetTarget(target);
            Initialize(speed);
        }

        /// <summary>
        /// 追尾ターゲットを設定し、BehaviorGraphAgent の Blackboard に反映する。
        /// </summary>
        /// <param name="target">設定するターゲット Transform</param>
        public void SetTarget(Transform target)
        {
            if (behaviorAgent != null && !string.IsNullOrEmpty(targetBlackboardKey))
            {
                behaviorAgent.SetVariableValue(targetBlackboardKey, target != null ? target.gameObject : null);
            }
        }

        /// <summary>
        /// MasterDataManager のキャッシュから初期エネミーマスターデータを取得して適用する。
        /// </summary>
        public void LoadEnemyData()
        {
            var data = MasterDataManager.GetEnemyMasterData(EnemyType.Salaryman);
            ApplyData(data);
        }

        /// <summary>
        /// EnemyMasterData の各設定値を対応するコンポーネントへ適用する。
        /// スプライトのロードと設定は CharacterVisual2D を通じてシームレスに行われます。
        /// </summary>
        /// <param name="data">適用するエネミーマスターデータ</param>
        public void ApplyData(EnemyMasterData data)
        {
            if (data == null) return;

            currentEnemyData = data;

            if (statusComponent == null) statusComponent = GetComponent<CharacterStatus>();
            if (movementComponent == null) movementComponent = GetComponent<CharacterMovement2D>();
            if (colliderComponent == null) colliderComponent = GetComponent<CircleCollider2D>();
            if (visualComponent == null) visualComponent = GetComponent<ICharacterVisual>();

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

            // スプライトの画像ロードは CharacterVisual2D に一任（シームレスにオンデマンドロード）
            if (visualComponent != null)
            {
                visualComponent.LoadSprite(data.imageName);
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = Resources.Load<Sprite>($"Sprites/Characters/{data.imageName}");
            }

            AttackPower = data.attackPower;
            AttackInterval = data.attackInterval;
            AttackRange = data.attackRange;
        }

        /// <summary>
        /// 死亡アクション（Die2DAction 等）の実行を通知し、死亡フラグの更新と移動停止を一度だけ行う。
        /// </summary>
        public void NotifyDeathActionExecuted()
        {
            if (isDeathHandled) return;
            isDeathHandled = true;
            movementComponent?.Stop();
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

            behaviorAgent.SetVariableValue(AttackPowerVariableName, AttackPower);
            behaviorAgent.SetVariableValue(AttackIntervalVariableName, AttackInterval);
            behaviorAgent.SetVariableValue(AttackRangeVariableName, AttackRange);
        }
    }
}
