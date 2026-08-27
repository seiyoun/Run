/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーなどのターゲットに向かって加速度的に吸引・移動する独立した MonoBehaviour コンポーネント。
 *                IAttractable を実装し、アイテムにアタッチすることでマグネット吸引機能を付与します。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// アイテム等にアタッチして使用する吸引（マグネット移動）制御コンポーネント。
    /// IAttractable を実装し、ターゲットが指定されると 2D 平面上でターゲットに向かって加速度的に追従移動します。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class Attractable : MonoBehaviour, IAttractable
    {
        private const float DefaultInitialSpeed = 8f;
        private const float DefaultAcceleration = 25f;
        private const float DefaultMaxSpeed = 35f;
        private const float DefaultArriveDistance = 0.45f;
        [Header("Attract Settings")]
        [Tooltip("吸い込み初速")]
        [SerializeField] private float initialAttractSpeed = DefaultInitialSpeed;

        [Tooltip("吸い込み加速度")]
        [SerializeField] private float attractAcceleration = DefaultAcceleration;

        [Tooltip("最大吸い込み速度")]
        [SerializeField] private float maxAttractSpeed = DefaultMaxSpeed;

        [Tooltip("ターゲットへ近接到達とみなす距離(m)")]
        [SerializeField] private float arriveDistance = DefaultArriveDistance;
        private Transform targetTransform;
        private bool isAttracted;
        private float currentAttractSpeed;
        public bool IsAttracted => isAttracted;
        public Transform Target => targetTransform;
        public float CurrentAttractSpeed => currentAttractSpeed;

        public event Action<Transform> OnAttractStarted;
        public event Action<Transform> OnAttractReached;
        public event Action OnAttractStopped;
        /// <summary>
        /// コライダーがトリガーに設定されていることを保証する。
        /// </summary>
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// 吸引中であれば 2D 平面上でターゲットに向かって加速度的に追従移動する。
        /// </summary>
        private void Update()
        {
            if (!isAttracted || targetTransform == null) return;

            Vector2 currentPos = transform.position;
            Vector2 targetPos = targetTransform.position;
            float distance = Vector2.Distance(currentPos, targetPos);

            // 近接到達判定
            if (distance <= arriveDistance)
            {
                OnAttractReached?.Invoke(targetTransform);
                return;
            }

            // 加速度的スピードアップ
            currentAttractSpeed = Mathf.Min(currentAttractSpeed + (attractAcceleration * Time.deltaTime), maxAttractSpeed);
            Vector2 dir = (targetPos - currentPos).normalized;
            Vector2 nextPos = currentPos + dir * (currentAttractSpeed * Time.deltaTime);

            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
        }
        /// <summary>
        /// 指定ターゲットへの吸い寄せを開始する。
        /// </summary>
        /// <param name="target">吸引先のターゲット Transform</param>
        /// <param name="initialSpeed">初期吸引速度（0以下の場合は設定値を使用）</param>
        public void AttractTo(Transform target, float initialSpeed = 0f)
        {
            if (target == null) return;

            targetTransform = target;
            isAttracted = true;
            currentAttractSpeed = initialSpeed > 0f ? initialSpeed : initialAttractSpeed;
            OnAttractStarted?.Invoke(target);
            DebugLogger.Log($"[Attractable] 吸引開始 -> Target: {target.name}, Pos: {transform.position}");
        }

        /// <summary>
        /// 吸引を中止し、現在の位置で停止する。
        /// </summary>
        public void StopAttract()
        {
            isAttracted = false;
            targetTransform = null;
            currentAttractSpeed = 0f;
            OnAttractStopped?.Invoke();
        }

        /// <summary>
        /// 吸引パラメータを外部から動的に設定する。
        /// </summary>
        /// <param name="initialSpeed">初速</param>
        /// <param name="acceleration">加速度</param>
        /// <param name="maxSpeed">最大速度</param>
        public void Configure(float initialSpeed, float acceleration, float maxSpeed)
        {
            initialAttractSpeed = Mathf.Max(0.1f, initialSpeed);
            attractAcceleration = Mathf.Max(0.1f, acceleration);
            maxAttractSpeed = Mathf.Max(initialAttractSpeed, maxSpeed);
        }
    }
}
