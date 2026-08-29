/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 制限時間のカウントダウンおよび非常口/改札の開放・方向ナビゲーションを管理するHUDコンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面中央上部に配置される脱出タイマーおよび非常口開放案内HUD。
    /// 制限時間がゼロになると非常口が開き、脱出ナビゲーションが開始されます。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EscapeTimerHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject exitAlertBanner;
        [SerializeField] private TextMeshProUGUI exitAlertText;
        [SerializeField] private RectTransform exitArrowIndicator;
        [SerializeField] private TextMeshProUGUI exitDistanceText;

        [Header("Timer Settings")]
        [SerializeField] private float initialTimeSeconds = 180f; // 3分

        private float remainingTime;
        private bool isExitUnlocked = false;
        private bool isTimerRunning = true;
        private Transform exitTargetTransform;
        private Transform playerTransform;

        public float RemainingTime => remainingTime;
        public bool IsExitUnlocked => isExitUnlocked;

        public event Action OnExitUnlocked;

        private void Awake()
        {
            remainingTime = initialTimeSeconds;
            if (exitAlertBanner != null) exitAlertBanner.SetActive(false);
            if (exitArrowIndicator != null) exitArrowIndicator.gameObject.SetActive(false);

            UpdateTimerDisplay(remainingTime);
        }

        private void Update()
        {
            if (isTimerRunning && remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime <= 0f)
                {
                    remainingTime = 0f;
                    UnlockExit();
                }

                UpdateTimerDisplay(remainingTime);
            }

            // 非常口ナビゲーションの更新
            if (isExitUnlocked && exitArrowIndicator != null && exitArrowIndicator.gameObject.activeSelf)
            {
                UpdateExitNavigation();
            }
        }

        /// <summary>
        /// タイマーを開始・リセットする。
        /// </summary>
        public void StartTimer(float durationSeconds)
        {
            initialTimeSeconds = durationSeconds;
            remainingTime = durationSeconds;
            isExitUnlocked = false;
            isTimerRunning = true;

            if (exitAlertBanner != null) exitAlertBanner.SetActive(false);
            if (exitArrowIndicator != null) exitArrowIndicator.gameObject.SetActive(false);

            UpdateTimerDisplay(remainingTime);
        }

        public void PauseTimer() => isTimerRunning = false;
        public void ResumeTimer() => isTimerRunning = true;

        /// <summary>
        /// 非常口を開放する。
        /// </summary>
        public void UnlockExit()
        {
            if (isExitUnlocked) return;

            isExitUnlocked = true;
            isTimerRunning = false;

            if (statusText != null)
            {
                statusText.text = "<color=#00FF88><b>非常口 開放中！</b></color>";
            }

            if (exitAlertBanner != null)
            {
                exitAlertBanner.SetActive(true);
            }

            if (exitAlertText != null)
            {
                exitAlertText.text = "【ALERT】 <color=#FFFF00>非常口が開いた！ 改札へ向かえ！</color>";
            }

            if (exitArrowIndicator != null)
            {
                exitArrowIndicator.gameObject.SetActive(true);
            }

            OnExitUnlocked?.Invoke();
        }

        /// <summary>
        /// 非常口オブジェクトとプレイヤーのTransformを設定し、ナビゲーションを有効化する。
        /// </summary>
        public void SetExitTarget(Transform exitTransform, Transform player)
        {
            exitTargetTransform = exitTransform;
            playerTransform = player;
        }

        private void UpdateTimerDisplay(float seconds)
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(seconds / 60f);
            int sec = Mathf.FloorToInt(seconds % 60f);

            if (isExitUnlocked)
            {
                timerText.text = "<color=#00FF66><b>脱出せよ！</b></color>";
            }
            else
            {
                // 残り30秒未満は赤く点滅
                string colorTag = seconds < 30f ? "<color=#FF4444>" : "<color=#FFFFFF>";
                timerText.text = $"{colorTag}{minutes:D2}:{sec:D2}</color>";
            }
        }

        private void UpdateExitNavigation()
        {
            if (exitTargetTransform == null) return;

            var playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
            var exitPos = (Vector2)exitTargetTransform.position;
            var dir = exitPos - playerPos;
            float distance = dir.magnitude;

            // 距離テキストの更新
            if (exitDistanceText != null)
            {
                exitDistanceText.text = $"非常口まで: <color=#00FF88>{distance:F1}m</color>";
            }

            // 矢印の回転（2D平面）
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            exitArrowIndicator.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        public void SetupReferences(TextMeshProUGUI timer, TextMeshProUGUI status, GameObject alertBanner, TextMeshProUGUI alertText, RectTransform arrow, TextMeshProUGUI distText)
        {
            timerText = timer;
            statusText = status;
            exitAlertBanner = alertBanner;
            exitAlertText = alertText;
            exitArrowIndicator = arrow;
            exitDistanceText = distText;
        }
    }
}

