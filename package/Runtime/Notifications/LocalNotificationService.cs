/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: iOS および Android のローカルプッシュ通知を一元管理する（共通ベース定義）。
 */

using System;

namespace Shiyuan.Foundation.Notifications
{
    /// <summary>
    /// モバイル端末向けローカルプッシュ通知を提供する部分クラス（共通定義部）。
    /// </summary>
    public sealed partial class LocalNotificationService : ILocalNotificationService
    {
        private static readonly Lazy<LocalNotificationService> instance =
            new Lazy<LocalNotificationService>(() => new LocalNotificationService());

        /// <summary>
        /// LocalNotificationService のシングルトンインスタンスを取得します。
        /// </summary>
        public static LocalNotificationService Instance => instance.Value;

        private bool isInitialized;
    }
}
