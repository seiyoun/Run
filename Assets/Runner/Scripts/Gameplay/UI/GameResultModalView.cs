/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム終了（GameOver/Result）時のリザルト画面モーダルUI。
 *                Addressables プレハブとして管理され、タイトル、結果テキスト、OKボタンの表示・操作を制御します。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// ゲームオーバーおよびリザルト表示を行うモーダルウィンドウビュー。
    /// Addressables からインスタンス化され、タイトル、結果テキスト（歩数・ポイント等）、OKボタンの表示を制御します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameResultModalView : MonoBehaviour
    {
        /// <summary>
        /// 現在アクティブなリザルトモーダルインスタンス。
        /// </summary>
        public static GameResultModalView Instance { get; private set; }

        private Action onOkAction;

        [Header("UI References")]
        [Tooltip("モーダルウィンドウ全体のルートGameObject")]
        [SerializeField] private GameObject modalRoot;

        [Tooltip("タイトル表示テキスト")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("リザルト詳細（歩数・ポイント等）表示テキスト")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Tooltip("OK（ホームへ戻る）ボタン")]
        [SerializeField] private Button okButton;

        private Shiyuan.Foundation.Addressables.AddressablePrefabLoader prefabLoader;

        /// <summary>リザルトモーダルが表示中かどうか</summary>
        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        /// <summary>
        /// コンポーネントの初期化を行い、OKボタンリスナーを登録する。
        /// </summary>
        private void Awake()
        {
            Instance = this;

            if (okButton != null)
            {
                okButton.onClick.AddListener(HandleOkClicked);
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        /// <summary>
        /// オブジェクト破棄時のリスナー解除およびAddressablesローダーの破棄を行う。
        /// </summary>
        private void OnDestroy()
        {
            if (okButton != null)
            {
                okButton.onClick.RemoveListener(HandleOkClicked);
            }

            prefabLoader?.Dispose();
            prefabLoader = null;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// プレハブを生成したAddressablePrefabLoaderをバインドし、破棄時のクリーンアップを委託する。
        /// </summary>
        /// <param name="loader">生成元AddressablePrefabLoader</param>
        public void BindLoader(Shiyuan.Foundation.Addressables.AddressablePrefabLoader loader)
        {
            prefabLoader = loader;
        }

        /// <summary>
        /// リザルトモーダルを表示し、タイトル・結果テキスト・OK押下時のコールバックを設定する。
        /// </summary>
        /// <param name="title">タイトル文字列（例: GAME OVER）</param>
        /// <param name="message">詳細結果メッセージ</param>
        /// <param name="onOkClicked">OKボタン押下時のコールバック</param>
        public void Show(string title, string message, Action onOkClicked = null)
        {
            onOkAction = onOkClicked;

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            gameObject.SetActive(true);
            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            transform.SetAsLastSibling();
        }

        /// <summary>
        /// リザルトモーダルを非表示にする。
        /// </summary>
        public void Hide()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            onOkAction = null;
        }

        /// <summary>
        /// OKボタン押下時の内部イベントハンドラ。
        /// </summary>
        private void HandleOkClicked()
        {
            var action = onOkAction;
            Hide();
            action?.Invoke();
        }
    }
}
