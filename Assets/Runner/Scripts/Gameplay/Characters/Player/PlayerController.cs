/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IMovable, IAttacker, IDamageable, IHealable, IMoneyCollector を実装し、
 *                PlayerData のデータに基づいてプレイヤーの移動・戦闘・歩数・アイテム吸引を制御するクラス。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの移動、戦闘、ステータス、歩数、およびアイテム吸引（マグネット）を統合制御するクラス。
    /// Inspector の SerializeField は持たず、PlayerData（JSON/データクラス）からすべてのパラメータをロードして動作します。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IMovable, IAttacker, IDamageable, IHealable, IMoneyCollector
    {
        // -------------------------------------------------------------
        // 1. const / static フィールド
        // -------------------------------------------------------------
        public static PlayerController Instance { get; private set; }

        private const float DefaultMagnetCheckInterval = 0.05f;

        private static readonly ContactFilter2D ItemContactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false,
            useDepth = false,
            useNormalAngle = false
        };

        // -------------------------------------------------------------
        // 2. [SerializeField] シリアライズフィールド (※PlayerDataのみを使用するため全廃)
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 3. private インスタンス変数
        // -------------------------------------------------------------
        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 facingDirection = Vector2.right;

        // PlayerData から反映されるパラメータ（初期値は持たず、PlayerData の適用時に設定）
        private float moveSpeed;
        private int attackPower;
        private float attackInterval;
        private float attackCooldownTimer;

        private float magnetRadius;
        private float magnetCheckTimer;

        // 歩数・移動距離・所持金ステート
        private int currentSteps;
        private float totalDistanceMoved;
        private float stepAccumulator;
        private float stepDistanceThreshold;
        private long pointsPerStep;
        private long currentMoney;

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

        // -------------------------------------------------------------
        // 4. public インスタンス変数
        // -------------------------------------------------------------
        // (public 変数は使用せずプロパティにカプセル化)

        // -------------------------------------------------------------
        // 5. プロパティ & イベント (Properties & Events)
        // -------------------------------------------------------------
        public PlayerData CurrentData => currentPlayerData;
        public ICharacterVisual CharacterVisual => characterVisual;
        public ICharacterAnimator CharacterAnimator => characterAnimator;
        public ICharacterStatus Status => characterStatus;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.1f, value);
        }

        public Vector2 MoveInput => moveInput;
        public Vector2 FacingDirection => facingDirection;

        public int AttackPower
        {
            get => attackPower;
            set => attackPower = Mathf.Max(1, value);
        }

        public float AttackInterval
        {
            get => attackInterval;
            set => attackInterval = Mathf.Max(0.05f, value);
        }

        public bool CanAttack => (characterStatus == null || !characterStatus.IsDead) && attackCooldownTimer <= 0f;
        public bool IsDead => characterStatus?.IsDead ?? false;

        public float MagnetRadius
        {
            get => magnetRadius;
            set
            {
                magnetRadius = Mathf.Max(0f, value);
#if SANDBOX || UNITY_EDITOR
                PlayerDebugRangeVisualizer.UpdateRadius(transform, magnetRadius);
#endif
            }
        }

        public int CurrentSteps => currentSteps;
        public float TotalDistanceMoved => totalDistanceMoved;
        public long CurrentMoney => currentMoney;

        // イベント
        public event Action OnAttack;
        public event Action<int> OnTakeDamage;
        public event Action OnDead;
        public event Action<int> OnHeal;
        public event Action<int> OnStepsChanged;
        public event Action<float> OnDistanceMoved;
        public event Action<long> OnMoneyCollected;

        // -------------------------------------------------------------
        // 6. Unity ライフサイクル関数
        // -------------------------------------------------------------

        /// <summary>
        /// シングルトンの初期化、物理コンポーネントの設定、およびパラメータのロードを行う。
        /// </summary>
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

        /// <summary>
        /// 入力コントローラー（InputController）が存在する場合にバインドを行う。
        /// </summary>
        private void Start()
        {
            if (boundInputController == null && InputController.Instance != null)
            {
                BindInput(InputController.Instance);
            }
        }

        /// <summary>
        /// 毎フレームの攻撃クールダウン更新、見た目・アニメーション同期、およびアイテム吸引検知を行う。
        /// </summary>
        private void Update()
        {
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

        /// <summary>
        /// 固定フレームごとの物理移動処理、歩数蓄積判定、およびポイント加算イベント通知を行う。
        /// </summary>
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

            // 移動による歩数加算・ポイ活・イベント通知
            float distance = delta.magnitude;
            if (distance > 0f)
            {
                totalDistanceMoved += distance;
                stepAccumulator += distance;

                while (stepAccumulator >= stepDistanceThreshold)
                {
                    stepAccumulator -= stepDistanceThreshold;
                    currentSteps++;
                    CollectMoney(pointsPerStep);
                    OnStepsChanged?.Invoke(currentSteps);
                }

                OnDistanceMoved?.Invoke(distance);

                if (GameHUDView.Instance != null)
                {
                    GameHUDView.Instance.OnPlayerMoved(distance);
                }
            }
        }

        /// <summary>
        /// インスタンス破棄時に入力バインドの解除およびステータスイベントの購読解除を行う。
        /// </summary>
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

        // -------------------------------------------------------------
        // 7. override 関数
        // -------------------------------------------------------------

        /// <summary>
        /// プレイヤーの現在の主要ステータスを表す文字列を返す。
        /// </summary>
        /// <returns>プレイヤー情報文字列</returns>
        public override string ToString()
        {
            return $"PlayerController (Steps: {currentSteps}, Money: {currentMoney}, Speed: {moveSpeed}, Magnet: {magnetRadius}m)";
        }

        // -------------------------------------------------------------
        // 8. public 関数
        // -------------------------------------------------------------

        #region IMovable Implementation

        /// <summary>
        /// 指定された方向へ移動入力を適用する。
        /// </summary>
        /// <param name="direction">移動入力ベクトル</param>
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

        /// <summary>
        /// 移動入力を停止し、物理速度をゼロにする。
        /// </summary>
        public void Stop()
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        #endregion

        #region IAttacker Implementation

        /// <summary>
        /// プレイヤーが現在向いている方向に向けて攻撃を実行する。
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

        /// <summary>
        /// プレイヤーに指定量のダメージを与える。
        /// </summary>
        /// <param name="amount">ダメージ量</param>
        public void TakeDamage(int amount)
        {
            characterStatus?.TakeDamage(amount);
        }

        #endregion

        #region IHealable Implementation

        /// <summary>
        /// プレイヤーのHPを指定量回復する。
        /// </summary>
        /// <param name="amount">回復量</param>
        public void Heal(int amount)
        {
            characterStatus?.Heal(amount);
        }

        #endregion

        #region IMoneyCollector Implementation

        /// <summary>
        /// お金・ポイントを回収して加算し、HUDへ反映する。
        /// </summary>
        /// <param name="amount">獲得金額/ポイント</param>
        public void CollectMoney(long amount)
        {
            if (amount <= 0) return;

            currentMoney += amount;
            OnMoneyCollected?.Invoke(amount);

            if (GameHUDView.Instance != null && GameHUDView.Instance.PointStepHUD != null)
            {
                GameHUDView.Instance.PointStepHUD.SetPoints(currentMoney);
            }
        }

        #endregion

        #region Input Binding

        /// <summary>
        /// InputController の入力イベントを購読する。
        /// </summary>
        /// <param name="inputController">バインド対象の InputController</param>
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
            }
        }

        /// <summary>
        /// InputController の入力イベント購読を解除する。
        /// </summary>
        public void UnbindInput()
        {
            if (boundInputController != null)
            {
                boundInputController.OnMoveInput -= HandleMoveInput;
                boundInputController = null;
            }
        }

        #endregion

        #region Data & Configuration

        /// <summary>
        /// PlayerData をロードし、プレイヤーの全パラメータに適用する。
        /// </summary>
        public void LoadPlayerData()
        {
            var jsonAsset = Resources.Load<TextAsset>("Data/PlayerData");
            if (jsonAsset != null && !string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                var data = PlayerData.FromJson(jsonAsset.text);
                ApplyData(data);
            }
            else
            {
                ApplyData(new PlayerData());
            }
        }

        /// <summary>
        /// 外部またはロードした PlayerData をプレイヤーに適用する。
        /// </summary>
        /// <param name="data">適用する PlayerData</param>
        public void ApplyData(PlayerData data)
        {
            if (data == null) return;

            currentPlayerData = data;
            moveSpeed = data.moveSpeed;
            attackPower = data.attackPower;
            attackInterval = data.attackInterval;
            magnetRadius = data.magnetRadius;
            stepDistanceThreshold = data.stepDistanceThreshold;
            pointsPerStep = data.pointsPerStep;

            if (characterStatus != null)
            {
                characterStatus.SetMaxHp(data.maxHp);
            }

#if SANDBOX || UNITY_EDITOR
            PlayerDebugRangeVisualizer.UpdateRadius(transform, magnetRadius);
#endif

            DebugLogger.Log($"[PlayerController] PlayerData 適用: Name={data.characterName}, MaxHP={data.maxHp}, Speed={data.moveSpeed}, Magnet={data.magnetRadius}m, StepDist={data.stepDistanceThreshold}m");
        }

        /// <summary>
        /// 見た目制御インターフェースを設定する。
        /// </summary>
        /// <param name="newVisual">新しい ICharacterVisual</param>
        public void SetVisual(ICharacterVisual newVisual) => characterVisual = newVisual;

        /// <summary>
        /// アニメーション制御インターフェースを設定する。
        /// </summary>
        /// <param name="newAnimator">新しい ICharacterAnimator</param>
        public void SetAnimator(ICharacterAnimator newAnimator) => characterAnimator = newAnimator;

        /// <summary>
        /// ステータス管理インターフェースを設定し、イベントの購読を更新する。
        /// </summary>
        /// <param name="newStatus">新しい ICharacterStatus</param>
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

        // -------------------------------------------------------------
        // 9. private 関数 / 内部ヘルパー
        // -------------------------------------------------------------

        /// <summary>
        /// 見た目・アニメーション・ステータスコンポーネントの自動検出・初期アタッチを行う。
        /// </summary>
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

        /// <summary>
        /// 見た目の向きおよびスプライトの移動エフェクトを更新する。
        /// </summary>
        private void UpdateVisuals()
        {
            if (characterVisual == null) return;

            characterVisual.SetFacingDirection(facingDirection);
            characterVisual.UpdateMovementVisuals(moveInput, moveSpeed, Time.deltaTime);
        }

        /// <summary>
        /// 移動状態に応じたアニメーション（移動/待機）を再生する。
        /// </summary>
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

        /// <summary>
        /// プレイヤー周囲のアイテム（IAttractable）を検知し、自身（transform）への吸引を開始させる。
        /// </summary>
        private void UpdateItemAttraction()
        {
            if (magnetRadius <= 0f) return;

            magnetCheckTimer -= Time.deltaTime;
            if (magnetCheckTimer > 0f) return;
            magnetCheckTimer = DefaultMagnetCheckInterval;

            int hitCount = Physics2D.OverlapCircle(transform.position, magnetRadius, ItemContactFilter, itemColliderBuffer);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = itemColliderBuffer[i];
                if (hit == null) continue;

                var attractable = hit.GetComponent<IAttractable>();
                if (attractable != null && !attractable.IsAttracted)
                {
                    attractable.AttractTo(transform);
                }
            }
        }

        /// <summary>
        /// InputController からの移動入力コールバックを処理する。
        /// </summary>
        /// <param name="input">入力ベクトル</param>
        private void HandleMoveInput(Vector2 input)
        {
            Move(input);
        }

        /// <summary>
        /// ステータスインターフェースからの各種イベントを購読する。
        /// </summary>
        /// <param name="status">購読対象の ICharacterStatus</param>
        private void SubscribeStatusEvents(ICharacterStatus status)
        {
            if (status == null) return;
            status.OnTakeDamage += HandleTakeDamage;
            status.OnHeal += HandleHeal;
            status.OnDead += HandleDead;
        }

        /// <summary>
        /// ステータスインターフェースからの各種イベントの購読を解除する。
        /// </summary>
        /// <param name="status">解除対象の ICharacterStatus</param>
        private void UnsubscribeStatusEvents(ICharacterStatus status)
        {
            if (status == null) return;
            status.OnTakeDamage -= HandleTakeDamage;
            status.OnHeal -= HandleHeal;
            status.OnDead -= HandleDead;
        }

        /// <summary>
        /// 被ダメージイベントを転送発火する。
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        private void HandleTakeDamage(int damage)
        {
            OnTakeDamage?.Invoke(damage);
        }

        /// <summary>
        /// 回復イベントを転送発火する。
        /// </summary>
        /// <param name="healAmount">回復量</param>
        private void HandleHeal(int healAmount)
        {
            OnHeal?.Invoke(healAmount);
        }

        /// <summary>
        /// 死亡イベントを処理し、移動停止および死亡アニメーションを再生する。
        /// </summary>
        private void HandleDead()
        {
            Stop();
            characterAnimator?.PlayDie();
            OnDead?.Invoke();
            DebugLogger.Log("[PlayerController] プレイヤーが死亡しました。");
        }
    }
}
