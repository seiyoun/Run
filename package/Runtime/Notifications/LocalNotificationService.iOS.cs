/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: iOS プラットフォームにおけるローカルプッシュ通知の具現クラス。
 */

#if UNITY_IOS
using System;
using UnityEngine;
using Unity.Notifications.iOS;

namespace Shiyuan.Foundation.Notifications
{
    /// <summary>
    /// iOS 向けローカルプッシュ通知を提供する部分クラスの実装。
    /// </summary>
    public sealed partial class LocalNotificationService
    {
        /// <summary>
        /// iOS 向けの通知初期化処理を行います。
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            // iOS では必要に応じて起動時に通知権限リクエスト等をここで行うか、個別に許可申請フローを呼び出します。
            isInitialized = true;
            Debug.Log("LocalNotificationService (iOS) の初期化が完了しました。");
        }

        /// <summary>
        /// 指定したIDと遅延時間後に iOS ローカル通知をスケジュールします（既存の同一IDの通知を上書きします）。
        /// </summary>
        public void Schedule(int id, string title, string body, TimeSpan delay, string channelId = "default_channel")
        {
            if (!isInitialized)
            {
                Initialize();
            }

            var notification = new iOSNotification
            {
                Identifier = id.ToString(),
                Title = title,
                Body = body,
                ShowInForeground = true,
                Trigger = new iOSNotificationTimeIntervalTrigger
                {
                    TimeInterval = delay,
                    Repeats = false
                }
            };

            iOSNotificationCenter.ScheduleNotification(notification);
            Debug.Log($"iOS ローカル通知をスケジュールしました。ID: {id}, Title: {title}, Delay: {delay.TotalSeconds}s");
        }

        /// <summary>
        /// スケジュールされているすべての iOS ローカル通知をキャンセルし、表示済みの通知とバッジもクリアします。
        /// </summary>
        public void CancelAll()
        {
            iOSNotificationCenter.RemoveAllScheduledNotifications();
            iOSNotificationCenter.RemoveAllDeliveredNotifications();
            iOSNotificationCenter.ApplicationBadge = 0;
            Debug.Log("すべての iOS ローカル通知をキャンセルし、表示済み通知とバッジもクリアしました。");
        }
    }
}
#endif
