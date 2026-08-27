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
    /// GameLoadingState から呼び出されてインスタンス化を実行し、破棄時に AddressablePrefabLoader を Dispose します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundSpawner : MonoBehaviour
    {
        // -------------------------------------------------------------
        // 1. const / static フィールド
        // -------------------------------------------------------------
        private const string BackgroundAddress = "ArenaBackground";

        // -------------------------------------------------------------
        // 2. [SerializeField] シリアライズフィールド
        // -------------------------------------------------------------
        [Header("Spawn Settings")]
        [Tooltip("スポーン位置（未指定の場合は本オブジェクトの位置）")]
        [SerializeField]
        private Transform spawnPoint;

        // -------------------------------------------------------------
        // 3. private インスタンス変数
        // -------------------------------------------------------------
        private AddressablePrefabLoader addressableLoader;

        // -------------------------------------------------------------
        // 4. public インスタンス変数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 5. プロパティ & イベント
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 6. Unity ライフサイクル関数
        // -------------------------------------------------------------

        /// <summary>
        /// AddressablePrefabLoader のインスタンスを初期化する。
        /// </summary>
        private void Awake()
        {
            addressableLoader = new AddressablePrefabLoader();
        }

        /// <summary>
        /// オブジェクト破棄時に AddressablePrefabLoader を Dispose してロードしたアセットを解放する。
        /// </summary>
        private void OnDestroy()
        {
            if (addressableLoader != null)
            {
                addressableLoader.Dispose();
                addressableLoader = null;
            }
        }

        // -------------------------------------------------------------
        // 7. override 関数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 8. public 関数
        // -------------------------------------------------------------

        /// <summary>
        /// Addressables から背景アセットをロードし、シーン上に生成する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された背景 GameObject インスタンス</returns>
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

        // -------------------------------------------------------------
        // 9. private 関数 / 内部ヘルパー
        // -------------------------------------------------------------
    }
}
