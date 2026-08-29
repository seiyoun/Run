/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 各種インターフェース（IMovable, IAttacker, IDamageable, IHealable, IMoneyCollector）に準拠した
 *                サブコンポーネント群を統括・協調動作させるプレイヤーのファサードコントローラー。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの各機能コンポーネント（移動、攻撃、体力、所持金、歩数、怒り、マグネット）を統括するファサードクラス。
    /// 各種インターフェースを実装し、外部呼び出しを内部の特化コンポーネントへ移譲します。
    /// </summary>
    [RequireComponent(typeof(CharacterMovement2D))]
    [RequireComponent(typeof(CharacterAttacker2D))]
    [RequireComponent(typeof(CharacterStatus))]
    [RequireComponent(typeof(CharacterVisual2D))]
    [RequireComponent(typeof(CharacterAnimator2D))]
    [RequireComponent(typeof(PlayerWallet))]
    [RequireComponent(typeof(PlayerStepTracker))]
    [RequireComponent(typeof(PlayerRage))]
    [RequireComponent(typeof(PlayerMagnet))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IMovable, IAttacker, IDamageable, IHealable, IMoneyCollector
    {
        public static PlayerController Instance { get; private set; }

        private CharacterMovement2D movementComponent;
        private CharacterAttacker2D attackerComponent;
        private CharacterStatus statusComponent;
        private CharacterVisual2D visualComponent;
        private CharacterAnimator2D animatorComponent;
        private PlayerWallet walletComponent;
        private PlayerStepTracker stepTrackerComponent;
        private PlayerRage rageComponent;
        private PlayerMagnet magnetComponent;
        private InputController boundInputController;
        private PlayerData currentPlayerData;
        private Vector3 lastPosition;

        /// <summary>現在のプレイヤー設定データ</summary>
        public PlayerData CurrentData => currentPlayerData;

        /// <summary>キャラクター外観コンポーネント</summary>
        public ICharacterVisual CharacterVisual => visualComponent;

        /// <summary>キャラクターアニメーションコンポーネント</summary>
        public ICharacterAnimator CharacterAnimator => animatorComponent;

        /// <summary>キャラクターステータスコンポーネント</summary>
        public ICharacterStatus Status => statusComponent;

        /// <summary>移動速度</summary>
        public float MoveSpeed
        {
            get => movementComponent != null ? movementComponent.MoveSpeed : 6f;
            set
            {
                if (movementComponent != null) movementComponent.MoveSpeed = value;
            }
        }

        /// <summary>現在の移動入力ベクトル</summary>
        public Vector2 MoveInput => movementComponent != null ? movementComponent.MoveInput : Vector2.zero;

        /// <summary>現在の向きベクトル</summary>
        public Vector2 FacingDirection => movementComponent != null ? movementComponent.FacingDirection : Vector2.right;

        /// <summary>基本攻撃力</summary>
        public int AttackPower
        {
            get => attackerComponent != null ? attackerComponent.AttackPower : 10;
            set
            {
                if (attackerComponent != null) attackerComponent.AttackPower = value;
            }
        }

        /// <summary>攻撃間隔（秒）</summary>
        public float AttackInterval
        {
            get => attackerComponent != null ? attackerComponent.AttackInterval : 1f;
            set
            {
                if (attackerComponent != null) attackerComponent.AttackInterval = value;
            }
        }

        /// <summary>現在攻撃可能かどうか</summary>
        public bool CanAttack => attackerComponent != null && attackerComponent.CanAttack;

        /// <summary>死亡状態かどうか</summary>
        public bool IsDead => statusComponent != null && statusComponent.IsDead;

        /// <summary>アイテム吸引半径(m)</summary>
        public float MagnetRadius
        {
            get => magnetComponent != null ? magnetComponent.MagnetRadius : 0f;
            set
            {
                if (magnetComponent != null) magnetComponent.MagnetRadius = value;
            }
        }

        /// <summary>現在の累積総歩数</summary>
        public int CurrentSteps => stepTrackerComponent != null ? stepTrackerComponent.CurrentSteps : 0;

        /// <summary>現在の累積総移動距離(m)</summary>
        public float TotalDistanceMoved => stepTrackerComponent != null ? stepTrackerComponent.TotalDistanceMoved : 0f;

        /// <summary>現在の所持ポイント/お金</summary>
        public long CurrentMoney => walletComponent != null ? walletComponent.CurrentMoney : 0;

        /// <summary>現在の怒りゲージ値</summary>
        public float CurrentRage => rageComponent != null ? rageComponent.CurrentRage : 0f;

        /// <summary>最大怒りゲージ値</summary>
        public float MaxRage => rageComponent != null ? rageComponent.MaxRage : 100f;

        /// <summary>怒りゲージの蓄積割合 (0.0 〜 1.0)</summary>
        public float RageRatio => rageComponent != null ? rageComponent.RageRatio : 0f;

        /// <summary>怒りゲージの溜まる速度（1秒あたり）</summary>
        public float RageGainRate => rageComponent != null ? rageComponent.RageGainRate : 0f;

        /// <summary>現在覚醒（無敵）状態かどうか</summary>
        public bool IsAwakened => rageComponent != null && rageComponent.IsAwakened;

        /// <summary>覚醒残り持続時間(秒)</summary>
        public float AwakeningRemainingTime => rageComponent != null ? rageComponent.AwakeningRemainingTime : 0f;

        /// <summary>攻撃実行時イベント</summary>
        public event Action OnAttack;

        /// <summary>被ダメージ時イベント</summary>
        public event Action<int> OnTakeDamage;

        /// <summary>死亡時イベント</summary>
        public event Action OnDead;

        /// <summary>回復時イベント</summary>
        public event Action<int> OnHeal;

        /// <summary>歩数変更時イベント</summary>
        public event Action<int> OnStepsChanged;

        /// <summary>移動距離発生時イベント</summary>
        public event Action<float> OnDistanceMoved;

        /// <summary>お金・ポイント獲得時イベント</summary>
        public event Action<long> OnMoneyCollected;

        /// <summary>怒り値変更時イベント</summary>
        public event Action<float, float> OnRageChanged;

        /// <summary>覚醒状態変更時イベント</summary>
        public event Action<bool, float> OnAwakeningChanged;

        /// <summary>
        /// シングルトンの初期化、サブコンポーネントの参照取得・バインド、データロードを行う。
        /// </summary>
        private void Awake()
        {
            Instance = this;
            lastPosition = transform.position;

            InitializeSubComponents();
            LoadPlayerData();
        }

        /// <summary>
        /// InputController のバインドを行う。
        /// </summary>
        private void Start()
        {
            if (boundInputController == null && InputController.Instance != null)
            {
                BindInput(InputController.Instance);
            }
        }

        /// <summary>
        /// 固定フレームごとに移動距離を算出し、歩数トラッカーおよびHUDへ通知する。
        /// </summary>
        private void FixedUpdate()
        {
            if (IsDead) return;

            float distance = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;

            if (distance > 0f && stepTrackerComponent != null)
            {
                stepTrackerComponent.ProcessMovementDistance(distance, walletComponent);

                if (GameHUDView.Instance != null)
                {
                    GameHUDView.Instance.OnPlayerMoved(distance);
                }
            }
        }

        /// <summary>
        /// 破棄時に入力バインドおよびイベント購読を解除する。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindInput();
            UnsubscribeSubComponentEvents();
        }

        /// <summary>
        /// オブジェクトの文字列表現を返す。
        /// </summary>
        /// <returns>プレイヤー情報文字列</returns>
        public override string ToString()
        {
            return $"PlayerController (Steps: {CurrentSteps}, Money: {CurrentMoney}, Speed: {MoveSpeed}, Magnet: {MagnetRadius}m)";
        }

        /// <summary>
        /// 毎フレームの更新処理（攻撃タイマー、怒りゲージ、アイテム吸引、外観、アニメーション）を一括駆動する。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間（ポーズ時は0）</param>
        public void OnUpdate(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                movementComponent?.Stop();
                animatorComponent?.PlayIdle();
                return;
            }

            if (IsDead)
            {
                movementComponent?.Stop();
                return;
            }

            attackerComponent?.OnUpdate(deltaTime);
            rageComponent?.OnUpdate(deltaTime, MoveInput.sqrMagnitude > 0.01f);
            magnetComponent?.OnUpdate(deltaTime);

            UpdateVisuals(deltaTime);
            UpdateAnimation();
        }

        /// <summary>
        /// 指定された方向へ移動入力を適用する。
        /// </summary>
        /// <param name="direction">移動入力ベクトル</param>
        public void Move(Vector2 direction)
        {
            if (IsDead)
            {
                movementComponent?.Stop();
                return;
            }

            movementComponent?.Move(direction);
        }

        /// <summary>
        /// 移動入力を停止し、物理速度をゼロにする。
        /// </summary>
        public void Stop()
        {
            movementComponent?.Stop();
        }

        /// <summary>
        /// 正面に向けて攻撃を実行する。
        /// </summary>
        public void Attack()
        {
            attackerComponent?.Attack();
        }

        /// <summary>
        /// 指定された方向に向けて攻撃を実行する。
        /// </summary>
        /// <param name="direction">攻撃方向ベクトル</param>
        public void Attack(Vector2 direction)
        {
            attackerComponent?.Attack(direction);
        }

        /// <summary>
        /// ダメージを受ける。
        /// </summary>
        /// <param name="amount">ダメージ量</param>
        public void TakeDamage(int amount)
        {
            statusComponent?.TakeDamage(amount);
        }

        /// <summary>
        /// HPを回復する。
        /// </summary>
        /// <param name="amount">回復量</param>
        public void Heal(int amount)
        {
            statusComponent?.Heal(amount);
        }

        /// <summary>
        /// お金・ポイントを加算する。
        /// </summary>
        /// <param name="amount">加算額</param>
        public void CollectMoney(long amount)
        {
            walletComponent?.CollectMoney(amount);
        }

        /// <summary>
        /// お金・ポイントを消費する。
        /// </summary>
        /// <param name="amount">消費額</param>
        /// <returns>消費に成功したかどうか</returns>
        public bool TryConsumeMoney(long amount)
        {
            return walletComponent != null && walletComponent.TryConsumeMoney(amount);
        }

        /// <summary>
        /// 怒りゲージを加算する。
        /// </summary>
        /// <param name="amount">加算量</param>
        public void AddRage(float amount)
        {
            rageComponent?.AddRage(amount);
        }


        /// <summary>
        /// 怒りゲージ値を設定する。
        /// </summary>
        /// <param name="value">設定値</param>
        public void SetRage(float value)
        {
            rageComponent?.SetRage(value);
        }

        /// <summary>
        /// 覚醒（無敵）モードを発動する。
        /// </summary>
        /// <param name="duration">持続時間(秒)</param>
        public void TriggerAwakening(float duration = 10f)
        {
            rageComponent?.TriggerAwakening(duration);
        }

        /// <summary>
        /// 覚醒モードを終了する。
        /// </summary>
        public void EndAwakening()
        {
            rageComponent?.EndAwakening();
        }

        /// <summary>
        /// InputController の移動入力をバインドする。
        /// </summary>
        /// <param name="inputController">バインド対象</param>
        public void BindInput(InputController inputController)
        {
            if (boundInputController != null)
            {
                UnbindInput();
            }

            boundInputController = inputController;
            if (boundInputController != null)
            {
                boundInputController.OnMoveInput += Move;
            }
        }

        /// <summary>
        /// InputController のバインドを解除する。
        /// </summary>
        public void UnbindInput()
        {
            if (boundInputController != null)
            {
                boundInputController.OnMoveInput -= Move;
                boundInputController = null;
            }
        }

        /// <summary>
        /// Resources から PlayerData をロードして全コンポーネントへ適用する。
        /// </summary>
        public void LoadPlayerData()
        {
            var jsonAsset = Resources.Load<TextAsset>("Data/PlayerData");
            var data = (jsonAsset != null && !string.IsNullOrWhiteSpace(jsonAsset.text))
                ? PlayerData.FromJson(jsonAsset.text)
                : new PlayerData();

            ApplyData(data);
        }

        /// <summary>
        /// PlayerData の各設定値を対応するサブコンポーネントへ分配・適用する。
        /// </summary>
        /// <param name="data">適用するデータ</param>
        public void ApplyData(PlayerData data)
        {
            if (data == null) return;

            currentPlayerData = data;

            if (movementComponent != null) movementComponent.MoveSpeed = data.moveSpeed;
            if (attackerComponent != null)
            {
                attackerComponent.AttackPower = data.attackPower;
                attackerComponent.AttackInterval = data.attackInterval;
            }
            if (statusComponent != null) statusComponent.SetMaxHp(data.maxHp, true);
            if (stepTrackerComponent != null)
            {
                stepTrackerComponent.StepDistanceThreshold = data.stepDistanceThreshold;
                stepTrackerComponent.PointsPerStep = data.pointsPerStep;
            }
            if (rageComponent != null)
            {
                rageComponent.MaxRage = data.maxRage;
                rageComponent.RageGainRate = data.rageGainRate;
                rageComponent.AwakeningDuration = data.awakeningDuration;
                rageComponent.SetRage(0f);
            }
            if (magnetComponent != null) magnetComponent.MagnetRadius = data.magnetRadius;

            DebugLogger.Log($"[PlayerController] PlayerData 適用完了: HP={data.maxHp}, Speed={data.moveSpeed}, Magnet={data.magnetRadius}m, Rage(Max={data.maxRage}, Gain={data.rageGainRate}/s)");
        }

        /// <summary>
        /// 全サブコンポーネントの自動検出・初期アタッチおよびイベント連携を行う。
        /// </summary>
        private void InitializeSubComponents()
        {
            movementComponent = EnsureSubComponent<CharacterMovement2D>();
            attackerComponent = EnsureSubComponent<CharacterAttacker2D>();
            statusComponent = EnsureSubComponent<CharacterStatus>();
            visualComponent = EnsureSubComponent<CharacterVisual2D>();
            animatorComponent = EnsureSubComponent<CharacterAnimator2D>();
            walletComponent = EnsureSubComponent<PlayerWallet>();
            stepTrackerComponent = EnsureSubComponent<PlayerStepTracker>();
            rageComponent = EnsureSubComponent<PlayerRage>();
            magnetComponent = EnsureSubComponent<PlayerMagnet>();

            SubscribeSubComponentEvents();
        }

        /// <summary>
        /// 指定された型のコンポーネントが存在しない場合に AddComponent して取得する。
        /// </summary>
        /// <typeparam name="T">コンポーネント型</typeparam>
        /// <returns>取得または追加されたコンポーネント</returns>
        private T EnsureSubComponent<T>() where T : Component
        {
            var comp = GetComponent<T>();
            if (comp == null)
            {
                comp = gameObject.AddComponent<T>();
            }
            return comp;
        }

        /// <summary>
        /// サブコンポーネント群のイベントを購読し、外部公開イベントへ中継する。
        /// </summary>
        private void SubscribeSubComponentEvents()
        {
            if (attackerComponent != null) attackerComponent.OnAttack += HandleAttack;
            if (statusComponent != null)
            {
                statusComponent.OnTakeDamage += HandleTakeDamage;
                statusComponent.OnHeal += HandleHeal;
                statusComponent.OnDead += HandleDead;
            }
            if (walletComponent != null) walletComponent.OnMoneyCollected += HandleMoneyCollected;
            if (stepTrackerComponent != null)
            {
                stepTrackerComponent.OnStepsChanged += HandleStepsChanged;
                stepTrackerComponent.OnDistanceMoved += HandleDistanceMoved;
            }
            if (rageComponent != null)
            {
                rageComponent.OnRageChanged += HandleRageChanged;
                rageComponent.OnAwakeningChanged += HandleAwakeningChanged;
            }
        }

        /// <summary>
        /// サブコンポーネント群のイベント購読を解除する。
        /// </summary>
        private void UnsubscribeSubComponentEvents()
        {
            if (attackerComponent != null) attackerComponent.OnAttack -= HandleAttack;
            if (statusComponent != null)
            {
                statusComponent.OnTakeDamage -= HandleTakeDamage;
                statusComponent.OnHeal -= HandleHeal;
                statusComponent.OnDead -= HandleDead;
            }
            if (walletComponent != null) walletComponent.OnMoneyCollected -= HandleMoneyCollected;
            if (stepTrackerComponent != null)
            {
                stepTrackerComponent.OnStepsChanged -= HandleStepsChanged;
                stepTrackerComponent.OnDistanceMoved -= HandleDistanceMoved;
            }
            if (rageComponent != null)
            {
                rageComponent.OnRageChanged -= HandleRageChanged;
                rageComponent.OnAwakeningChanged -= HandleAwakeningChanged;
            }
        }

        /// <summary>
        /// 外観の向きおよび移動エフェクトを更新する。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        private void UpdateVisuals(float deltaTime)
        {
            if (visualComponent == null || movementComponent == null) return;

            visualComponent.SetFacingDirection(movementComponent.FacingDirection);
            visualComponent.UpdateMovementVisuals(movementComponent.MoveInput, movementComponent.MoveSpeed, deltaTime);
        }

        /// <summary>
        /// 移動入力に応じたアニメーションを再生する。
        /// </summary>
        private void UpdateAnimation()
        {
            if (animatorComponent == null || movementComponent == null) return;

            if (movementComponent.MoveInput.sqrMagnitude > 0.01f)
            {
                animatorComponent.PlayMove(movementComponent.MoveInput.magnitude);
            }
            else
            {
                animatorComponent.PlayIdle();
            }
        }

        private void HandleAttack() => OnAttack?.Invoke();
        private void HandleTakeDamage(int damage) => OnTakeDamage?.Invoke(damage);
        private void HandleHeal(int healAmount) => OnHeal?.Invoke(healAmount);
        private void HandleStepsChanged(int steps) => OnStepsChanged?.Invoke(steps);
        private void HandleDistanceMoved(float dist) => OnDistanceMoved?.Invoke(dist);
        private void HandleMoneyCollected(long amount) => OnMoneyCollected?.Invoke(amount);
        private void HandleRageChanged(float cur, float max) => OnRageChanged?.Invoke(cur, max);
        private void HandleAwakeningChanged(bool awakened, float time) => OnAwakeningChanged?.Invoke(awakened, time);

        /// <summary>
        /// 死亡時に移動停止および死亡アニメーションを再生する。
        /// </summary>
        private void HandleDead()
        {
            movementComponent?.Stop();
            animatorComponent?.PlayDie();
            OnDead?.Invoke();
            DebugLogger.Log("[PlayerController] プレイヤーが力尽きました。");
        }
    }
}
