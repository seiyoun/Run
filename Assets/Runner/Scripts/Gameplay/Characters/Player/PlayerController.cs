/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IMovable, ICharacterVisual, ICharacterAnimator, ICharacterStatus を協調させてプレイヤーを制御するクラス。
 */

using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour, IMovable
    {
        public static PlayerController Instance { get; private set; }

        [Header("Data Configuration")]
        [Tooltip("プレイヤー専用パラメータ JSON アセット (未設定時は Inspector のデフォルト値を使用)")]
        [SerializeField]
        private TextAsset playerDataAsset;

        [Header("Movement Settings")]
        [Tooltip("移動速度 (PlayerData が設定されている場合はそちらが優先されます)")]
        [SerializeField]
        private float moveSpeed = 6f;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 facingDirection = Vector2.right;

        private InputController boundInputController;
        private ICharacterVisual characterVisual;
        private ICharacterAnimator characterAnimator;
        private ICharacterStatus characterStatus;
        private PlayerData currentPlayerData;

        public PlayerData CurrentData => currentPlayerData;

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

        public ICharacterVisual CharacterVisual => characterVisual;
        public ICharacterAnimator CharacterAnimator => characterAnimator;
        public ICharacterStatus Status => characterStatus;

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

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

            characterStatus = GetComponent<ICharacterStatus>() ?? GetComponentInChildren<ICharacterStatus>();
            if (characterStatus == null)
            {
                characterStatus = gameObject.AddComponent<CharacterStatus>();
            }

            LoadPlayerData();
        }

        private void Start()
        {
            if (boundInputController == null && InputController.Instance != null)
            {
                BindInput(InputController.Instance);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindInput();
        }

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
                    moveSpeed = moveSpeed > 0f ? moveSpeed : 6f
                };
                ApplyData(defaultData);
            }
        }

        public void ApplyData(PlayerData data)
        {
            if (data == null) return;

            currentPlayerData = data;
            MoveSpeed = data.moveSpeed > 0f ? data.moveSpeed : 6f;

            if (characterStatus != null)
            {
                characterStatus.SetMaxHp(data.maxHp > 0 ? data.maxHp : 100);
            }

            DebugLogger.Log($"[PlayerController] PlayerData 適用: Name={data.characterName}, MaxHP={data.maxHp}, Speed={data.moveSpeed}");
        }

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

        /// <summary>
        /// プレイヤーの攻撃を実行する（敵接近時の自動攻撃やスキル等から呼び出し）。
        /// </summary>
        public void Attack()
        {
            if (characterStatus != null && characterStatus.IsDead) return;
            characterAnimator?.TriggerAttack();
        }

        private void Update()
        {
            if (characterStatus != null && characterStatus.IsDead)
            {
                moveInput = Vector2.zero;
                return;
            }

            // 見た目インターフェース（ICharacterVisual）へ向きとアニメーションを伝達
            if (characterVisual != null)
            {
                characterVisual.SetFacingDirection(facingDirection);
                characterVisual.UpdateMovementVisuals(moveInput, moveSpeed, Time.deltaTime);
            }

            // アニメーションインターフェース（ICharacterAnimator）へ状態を伝達
            if (characterAnimator != null)
            {
                if (moveInput.sqrMagnitude > 0.01f)
                {
                    characterAnimator.PlayMove(moveInput.magnitude);
                }
                else
                {
                    characterAnimator.PlayIdle();
                }
            }
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
        }

        public void SetVisual(ICharacterVisual newVisual) => characterVisual = newVisual;
        public void SetAnimator(ICharacterAnimator newAnimator) => characterAnimator = newAnimator;
        public void SetStatus(ICharacterStatus newStatus) => characterStatus = newStatus;
    }
}
