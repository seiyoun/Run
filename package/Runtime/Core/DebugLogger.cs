/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: デバッグログ出力を管理する。
 */

using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Shiyuan.Foundation.Core
{
    public static class DebugLogger
    {
        /// <summary>
        /// DEBUG_LOG シンボルが有効な場合に通常ログを出力する。
        /// </summary>
        [Conditional("DEBUG_LOG")]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// DEBUG_LOG シンボルが有効な場合に警告ログを出力する。
        /// </summary>
        [Conditional("DEBUG_LOG")]
        public static void Warning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// DEBUG_LOG シンボルが有効な場合にエラーログを出力する。
        /// </summary>
        [Conditional("DEBUG_LOG")]
        public static void Error(string message)
        {
            Debug.LogError(message);
        }

        /// <summary>
        /// DEBUG_LOG シンボルが有効な場合に例外ログを出力する。
        /// </summary>
        [Conditional("DEBUG_LOG")]
        public static void Exception(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
