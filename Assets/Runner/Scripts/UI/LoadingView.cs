/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: シーン遷移時やデータロード時に最前面で表示するローディングUIを制御する。
 */

using Shiyuan.Foundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 最前面でシーン遷移中やデータ通信中のローディング表示を行うシングルトン UI。
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-980)]
    public sealed class LoadingView : SingletonMonoBehaviour<LoadingView>
    {
        [Header("UI References")]
        [SerializeField]
        private Canvas rootCanvas;

        [SerializeField]
        private CanvasGroup canvasGroup;

        [SerializeField]
        private RectTransform spinnerIcon;

        [SerializeField]
        private TextMeshProUGUI messageText;

        [SerializeField]
        private Slider progressBar;

        [Header("Animation Settings")]
        [SerializeField]
        private float spinnerSpeed = 360f;

        [SerializeField]
        private float fadeDuration = 0.2f;

        [Header("Default Message")]
        [SerializeField]
        private string defaultLoadingMessage = "Loading...";

        /// <summary>
        /// シーン遷移後もローディング UI を保持する。
        /// </summary>
        protected override bool ShouldDontDestroyOnLoad => true;

        private bool isShowing;
        private float targetAlpha;

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance)
            {
                return;
            }

            if (rootCanvas == null)
            {
                rootCanvas = GetComponent<Canvas>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            // 初期状態は描画・レイキャストともに完全にオフにする
            SetVisibleImmediate(false);
            DebugLogger.Log("[LoadingView] ローディング UI が正常に初期化されました。");
        }

        private void Update()
        {
            // スピナー回転
            if (isShowing && spinnerIcon != null)
            {
                spinnerIcon.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);
            }

            // CanvasGroup のフェード制御
            if (canvasGroup != null && !Mathf.Approximately(canvasGroup.alpha, targetAlpha))
            {
                if (fadeDuration <= 0f)
                {
                    canvasGroup.alpha = targetAlpha;
                }
                else
                {
                    var speed = 1f / fadeDuration;
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, speed * Time.unscaledDeltaTime);
                }

                // 完全非表示になったら Canvas とレイキャストを完全に無効化
                if (targetAlpha <= 0f && canvasGroup.alpha <= 0.001f)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                    if (rootCanvas != null)
                    {
                        rootCanvas.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// ローディング画面を表示する。
        /// </summary>
        /// <param name="message">表示するカスタムメッセージ（省略時はデフォルト）</param>
        public void Show(string message = null)
        {
            isShowing = true;
            targetAlpha = 1f;

            if (rootCanvas != null)
            {
                rootCanvas.enabled = true;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                if (fadeDuration <= 0f)
                {
                    canvasGroup.alpha = 1f;
                }
            }

            if (messageText != null)
            {
                messageText.text = !string.IsNullOrEmpty(message) ? message : defaultLoadingMessage;
            }

            if (progressBar != null)
            {
                progressBar.value = 0f;
            }

            DebugLogger.Log($"[LoadingView] Show() 実行: message='{messageText?.text}'");
        }

        /// <summary>
        /// ローディング画面を非表示にする。
        /// </summary>
        public void Hide()
        {
            isShowing = false;
            targetAlpha = 0f;

            // 非表示処理開始時点で即座にレイキャストを解放し、背後UIのクリックを妨げないようにする
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (fadeDuration <= 0f)
            {
                SetVisibleImmediate(false);
            }

            DebugLogger.Log("[LoadingView] Hide() 実行");
        }

        /// <summary>
        /// 進捗率（0.0 〜 1.0）を設定する。
        /// </summary>
        public void SetProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(progress);
            }
        }

        /// <summary>
        /// ローディングメッセージを更新する。
        /// </summary>
        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        /// <summary>
        /// 即時で表示・非表示を切り替える。
        /// </summary>
        public void SetVisibleImmediate(bool visible)
        {
            targetAlpha = visible ? 1f : 0f;
            isShowing = visible;

            if (rootCanvas != null)
            {
                rootCanvas.enabled = visible;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
                canvasGroup.blocksRaycasts = visible;
                canvasGroup.interactable = visible;
            }
        }
    }
}
