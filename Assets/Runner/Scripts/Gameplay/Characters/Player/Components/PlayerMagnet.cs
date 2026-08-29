/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー周囲のアイテム（IAttractable）を検知し、自身（Transform）へ引き寄せるマグネットコンポーネント。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 指定された半径内のアイテム（IAttractable）を検知し、自身へ引き寄せる吸引コンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerMagnet : MonoBehaviour
    {
        private const float DefaultMagnetRadius = 3.5f;
        private const float DefaultCheckInterval = 0.05f;

        private static readonly ContactFilter2D ItemContactFilter = CreateDefaultContactFilter();

        [Header("Magnet Settings")]
        [Tooltip("アイテム吸引範囲の半径(m)")]
        [SerializeField] private float magnetRadius = DefaultMagnetRadius;

        [Tooltip("アイテム検知の判定間隔(秒)")]
        [SerializeField] private float checkInterval = DefaultCheckInterval;

        private float checkTimer;
        private readonly Collider2D[] itemColliderBuffer = new Collider2D[32];

        /// <summary>アイテム吸引半径(m)</summary>
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

        /// <summary>
        /// フレームごとに吸引判定タイマーを更新し、範囲内のアイテムを引き寄せる。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        public void OnUpdate(float deltaTime)
        {
            if (magnetRadius <= 0f) return;

            checkTimer -= deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = checkInterval;

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
        /// トリガーコライダーを含む全レイヤーを対象とする ContactFilter2D を生成する。
        /// </summary>
        /// <returns>正規初期化済みの ContactFilter2D</returns>
        private static ContactFilter2D CreateDefaultContactFilter()
        {
            var filter = ContactFilter2D.noFilter;
            filter.useTriggers = true;
            return filter;
        }
    }
}

