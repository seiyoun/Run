/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: アプリ全体で使用するローカライズ文字列の初期化と取得を管理する。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;

namespace Shiyuan.Foundation.Localization
{
    public sealed class LocalizationManager : SingletonMonoBehaviour<LocalizationManager>
    {
        private const string DefaultTableName = "Localization";
        private const string LocalePreferenceKey = "Hero.Localization.LocaleCode";

        [SerializeField]
        private string tableName = DefaultTableName;

        private readonly object initializeLock = new object();
        private CancellationTokenSource destroyCancellationTokenSource;
        private Task initializeTask;
        private StringTable loadedStringTable;
        private Locale loadedLocale;
        private bool isApplyingSavedLocale;

        public bool IsInitialized { get; private set; }

        public string CurrentLocaleCode => LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : PlayerPrefs.GetString(LocalePreferenceKey, string.Empty);

        /// <summary>
        /// インスタンスを登録し、ロケール変更時の再読み込みを購読する。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance)
            {
                return;
            }

            destroyCancellationTokenSource = new CancellationTokenSource();
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            StartInitialize();
        }

        /// <summary>
        /// Unity Localization を初期化し、既定の文字列テーブルを読み込む。
        /// </summary>
        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            lock (initializeLock)
            {
                if (initializeTask == null || initializeTask.IsFaulted || initializeTask.IsCanceled)
                {
                    initializeTask = InitializeInternalAsync(cancellationToken);
                }

                return initializeTask;
            }
        }

        /// <summary>
        /// キーに対応するローカライズ済みテキストを同期取得する。
        /// </summary>
        public string GetText(string key, params object[] arguments)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (!IsInitialized || loadedStringTable == null)
            {
                return key;
            }

            var entry = loadedStringTable.GetEntry(key);
            if (entry == null)
            {
                return key;
            }

            return arguments == null || arguments.Length == 0
                ? entry.GetLocalizedString()
                : entry.GetLocalizedString(arguments);
        }

        /// <summary>
        /// 初期化済みのロケール一覧から言語コードを指定してロケールを切り替える。
        /// </summary>
        public bool ChangeLocale(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return false;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                return false;
            }

            ChangeLocale(locale);
            return true;
        }

        /// <summary>
        /// 起動時のローカライズ初期化を開始し、失敗時にログを出力する。
        /// </summary>
        private async void StartInitialize()
        {
            try
            {
                await InitializeAsync();
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
        /// Unity Localization の初期化と文字列テーブルの読み込みを実行する。
        /// </summary>
        private async Task InitializeInternalAsync(CancellationToken cancellationToken)
        {
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                destroyCancellationTokenSource.Token,
                cancellationToken);

            var token = linkedCancellationTokenSource.Token;
            await WaitForOperationAsync(LocalizationSettings.InitializationOperation, token);
            ApplySavedLocale();

            var targetTableName = string.IsNullOrWhiteSpace(tableName) ? DefaultTableName : tableName;
            var tableOperation = LocalizationSettings.StringDatabase.GetTableAsync(targetTableName);
            loadedStringTable = await WaitForOperationAsync(tableOperation, token);
            loadedLocale = LocalizationSettings.SelectedLocale;
            IsInitialized = loadedStringTable != null;
        }

        /// <summary>
        /// 保存済みの言語コードがある場合に選択ロケールへ反映する。
        /// </summary>
        private void ApplySavedLocale()
        {
            var savedLocaleCode = PlayerPrefs.GetString(LocalePreferenceKey, string.Empty);
            if (string.IsNullOrWhiteSpace(savedLocaleCode))
            {
                return;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(savedLocaleCode);
            if (locale == null || LocalizationSettings.SelectedLocale == locale)
            {
                return;
            }

            isApplyingSavedLocale = true;
            try
            {
                LocalizationSettings.SelectedLocale = locale;
            }
            finally
            {
                isApplyingSavedLocale = false;
            }
        }

        /// <summary>
        /// 指定されたロケールを選択状態へ反映し、次回起動用に保存する。
        /// </summary>
        private static void ChangeLocale(Locale locale)
        {
            if (locale == null)
            {
                throw new ArgumentNullException(nameof(locale), "ロケールが未設定です。");
            }

            SaveLocaleCode(locale.Identifier.Code);

            if (LocalizationSettings.SelectedLocale != locale)
            {
                LocalizationSettings.SelectedLocale = locale;
            }
        }

        /// <summary>
        /// 言語コードを次回起動用に保存する。
        /// </summary>
        private static void SaveLocaleCode(string localeCode)
        {
            PlayerPrefs.SetString(LocalePreferenceKey, localeCode);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// ロケール変更時に読み込み済み文字列テーブルを更新する。
        /// </summary>
        private void OnSelectedLocaleChanged(Locale locale)
        {
            if (!IsPrimaryInstance)
            {
                return;
            }

            if (locale != null)
            {
                SaveLocaleCode(locale.Identifier.Code);
            }

            if (isApplyingSavedLocale)
            {
                return;
            }

            lock (initializeLock)
            {
                ReleaseLoadedTable();
                initializeTask = ReloadTableAsync();
            }
        }

        /// <summary>
        /// 読み込み済み文字列テーブルを解放する。
        /// </summary>
        private void ReleaseLoadedTable()
        {
            if (loadedStringTable == null || loadedLocale == null)
            {
                loadedStringTable = null;
                loadedLocale = null;
                IsInitialized = false;
                return;
            }

            var targetTableName = string.IsNullOrWhiteSpace(tableName) ? DefaultTableName : tableName;
            LocalizationSettings.StringDatabase.ReleaseTable(targetTableName, loadedLocale);

            loadedStringTable = null;
            loadedLocale = null;
            IsInitialized = false;
        }

        /// <summary>
        /// ロケール変更後の文字列テーブル再読み込みを実行し、失敗時にログを出力する。
        /// </summary>
        private async Task ReloadTableAsync()
        {
            try
            {
                await InitializeInternalAsync(destroyCancellationTokenSource.Token);
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
        /// Addressables の非同期操作完了まで待機し、失敗時は例外化する。
        /// </summary>
        private static async Task<T> WaitForOperationAsync<T>(AsyncOperationHandle<T> operation, CancellationToken cancellationToken)
        {
            while (!operation.IsDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (operation.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"ローカライズ読み込みに失敗しました: {operation.OperationException?.Message}");
            }

            return operation.Result;
        }

        /// <summary>
        /// 破棄時に購読とキャンセル用トークンを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (IsPrimaryInstance)
            {
                LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
                destroyCancellationTokenSource?.Cancel();
                ReleaseLoadedTable();
                destroyCancellationTokenSource?.Dispose();
                destroyCancellationTokenSource = null;
            }

            base.OnDestroy();
        }
    }
}
