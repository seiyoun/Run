/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの怒りゲージ蓄積・減衰・覚醒（無敵）モードの持続タイマーを制御するコンポーネント。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの怒りゲージおよび覚醒モードを管理するコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerRage : MonoBehaviour
    {
        private const float DefaultMaxRage = 100f;
        private const float DefaultGainRate = 10f;
        private const float DefaultDecayRate = 5f;

        [Header("Rage Settings")]
        [Tooltip("最大怒りゲージ値")]
        [SerializeField] private float maxRage = DefaultMaxRage;

        [Tooltip("怒りゲージの溜まる速度（1秒あたり）")]
        [SerializeField] private float rageGainRate = DefaultGainRate;

        [Tooltip("怒りゲージの減る速度（1秒あたり）")]
        [SerializeField] private float rageDecayRate = DefaultDecayRate;

        private float currentRage;
        private bool isAwakened;
        private float awakeningDuration;
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

        /// <summary>怒りゲージの減る速度（1秒あたり）</summary>
        public float RageDecayRate
        {
            get => rageDecayRate;
            set => rageDecayRate = Mathf.Max(0f, value);
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
        /// 怒りゲージの自動蓄積・減衰および覚醒持続タイマーを毎フレーム更新する。
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
                    OnAwakeningChanged?.Invoke(true, awakeningRemainingTime);
                }
            }
            else
            {
                if (isMoving && rageGainRate > 0f)
                {
                    AddRage(rageGainRate * deltaTime);
                }
                else if (!isMoving && rageDecayRate > 0f && currentRage > 0f)
                {
                    ConsumeRage(rageDecayRate * deltaTime);
                }
            }
        }

        /// <summary>
        /// 怒りゲージを加算する。
        /// </summary>
        /// <param name="amount">加算量</param>
        public void AddRage(float amount)
        {
            if (amount <= 0f || maxRage <= 0f) return;

            currentRage = Mathf.Clamp(currentRage + amount, 0f, maxRage);
            OnRageChanged?.Invoke(currentRage, maxRage);
        }

        /// <summary>
        /// 怒りゲージを消費・減算する。
        /// </summary>
        /// <param name="amount">消費量</param>
        public void ConsumeRage(float amount)
        {
            if (amount <= 0f || maxRage <= 0f) return;

            currentRage = Mathf.Clamp(currentRage - amount, 0f, maxRage);
            OnRageChanged?.Invoke(currentRage, maxRage);
        }

        /// <summary>
        /// 怒りゲージ値を直接設定する。
        /// </summary>
        /// <param name="value">設定値</param>
        public void SetRage(float value)
        {
            currentRage = Mathf.Clamp(value, 0f, maxRage);
            OnRageChanged?.Invoke(currentRage, maxRage);
        }

        /// <summary>
        /// 覚醒（無敵・ぶっ飛ばしモード）を発動する。
        /// </summary>
        /// <param name="duration">覚醒持続時間(秒)</param>
        public void TriggerAwakening(float duration = 10f)
        {
            isAwakened = true;
            awakeningDuration = Mathf.Max(0.5f, duration);
            awakeningRemainingTime = awakeningDuration;
            SetRage(maxRage);
            OnAwakeningChanged?.Invoke(true, awakeningRemainingTime);
            DebugLogger.Log($"[PlayerRage] 覚醒モード発動！ 持続時間={awakeningDuration}s");
        }

        /// <summary>
        /// 覚醒状態を終了し、怒りゲージをリセットする。
        /// </summary>
        public void EndAwakening()
        {
            if (!isAwakened) return;

            isAwakened = false;
            awakeningRemainingTime = 0f;
            SetRage(0f);
            OnAwakeningChanged?.Invoke(false, 0f);
            DebugLogger.Log("[PlayerRage] 覚醒モード終了。");
        }
    }
}

