/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム全体の入力を一元管理し、コールバックイベントを提供するコントローラー。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runner
{
    /// <summary>
    /// Input System からの入力を監視・集約し、イベント/コールバックを発行する入力管理シングルトン。
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-900)]
    public sealed class InputController : SingletonMonoBehaviour<InputController>
    {
        [Header("Input Actions Reference")]
        [Tooltip("使用する Input Actions アセット")]
        [SerializeField]
        private InputActionAsset inputActionsAsset;

        [Header("Input Sensitivity")]
        [SerializeField]
        private float touchSensitivity = 60f;

        /// <summary>
        /// 移動入力が更新された際に発火するイベント (正規化されたVector2)。
        /// </summary>
        public event Action<Vector2> OnMoveInput;

        /// <summary>
        /// 攻撃ボタンが押された際に発火するイベント。
        /// </summary>
        public event Action OnAttackInput;

        /// <summary>
        /// 現在の移動入力ベクトル。
        /// </summary>
        public Vector2 MoveVector { get; private set; }

        private InputAction moveAction;
        private InputAction attackAction;

        // タッチ / マウスドラッグ用
        private Vector2 touchStartPos;
        private bool isTouching;

        protected override bool ShouldDontDestroyOnLoad => false;

        protected override void Awake()
        {
            base.Awake();
            SetupInputActions();
        }

        private void OnEnable()
        {
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DisposeActions();
        }

        private void SetupInputActions()
        {
            if (inputActionsAsset != null)
            {
                inputActionsAsset.Enable();
                var playerMap = inputActionsAsset.FindActionMap("Player");
                playerMap?.Enable();

                moveAction = inputActionsAsset.FindAction("Player/Move") ?? inputActionsAsset.FindAction("Move");
                attackAction = inputActionsAsset.FindAction("Player/Attack") ?? inputActionsAsset.FindAction("Attack");
            }

            // アセット未設定時のフォールバック
            if (moveAction == null)
            {
                moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                moveAction.AddBinding("<Gamepad>/leftStick");
                moveAction.AddBinding("<Gamepad>/dpad");
            }

            if (attackAction == null)
            {
                attackAction = new InputAction("Attack", InputActionType.Button);
                attackAction.AddBinding("<Keyboard>/space");
                attackAction.AddBinding("<Gamepad>/buttonSouth");
            }

            attackAction.performed += OnAttackPerformed;
            EnableActions();
        }

        private void EnableActions()
        {
            inputActionsAsset?.Enable();
            moveAction?.actionMap?.Enable();
            moveAction?.Enable();
            attackAction?.Enable();
        }

        private void DisableActions()
        {
            moveAction?.Disable();
            attackAction?.Disable();
            inputActionsAsset?.Disable();
        }

        private void DisposeActions()
        {
            if (attackAction != null)
            {
                attackAction.performed -= OnAttackPerformed;
                attackAction.Dispose();
                attackAction = null;
            }

            if (moveAction != null)
            {
                moveAction.Dispose();
                moveAction = null;
            }
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            OnAttackInput?.Invoke();
        }

        private void Update()
        {
            var rawInput = Vector2.zero;

            // 1. Input Actions からの読み取り
            if (moveAction != null && moveAction.enabled)
            {
                rawInput = moveAction.ReadValue<Vector2>();
            }

            // 2. キーボード直接読み取り（フォールバック）
            if (rawInput.sqrMagnitude < 0.01f && Keyboard.current != null)
            {
                var k = Keyboard.current;
                var x = 0f;
                var y = 0f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;

                if (x != 0f || y != 0f)
                {
                    rawInput = new Vector2(x, y);
                }
            }

            // 3. ゲームパッド直接読み取り
            if (rawInput.sqrMagnitude < 0.01f && Gamepad.current != null)
            {
                var stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    rawInput = stick;
                }
            }

            // 4. モバイルタッチ / マウスドラッグ操作の合成
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                var touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
                if (!isTouching)
                {
                    isTouching = true;
                    touchStartPos = touchPos;
                }
                else
                {
                    var delta = (touchPos - touchStartPos) / (Screen.dpi > 0 ? Screen.dpi * 0.5f : touchSensitivity);
                    if (delta.sqrMagnitude > 0.04f)
                    {
                        rawInput += delta;
                    }
                }
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                var mousePos = Mouse.current.position.ReadValue();
                if (!isTouching)
                {
                    isTouching = true;
                    touchStartPos = mousePos;
                }
                else
                {
                    var delta = (mousePos - touchStartPos) / touchSensitivity;
                    if (delta.sqrMagnitude > 0.04f)
                    {
                        rawInput += delta;
                    }
                }
            }
            else
            {
                isTouching = false;
            }

            if (rawInput.sqrMagnitude > 1f)
            {
                rawInput.Normalize();
            }

            MoveVector = rawInput;
            OnMoveInput?.Invoke(MoveVector);
        }

        /// <summary>
        /// 外部から直接移動入力をエミュレート・注入する（テストやUI仮想スティック用）。
        /// </summary>
        public void SetInputDirect(Vector2 move)
        {
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }
            MoveVector = move;
            OnMoveInput?.Invoke(MoveVector);
        }
    }
}
