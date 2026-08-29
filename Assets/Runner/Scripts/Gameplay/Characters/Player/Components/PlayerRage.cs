/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの怒りゲージ蓄積および最大到達時の覚醒（無敵）モード減衰タイマーを制御するコンポーネント。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの怒りゲージおよび覚醒モードを管理するコンポーネント。
    /// 移動によってのみ怒りが溜まり、ゲージが最大値に達した時のみ覚醒モードとなりゲージが0へ減少します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRage : MonoBehaviour
    {
        private const float DefaultMaxRage = 100f;
        private const float DefaultGainRate = 10f;
        private const float DefaultAwakeningDuration = 10f;

        [Header("Rage Settings")]
        [Tooltip("最大怒りゲージ値")]
        [SerializeField] private float maxRage = DefaultMaxRage;

        [Tooltip("怒りゲージの溜まる速度（1秒あたり）")]
        [SerializeField] private float rageGainRate = DefaultGainRate;

        [Tooltip("怒りMAX時の覚醒持続時間 (秒)")]
        [SerializeField] private float awakeningDuration = DefaultAwakeningDuration;

        private float currentRage;
        private bool isAwakened;
        private float awakeningRemainingTime;

        /// <summary>現在の怒りゲージ値</summary>
        public float CurrentRage => currentRage;

        /// <summary>最大怒りゲージ値</summary>
        public float MaxRage
        {
            get => maxRage;
            set => maxRage = Mathf.Max(1f, value);
        }

        /// <summary>怒りゲージの蓄積割合 (0.0 〜 1.0)</summary>
        public float RageRatio => maxRage > 0f ? Mathf.Clamp01(currentRage / maxRage) : 0f;

        /// <summary>怒りゲージの溜まる速度（1秒あたり）</summary>
        public float RageGainRate
        {
            get => rageGainRate;
            set => rageGainRate = Mathf.Max(0f, value);
        }

        /// <summary>怒りMAX時の覚醒持続時間(秒)</summary>
        public float AwakeningDuration
        {
            get => awakeningDuration;
            set => awakeningDuration = Mathf.Max(0.5f, value);
        }

        /// <summary>現在覚醒（無敵）状態かどうか</summary>
        public bool IsAwakened => isAwakened;

        /// <summary>覚醒残り持続時間(秒)</summary>
        public float AwakeningRemainingTime => awakeningRemainingTime;

        /// <summary>怒り値が変化した際に発火するイベント (現在値, 最大値)</summary>
        public event Action<float, float> OnRageChanged;

        /// <summary>覚醒状態が変化した際に発火するイベント (覚醒中か, 残り秒数)</summary>
        public event Action<bool, float> OnAwakeningChanged;

        /// <summary>
        /// 移動による怒り蓄積および最大到達後の覚醒カウントダウン（ゲージ減少）を更新する。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        /// <param name="isMoving">現在移動中かどうか</param>
        public void OnUpdate(float deltaTime, bool isMoving)
        {
            if (deltaTime <= 0f) return;

            if (isAwakened)
            {
                awakeningRemainingTime -= deltaTime;
                if (awakeningRemainingTime <= 0f)
                {
                    EndAwakening();
                }
                else
                {
                    // 覚醒時間経過に応じてゲージが最大から0へ減少
                    currentRage = Mathf.Clamp((awakeningRemainingTime / awakeningDuration) * maxRage, 0f, maxRage);
                    OnRageChanged?.Invoke(currentRage, maxRage);
                    OnAwakeningChanged?.Invoke(true, awakeningRemainingTime);
                }
            }
            else
            {
                // 移動時のみ怒り蓄積（静止時の減速・減衰は行わない）
                if (isMoving && rageGainRate > 0f)
                {
                    AddRage(rageGainRate * deltaTime);
                }
            }
        }

        /// <summary>
        /// 怒りゲージを加算し、最大値に達した場合は覚醒モードを発動する。
        /// </summary>
        /// <param name="amount">加算量</param>
        public void AddRage(float amount)
        {
            if (amount <= 0f || maxRage <= 0f || isAwakened) return;

            currentRage = Mathf.Clamp(currentRage + amount, 0f, maxRage);
            OnRageChanged?.Invoke(currentRage, maxRage);

            if (currentRage >= maxRage)
            {
                TriggerAwakening(awakeningDuration);
            }
        }

        /// <summary>
        /// 怒りゲージ値を直接設定する。
        /// </summary>
        /// <param name="value">設定値</param>
        public void SetRage(float value)
        {
            if (isAwakened) return;

            currentRage = Mathf.Clamp(value, 0f, maxRage);
            OnRageChanged?.Invoke(currentRage, maxRage);

            if (currentRage >= maxRage)
            {
                TriggerAwakening(awakeningDuration);
            }
        }

        /// <summary>
        /// 覚醒（無敵・ぶっ飛ばしモード）を発動し、ゲージ減少タイマーを開始する。
        /// </summary>
        /// <param name="duration">覚醒持続時間(秒)</param>
        public void TriggerAwakening(float duration = 10f)
        {
            if (isAwakened) return;

            isAwakened = true;
            awakeningDuration = Mathf.Max(0.5f, duration);
            awakeningRemainingTime = awakeningDuration;
            currentRage = maxRage;

            OnRageChanged?.Invoke(currentRage, maxRage);
            OnAwakeningChanged?.Invoke(true, awakeningRemainingTime);
            DebugLogger.Log($"[PlayerRage] 怒りMAX到達！ 覚醒モード発動（持続時間={awakeningDuration}s）");
        }

        /// <summary>
        /// 覚醒状態を終了し、怒りゲージを完全にリセットする。
        /// </summary>
        public void EndAwakening()
        {
            if (!isAwakened) return;

            isAwakened = false;
            awakeningRemainingTime = 0f;
            currentRage = 0f;

            OnRageChanged?.Invoke(0f, maxRage);
            OnAwakeningChanged?.Invoke(false, 0f);
            DebugLogger.Log("[PlayerRage] 覚醒モード終了。怒りゲージがリセットされました。");
        }
    }
}
