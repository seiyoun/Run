/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 脱出ゲートのプレハブでプレイヤー接触を通知する。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ステージ上の脱出ゲートにプレイヤーが入ったときに通知する。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public sealed class EscapePoint : MonoBehaviour
    {
        /// <summary>現在ロードされている脱出ポイント</summary>
        public static EscapePoint Instance { get; private set; }

        /// <summary>プレイヤーが脱出ポイントに入ったときのイベント</summary>
        public event Action OnPlayerEntered;

        /// <summary>脱出ポイントが破棄されるときのイベント</summary>
        public event Action OnDestroyed;

        private bool wasEntered;

        /// <summary>
        /// プレハブ上の接触範囲をトリガーとして設定する。
        /// </summary>
        private void Awake()
        {
            Instance = this;
            var trigger = GetComponent<BoxCollider2D>();
            trigger.isTrigger = true;
        }

        /// <summary>破棄時に生成元へ通知する。</summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            OnDestroyed?.Invoke();
        }

        /// <summary>
        /// プレイヤーの最初の接触だけを脱出イベントとして通知する。
        /// </summary>
        /// <param name="other">接触したコライダー</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (wasEntered || other.GetComponent<PlayerController>() == null) return;

            wasEntered = true;
            OnPlayerEntered?.Invoke();
        }
    }
}
