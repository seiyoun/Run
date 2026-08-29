/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ポイ活（所持ポイント/お金）および逃走歩数の表示、ジャスト回避ポップアップ演出を行う純粋なHUDビューコンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面右上に配置されるポイ活・歩数表示HUD（View）。
    /// ゲームパラメータや加算計算は持たず、渡された数値の描画およびアニメーション演出に専念します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PointStepHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("所持ポイント表示テキスト")]
        [SerializeField] private TextMeshProUGUI pointText;

        [Tooltip("歩数表示テキスト")]
        [SerializeField] private TextMeshProUGUI stepText;

        [Tooltip("ジャスト回避ボーナスポップアップテキスト")]
        [SerializeField] private TextMeshProUGUI justDodgePopupText;

        [Tooltip("ジャスト回避ポップアップの CanvasGroup")]
        [SerializeField] private CanvasGroup justDodgeCanvasGroup;

        [Header("Item Arrival Progress")]
        [Tooltip("アイテム入荷までの進捗テキスト")]
        [SerializeField] private TextMeshProUGUI restockText;

        [Tooltip("アイテム入荷進捗バーImage（Filledタイプ）")]
        [SerializeField] private Image restockProgressBar;

        [Tooltip("アイテム入荷進捗バーSlider")]
        [SerializeField] private Slider restockSlider;

        [Header("Animation Settings")]
        [Tooltip("ポイントカウントアップの補間速度")]
        [SerializeField] private float countUpSpeed = 10f;

        [Tooltip("ジャスト回避ポップアップの表示持続時間(秒)")]
        [SerializeField] private float dodgePopupDuration = 1.2f;

        [Tooltip("進捗ゲージの補間速度")]
        [SerializeField] private float progressLerpSpeed = 5f;

        private long currentDisplayPoint;
        private long targetPoint;
        private int currentSteps;
        private float dodgePopupTimer;
        private Vector2 popupInitialPos;
        private float currentDisplayProgress;
        private float targetProgress;
        private long currentRemainingRestockPoints;

        /// <summary>現在設定されている目標所持ポイント</summary>
        public long CurrentPoint => targetPoint;

        /// <summary>現在設定されている表示歩数</summary>
        public int CurrentSteps => currentSteps;

        /// <summary>現在の入荷進捗率 (0.0 〜 1.0)</summary>
        public float TargetProgress => targetProgress;

        /// <summary>
        /// 初期表示状態の設定および初期座標の記録を行う。
        /// </summary>
        private void Awake()
        {
            if (justDodgeCanvasGroup != null)
            {
                justDodgeCanvasGroup.alpha = 0f;
                popupInitialPos = justDodgePopupText != null ? justDodgePopupText.rectTransform.anchoredPosition : Vector2.zero;
            }

            UpdatePointDisplay(0);
            UpdateStepDisplay(0);
            UpdateRestockText(0);
            UpdateProgressDisplay(0f);
        }

        /// <summary>
        /// ポイントの滑らかなカウントアップ、入荷ゲージ補間、および回避ポップアップのアニメーションを更新する。
        /// </summary>
        private void Update()
        {
            if (currentDisplayPoint != targetPoint)
            {
                currentDisplayPoint = (long)Mathf.MoveTowards(currentDisplayPoint, targetPoint, Mathf.Max(1f, Mathf.Abs(targetPoint - currentDisplayPoint) * Time.unscaledDeltaTime * countUpSpeed));
                UpdatePointDisplay(currentDisplayPoint);
            }

            if (!Mathf.Approximately(currentDisplayProgress, targetProgress))
            {
                currentDisplayProgress = Mathf.MoveTowards(currentDisplayProgress, targetProgress, Time.unscaledDeltaTime * progressLerpSpeed);
                UpdateProgressDisplay(currentDisplayProgress);
            }

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
        /// 表示する所持ポイントを設定する。
        /// </summary>
        /// <param name="points">設定するポイント数</param>
        /// <param name="instant">アニメーションせず即時反映するかどうか</param>
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
        /// 表示する歩数を設定する。
        /// </summary>
        /// <param name="steps">設定する歩数</param>
        public void SetSteps(int steps)
        {
            currentSteps = Math.Max(0, steps);
            UpdateStepDisplay(currentSteps);
        }

        /// <summary>
        /// アイテム入荷までの進捗情報（残りポイント・進捗率）を設定する。
        /// </summary>
        /// <param name="remainingPoints">入荷までの残り必要ポイント</param>
        /// <param name="progress">進捗率 (0.0 〜 1.0)</param>
        /// <param name="instant">アニメーションせず即時反映するかどうか</param>
        public void SetRestockProgress(long remainingPoints, float progress, bool instant = false)
        {
            currentRemainingRestockPoints = Math.Max(0, remainingPoints);
            targetProgress = Mathf.Clamp01(progress);

            UpdateRestockText(currentRemainingRestockPoints);

            if (instant)
            {
                currentDisplayProgress = targetProgress;
                UpdateProgressDisplay(currentDisplayProgress);
            }
        }

        /// <summary>
        /// ジャスト回避成功時のポップアップ演出を表示する。
        /// </summary>
        /// <param name="bonusPoints">獲得したボーナスポイント額</param>
        public void ShowJustDodgePopup(int bonusPoints)
        {
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

        /// <summary>
        /// ジャスト回避成功時の演出を発火する（後方互換エイリアス）。
        /// </summary>
        /// <param name="bonusPoints">獲得したボーナスポイント額</param>
        public void TriggerJustDodge(int bonusPoints) => ShowJustDodgePopup(bonusPoints);

        /// <summary>
        /// コード生成時等のUI参照バインドを行う。
        /// </summary>
        /// <param name="point">ポイントテキスト</param>
        /// <param name="step">歩数テキスト</param>
        /// <param name="dodgeText">回避ポップアップテキスト</param>
        /// <param name="dodgeGroup">回避ポップアップCanvasGroup</param>
        /// <param name="restock">入荷進捗テキスト</param>
        /// <param name="progressBar">入荷進捗バーImage</param>
        /// <param name="slider">入荷進捗バーSlider</param>
        public void SetupReferences(
            TextMeshProUGUI point,
            TextMeshProUGUI step,
            TextMeshProUGUI dodgeText,
            CanvasGroup dodgeGroup,
            TextMeshProUGUI restock = null,
            Image progressBar = null,
            Slider slider = null)
        {
            pointText = point;
            stepText = step;
            justDodgePopupText = dodgeText;
            justDodgeCanvasGroup = dodgeGroup;
            restockText = restock;
            restockProgressBar = progressBar;
            restockSlider = slider;

            if (dodgeText != null) popupInitialPos = dodgeText.rectTransform.anchoredPosition;
        }

        /// <summary>
        /// ポイントテキストの表示文字列をフォーマット・更新する。
        /// </summary>
        /// <param name="points">表示するポイント値</param>
        private void UpdatePointDisplay(long points)
        {
            if (pointText != null)
            {
                pointText.text = $"<color=#FFD700>¥</color> {points:N0} <size=70%><color=#A0E0FF>pt</color></size>";
            }
        }

        /// <summary>
        /// 歩数テキストの表示文字列をフォーマット・更新する。
        /// </summary>
        /// <param name="steps">表示する歩数値</param>
        private void UpdateStepDisplay(int steps)
        {
            if (stepText != null)
            {
                stepText.text = $"{steps:N0} <size=75%>歩</size>";
            }
        }

        /// <summary>
        /// アイテム入荷進捗テキストの表示文字列をフォーマット・更新する。
        /// </summary>
        /// <param name="remainingPoints">入荷までの残りポイント</param>
        private void UpdateRestockText(long remainingPoints)
        {
            if (restockText != null)
            {
                restockText.text = $"入荷まで: あと <color=#FFDF00>{remainingPoints:N0}</color> pt";
            }
        }

        /// <summary>
        /// アイテム入荷進捗ゲージ（Image/Slider）の描画を更新する。
        /// </summary>
        /// <param name="progress">進捗率 (0.0 〜 1.0)</param>
        private void UpdateProgressDisplay(float progress)
        {
            if (restockProgressBar != null)
            {
                restockProgressBar.fillAmount = progress;
            }

            if (restockSlider != null)
            {
                restockSlider.value = progress;
            }
        }
    }
}
