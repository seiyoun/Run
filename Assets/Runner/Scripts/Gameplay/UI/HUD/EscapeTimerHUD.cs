/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 制限時間のカウント表示および非常口開放・方向ナビゲーションを描画する純粋なHUDビューコンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面中央上部に配置される脱出タイマーおよび非常口開放案内HUD（View）。
    /// ゲーム進行層から渡された残り時間の描画、非常口開放アラート、およびナビゲーション矢印の回転制御に専念します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EscapeTimerHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("タイマー表示テキスト")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Tooltip("ステータス表示テキスト")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("非常口開放アラートバナー")]
        [SerializeField] private GameObject exitAlertBanner;

        [Tooltip("非常口開放アラートテキスト")]
        [SerializeField] private TextMeshProUGUI exitAlertText;

        [Tooltip("非常口方向を示すナビゲーション矢印")]
        [SerializeField] private RectTransform exitArrowIndicator;

        [Tooltip("非常口までの距離表示テキスト")]
        [SerializeField] private TextMeshProUGUI exitDistanceText;

        private bool isExitUnlocked;
        private Transform exitTargetTransform;
        private Transform playerTransform;

        /// <summary>非常口開放状態が表示中かどうか</summary>
        public bool IsExitUnlocked => isExitUnlocked;

        /// <summary>
        /// 初期表示状態の設定を行う。
        /// </summary>
        private void Awake()
        {
            EnsureNavigationUI();
            if (exitAlertBanner != null) exitAlertBanner.SetActive(false);
            if (exitArrowIndicator != null) exitArrowIndicator.gameObject.SetActive(false);

            SetRemainingTime(0f);
        }

        /// <summary>
        /// 非常口開放時のナビゲーション矢印の回転および距離表示を毎フレーム更新する。
        /// </summary>
        private void Update()
        {
            if (isExitUnlocked && exitArrowIndicator != null && exitArrowIndicator.gameObject.activeSelf)
            {
                UpdateExitNavigation();
            }
        }

        /// <summary>
        /// 残り時間を表示に反映する。
        /// </summary>
        /// <param name="seconds">残り秒数</param>
        public void SetRemainingTime(float seconds)
        {
            if (timerText == null) return;

            if (isExitUnlocked)
            {
                timerText.text = "<color=#00FF66><b>脱出せよ！</b></color>";
                return;
            }

            int minutes = Mathf.FloorToInt(Mathf.Max(0f, seconds) / 60f);
            int sec = Mathf.FloorToInt(Mathf.Max(0f, seconds) % 60f);

            string colorTag = seconds < 30f ? "<color=#FF4444>" : "<color=#FFFFFF>";
            timerText.text = $"{colorTag}{minutes:D2}:{sec:D2}</color>";
        }

        /// <summary>
        /// 非常口の開放・閉鎖状態を設定し、UIの表示状態を切り替える。
        /// </summary>
        /// <param name="unlocked">非常口が開放されているかどうか</param>
        public void SetExitUnlocked(bool unlocked)
        {
            isExitUnlocked = unlocked;

            if (isExitUnlocked)
            {
                if (timerText != null)
                {
                    timerText.text = "<color=#00FF66><b>脱出せよ！</b></color>";
                }

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
                    exitArrowIndicator.gameObject.SetActive(exitTargetTransform != null);
                }
            }
            else
            {
                if (exitAlertBanner != null) exitAlertBanner.SetActive(false);
                if (exitArrowIndicator != null) exitArrowIndicator.gameObject.SetActive(false);
                if (exitDistanceText != null) exitDistanceText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 非常口オブジェクトとプレイヤーのTransformを設定し、ナビゲーションを有効化する。
        /// </summary>
        /// <param name="exitTransform">非常口の Transform</param>
        /// <param name="player">プレイヤーの Transform</param>
        public void SetExitTarget(Transform exitTransform, Transform player)
        {
            exitTargetTransform = exitTransform;
            playerTransform = player;
            bool showNavigation = isExitUnlocked && exitTargetTransform != null && playerTransform != null;
            if (exitArrowIndicator != null) exitArrowIndicator.gameObject.SetActive(showNavigation);
            if (exitDistanceText != null) exitDistanceText.gameObject.SetActive(showNavigation);
        }

        /// <summary>
        /// コード生成時等のUI参照バインドを行う。
        /// </summary>
        /// <param name="timer">タイマーテキスト</param>
        /// <param name="status">ステータステキスト</param>
        /// <param name="alertBanner">アラートバナー</param>
        /// <param name="alertText">アラートテキスト</param>
        /// <param name="arrow">矢印Transform</param>
        /// <param name="distText">距離テキスト</param>
        public void SetupReferences(TextMeshProUGUI timer, TextMeshProUGUI status, GameObject alertBanner, TextMeshProUGUI alertText, RectTransform arrow, TextMeshProUGUI distText)
        {
            timerText = timer;
            statusText = status;
            exitAlertBanner = alertBanner;
            exitAlertText = alertText;
            exitArrowIndicator = arrow;
            exitDistanceText = distText;
        }

        /// <summary>
        /// シーンに未設定のナビゲーション表示を既存HUDの親Canvas上に生成する。
        /// </summary>
        private void EnsureNavigationUI()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null || timerText == null) return;

            if (exitArrowIndicator == null)
            {
                var arrowObject = new GameObject("ExitArrowIndicator", typeof(RectTransform));
                exitArrowIndicator = arrowObject.GetComponent<RectTransform>();
                exitArrowIndicator.SetParent(parent, false);
                exitArrowIndicator.anchorMin = new Vector2(0.5f, 1f);
                exitArrowIndicator.anchorMax = new Vector2(0.5f, 1f);
                exitArrowIndicator.anchoredPosition = new Vector2(0f, -165f);
                exitArrowIndicator.sizeDelta = new Vector2(80f, 70f);
                var arrowText = arrowObject.AddComponent<TextMeshProUGUI>();
                arrowText.font = timerText.font;
                arrowText.text = "▲";
                arrowText.fontSize = 52f;
                arrowText.alignment = TextAlignmentOptions.Center;
                arrowText.color = new Color(0f, 1f, 0.53f);
                arrowText.raycastTarget = false;
            }

            if (exitDistanceText == null)
            {
                var distanceObject = new GameObject("ExitDistanceText", typeof(RectTransform));
                var distanceTransform = distanceObject.GetComponent<RectTransform>();
                distanceTransform.SetParent(parent, false);
                distanceTransform.anchorMin = new Vector2(0.5f, 1f);
                distanceTransform.anchorMax = new Vector2(0.5f, 1f);
                distanceTransform.anchoredPosition = new Vector2(0f, -225f);
                distanceTransform.sizeDelta = new Vector2(400f, 42f);
                exitDistanceText = distanceObject.AddComponent<TextMeshProUGUI>();
                exitDistanceText.font = timerText.font;
                exitDistanceText.fontSize = 25f;
                exitDistanceText.alignment = TextAlignmentOptions.Center;
                exitDistanceText.raycastTarget = false;
            }
        }

        /// <summary>
        /// プレイヤーから非常口への方向ベクトルと距離を算出してナビゲーション表示を更新する。
        /// </summary>
        private void UpdateExitNavigation()
        {
            if (exitTargetTransform == null) return;

            var playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
            var exitPos = (Vector2)exitTargetTransform.position;
            var dir = exitPos - playerPos;
            float distance = dir.magnitude;

            if (exitDistanceText != null)
            {
                exitDistanceText.text = $"非常口まで: <color=#00FF88>{distance:F1}m</color>";
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            exitArrowIndicator.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }
}
