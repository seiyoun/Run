/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IMovable, IAttacker, IDamageable, IHealable, ICharacterVisual, ICharacterAnimator, ICharacterStatus を協調させてプレイヤーを制御するクラス。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IMovable, IAttacker, IDamageable, IHealable, IMoneyCollector
    {
        public static PlayerController Instance { get; private set; }

        #region Serialized Fields

        [Header("Data Configuration")]
        [Tooltip("プレイヤー専用パラメータ JSON アセット (未設定時は Inspector のデフォルト値を使用)")]
        [SerializeField]
        private TextAsset playerDataAsset;

        [Header("Movement Settings")]
        [Tooltip("移動速度 (PlayerData が設定されている場合はそちらが優先されます)")]
        [SerializeField]
        private float moveSpeed = 6f;

        [Header("Combat Settings")]
        [Tooltip("攻撃力 (PlayerData が設定されている場合はそちらが優先されます)")]
        [SerializeField]
        private int attackPower = 10;

        [Tooltip("攻撃間隔/クールダウン時間(秒) (PlayerData が設定されている場合はそちらが優先されます)")]
        [SerializeField]
        private float attackInterval = 1.0f;

        [Header("Item Magnet Settings")]
        [Tooltip("周囲のアイテムを吸い込む範囲の半径(m)")]
        [SerializeField]
        private float magnetRadius = 3.5f;

        [Tooltip("周囲のアイテムを検索する間隔(秒)")]
        [SerializeField]
        private float magnetCheckInterval = 0.05f;

        #endregion

        #region Private Fields

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 facingDirection = Vector2.right;
        private float attackCooldownTimer;
        private float magnetCheckTimer;

        /// <summary>GC Alloc を発生させずに周囲のアイテムを取得するためのキャッシュバッファ</summary>
        private readonly Collider2D[] itemColliderBuffer = new Collider2D[32];

        /// <summary>現在バインドされている入力コントローラー</summary>
        private InputController boundInputController;

        /// <summary>キャラクターの見た目（向き・スプライト制御）インターフェース</summary>
        private ICharacterVisual characterVisual;

        /// <summary>キャラクターのアニメーション制御インターフェース</summary>
        private ICharacterAnimator characterAnimator;

        /// <summary>キャラクターのステータス（HP・被ダメージ・回復）管理インターフェース</summary>
        private ICharacterStatus characterStatus;

        /// <summary>適用中のプレイヤーパラメータデータ</summary>
        private PlayerData currentPlayerData;

        #endregion

        #region Properties & Events

        public PlayerData CurrentData => currentPlayerData;
        public ICharacterVisual CharacterVisual => characterVisual;
        public ICharacterAnimator CharacterAnimator => characterAnimator;
        public ICharacterStatus Status => characterStatus;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            InitializeComponents();
            LoadPlayerData();
        }

        private void Start()
        {
            if (boundInputController == null && InputController.Instance != null)
            {
                BindInput(InputController.Instance);
            }
        }

        private void Update()
        {
            // 攻撃クールダウンタイマーの更新
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }

            if (characterStatus != null && characterStatus.IsDead)
            {
                moveInput = Vector2.zero;
                return;
            }

            UpdateVisuals();
            UpdateAnimation();
            UpdateItemAttraction();
        }

        private void FixedUpdate()
        {
            if (characterStatus != null && characterStatus.IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // MovePosition による確実な物理移動
            var delta = moveInput * (moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + delta);
            rb.linearVelocity = moveInput * moveSpeed;

            // 移動によるポイ活・歩数・怒りゲージの蓄積
            if (delta.sqrMagnitude > 0f && GameHUDView.Instance != null)
            {
                GameHUDView.Instance.OnPlayerMoved(delta.magnitude);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindInput();

            if (characterStatus != null)
            {
                UnsubscribeStatusEvents(characterStatus);
            }
        }

        #endregion

        #region IMovable Implementation

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }

        public Vector2 MoveInput => moveInput;
        public Vector2 FacingDirection => facingDirection;

        public void Move(Vector2 direction)
        {
            if (characterStatus != null && characterStatus.IsDead)
            {
                moveInput = Vector2.zero;
                return;
            }

            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            moveInput = direction;

            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                facingDirection = new Vector2(Mathf.Sign(moveInput.x), 0f);
            }
        }

        public void Stop()
        {
            moveInput = Vector2.zero;
        }

        #endregion

        #region IAttacker Implementation

        public int AttackPower
        {
            get => attackPower;
            set => attackPower = Mathf.Max(0, value);
        }

        public float AttackInterval
        {
            get => attackInterval;
            set => attackInterval = Mathf.Max(0.01f, value);
        }

        public bool CanAttack => (characterStatus == null || !characterStatus.IsDead) && attackCooldownTimer <= 0f;

        public event Action OnAttack;

        /// <summary>
        /// プレイヤーの攻撃を実行する（正面または向いている方向への攻撃）。
        /// </summary>
        public void Attack()
        {
            Attack(facingDirection);
        }

        /// <summary>
        /// 指定された方向に向けて攻撃を実行する。
        /// </summary>
        /// <param name="direction">攻撃方向ベクトル</param>
        public void Attack(Vector2 direction)
        {
            if (!CanAttack) return;

            if (direction.sqrMagnitude > 0.01f)
            {
                facingDirection = new Vector2(Mathf.Sign(direction.x), 0f);
                characterVisual?.SetFacingDirection(facingDirection);
            }

            attackCooldownTimer = attackInterval;
            characterAnimator?.TriggerAttack();
            OnAttack?.Invoke();
        }

        #endregion

        #region IDamageable Implementation

        public bool IsDead => characterStatus?.IsDead ?? false;

        public event Action<int> OnTakeDamage;
        public event Action OnDead;

        /// <summary>
        /// プレイヤーにダメージを与える。
        /// </summary>
        /// <param name="amount">ダメージ量</param>
        public void TakeDamage(int amount)
        {
            characterStatus?.TakeDamage(amount);
        }

        #endregion

        #region IHealable Implementation

        public event Action<int> OnHeal;

        /// <summary>
        /// プレイヤーのHPを回復する。
        /// </summary>
        /// <param name="amount">回復量</param>
        public void Heal(int amount)
        {
            characterStatus?.Heal(amount);
        }

        #endregion

        #region IMoneyCollector Implementation

        public long CurrentMoney => GameHUDView.Instance != null && GameHUDView.Instance.PointStepHUD != null 
            ? GameHUDView.Instance.PointStepHUD.CurrentPoint 
            : 0;

        public event Action<long> OnMoneyCollected;

        /// <summary>
        /// お金・ポイントを回収して加算する。
        /// </summary>
        /// <param name="amount">獲得金額/ポイント</param>
        public void CollectMoney(long amount)
        {
            if (amount <= 0) return;

            if (GameHUDView.Instance != null && GameHUDView.Instance.PointStepHUD != null)
            {
                GameHUDView.Instance.PointStepHUD.AddPoints(amount);
            }

            OnMoneyCollected?.Invoke(amount);
        }

        #endregion

        #region Input Handling

        public void BindInput(InputController inputController)
        {
            if (boundInputController != null)
            {
                UnbindInput();
            }

            boundInputController = inputController;
            if (boundInputController != null)
            {
                boundInputController.OnMoveInput += HandleMoveInput;
                DebugLogger.Log($"[PlayerController] InputController ({inputController.name}) にバインド完了");
            }
        }

        public void UnbindInput()
        {
            if (boundInputController != null)
            {
                boundInputController.OnMoveInput -= HandleMoveInput;
                boundInputController = null;
                DebugLogger.Log("[PlayerController] InputController の接続を解除しました。");
            }

            Stop();
        }

        private void HandleMoveInput(Vector2 input)
        {
            Move(input);
        }

        #endregion

        #region Data & Configuration

        public void LoadPlayerData()
        {
            if (playerDataAsset != null && !string.IsNullOrWhiteSpace(playerDataAsset.text))
            {
                var data = PlayerData.FromJson(playerDataAsset.text);
                ApplyData(data);
            }
            else
            {
                var defaultData = new PlayerData
                {
                    maxHp = characterStatus != null ? characterStatus.MaxHp : 100,
                    moveSpeed = moveSpeed > 0f ? moveSpeed : 6f,
                    attackPower = attackPower > 0 ? attackPower : 10,
                    attackInterval = attackInterval > 0f ? attackInterval : 1.0f
                };
                ApplyData(defaultData);
            }
        }

        public void ApplyData(PlayerData data)
        {
            if (data == null) return;

            currentPlayerData = data;
            MoveSpeed = data.moveSpeed > 0f ? data.moveSpeed : 6f;
            AttackPower = data.attackPower > 0 ? data.attackPower : 10;
            AttackInterval = data.attackInterval > 0f ? data.attackInterval : 1.0f;

            if (characterStatus != null)
            {
                characterStatus.SetMaxHp(data.maxHp > 0 ? data.maxHp : 100);
            }

            DebugLogger.Log($"[PlayerController] PlayerData 適用: Name={data.characterName}, MaxHP={data.maxHp}, Speed={data.moveSpeed}, Atk={data.attackPower}, AtkInterval={data.attackInterval}");
        }

        #endregion

        #region Private Helper Methods

        private void InitializeComponents()
        {
            characterVisual = GetComponent<ICharacterVisual>() ?? GetComponentInChildren<ICharacterVisual>();
            if (characterVisual == null)
            {
                characterVisual = gameObject.AddComponent<CharacterVisual2D>();
            }

            characterAnimator = GetComponent<ICharacterAnimator>() ?? GetComponentInChildren<ICharacterAnimator>();
            if (characterAnimator == null)
            {
                characterAnimator = gameObject.AddComponent<CharacterAnimator2D>();
            }

            var status = GetComponent<ICharacterStatus>() ?? GetComponentInChildren<ICharacterStatus>();
            if (status == null)
            {
                status = gameObject.AddComponent<CharacterStatus>();
            }
            SetStatus(status);
        }

        private void UpdateVisuals()
        {
            if (characterVisual == null) return;

            characterVisual.SetFacingDirection(facingDirection);
            characterVisual.UpdateMovementVisuals(moveInput, moveSpeed, Time.deltaTime);
        }

        private void UpdateAnimation()
        {
            if (characterAnimator == null) return;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                characterAnimator.PlayMove(moveInput.magnitude);
            }
            else
            {
                characterAnimator.PlayIdle();
            }
        }

        private void SubscribeStatusEvents(ICharacterStatus status)
        {
            if (status == null) return;
            status.OnTakeDamage += HandleTakeDamage;
            status.OnHeal += HandleHeal;
            status.OnDead += HandleDead;
        }

        private void UnsubscribeStatusEvents(ICharacterStatus status)
        {
            if (status == null) return;
            status.OnTakeDamage -= HandleTakeDamage;
            status.OnHeal -= HandleHeal;
            status.OnDead -= HandleDead;
        }

        private void HandleTakeDamage(int damage)
        {
            OnTakeDamage?.Invoke(damage);
        }

        private void HandleHeal(int healAmount)
        {
            OnHeal?.Invoke(healAmount);
        }

        private void HandleDead()
        {
            Stop();
            characterAnimator?.PlayDie();
            OnDead?.Invoke();
            DebugLogger.Log("[PlayerController] プレイヤーが死亡しました。");
        }

        #endregion

        #region Item Magnet Attraction

        private static readonly ContactFilter2D ItemContactFilter = new ContactFilter2D
        {
            useTriggers = true
        };

        /// <summary>
        /// プレイヤー周囲のアイテム（IAttractable）を検索し、自身に向かって吸い寄せを開始させる。
        /// </summary>
        private void UpdateItemAttraction()
        {
            if (magnetRadius <= 0f) return;

            magnetCheckTimer -= Time.deltaTime;
            if (magnetCheckTimer > 0f) return;
            magnetCheckTimer = magnetCheckInterval;

            // 最新の Unity 物理 API（ContactFilter2D によるゼロGC検出）
            int hitCount = Physics2D.OverlapCircle(transform.position, magnetRadius, ItemContactFilter, itemColliderBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = itemColliderBuffer[i];
                if (hit == null) continue;

                var attractable = hit.GetComponent<IAttractable>();
                if (attractable != null && !attractable.IsAttracted)
                {
                    // プレイヤー自身（transform）を渡して吸い込みを開始
                    attractable.AttractTo(transform);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (magnetRadius > 0f)
            {
                Gizmos.color = new Color(0f, 0.9f, 1f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
        }

        #endregion

        #region Dependency Injection / Setters

        public void SetVisual(ICharacterVisual newVisual) => characterVisual = newVisual;
        public void SetAnimator(ICharacterAnimator newAnimator) => characterAnimator = newAnimator;

        public void SetStatus(ICharacterStatus newStatus)
        {
            if (characterStatus != null)
            {
                UnsubscribeStatusEvents(characterStatus);
            }

            characterStatus = newStatus;

            if (characterStatus != null)
            {
                SubscribeStatusEvents(characterStatus);
            }
        }

        #endregion
    }
}
