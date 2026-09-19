/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エラーと例外ログを永続保存する。
 */

#if SANDBOX && DEBUG_LOG

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Shiyuan.Foundation.Core
{
    public static class PersistentExceptionLogWriter
    {
        private const string LogDirectoryName = "ExceptionLogs";
        private static readonly object SyncRoot = new object();

        private static bool isSubscribed;
        private static string logDirectoryPath;
        private static string logFilePath;

        /// <summary>
        /// Unity のログイベントを購読し、エラーと例外を永続保存できる状態にする。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (isSubscribed)
            {
                return;
            }

            EnsureLogDirectoryPath();
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            isSubscribed = true;
        }

        /// <summary>
        /// Unity から通知されたログのうち、エラーと例外だけを永続ログへ出力する。
        /// </summary>
        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception)
            {
                return;
            }

            Write(type, condition, stackTrace);
        }

        /// <summary>
        /// ログ内容を起動ごとのファイルへ追記する。
        /// </summary>
        private static void Write(LogType type, string message, string stackTrace)
        {
            try
            {
                EnsureLogDirectoryPath();

                var now = DateTime.Now;
                var builder = new StringBuilder();
                builder.AppendLine("------------------------------------------------------------");
                builder.AppendLine($"Time: {now:yyyy-MM-dd HH:mm:ss.fff}");
                builder.AppendLine($"Type: {type}");
                builder.AppendLine("Message:");
                builder.AppendLine(message ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(stackTrace))
                {
                    builder.AppendLine("StackTrace:");
                    builder.AppendLine(stackTrace);
                }

                lock (SyncRoot)
                {
                    Directory.CreateDirectory(logDirectoryPath);
                    File.AppendAllText(logFilePath, builder.ToString());
                }
            }
            catch
            {
                // ログ出力失敗時に再帰的なログ発生を避けるため、例外は握りつぶす。
            }
        }

        /// <summary>
        /// 保存先パスが未設定の場合に初期化する。
        /// </summary>
        private static void EnsureLogDirectoryPath()
        {
            if (!string.IsNullOrEmpty(logDirectoryPath) && !string.IsNullOrEmpty(logFilePath))
            {
                return;
            }

            lock (SyncRoot)
            {
                if (!string.IsNullOrEmpty(logDirectoryPath) && !string.IsNullOrEmpty(logFilePath))
                {
                    return;
                }

                logDirectoryPath = Path.Combine(Application.persistentDataPath, LogDirectoryName);
                logFilePath = Path.Combine(logDirectoryPath, $"{DateTime.Now:yyyyMMdd_HHmmss}.log");
            }
        }
    }
}

#endif
