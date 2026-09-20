/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー等の追従対象の近傍を浮遊しながら滑らかに追従移動するドローン追従コンポーネント。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ドローンオブジェクトをプレイヤーなどのターゲット付近に浮遊・追従させるコンポーネント。
    /// サイン波による上下のボビング演出およびスムーズな位置補間を提供します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DroneFollower : MonoBehaviour
    {
        private const float DefaultSmoothTime = 0.05f;
        private const float DefaultHoverFrequency = 3.0f;
        private const float DefaultHoverAmplitude = 0.08f;

        [Header("Follow Settings")]
        [Tooltip("追従対象の Transform（未設定時は PlayerController.Instance を自動検知）")]
        [SerializeField] private Transform target;

        [Tooltip("ターゲットを基準とした基本追従オフセット位置")]
        [SerializeField] private Vector3 followOffset = new Vector3(-0.45f, 0.55f, 0f);

        [Tooltip("位置補間の追従時間（秒）")]
        [SerializeField] private float smoothTime = DefaultSmoothTime;

        [Header("Hover Settings")]
        [Tooltip("上下浮遊ボビングの速度（周波数）")]
        [SerializeField] private float hoverFrequency = DefaultHoverFrequency;

        [Tooltip("上下浮遊ボビングの振幅 (m)")]
        [SerializeField] private float hoverAmplitude = DefaultHoverAmplitude;

        private SpriteRenderer spriteRenderer;
        private Vector3 currentVelocity;
        private float hoverTimeOffset;

        /// <summary>現在の追従対象 Transform</summary>
        public Transform Target => target;

        /// <summary>追従オフセット座標</summary>
        public Vector3 FollowOffset
        {
            get => followOffset;
            set => followOffset = value;
        }

        /// <summary>
        /// コンポーネント参照の取得およびランダムな浮遊時間オフセットを初期化する。
        /// </summary>
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            hoverTimeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// 初回フレームでのターゲット初期検知を行う。
        /// </summary>
        private void Start()
        {
            EnsureTarget();
        }

        /// <summary>
        /// プレイヤーの移動完了後に滑らかな追従と浮遊ボビングを行う。
        /// </summary>
        private void LateUpdate()
        {
            FollowTarget();
        }

        /// <summary>
        /// オブジェクトの文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            string targetName = target != null ? target.name : "None";
            return $"DroneFollower (Target: {targetName}, Offset: {followOffset})";
        }

        /// <summary>
        /// 追従対象の Transform を明示的に設定する。
        /// </summary>
        /// <param name="newTarget">新しい追従対象</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// ターゲットが未設定の場合に PlayerController.Instance の Transform を自動設定する。
        /// </summary>
        private void EnsureTarget()
        {
            if (target != null) return;

            var player = PlayerController.Instance;
            if (player != null)
            {
                target = player.transform;
            }
        }

        /// <summary>
        /// ターゲット位置と浮遊ボビングを計算し、SmoothDamp で滑らかに移動する。
        /// </summary>
        private void FollowTarget()
        {
            EnsureTarget();
            if (target == null) return;

            var targetPos = target.position;

            // プレイヤーの向きに応じて左右オフセットとスプライト反転を調整
            bool isFacingLeft = false;
            var player = PlayerController.Instance;
            if (player != null)
            {
                isFacingLeft = player.FacingDirection.x < 0f;
            }
            else if (target.localScale.x < 0f)
            {
                isFacingLeft = true;
            }

            var adjustedOffset = followOffset;
            if (isFacingLeft)
            {
                adjustedOffset.x = -adjustedOffset.x;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = isFacingLeft;
            }

            // サイン波による浮遊（ボビング）演出
            float hoverY = Mathf.Sin((Time.time + hoverTimeOffset) * hoverFrequency) * hoverAmplitude;
            var destination = targetPos + adjustedOffset + new Vector3(0f, hoverY, 0f);

            transform.position = Vector3.SmoothDamp(transform.position, destination, ref currentVelocity, smoothTime);
        }
    }
}

