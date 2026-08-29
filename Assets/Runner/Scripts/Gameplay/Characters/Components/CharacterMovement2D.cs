/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IMovable を実装し、Rigidbody2D による2Dキャラクターの移動物理、入力、速度、向きを制御する汎用コンポーネント。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Rigidbody2D を用いてキャラクターの移動・停止・向きの更新を制御する移動コンポーネント。
    /// プレイヤーだけでなく敵やNPC等にも共通で利用可能な IMovable 実装です。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class CharacterMovement2D : MonoBehaviour, IMovable
    {
        private const float DefaultSpeed = 6.0f;

        [Header("Movement Settings")]
        [Tooltip("移動速度")]
        [SerializeField] private float moveSpeed = DefaultSpeed;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 facingDirection = Vector2.right;

        /// <summary>移動速度</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>現在の移動入力ベクトル</summary>
        public Vector2 MoveInput => moveInput;

        /// <summary>現在向いている水平方向ベクトル</summary>
        public Vector2 FacingDirection => facingDirection;

        /// <summary>
        /// Rigidbody2D の参照取得および物理設定の初期化を行う。
        /// </summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        /// <summary>
        /// 固定フレームごとに物理移動を Rigidbody2D に反映する。
        /// </summary>
        private void FixedUpdate()
        {
            if (rb == null) return;

            var delta = moveInput * (moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(rb.position + delta);
            rb.linearVelocity = moveInput * moveSpeed;
        }

        /// <summary>
        /// 指定された方向ベクトルに従って移動入力を適用する。
        /// </summary>
        /// <param name="direction">移動方向ベクトル</param>
        public void Move(Vector2 direction)
        {
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
    }
}
