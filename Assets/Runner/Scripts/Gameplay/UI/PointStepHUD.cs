/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ポイ活（所持ポイント/お金）および逃走歩数を表示し、ジャスト回避演出を制御するUIコンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面右上に配置されるポイ活・歩数表示HUD。
    /// ジャスト回避時のボーナスポイント獲得演出も行います。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PointStepHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI pointText;
        [SerializeField] private TextMeshProUGUI stepText;
        [SerializeField] private TextMeshProUGUI justDodgePopupText;
        [SerializeField] private CanvasGroup justDodgeCanvasGroup;

        [Header("Step & Point Settings")]
        [Tooltip("1歩と判定する移動距離(m)")]
        [SerializeField] private float distancePerStep = 0.65f;
        [Tooltip("1歩ごとに獲得するポイント")]
        [SerializeField] private int pointsPerStep = 2;

        [Header("Animation Settings")]
        [SerializeField] private float countUpSpeed = 10f;
        [SerializeField] private float dodgePopupDuration = 1.2f;

        private long currentDisplayPoint = 0;
        private long targetPoint = 0;
        private int currentSteps = 0;
        private float accumulatedDistance = 0f;

        private float dodgePopupTimer = 0f;
        private Vector2 popupInitialPos;

        public long CurrentPoint => targetPoint;
        public int CurrentSteps => currentSteps;

        private void Awake()
        {
            if (justDodgeCanvasGroup != null)
            {
                justDodgeCanvasGroup.alpha = 0f;
                popupInitialPos = justDodgePopupText != null ? justDodgePopupText.rectTransform.anchoredPosition : Vector2.zero;
            }

            UpdatePointDisplay(0);
            UpdateStepDisplay(0);
        }

        private void Update()
        {
            // ポイントの滑らかなカウントアップアニメーション
            if (currentDisplayPoint != targetPoint)
            {
                currentDisplayPoint = (long)Mathf.MoveTowards(currentDisplayPoint, targetPoint, Mathf.Max(1f, Mathf.Abs(targetPoint - currentDisplayPoint) * Time.unscaledDeltaTime * countUpSpeed));
                UpdatePointDisplay(currentDisplayPoint);
            }

            // ジャスト回避ポップアップのフェードアウト＆浮遊アニメーション
            if (dodgePopupTimer > 0f)
            {
                dodgePopupTimer -= Time.unscaledDeltaTime;
                float progress = 1f - (dodgePopupTimer / dodgePopupDuration);

                if (justDodgeCanvasGroup != null)
                {
                    justDodgeCanvasGroup.alpha = Mathf.Clamp01((1f - progress) * 2f);
                }

                if (justDodgePopupText != null)
                {
                    justDodgePopupText.rectTransform.anchoredPosition = popupInitialPos + new Vector2(0f, progress * 40f);
                }

                if (dodgePopupTimer <= 0f && justDodgeCanvasGroup != null)
                {
                    justDodgeCanvasGroup.alpha = 0f;
                }
            }
        }

        /// <summary>
        /// 所持ポイントを設定する。
        /// </summary>
        public void SetPoints(long points, bool instant = false)
        {
            targetPoint = Math.Max(0, points);
            if (instant)
            {
                currentDisplayPoint = targetPoint;
                UpdatePointDisplay(currentDisplayPoint);
            }
        }

        /// <summary>
        /// ポイントを加算する。
        /// </summary>
        public void AddPoints(long amount)
        {
            if (amount <= 0) return;
            SetPoints(targetPoint + amount);
        }

        /// <summary>
        /// ポイントを消費する（購入時など）。
        /// </summary>
        public bool TryConsumePoints(long amount)
        {
            if (amount <= 0 || targetPoint < amount) return false;
            SetPoints(targetPoint - amount);
            return true;
        }

        /// <summary>
        /// 移動距離を加算し、一定距離（1歩）ごとに歩数・ポイントを加算する。
        /// </summary>
        /// <param name="deltaDistance">前フレームからの移動距離</param>
        public void OnDistanceMoved(float deltaDistance)
        {
            if (deltaDistance <= 0f) return;

            accumulatedDistance += deltaDistance;
            if (distancePerStep > 0f && accumulatedDistance >= distancePerStep)
            {
                int steps = Mathf.FloorToInt(accumulatedDistance / distancePerStep);
                accumulatedDistance %= distancePerStep;

                AddSteps(steps);
                AddPoints((long)steps * pointsPerStep);
            }
        }

        /// <summary>
        /// 歩数を設定・加算する。
        /// </summary>
        public void SetSteps(int steps)
        {
            currentSteps = Math.Max(0, steps);
            UpdateStepDisplay(currentSteps);
        }

        public void AddSteps(int steps)
        {
            if (steps <= 0) return;
            SetSteps(currentSteps + steps);
        }

        /// <summary>
        /// ジャスト回避成功時の演出をトリガーする。
        /// </summary>
        /// <param name="bonusPoints">獲得したボーナスポイント</param>
        public void TriggerJustDodge(int bonusPoints)
        {
            AddPoints(bonusPoints);

            if (justDodgePopupText != null)
            {
                justDodgePopupText.text = $"<color=#FFDF00>JUST DODGE!</color>\n<size=80%>+¥{bonusPoints:N0} pt</size>";
                justDodgePopupText.rectTransform.anchoredPosition = popupInitialPos;
            }

            if (justDodgeCanvasGroup != null)
            {
                justDodgeCanvasGroup.alpha = 1f;
            }

            dodgePopupTimer = dodgePopupDuration;
        }

        private void UpdatePointDisplay(long points)
        {
            if (pointText != null)
            {
                pointText.text = $"<color=#FFD700>¥</color> {points:N0} <size=70%><color=#A0E0FF>pt</color></size>";
            }
        }

        private void UpdateStepDisplay(int steps)
        {
            if (stepText != null)
            {
                stepText.text = $"👟 {steps:N0} <size=75%>歩</size>";
            }
        }

        /// <summary>
        /// UI要素のバインド用セッター
        /// </summary>
        public void SetupReferences(TextMeshProUGUI point, TextMeshProUGUI step, TextMeshProUGUI dodgeText, CanvasGroup dodgeGroup)
        {
            pointText = point;
            stepText = step;
            justDodgePopupText = dodgeText;
            justDodgeCanvasGroup = dodgeGroup;
            if (dodgeText != null) popupInitialPos = dodgeText.rectTransform.anchoredPosition;
        }
    }
}

