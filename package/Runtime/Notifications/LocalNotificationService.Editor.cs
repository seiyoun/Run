/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エディタおよびその他の未サポートプラットフォームにおけるローカルプッシュ通知のモック（ダミー）クラス。
 */

#if !UNITY_ANDROID && !UNITY_IOS
using System;
using UnityEngine;

namespace Shiyuan.Foundation.Notifications
{
    /// <summary>
    /// エディタ環境向けローカルプッシュ通知のダミー挙動を提供する部分クラスの実装。
    /// </summary>
    public sealed partial class LocalNotificationService
    {
        /// <summary>
        /// エディタ向けの通知初期化（ダミーログ出力のみ）を行います。
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            Debug.Log("LocalNotificationService (Mock) の初期化が完了しました。");
        }

        /// <summary>
        /// 指定したIDと遅延時間後にエディタ向け（Mock）ローカル通知のダミーログを出力します。
        /// </summary>
        public void Schedule(int id, string title, string body, TimeSpan delay, string channelId = "default_channel")
        {
            if (!isInitialized)
            {
                Initialize();
            }

            Debug.Log($"[Mock] ローカル通知をスケジュールしました。ID: {id}, Title: {title}, Body: {body}, Delay: {delay.TotalSeconds}s");
        }

        /// <summary>
        /// エディタ向け（Mock）ローカル通知のキャンセルをシミュレートします。
        /// </summary>
        public void CancelAll()
        {
            Debug.Log("[Mock] すべてのローカル通知をキャンセルしました。");
        }
    }
}
#endif
