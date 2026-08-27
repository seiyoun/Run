/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 一定額ポイントが貯まった際に画面上部にスライドインするスマホ風「タイムセール通知」バナーUI。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// スマートフォンのプッシュ通知風タイムセールバナー。
    /// タップされるとショップモーダルを開くイベントを発火します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SaleNotificationBanner : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform bannerRoot;
        [SerializeField] private Button bannerButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Animation Settings")]
        [SerializeField] private float slideSpeed = 8f;
        [SerializeField] private float displayDuration = 15f;
        [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 180);
        [SerializeField] private Vector2 visiblePosition = new Vector2(0, -70);

        private bool isShowing = false;
        private float remainingDisplayTime = 0f;
        private Vector2 targetPosition;

        public bool IsShowing => isShowing;

        public event Action OnBannerClicked;
        public event Action OnBannerExpired;

        private void Awake()
        {
            if (bannerRoot == null) bannerRoot = (RectTransform)transform;
            targetPosition = hiddenPosition;
            bannerRoot.anchoredPosition = hiddenPosition;

            if (bannerButton != null)
            {
                bannerButton.onClick.AddListener(HandleClick);
            }
        }

        private void Update()
        {
            // 位置のスムーズ補間
            if (bannerRoot != null)
            {
                bannerRoot.anchoredPosition = Vector2.Lerp(bannerRoot.anchoredPosition, targetPosition, Time.unscaledDeltaTime * slideSpeed);
            }

            // 表示カウントダウン
            if (isShowing && remainingDisplayTime > 0f)
            {
                remainingDisplayTime -= Time.unscaledDeltaTime;
                if (timerText != null)
                {
                    timerText.text = $"終了まで {Mathf.CeilToInt(remainingDisplayTime)}秒";
                }

                if (remainingDisplayTime <= 0f)
                {
                    HideBanner();
                    OnBannerExpired?.Invoke();
                }
            }
        }

        /// <summary>
        /// タイムセール通知バナーを表示する。
        /// </summary>
        /// <param name="duration">表示秒数</param>
        /// <param name="title">タイトル</param>
        /// <param name="message">メッセージ本文</param>
        public void ShowBanner(float duration = 15f, string title = "⚡️ ゲリラタイムセール開催中！", string message = "限定アイテム入荷！今すぐタップしてチェック ➔")
        {
            isShowing = true;
            remainingDisplayTime = duration;
            targetPosition = visiblePosition;

            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            if (timerText != null) timerText.text = $"終了まで {Mathf.CeilToInt(duration)}秒";

            gameObject.SetActive(true);
        }

        /// <summary>
        /// バナーを非表示にする。
        /// </summary>
        public void HideBanner()
        {
            isShowing = false;
            remainingDisplayTime = 0f;
            targetPosition = hiddenPosition;
        }

        private void HandleClick()
        {
            HideBanner();
            OnBannerClicked?.Invoke();
        }

        public void SetupReferences(RectTransform root, Button button, TextMeshProUGUI title, TextMeshProUGUI message, TextMeshProUGUI timer)
        {
            bannerRoot = root;
            bannerButton = button;
            titleText = title;
            messageText = message;
            timerText = timer;

            if (bannerButton != null)
            {
                bannerButton.onClick.RemoveAllListeners();
                bannerButton.onClick.AddListener(HandleClick);
            }
        }
    }
}

