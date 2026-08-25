/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクターの頭上に追従して HP ゲージを表示する World Space UI コンポーネント。
 */

using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// キャラクター（プレイヤーや敵）の頭上に配置される頭上 HP バー。
    /// 親オブジェクトの ICharacterStatus と連動して滑らかに増減します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingHpBar : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("HP バーの塗りつぶし画像（Image Type: Filled）")]
        [SerializeField]
        private Image fillImage;

        [Tooltip("HP バーの背景画像")]
        [SerializeField]
        private Image backgroundImage;

        [Header("Display Settings")]
        [Tooltip("HPが満タンのときでも常に表示するか")]
        [SerializeField]
        private bool alwaysShow = true;

        [Tooltip("頭上からのローカルオフセット")]
        [SerializeField]
        private Vector3 offset = new Vector3(0f, 0.75f, 0f);

        [Header("Colors")]
        [SerializeField]
        private Color highHpColor = new Color(0.2f, 0.85f, 0.4f, 1f);

        [SerializeField]
        private Color midHpColor = new Color(0.95f, 0.75f, 0.15f, 1f);

        [SerializeField]
        private Color lowHpColor = new Color(0.9f, 0.25f, 0.2f, 1f);

        [Header("Animation")]
        [SerializeField]
        private float fillLerpSpeed = 10f;

        private ICharacterStatus boundStatus;
        private float targetFill = 1f;
        private float currentFill = 1f;
        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            transform.localPosition = offset;
        }

        private void Start()
        {
            // 親オブジェクトから ICharacterStatus を自動取得してバインド
            if (boundStatus == null)
            {
                var status = GetComponentInParent<ICharacterStatus>();
                if (status != null)
                {
                    Bind(status);
                }
            }
        }

        private void OnDestroy()
        {
            Unbind();
        }

        /// <summary>
        /// 対象の ICharacterStatus とバインドする。
        /// </summary>
        public void Bind(ICharacterStatus status)
        {
            if (boundStatus != null)
            {
                Unbind();
            }

            boundStatus = status;
            if (boundStatus != null)
            {
                boundStatus.OnHpChanged += HandleHpChanged;
                HandleHpChanged(boundStatus.CurrentHp, boundStatus.MaxHp);
            }
        }

        /// <summary>
        /// バインドを解除する。
        /// </summary>
        public void Unbind()
        {
            if (boundStatus != null)
            {
                boundStatus.OnHpChanged -= HandleHpChanged;
                boundStatus = null;
            }
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            targetFill = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;

            if (!alwaysShow && canvas != null)
            {
                canvas.enabled = currentHp < maxHp && currentHp > 0;
            }
        }

        private void LateUpdate()
        {
            // 親オブジェクトの向き反転や回転の影響を受けないよう、頭上バーの向きとスケールを固定
            transform.localPosition = offset;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.01f;

            // ゲージの滑らかな追従アニメーション
            if (fillImage != null)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillLerpSpeed);
                fillImage.fillAmount = currentFill;

                // 残量に応じた色の変化
                if (currentFill > 0.5f)
                {
                    fillImage.color = Color.Lerp(midHpColor, highHpColor, (currentFill - 0.5f) * 2f);
                }
                else
                {
                    fillImage.color = Color.Lerp(lowHpColor, midHpColor, currentFill * 2f);
                }
            }
        }
    }
}
