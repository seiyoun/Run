/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables から背景プレハブをロード・生成するシーン限定シングルトンスポナー。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Addressables から背景プレハブをロード・生成するスポナークラス。
    /// GameLoadingState から呼び出されてインスタンス化を実行し、破棄時に AddressablePrefabLoader を Dispose します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundSpawner : SingletonMonoBehaviour<BackgroundSpawner>
    {
        private const string BackgroundAddress = "ArenaBackground";

        private AddressablePrefabLoader addressableLoader;

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<BackgroundSpawner>.Instance != null;

        /// <summary>
        /// BackgroundSpawner の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static BackgroundSpawner Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<BackgroundSpawner>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<BackgroundSpawner>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(BackgroundSpawner));
                return obj.AddComponent<BackgroundSpawner>();
            }
        }

        /// <summary>
        /// シングルトンの初期化および AddressablePrefabLoader のインスタンスを初期化する。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;

            addressableLoader = new AddressablePrefabLoader();
        }

        /// <summary>
        /// オブジェクト破棄時に AddressablePrefabLoader を Dispose してロードしたアセットを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            if (addressableLoader != null)
            {
                addressableLoader.Dispose();
                addressableLoader = null;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Addressables から背景アセットをロードし、シーン上に生成する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された背景 GameObject インスタンス</returns>
        public async Task<GameObject> SpawnBackgroundAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var bgObj = await addressableLoader.LoadAsync(BackgroundAddress, cancellationToken);
                if (bgObj != null)
                {
                    DebugLogger.Log("[BackgroundSpawner] Addressables から背景のロード・生成が完了しました。");
                    return bgObj;
                }
                else
                {
                    DebugLogger.Error($"[BackgroundSpawner] Addressables ({BackgroundAddress}) から生成された GameObject が null です。");
                    return null;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[BackgroundSpawner] Addressables ({BackgroundAddress}) のロードに失敗しました: {ex.Message}");
                throw;
            }
        }
    }
}

