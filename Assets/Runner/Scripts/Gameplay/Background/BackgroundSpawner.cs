/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables から背景プレハブをロード・生成するスポナークラス。
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
    /// GameLoadingState から呼び出されてインスタンス化を実行します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundSpawner : MonoBehaviour
    {
        private const string BackgroundAddress = "ArenaBackground";

        [Header("Spawn Settings")]
        [Tooltip("スポーン位置（未指定の場合は本オブジェクトの位置）")]
        [SerializeField]
        private Transform spawnPoint;

        private AddressablePrefabLoader addressableLoader;

        private void Awake()
        {
            addressableLoader = new AddressablePrefabLoader();
        }

        private void OnDestroy()
        {
            if (addressableLoader != null)
            {
                addressableLoader.Dispose();
                addressableLoader = null;
            }
        }

        /// <summary>
        /// Addressables から背景アセットをロードし、シーン上に生成する。
        /// </summary>
        public async Task<GameObject> SpawnBackgroundAsync(CancellationToken cancellationToken = default)
        {
            var spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject bgObj = null;

            try
            {
                bgObj = await addressableLoader.LoadAsync(BackgroundAddress, cancellationToken);
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[BackgroundSpawner] Addressables ({BackgroundAddress}) のロードに失敗しました: {ex.Message}");
                throw;
            }

            if (bgObj == null)
            {
                DebugLogger.Error($"[BackgroundSpawner] Addressables ({BackgroundAddress}) から生成された GameObject が null です。");
                return null;
            }

            bgObj.transform.position = spawnPos;
            DebugLogger.Log($"[BackgroundSpawner] Addressables から背景のロード・生成が完了しました。Pos: {spawnPos}");
            return bgObj;
        }
    }
}

