/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム画面上部に配置される経験値バー（EXP Bar）UI コンポーネント。
 *                プレイヤーの経験値獲得とレベルアップを滑らかなゲージアニメーションで描画します。
 */

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面上部の経験値バーおよびレベル表示を管理する UI クラス。
    /// PlayerExperience のイベントを購読し、リアルタイムにゲージと数値を更新します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpBarView : MonoBehaviour
    {
        [Header("UI Component References")]
        [Tooltip("経験値ゲージの Fill Image (Image Type = Filled)")]
        [SerializeField] private Image fillImage;

        [Tooltip("レベル表示テキスト (例: 'Lv.1')")]
        [SerializeField] private TextMeshProUGUI levelText;

        [Tooltip("経験値量表示テキスト (例: '10 / 50')")]
        [SerializeField] private TextMeshProUGUI expText;

        [Header("Animation Settings")]
        [Tooltip("ゲージ補間速度")]
        [SerializeField] private float smoothSpeed = 10f;

        private PlayerExperience boundExperience;
        private float targetFillAmount = 0f;
        private float currentFillAmount = 0f;

        private void Awake()
        {
            // 初期表示
            if (levelText != null) levelText.text = "Lv.1";
            if (expText != null) expText.text = "0 / 10";
            if (fillImage != null) fillImage.fillAmount = 0f;
        }

        private void Start()
        {
            TryBindPlayerExperience();
        }

        private void Update()
        {
            if (boundExperience == null)
            {
                TryBindPlayerExperience();
            }

            // ゲージの滑らかな Lerp アニメーション
            if (fillImage != null)
            {
                if (Mathf.Abs(currentFillAmount - targetFillAmount) > 0.0005f)
                {
                    currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.unscaledDeltaTime * smoothSpeed);
                    fillImage.fillAmount = currentFillAmount;
                }
                else
                {
                    fillImage.fillAmount = targetFillAmount;
                }
            }
        }

        private void OnDestroy()
        {
            UnbindExperience();
        }

        private void TryBindPlayerExperience()
        {
            var player = PlayerController.Instance;
            if (player != null && player.Experience != null)
            {
                BindExperience(player.Experience);
            }
        }

        public void BindExperience(PlayerExperience experience)
        {
            if (boundExperience == experience) return;

            UnbindExperience();

            boundExperience = experience;
            if (boundExperience != null)
            {
                boundExperience.OnExpChanged += HandleExpChanged;
                boundExperience.OnLevelUp += HandleLevelUp;

                // バインド時の現在状態を即時反映
                HandleExpChanged(boundExperience.CurrentExp, boundExperience.RequiredExp, boundExperience.CurrentLevel);
            }
        }

        public void UnbindExperience()
        {
            if (boundExperience != null)
            {
                boundExperience.OnExpChanged -= HandleExpChanged;
                boundExperience.OnLevelUp -= HandleLevelUp;
                boundExperience = null;
            }
        }

        private void HandleExpChanged(int currentExp, int requiredExp, int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{level}";
            }

            if (expText != null)
            {
                expText.text = $"{currentExp} / {requiredExp}";
            }

            targetFillAmount = requiredExp > 0 ? Mathf.Clamp01((float)currentExp / requiredExp) : 0f;

            // レベルアップで目標値が下がった（ラップアラウンドした）場合は 0 から再アニメーション
            if (currentFillAmount > targetFillAmount)
            {
                currentFillAmount = 0f;
            }
        }

        private void HandleLevelUp(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = $"Lv.{newLevel}";
            }
        }
    }
}

