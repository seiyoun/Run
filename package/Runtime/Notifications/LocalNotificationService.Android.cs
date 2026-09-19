/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Android プラットフォームにおけるローカルプッシュ通知の具現クラス。
 */

#if UNITY_ANDROID
using System;
using UnityEngine;
using Unity.Notifications.Android;

namespace Shiyuan.Foundation.Notifications
{
    /// <summary>
    /// Android 向けローカルプッシュ通知を提供する部分クラスの実装。
    /// </summary>
    public sealed partial class LocalNotificationService
    {
        /// <summary>
        /// Android 向けの通知初期化処理。デフォルト通知チャンネルの登録を行います。
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            RegisterAndroidChannel("default_channel", "通常通知", "アプリからの一般的な通知です。");
            isInitialized = true;
            Debug.Log("LocalNotificationService (Android) の初期化が完了しました。");
        }

        /// <summary>
        /// 指定したIDと遅延時間後に Android ローカル通知をスケジュールします（既存の同一IDの通知を上書きします）。
        /// </summary>
        public void Schedule(int id, string title, string body, TimeSpan delay, string channelId = "default_channel")
        {
            if (!isInitialized)
            {
                Initialize();
            }

            var fireTime = DateTime.Now + delay;
            var notification = new AndroidNotification
            {
                Title = title,
                Text = body,
                FireTime = fireTime,
                SmallIcon = "icon_0",
                LargeIcon = "icon_1"
            };

            AndroidNotificationCenter.SendNotificationWithExplicitID(notification, channelId, id);
            Debug.Log($"Android ローカル通知をスケジュールしました。ID: {id}, Title: {title}, FireTime: {fireTime}");
        }

        /// <summary>
        /// スケジュールされているすべての Android ローカル通知をキャンセルします。
        /// </summary>
        public void CancelAll()
        {
            AndroidNotificationCenter.CancelAllNotifications();
            Debug.Log("すべての Android ローカル通知をキャンセルしました。");
        }

        /// <summary>
        /// Android 8.0 以上で必須となる通知チャンネルを登録します。
        /// </summary>
        private void RegisterAndroidChannel(string id, string name, string description)
        {
            var channel = new AndroidNotificationChannel
            {
                Id = id,
                Name = name,
                Importance = Importance.Default,
                Description = description
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
            Debug.Log($"Android 通知チャンネルを登録しました。ID: {id}");
        }
    }
}
#endif
