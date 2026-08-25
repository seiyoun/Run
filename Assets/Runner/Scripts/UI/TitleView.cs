/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Title シーンの UI 表示およびボタン操作イベントを制御する。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// Title 画面の UI コンポーネント。スタートボタン押下イベントを発行する。
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        [SerializeField]
        private Button startButton;

        /// <summary>
        /// スタートボタンが押下された際に発火するイベント。
        /// </summary>
        public event Action OnStartClicked;

        private void Awake()
        {
            if (startButton == null)
            {
                startButton = GetComponentInChildren<Button>();
            }

            if (startButton != null)
            {
                startButton.onClick.AddListener(HandleClickStart);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleClickStart);
            }
        }

        private void HandleClickStart()
        {
            DebugLogger.Log("[TitleView] スタートボタンが押下されました。");

            if (startButton != null)
            {
                startButton.interactable = false;
            }

            OnStartClicked?.Invoke();
        }
    }
}
