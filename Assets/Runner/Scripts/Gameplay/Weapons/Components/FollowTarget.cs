/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ターゲット付近に滑らかに追従移動および浮遊ボビングを行う独立コンポーネント。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ターゲット付近に浮遊・追従移動する独立コンポーネント。
    /// IFollowTarget を実装し、対象 Transform へのスムーズな追従とボビング演出を提供します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FollowTarget : MonoBehaviour, IFollowTarget
    {
        private const float DefaultSmoothTime = 0.05f;
        private const float DefaultHoverFrequency = 3.0f;
        private const float DefaultHoverAmplitude = 0.08f;

        [Header("Follow Settings")]
        [Tooltip("追従対象の Transform")]
        [SerializeField] private Transform target;

        [Tooltip("ターゲットを基準とした基本追従オフセット位置")]
        [SerializeField] private Vector3 followOffset = new Vector3(-0.45f, 0.55f, 0f);

        [Tooltip("位置補間の追従時間（秒）")]
        [SerializeField] private float smoothTime = DefaultSmoothTime;

        [Tooltip("向き反転時にオフセットのX座標を反転させるか")]
        [SerializeField] private bool flipOffsetWithFacing = true;

        [Header("Hover Settings")]
        [Tooltip("上下浮遊ボビングの速度（周波数）")]
        [SerializeField] private float hoverFrequency = DefaultHoverFrequency;

        [Tooltip("上下浮遊ボビングの振幅 (m)")]
        [SerializeField] private float hoverAmplitude = DefaultHoverAmplitude;

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

        /// <summary>向き反転時にオフセットのX座標を反転させるか</summary>
        public bool FlipOffsetWithFacing
        {
            get => flipOffsetWithFacing;
            set => flipOffsetWithFacing = value;
        }

        /// <summary>
        /// ランダムな浮遊時間オフセットを初期化する。
        /// </summary>
        private void Awake()
        {
            hoverTimeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// ターゲットの移動完了後に滑らかな追従と浮遊ボビングを行う。
        /// </summary>
        private void LateUpdate()
        {
            UpdateFollow();
        }

        /// <summary>
        /// オブジェクトの文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            string targetName = target != null ? target.name : "None";
            return $"FollowTarget (Target: {targetName}, Offset: {followOffset})";
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
        /// ターゲット位置と浮遊ボビングを計算し、SmoothDamp で滑らかに移動する。
        /// </summary>
        private void UpdateFollow()
        {
            if (target == null) return;

            var targetPos = target.position;

            // ターゲットの向きに応じて左右オフセットとスプライト反転を調整
            bool isFacingLeft = false;
            var movable = target.GetComponent<IMovable>();
            if (movable != null)
            {
                isFacingLeft = movable.FacingDirection.x < 0f;
            }
            else if (target.localScale.x < 0f)
            {
                isFacingLeft = true;
            }

            var adjustedOffset = followOffset;
            if (flipOffsetWithFacing && isFacingLeft)
            {
                adjustedOffset.x = -adjustedOffset.x;
            }

            // サイン波による浮遊（ボビング）演出
            float hoverY = Mathf.Sin((Time.time + hoverTimeOffset) * hoverFrequency) * hoverAmplitude;
            var destination = targetPos + adjustedOffset + new Vector3(0f, hoverY, 0f);

            transform.position = Vector3.SmoothDamp(transform.position, destination, ref currentVelocity, smoothTime);
        }
    }
}
