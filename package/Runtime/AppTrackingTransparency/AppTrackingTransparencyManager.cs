/*
 * Author: shiyuan.jin
 * Contact: shiyuan0106bot@gmail.com
 * Description: Manager to request App Tracking Transparency (ATT) authorization on iOS.
 */

using System;
using System.Runtime.InteropServices;
using AOT;

namespace Shiyuan.Foundation.AppTrackingTransparency
{
    /// <summary>
    /// iOS の App Tracking Transparency (ATT) トラッキング許可要求を管理するクラス。
    /// </summary>
    public static class AppTrackingTransparencyManager
    {
#if UNITY_IOS && !UNITY_EDITOR
        private delegate void ATTCallback(int status);

        [DllImport("__Internal")]
        private static extern void RequestTrackingAuthorization(ATTCallback callback);

        [DllImport("__Internal")]
        private static extern int GetTrackingAuthorizationStatus();

        private static Action<AuthorizationStatus> _onCompletedCallback;

        [MonoPInvokeCallback(typeof(ATTCallback))]
        private static void OnATTCompleted(int status)
        {
            if (_onCompletedCallback != null)
            {
                var callback = _onCompletedCallback;
                _onCompletedCallback = null;
                
                // Unityのメインスレッドで実行されるように呼び出す
                callback.Invoke((AuthorizationStatus)status);
            }
        }
#endif

        /// <summary>
        /// iOS のトラッキング許可ステータス。
        /// </summary>
        public enum AuthorizationStatus
        {
            NotDetermined = 0,
            Restricted = 1,
            Denied = 2,
            Authorized = 3,
            Unsupported = -1 // iOS 以外のプラットフォームやエディタ環境
        }

        /// <summary>
        /// 現在のトラッキング許可ステータスを取得する。
        /// </summary>
        public static AuthorizationStatus GetStatus()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                return (AuthorizationStatus)GetTrackingAuthorizationStatus();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return AuthorizationStatus.Unsupported;
            }
#else
            return AuthorizationStatus.Unsupported;
#endif
        }

        /// <summary>
        /// トラッキング許可ダイアログを要求する。
        /// </summary>
        public static void RequestAuthorization(Action<AuthorizationStatus> onCompleted)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                _onCompletedCallback = onCompleted;
                RequestTrackingAuthorization(OnATTCompleted);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                onCompleted?.Invoke(AuthorizationStatus.Unsupported);
            }
#else
            onCompleted?.Invoke(AuthorizationStatus.Unsupported);
#endif
        }
    }
}
