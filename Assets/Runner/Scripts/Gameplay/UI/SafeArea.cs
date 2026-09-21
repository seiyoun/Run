/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: uGUI の RectTransform をデバイスの Screen.safeArea に合わせて自動調整するコンポーネント。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// アタッチされた RectTransform のアンカーをデバイスの Safe Area（安全領域）に自動追従させる UI コンポーネント。
    /// ノッチやホームバーなどによる UI の見切れ・重なりを防止します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform targetRectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        /// <summary>
        /// RectTransform の参照キャッシュおよび初回 Safe Area 適用を行う。
        /// </summary>
        private void Awake()
        {
            targetRectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        /// <summary>
        /// 画面サイズや Safe Area の変更を検知してアンカーを更新する。
        /// </summary>
        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        /// <summary>
        /// 現在の Screen.safeArea を RectTransform のアンカー（anchorMin, anchorMax）に反映する。
        /// </summary>
        public void ApplySafeArea()
        {
            if (targetRectTransform == null)
            {
                targetRectTransform = GetComponent<RectTransform>();
            }

            if (Screen.width <= 0 || Screen.height <= 0) return;

            var safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            targetRectTransform.anchorMin = anchorMin;
            targetRectTransform.anchorMax = anchorMax;
            targetRectTransform.offsetMin = Vector2.zero;
            targetRectTransform.offsetMax = Vector2.zero;
        }
    }
}

