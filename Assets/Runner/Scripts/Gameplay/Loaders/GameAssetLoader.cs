/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game シーン内で使用されるキャラクター画像等のアセットロード・キャッシュ・一括解放を管理するシーン限定シングルトン。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runner
{
    /// <summary>
    /// Game シーン限定のアセットロード・キャッシュ管理シングルトン。
    /// スプライト等の Addressables アセットを非同期ロードしてキャッシュし、シーン終了または明示的な解放時に一括破棄します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameAssetLoader : SingletonMonoBehaviour<GameAssetLoader>
    {
        private readonly Dictionary<string, Sprite> loadedSprites = new();
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> spriteHandles = new();

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<GameAssetLoader>.Instance != null;

        /// <summary>現在キャッシュされているスプライト数</summary>
        public int CachedSpriteCount => loadedSprites.Count;

        /// <summary>
        /// GameAssetLoader の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static GameAssetLoader Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<GameAssetLoader>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<GameAssetLoader>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(GameAssetLoader));
                return obj.AddComponent<GameAssetLoader>();
            }
        }

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;
        }

        /// <summary>
        /// オブジェクト破棄時にロード済みアセットを全解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            ReleaseAll();
            base.OnDestroy();
        }

        /// <summary>
        /// 指定されたスプライトアセットを Addressables から非同期ロードしてキャッシュする。
        /// 既にキャッシュ済みの場合は即座にキャッシュから返します。
        /// </summary>
        /// <param name="imageName">スプライト画像名（Addressables アドレス）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>ロードされた Sprite（失敗時は null）</returns>
        public async Task<Sprite> LoadSpriteAsync(string imageName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(imageName)) return null;

            if (loadedSprites.TryGetValue(imageName, out var cached))
            {
                return cached;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<Sprite>(imageName);
                spriteHandles[imageName] = handle;

                while (!handle.IsDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    loadedSprites[imageName] = handle.Result;
                    DebugLogger.Log($"[GameAssetLoader] スプライトをロード・キャッシュしました: {imageName}");
                    return handle.Result;
                }

                DebugLogger.Warning($"[GameAssetLoader] スプライトのロードに失敗しました: {imageName}");
                return null;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[GameAssetLoader] スプライトロード時例外 ({imageName}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// キャッシュ済みのスプライトを同期的に取得する。
        /// </summary>
        /// <param name="imageName">スプライト画像名</param>
        /// <param name="sprite">取得されたスプライト</param>
        /// <returns>キャッシュに存在すれば true</returns>
        public bool TryGetLoadedSprite(string imageName, out Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(imageName))
            {
                sprite = null;
                return false;
            }

            return loadedSprites.TryGetValue(imageName, out sprite);
        }

        /// <summary>
        /// キャッシュされているすべてのスプライトアセットのハンドルを解放し、キャッシュをクリアする。
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var kvp in spriteHandles)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                    DebugLogger.Log($"[GameAssetLoader] スプライトアセットを解放しました: {kvp.Key}");
                }
            }

            spriteHandles.Clear();
            loadedSprites.Clear();
        }
    }
}

