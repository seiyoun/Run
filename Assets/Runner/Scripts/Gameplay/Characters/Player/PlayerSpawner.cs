/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables からプレイヤーキャラクターをロード・生成するスポナークラス。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Addressables からプレイヤーをロード・生成するスポナークラス。
    /// GameLoadingState から呼び出されてインスタンス化を実行し、破棄時に AddressablePrefabLoader を Dispose します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSpawner : MonoBehaviour
    {
        private const string PlayerAddress = "Player";
        [Header("Spawn Settings")]
        [Tooltip("スポーン位置（未指定の場合は本オブジェクトの位置）")]
        [SerializeField]
        private Transform spawnPoint;
        private AddressablePrefabLoader addressableLoader;
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
        /// <summary>
        /// Addressables からプレイヤーアセットをロードし、シーン上に生成する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された PlayerController インスタンス</returns>
        public async Task<PlayerController> SpawnPlayerAsync(CancellationToken cancellationToken = default)
        {
            var spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject playerObj = null;

            try
            {
                playerObj = await addressableLoader.LoadAsync(PlayerAddress, cancellationToken);
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[PlayerSpawner] Addressables ({PlayerAddress}) のロードに失敗しました: {ex.Message}");
                throw;
            }

            if (playerObj == null)
            {
                DebugLogger.Error($"[PlayerSpawner] Addressables ({PlayerAddress}) から生成された GameObject が null です。");
                return null;
            }

            var player = playerObj.GetComponent<PlayerController>();
            if (player == null)
            {
                DebugLogger.Error($"[PlayerSpawner] ロードされたプレハブに PlayerController がアタッチされていません。");
                return null;
            }

            player.transform.position = spawnPos;
            SetupPlayerCamera(player);

            DebugLogger.Log($"[PlayerSpawner] Addressables からプレイヤーのロード・生成が完了しました。Pos: {spawnPos}");
            return player;
        }
        /// <summary>
        /// Cinemachine カメラの追従ターゲットをプレイヤーに設定する。
        /// </summary>
        /// <param name="player">追従対象の PlayerController</param>
        private void SetupPlayerCamera(PlayerController player)
        {
            var vcam = FindFirstObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Target.TrackingTarget = player.transform;
            }
        }
    }
}
