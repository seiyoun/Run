/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Unity Behavior（BehaviorGraphAgent）と連携してエネミーの行動を制御するコントローラー。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
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
    public sealed class EnemyController : MonoBehaviour, IKnockbackable, IDroppable
    {
        private const float DefaultMoveSpeed = 3.0f;
        private const string TargetVariableName = "Target";
        private const string SelfVariableName = "Self";
        private const string AttackPowerVariableName = "AttackPower";
        private const string AttackIntervalVariableName = "AttackInterval";
        private const string AttackRangeVariableName = "AttackRange";
        private const string IsKnockedBackVariableName = "IsKnockedBack";
        private const string KnockbackDirectionVariableName = "KnockbackDirection";
        private const string KnockbackForceVariableName = "KnockbackForce";

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
        private ICharacterAnimator animatorComponent;
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

        /// <summary>現在設定されているエネミー種別</summary>
        public EnemyType EnemyType => (EnemyType)(currentEnemyData != null ? currentEnemyData.enemyType : 0);

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
        /// 撃破時にアイテムドロップを要求するイベント（引数: ドロップワールド座標）。
        /// IDroppable インターフェースの実装。
        /// </summary>
        public event Action<Vector3> OnDropRequested;

        /// <summary>撃破時に通知されるイベント (引数: 自身のエネミーコントローラー)</summary>
        public event Action<EnemyController> OnDefeated;

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
            animatorComponent = GetComponent<ICharacterAnimator>() ?? GetComponentInChildren<ICharacterAnimator>();

            if (statusComponent != null)
            {
                statusComponent.OnTakeDamage += HandleTakeDamage;
            }
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
        /// オブジェクト破棄時の参照解放とイベント購読解除を行う。
        /// </summary>
        private void OnDestroy()
        {
            if (statusComponent != null)
            {
                statusComponent.OnTakeDamage -= HandleTakeDamage;
            }

            OnDefeated = null;
            OnDropRequested = null;
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
        /// EnemyMasterData の各設定値を対応するコンポーネントへ適用する。
        /// スプライトが指定されていない場合はシームレスに非同期ロードして反映します。
        /// </summary>
        /// <param name="data">適用するエネミーマスターデータ</param>
        /// <param name="sprite">適用するエネミースプライト（未指定時は自動ロード）</param>
        public void ApplyData(EnemyMasterData data, Sprite sprite = null)
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

            if (sprite != null)
            {
                visualComponent?.SetSprite(sprite);
            }
            else if (!string.IsNullOrEmpty(data.imageName))
            {
                visualComponent?.SetSprite(data.imageName);
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
            OnDropRequested?.Invoke(transform.position);
            OnDefeated?.Invoke(this);
        }

        /// <summary>
        /// オブジェクトプールからの再取得時にエネミーの状態をリセットし、再初期化する。
        /// </summary>
        /// <param name="position">リスポーンワールド座標</param>
        /// <param name="enemyType">エネミー種別</param>
        /// <param name="target">追尾対象 Transform</param>
        public void ResetForPool(Vector3 position, EnemyType enemyType, Transform target)
        {
            transform.position = position;
            isDeathHandled = false;

            if (colliderComponent != null)
            {
                colliderComponent.enabled = true;
            }

            var data = MasterDataManager.GetEnemyMasterData(enemyType);
            ApplyData(data);
            SetTarget(target);

            if (movementComponent != null)
            {
                movementComponent.enabled = true;
                movementComponent.Stop();
            }

            if (behaviorAgent != null)
            {
                behaviorAgent.SetVariableValue(IsKnockedBackVariableName, false);
                behaviorAgent.Restart();
            }
        }

        /// <summary>
        /// 指定された方向と力でノックバック外力を Blackboard 変数に同期し、ノックバック状態を開始する。
        /// </summary>
        /// <param name="direction">ノックバック方向ベクトル</param>
        /// <param name="force">ノックバックの強さ</param>
        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (IsDead || force <= 0f) return;

            if (behaviorAgent != null)
            {
                behaviorAgent.SetVariableValue(KnockbackDirectionVariableName, direction.normalized);
                behaviorAgent.SetVariableValue(KnockbackForceVariableName, force);
                behaviorAgent.SetVariableValue(IsKnockedBackVariableName, true);
            }
        }

        /// <summary>
        /// BehaviorGraphAgent の Blackboard 変数（Self, Target, 攻撃設定, ノックバック設定等）へ最新の参照を同期する。
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
            behaviorAgent.SetVariableValue(IsKnockedBackVariableName, false);
            behaviorAgent.SetVariableValue(KnockbackDirectionVariableName, Vector2.zero);
            behaviorAgent.SetVariableValue(KnockbackForceVariableName, 0f);
        }

        /// <summary>
        /// 被ダメージ時にアニメーションを再生する。
        /// </summary>
        /// <param name="damage">受けたダメージ量</param>
        private void HandleTakeDamage(int damage)
        {
            if (!IsDead)
            {
                animatorComponent?.TriggerHit();
            }
        }
    }
}
