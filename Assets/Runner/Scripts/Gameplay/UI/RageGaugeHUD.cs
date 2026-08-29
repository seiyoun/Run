/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの怒りゲージ蓄積および覚醒（無敵化）状態を描画する純粋なHUDビューコンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面下部に配置される怒りゲージHUD（View）。
    /// ゲームパラメータやタイマー減算ロジックは持たず、プレイヤーから通知された怒り値・覚醒状態の描画とアニメーション演出に専念します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RageGaugeHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("ゲージバーの Fill Image")]
        [SerializeField] private Image fillImage;

        [Tooltip("怒り値/覚醒状態表示テキスト")]
        [SerializeField] private TextMeshProUGUI rageText;

        [Tooltip("ステータス案内ラベルテキスト")]
        [SerializeField] private TextMeshProUGUI statusLabelText;

        [Tooltip("覚醒エフェクト用 CanvasGroup")]
        [SerializeField] private CanvasGroup awakeningEffectGroup;

        [Header("Colors")]
        [Tooltip("通常時のゲージカラー")]
        [SerializeField] private Color normalColor = new Color(1f, 0.45f, 0.1f, 1f);

        [Tooltip("ゲージMAX時のカラー")]
        [SerializeField] private Color fullColor = new Color(1f, 0.15f, 0.15f, 1f);

        [Tooltip("覚醒モード時のカラー")]
        [SerializeField] private Color awakeningColor = new Color(1f, 0.85f, 0.1f, 1f);

        [Header("Animation Settings")]
        [Tooltip("ゲージ伸縮の補間速度")]
        [SerializeField] private float smoothSpeed = 10f;

        private float targetFillAmount;
        private float currentFillAmount;
        private bool isAwakened;
        private float awakeningRemainingTime;

        /// <summary>覚醒状態が表示中かどうか</summary>
        public bool IsAwakened => isAwakened;

        /// <summary>
        /// 初期表示状態の設定を行う。
        /// </summary>
        private void Awake()
        {
            if (awakeningEffectGroup != null)
            {
                awakeningEffectGroup.alpha = 0f;
            }

            SetRage(0f, 100f, true);
        }

        /// <summary>
        /// ゲージバーの滑らかな Lerp 補間および覚醒時の点滅演出を更新する。
        /// </summary>
        private void Update()
        {
            if (isAwakened)
            {
                if (awakeningEffectGroup != null)
                {
                    awakeningEffectGroup.alpha = 0.6f + Mathf.PingPong(Time.unscaledTime * 4f, 0.4f);
                }
            }
            else
            {
                if (targetFillAmount >= 1f && awakeningEffectGroup != null)
                {
                    awakeningEffectGroup.alpha = Mathf.PingPong(Time.unscaledTime * 3f, 0.7f);
                }
                else if (awakeningEffectGroup != null)
                {
                    awakeningEffectGroup.alpha = 0f;
                }
            }

            if (fillImage != null)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.unscaledDeltaTime * smoothSpeed);
                fillImage.fillAmount = currentFillAmount;
            }
        }

        /// <summary>
        /// 怒りゲージの表示値を設定する。
        /// </summary>
        /// <param name="current">現在の怒り値</param>
        /// <param name="max">最大怒り値</param>
        /// <param name="instant">補間せず即時反映するかどうか</param>
        public void SetRage(float current, float max, bool instant = false)
        {
            float clampedMax = Mathf.Max(1f, max);
            float clampedCurrent = Mathf.Clamp(current, 0f, clampedMax);
            targetFillAmount = clampedCurrent / clampedMax;

            if (instant)
            {
                currentFillAmount = targetFillAmount;
                if (fillImage != null) fillImage.fillAmount = currentFillAmount;
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 覚醒モードの表示状態および残り時間を設定する。
        /// </summary>
        /// <param name="awakened">覚醒中かどうか</param>
        /// <param name="remainingTime">覚醒残り時間(秒)</param>
        public void SetAwakened(bool awakened, float remainingTime = 0f)
        {
            isAwakened = awakened;
            awakeningRemainingTime = Mathf.Max(0f, remainingTime);

            UpdateDisplay();
        }

        /// <summary>
        /// コード生成時等のUI参照バインドを行う。
        /// </summary>
        /// <param name="fill">ゲージ Fill Image</param>
        /// <param name="rage">怒りテキスト</param>
        /// <param name="statusLabel">ステータスラベル</param>
        /// <param name="effectGroup">エフェクトCanvasGroup</param>
        public void SetupReferences(Image fill, TextMeshProUGUI rage, TextMeshProUGUI statusLabel, CanvasGroup effectGroup)
        {
            fillImage = fill;
            rageText = rage;
            statusLabelText = statusLabel;
            awakeningEffectGroup = effectGroup;
        }

        /// <summary>
        /// ゲージの色およびテキスト表示を現在の割合に基づいて更新する。
        /// </summary>
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
    }
}
