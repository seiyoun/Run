/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: TextMeshPro にローカライズ済みテキストを設定する。
 */

using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Shiyuan.Foundation.Localization
{
    [DisallowMultipleComponent]
    public sealed class LocalizedTMPText : MonoBehaviour
    {
        private TMP_Text targetText;

        [SerializeField]
        private string localizationKey;

        private bool isDestroyed;

        /// <summary>
        /// TextMeshPro の参照を取得する。
        /// </summary>
        private void Awake()
        {
            targetText = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// ロケール変更を購読し、表示テキストを更新する。
        /// </summary>
        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            ApplyLocalizedTextAsync();
        }

        /// <summary>
        /// ロケール変更の購読を解除する。
        /// </summary>
        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        /// <summary>
        /// Inspector 変更時に TextMeshPro の参照を取得し、表示テキストを更新する。
        /// </summary>
        private void OnValidate()
        {
            targetText = GetComponent<TMP_Text>();

            if (Application.isPlaying && isActiveAndEnabled)
            {
                ApplyLocalizedTextAsync();
            }
        }

        /// <summary>
        /// 外部からローカライズキーを設定し、表示テキストを更新する。
        /// </summary>
        public void SetKey(string key)
        {
            localizationKey = key;
            ApplyLocalizedTextAsync();
        }

        /// <summary>
        /// 現在設定されているキーで表示テキストを更新する。
        /// </summary>
        public void Refresh()
        {
            ApplyLocalizedTextAsync();
        }

        /// <summary>
        /// TextMeshPro の参照を同じ GameObject から取得する。
        /// </summary>
        private void ResolveTargetText()
        {
            targetText = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// ロケール変更時に表示テキストを更新する。
        /// </summary>
        private void OnSelectedLocaleChanged(Locale locale)
        {
            ApplyLocalizedTextAsync();
        }

        /// <summary>
        /// LocalizationManager の初期化を待ってから TextMeshPro に文字列を反映する。
        /// </summary>
        private async void ApplyLocalizedTextAsync()
        {
            try
            {
                await WaitForLocalizationAsync();

                if (isDestroyed || !isActiveAndEnabled)
                {
                    return;
                }

                ApplyLocalizedText();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
#if DEBUG_LOG
                Debug.LogException(exception);
#endif
            }
        }

        /// <summary>
        /// LocalizationManager の初期化完了まで待機する。
        /// </summary>
        private async Task WaitForLocalizationAsync()
        {
            while (LocalizationManager.Instance == null)
            {
                if (isDestroyed || !isActiveAndEnabled)
                {
                    throw new OperationCanceledException();
                }

                await Task.Yield();
            }

            await LocalizationManager.Instance.InitializeAsync();
        }

        /// <summary>
        /// 設定キーからローカライズ済み文字列を取得して TextMeshPro に設定する。
        /// </summary>
        private void ApplyLocalizedText()
        {
            ResolveTargetText();

            if (targetText == null)
            {
                return;
            }

            if (LocalizationManager.Instance == null)
            {
                return;
            }

            targetText.text = LocalizationManager.Instance.GetText(localizationKey);
        }

        /// <summary>
        /// 破棄済み状態を記録する。
        /// </summary>
        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            isDestroyed = true;
        }
    }
}
