/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの怒りゲージ蓄積および覚醒（無敵化・ぶっ飛ばしモード）状態のUI描画コンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面下部に配置される怒りゲージHUD。
    /// 逃走やジャスト回避で蓄積され、MAX時に覚醒演出および持続時間バーに切り替わります。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RageGaugeHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI rageText;
        [SerializeField] private TextMeshProUGUI statusLabelText;
        [SerializeField] private CanvasGroup awakeningEffectGroup;

        [Header("Growth Settings")]
        [Tooltip("時間経過によって自動で怒りを増やすか")]
        [SerializeField] private bool increaseOverTime = true;
        [Tooltip("1秒あたりの自動増加量（例: 2.5f で約40秒でMAX）")]
        [SerializeField] private float ragePerSecond = 2.5f;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(1f, 0.45f, 0.1f, 1f); // 炎オレンジ
        [SerializeField] private Color fullColor = new Color(1f, 0.15f, 0.15f, 1f);   // 激怒レッド
        [SerializeField] private Color awakeningColor = new Color(1f, 0.85f, 0.1f, 1f); // 覚醒ゴールド

        [Header("Animation Settings")]
        [SerializeField] private float smoothSpeed = 10f;

        private float currentRage = 0f;
        private float maxRage = 100f;
        private float targetFillAmount = 0f;
        private float currentFillAmount = 0f;

        private bool isAwakened = false;
        private float awakeningDuration = 0f;
        private float awakeningRemainingTime = 0f;

        public bool IsAwakened => isAwakened;
        public float CurrentRage => currentRage;
        public float MaxRage => maxRage;

        public event Action OnAwakeningTriggered;

        private void Awake()
        {
            if (awakeningEffectGroup != null)
            {
                awakeningEffectGroup.alpha = 0f;
            }

            SetRage(0f, 100f, true);
        }

        private void Update()
        {
            if (isAwakened)
            {
                // 覚醒中の残り時間カウントダウン
                if (awakeningRemainingTime > 0f)
                {
                    awakeningRemainingTime -= Time.unscaledDeltaTime;
                    targetFillAmount = Mathf.Clamp01(awakeningRemainingTime / awakeningDuration);

                    if (rageText != null)
                    {
                        rageText.text = $"<color=#FFE040><b>覚醒発動中!</b></color> {awakeningRemainingTime:F1}s";
                    }

                    // 覚醒中の点滅演出
                    if (awakeningEffectGroup != null)
                    {
                        awakeningEffectGroup.alpha = 0.6f + Mathf.PingPong(Time.unscaledTime * 4f, 0.4f);
                    }

                    if (awakeningRemainingTime <= 0f)
                    {
                        EndAwakening();
                    }
                }
            }
            else
            {
                // 時間経過による自動怒り蓄積
                if (increaseOverTime && targetFillAmount < 1f)
                {
                    AddRage(ragePerSecond * Time.deltaTime);
                }

                // ゲージMAX時の点滅
                if (targetFillAmount >= 1f && awakeningEffectGroup != null)
                {
                    awakeningEffectGroup.alpha = Mathf.PingPong(Time.unscaledTime * 3f, 0.7f);
                }
                else if (awakeningEffectGroup != null)
                {
                    awakeningEffectGroup.alpha = 0f;
                }
            }

            // ゲージの滑らかな Lerp アニメーション（左から右に伸びる）
            if (fillImage != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.unscaledDeltaTime * smoothSpeed);
                fillImage.fillAmount = currentFillAmount;
            }
        }

        /// <summary>
        /// 怒り値を設定する。
        /// </summary>
        public void SetRage(float current, float max, bool instant = false)
        {
            if (isAwakened) return;

            currentRage = Mathf.Clamp(current, 0f, max);
            maxRage = Mathf.Max(1f, max);
            targetFillAmount = currentRage / maxRage;

            if (instant)
            {
                currentFillAmount = targetFillAmount;
                if (fillImage != null) fillImage.fillAmount = currentFillAmount;
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 怒り値を加算する。
        /// </summary>
        public void AddRage(float amount)
        {
            if (isAwakened || amount <= 0f) return;
            SetRage(currentRage + amount, maxRage);

            if (currentRage >= maxRage)
            {
                OnRageFull();
            }
        }

        /// <summary>
        /// 覚醒（ぶっ飛ばしモード）を発動する。
        /// </summary>
        /// <param name="duration">覚醒持続時間(秒)</param>
        public void TriggerAwakening(float duration)
        {
            isAwakened = true;
            awakeningDuration = Mathf.Max(0.5f, duration);
            awakeningRemainingTime = awakeningDuration;
            targetFillAmount = 1f;
            currentFillAmount = 1f;

            if (fillImage != null)
            {
                fillImage.color = awakeningColor;
                fillImage.fillAmount = 1f;
            }

            if (statusLabelText != null)
            {
                statusLabelText.text = "<color=#FF4400>【 ぶっ飛ばし無敵モード 】</color>";
            }

            OnAwakeningTriggered?.Invoke();
        }

        /// <summary>
        /// 覚醒状態を終了する。
        /// </summary>
        public void EndAwakening()
        {
            isAwakened = false;
            awakeningRemainingTime = 0f;
            SetRage(0f, maxRage, true);
        }

        private void OnRageFull()
        {
            if (statusLabelText != null)
            {
                statusLabelText.text = "<color=#FF0040>【 怒りMAX! 覚醒可能 】</color>";
            }
        }

        private void UpdateDisplay()
        {
            if (fillImage != null)
            {
                fillImage.color = targetFillAmount >= 1f ? fullColor : normalColor;
            }

            if (rageText != null)
            {
                int percentage = Mathf.RoundToInt(targetFillAmount * 100f);
                rageText.text = $"怒りゲージ: <color=#FFA040>{percentage}%</color>";
            }

            if (statusLabelText != null && !isAwakened)
            {
                statusLabelText.text = targetFillAmount >= 1f 
                    ? "<color=#FF0040>【 怒りMAX! 覚醒 READY 】</color>" 
                    : "逃げて怒りを溜めろ！";
            }
        }

        /// <summary>
        /// UI参照の初期設定
        /// </summary>
        public void SetupReferences(Image fill, TextMeshProUGUI rage, TextMeshProUGUI statusLabel, CanvasGroup effectGroup)
        {
            fillImage = fill;
            rageText = rage;
            statusLabelText = statusLabel;
            awakeningEffectGroup = effectGroup;
        }
    }
}

