/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 画面左上に配置されるバフアイコン・残り持続時間の表示を行う純粋なHUDビューコンポーネント。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面左上に配置されるバフ情報表示HUD（View）。
    /// ゲームロジックやプレイヤー参照は保持せず、外部から渡されたバフ情報の描画に専念します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuffHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("バフ表示コンテナオブジェクト（表示/非表示切り替え用）")]
        [SerializeField] private GameObject contentRoot;

        [Tooltip("バフアイコン画像コンポーネント")]
        [SerializeField] private Image iconImage;

        [Tooltip("残り時間のクールダウンオーバーレイ画像（Filledタイプ）")]
        [SerializeField] private Image cooldownOverlayImage;

        [Tooltip("残り効果時間テキスト")]
        [SerializeField] private TextMeshProUGUI durationText;

        /// <summary>現在バフアイコンが表示中かどうか</summary>
        public bool IsShowing => contentRoot != null ? contentRoot.activeSelf : gameObject.activeSelf;

        /// <summary>
        /// 初期表示状態の設定を行う。
        /// </summary>
        private void Awake()
        {
            SetVisible(false);
        }

        /// <summary>
        /// バフの残り時間および総持続時間を設定し、表示を更新する。
        /// </summary>
        /// <param name="remainingDuration">残り効果時間（秒）</param>
        /// <param name="totalDuration">総効果持続時間（秒）</param>
        public void SetBuff(float remainingDuration, float totalDuration)
        {
            if (remainingDuration <= 0f)
            {
                ClearBuff();
                return;
            }

            if (!IsShowing)
            {
                SetVisible(true);
            }

            if (durationText != null)
            {
                durationText.text = $"{remainingDuration:F1}s";
            }

            if (cooldownOverlayImage != null && totalDuration > 0f)
            {
                cooldownOverlayImage.fillAmount = Mathf.Clamp01(1f - (remainingDuration / totalDuration));
            }
        }

        /// <summary>
        /// バフ表示を非表示にする。
        /// </summary>
        public void ClearBuff()
        {
            if (IsShowing)
            {
                SetVisible(false);
            }
        }

        /// <summary>
        /// コード生成時等のUI参照バインドを行う。
        /// </summary>
        /// <param name="root">表示ルートオブジェクト</param>
        /// <param name="icon">アイコンImage</param>
        /// <param name="cooldownOverlay">クールダウンオーバーレイImage</param>
        /// <param name="text">残り時間Text</param>
        public void SetupReferences(GameObject root, Image icon, Image cooldownOverlay, TextMeshProUGUI text)
        {
            contentRoot = root;
            iconImage = icon;
            cooldownOverlayImage = cooldownOverlay;
            durationText = text;
        }

        /// <summary>
        /// 表示コンテナのアクティブ状態を切り替える。
        /// </summary>
        /// <param name="visible">表示するかどうか</param>
        private void SetVisible(bool visible)
        {
            if (contentRoot != null)
            {
                contentRoot.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
