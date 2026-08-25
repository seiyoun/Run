/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Home シーンの UI 表示およびゲーム開始ボタンイベントを制御する。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// Home 画面の UI コンポーネント。ゲーム開始ボタン押下イベントを発行する。
    /// </summary>
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField]
        private Button playButton;

        /// <summary>
        /// ゲーム開始ボタンが押下された際に発火するイベント。
        /// </summary>
        public event Action OnPlayClicked;

        private void Awake()
        {
            if (playButton == null)
            {
                playButton = GetComponentInChildren<Button>();
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(HandleClickPlay);
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandleClickPlay);
            }
        }

        private void HandleClickPlay()
        {
            DebugLogger.Log("[HomeView] ゲーム開始ボタンが押下されました。");

            if (playButton != null)
            {
                playButton.interactable = false;
            }

            OnPlayClicked?.Invoke();
        }
    }
}
