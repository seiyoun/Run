/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの各サブコンポーネント（移動、攻撃、体力、所持金、歩数、マグネット、バフ）を統括・公開し、
 *                入力バインドおよび全体協調動作を制御するコントローラー。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの各機能サブコンポーネントへの参照ハブとして機能し、
    /// 入力受付や状態変化（死亡・ポーズ等）の全体協調を管理するコントローラークラス。
    /// </summary>
    [RequireComponent(typeof(CharacterMovement2D))]
    [RequireComponent(typeof(CharacterAttacker2D))]
    [RequireComponent(typeof(CharacterStatus))]
    [RequireComponent(typeof(CharacterVisual2D))]
    [RequireComponent(typeof(CharacterAnimator2D))]
    [RequireComponent(typeof(PlayerWallet))]
    [RequireComponent(typeof(PlayerStepTracker))]
    [RequireComponent(typeof(PlayerMagnet))]
    [RequireComponent(typeof(CharacterBuffHandler))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IBuffTarget
    {
        public static PlayerController Instance { get; private set; }

        private CharacterMovement2D movementComponent;
        private CharacterAttacker2D attackerComponent;
        private CharacterStatus statusComponent;
        private CharacterVisual2D visualComponent;
        private CharacterAnimator2D animatorComponent;
        private PlayerWallet walletComponent;
        private PlayerStepTracker stepTrackerComponent;
        private PlayerMagnet magnetComponent;
        private CharacterBuffHandler buffHandlerComponent;
        private InputController boundInputController;
        private Vector3 lastPosition;

        /// <summary>対象エンティティの GameObject（IBuffTarget 実装）</summary>
        public GameObject GameObject => gameObject;

        /// <summary>バフ管理コンポーネント</summary>
        public CharacterBuffHandler Buffs => buffHandlerComponent;

        /// <summary>キャラクターアニメーションコンポーネント</summary>
        public CharacterAnimator2D CharacterAnimator => animatorComponent;

        /// <summary>キャラクターステータス（HP・ダメージ・回復）コンポーネント</summary>
        public CharacterStatus Status => statusComponent;

        /// <summary>移動速度（Movement.MoveSpeed への委譲）</summary>
        public float MoveSpeed
        {
            get => movementComponent != null ? movementComponent.MoveSpeed : 0f;
            set
            {
                if (movementComponent != null) movementComponent.MoveSpeed = value;
            }
        }

        /// <summary>現在の移動入力ベクトル（Movement.MoveInput への委譲）</summary>
        public Vector2 MoveInput => movementComponent != null ? movementComponent.MoveInput : Vector2.zero;

        /// <summary>アイテム吸引半径(m)（Magnet.MagnetRadius への委譲）</summary>
        public float MagnetRadius
        {
            get => magnetComponent != null ? magnetComponent.MagnetRadius : 0f;
            set
            {
                if (magnetComponent != null) magnetComponent.MagnetRadius = value;
            }
        }

        /// <summary>現在の累積総歩数（StepTracker.CurrentSteps への委譲）</summary>
        public int CurrentSteps => stepTrackerComponent != null ? stepTrackerComponent.CurrentSteps : 0;

        /// <summary>現在の所持ポイント/お金（Wallet.CurrentMoney への委譲）</summary>
        public long CurrentMoney => walletComponent != null ? walletComponent.CurrentMoney : 0;

        /// <summary>ゲーム開始からの累積獲得ポイント/お金（Wallet.TotalEarnedMoney への委譲）</summary>
        public long TotalEarnedMoney => walletComponent != null ? walletComponent.TotalEarnedMoney : 0;

        /// <summary>歩数変更時イベント</summary>
        public event Action<int> OnStepsChanged;

        /// <summary>お金・ポイント獲得時イベント</summary>
        public event Action<long> OnMoneyCollected;

        /// <summary>
        /// シングルトンの初期化、サブコンポーネントの参照取得・初期化を行う。
        /// </summary>
        private void Awake()
        {
            Instance = this;
            lastPosition = transform.position;

            InitializeSubComponents();
            LoadPlayerData();
        }

        /// <summary>
        /// 初回フレームで入力コントローラーの自動バインドを試みる。
        /// </summary>
        private void Start()
        {
            if (boundInputController == null && InputController.Instance != null)
            {
                BindInput(InputController.Instance);
            }
        }

        /// <summary>
        /// 固定フレームごとに移動距離を算出し、歩数トラッカーへ通知する。
        /// </summary>
        private void FixedUpdate()
        {
            if (statusComponent != null && statusComponent.IsDead) return;

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
        /// 毎フレームの協調更新（ポーズ判定、死亡判定、外観・アニメーション同期）を実行する。
        /// </summary>
        private void Update()
        {
            bool isPaused = (GameHUDView.Instance != null && GameHUDView.Instance.ShopModal != null && GameHUDView.Instance.ShopModal.IsOpen)
                            || Time.timeScale <= 0f;
            if (isPaused)
            {
                movementComponent?.Stop();
                animatorComponent?.PlayIdle();
                return;
            }

            if (statusComponent != null && statusComponent.IsDead)
            {
                movementComponent?.Stop();
                return;
            }

            float deltaTime = Time.deltaTime;
            attackerComponent?.OnUpdate(deltaTime);
            magnetComponent?.OnUpdate(deltaTime);

            UpdateVisuals(deltaTime);
            UpdateAnimation();
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
        /// プレイヤーにバフを付与する（IBuffTarget 実装）。
        /// </summary>
        /// <param name="buff">付与する IBuff インスタンス</param>
        public void AddBuff(IBuff buff)
        {
            if (buff == null || buffHandlerComponent == null) return;

            buffHandlerComponent.AddBuff(buff);
        }

        /// <summary>
        /// 指定された種別のバフを解除する（IBuffTarget 実装）。
        /// </summary>
        /// <param name="type">解除するバフ種別</param>
        public void RemoveBuff(BuffType type)
        {
            buffHandlerComponent?.RemoveBuff(type);
        }

        /// <summary>
        /// 指定された方向へ移動入力を適用する。
        /// </summary>
        /// <param name="direction">移動入力ベクトル</param>
        private void Move(Vector2 direction)
        {
            if (statusComponent != null && statusComponent.IsDead)
            {
                movementComponent?.Stop();
                return;
            }

            movementComponent?.Move(direction);
        }

        /// <summary>
        /// MasterDataManager のキャッシュから PlayerMasterData を取得して全コンポーネントへ適用する。
        /// </summary>
        private void LoadPlayerData()
        {
            var data = MasterDataManager.GetPlayerMasterData();
            ApplyData(data);
        }

        /// <summary>
        /// PlayerMasterData の各設定値を対応するサブコンポーネントへ分配・適用する。
        /// </summary>
        /// <param name="data">適用するマスターデータ</param>
        private void ApplyData(PlayerMasterData data)
        {
            if (data == null) return;

            if (movementComponent != null)
            {
                movementComponent.MoveSpeed = data.moveSpeed;
            }

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
            if (magnetComponent != null) magnetComponent.MagnetRadius = data.magnetRadius;

            DebugLogger.Log($"[PlayerController] PlayerData 適用完了: HP={data.maxHp}, Speed={data.moveSpeed}, Magnet={data.magnetRadius}m");
        }

        /// <summary>
        /// 全サブコンポーネントの自動検出・初期アタッチおよびイベント連携を行う。
        /// </summary>
        private void InitializeSubComponents()
        {
            movementComponent = EnsureSubComponent<CharacterMovement2D>();
            movementComponent.ClampToStageBounds = true;
            attackerComponent = EnsureSubComponent<CharacterAttacker2D>();
            statusComponent = EnsureSubComponent<CharacterStatus>();
            visualComponent = EnsureSubComponent<CharacterVisual2D>();
            animatorComponent = EnsureSubComponent<CharacterAnimator2D>();
            walletComponent = EnsureSubComponent<PlayerWallet>();
            stepTrackerComponent = EnsureSubComponent<PlayerStepTracker>();
            magnetComponent = EnsureSubComponent<PlayerMagnet>();
            buffHandlerComponent = EnsureSubComponent<CharacterBuffHandler>();

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
        /// サブコンポーネントのイベントを購読する。
        /// </summary>
        private void SubscribeSubComponentEvents()
        {
            if (statusComponent != null)
            {
                statusComponent.OnTakeDamage += HandleTakeDamage;
                statusComponent.OnDead += HandleDead;
            }

            if (stepTrackerComponent != null)
            {
                stepTrackerComponent.OnStepsChanged += HandleStepsChanged;
            }

            if (walletComponent != null)
            {
                walletComponent.OnMoneyCollected += HandleMoneyCollected;
            }

            if (buffHandlerComponent != null)
            {
                buffHandlerComponent.OnBuffApplied += HandleBuffApplied;
                buffHandlerComponent.OnBuffRemoved += HandleBuffRemoved;
            }
        }

        /// <summary>
        /// サブコンポーネントのイベント購読を解除する。
        /// </summary>
        private void UnsubscribeSubComponentEvents()
        {
            if (statusComponent != null)
            {
                statusComponent.OnTakeDamage -= HandleTakeDamage;
                statusComponent.OnDead -= HandleDead;
            }

            if (stepTrackerComponent != null)
            {
                stepTrackerComponent.OnStepsChanged -= HandleStepsChanged;
            }

            if (walletComponent != null)
            {
                walletComponent.OnMoneyCollected -= HandleMoneyCollected;
            }

            if (buffHandlerComponent != null)
            {
                buffHandlerComponent.OnBuffApplied -= HandleBuffApplied;
                buffHandlerComponent.OnBuffRemoved -= HandleBuffRemoved;
            }
        }

        /// <summary>
        /// バフ有効化時にパラメータの加算・効果登録を行う。
        /// </summary>
        /// <param name="buff">有効化された IBuff インスタンス</param>
        private void HandleBuffApplied(IBuff buff)
        {
            switch (buff)
            {
                case SpeedBuff speedBuff:
                    if (movementComponent != null)
                    {
                        movementComponent.MoveSpeed += speedBuff.Value;
                    }
                    break;
                case HpRegenBuff regenBuff:
                    regenBuff.OnHealTick += HandleRegenTick;
                    break;
            }
        }

        /// <summary>
        /// バフ無効化時にパラメータの減算・効果解除を行う。
        /// </summary>
        /// <param name="buff">無効化された IBuff インスタンス</param>
        private void HandleBuffRemoved(IBuff buff)
        {
            switch (buff)
            {
                case SpeedBuff speedBuff:
                    if (movementComponent != null)
                    {
                        movementComponent.MoveSpeed = Mathf.Max(0f, movementComponent.MoveSpeed - speedBuff.Value);
                    }
                    break;
                case HpRegenBuff regenBuff:
                    regenBuff.OnHealTick -= HandleRegenTick;
                    break;
            }
        }

        /// <summary>
        /// リジェネバフのTick時にHPを回復する。
        /// </summary>
        /// <param name="amount">回復量</param>
        private void HandleRegenTick(int amount)
        {
            statusComponent?.Heal(amount);
        }

        /// <summary>
        /// 外観の向きを更新する。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        private void UpdateVisuals(float deltaTime)
        {
            if (visualComponent == null || movementComponent == null) return;

            visualComponent.SetFacingDirection(movementComponent.FacingDirection);
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

        /// <summary>
        /// 被ダメージ時に被弾フラッシュおよびアニメーションを再生する。
        /// </summary>
        /// <param name="damage">受けたダメージ量</param>
        private void HandleTakeDamage(int damage)
        {
            visualComponent?.PlayHitFlash();
            if (statusComponent != null && !statusComponent.IsDead)
            {
                animatorComponent?.TriggerHit();
            }
        }

        /// <summary>
        /// 死亡時に移動停止および死亡アニメーションを再生する。
        /// </summary>
        private void HandleDead()
        {
            movementComponent?.Stop();
            animatorComponent?.PlayDie();
            DebugLogger.Log("[PlayerController] プレイヤーが力尽きました。");
        }

        private void HandleStepsChanged(int steps) => OnStepsChanged?.Invoke(steps);
        private void HandleMoneyCollected(long amount) => OnMoneyCollected?.Invoke(amount);
    }
}
