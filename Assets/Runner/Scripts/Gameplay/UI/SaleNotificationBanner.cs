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
        [Tooltip("バナー全体のRectTransform")]
        [SerializeField] private RectTransform bannerRoot;

        [Tooltip("バナー全体のタップ判定ボタン")]
        [SerializeField] private Button bannerButton;

        [Tooltip("セールタイトルテキスト")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("セールメッセージテキスト")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Tooltip("セール終了カウントダウンテキスト")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Animation Settings")]
        [Tooltip("スライドアニメーションの補間速度")]
        [SerializeField] private float slideSpeed = 8f;

        [Tooltip("バナーの表示持続秒数（0以下の場合はタップされるまで無制限に常時表示）")]
        [SerializeField] private float displayDuration = 0f;

        [Tooltip("非表示時のアンカー座標")]
        [SerializeField] private Vector2 hiddenPosition = new Vector2(0, 180);

        [Tooltip("表示時のアンカー座標")]
        [SerializeField] private Vector2 visiblePosition = new Vector2(0, -70);

        private bool isShowing;
        private float remainingDisplayTime;
        private Vector2 targetPosition;

        /// <summary>バナーが表示中かどうか</summary>
        public bool IsShowing => isShowing;

        /// <summary>バナーがタップされた際のコールバック</summary>
        public event Action OnBannerClicked;

        /// <summary>バナー表示時間が満了した際のコールバック</summary>
        public event Action OnBannerExpired;

        /// <summary>
        /// コンポーネントの初期化を行う。
        /// </summary>
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

        /// <summary>
        /// 毎フレームのアニメーション補間とタイマー更新を行う。
        /// </summary>
        private void Update()
        {
            if (bannerRoot != null)
            {
                bannerRoot.anchoredPosition = Vector2.Lerp(bannerRoot.anchoredPosition, targetPosition, Time.unscaledDeltaTime * slideSpeed);
            }

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
        /// アイテム入荷通知バナーを表示する。
        /// </summary>
        /// <param name="duration">表示秒数（負数の場合はインスペクタ設定値、0以下の場合はタップされるまで無制限表示）</param>
        /// <param name="title">タイトル</param>
        /// <param name="message">メッセージ本文</param>
        public void ShowBanner(float duration = -1f, string title = "【新着アイテム入荷！】", string message = "おすすめアイテムが入荷しました！タップしてチェック ▶")
        {
            float actualDuration = duration >= 0f ? duration : displayDuration;
            isShowing = true;
            remainingDisplayTime = actualDuration;
            targetPosition = visiblePosition;

            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            if (timerText != null)
            {
                timerText.text = actualDuration > 0f ? $"終了まで {Mathf.CeilToInt(actualDuration)}秒" : "タップして確認 ▶";
            }

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

        /// <summary>
        /// プロシージャルUI生成時に参照を一括設定する。
        /// </summary>
        /// <param name="root">バナールートRectTransform</param>
        /// <param name="button">バナーボタン</param>
        /// <param name="title">タイトルテキスト</param>
        /// <param name="message">メッセージテキスト</param>
        /// <param name="timer">タイマーテキスト</param>
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

        /// <summary>
        /// バナータップ時の内部処理を実行する。
        /// </summary>
        private void HandleClick()
        {
            HideBanner();
            OnBannerClicked?.Invoke();
        }
    }
}

