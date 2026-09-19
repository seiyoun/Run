/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ローカルプッシュ通知サービスのインターフェースを定義する。
 */

using System;

namespace Shiyuan.Foundation.Notifications
{
    /// <summary>
    /// モバイル端末向けローカルプッシュ通知を制御するサービスのインターフェース。
    /// </summary>
    public interface ILocalNotificationService
    {
        /// <summary>
        /// 通知サービスを初期化し、必要な通知チャンネル（Androidのみ）などを登録します。
        /// </summary>
        void Initialize();

        /// <summary>
        /// 指定したIDと遅延時間後にローカル通知をスケジュールします。
        /// 同じIDを指定した場合、既存のスケジュールは上書きされます。
        /// </summary>
        /// <param name="id">通知を一意に識別するID</param>
        /// <param name="title">通知のタイトル</param>
        /// <param name="body">通知の本文</param>
        /// <param name="delay">現在時刻からの遅延時間</param>
        /// <param name="channelId">通知のグループ化に使用するチャンネルID（Androidのみ有効）</param>
        void Schedule(int id, string title, string body, TimeSpan delay, string channelId = "default_channel");

        /// <summary>
        /// スケジュールされているすべてのローカル通知をキャンセルします。
        /// </summary>
        void CancelAll();
    }
}
