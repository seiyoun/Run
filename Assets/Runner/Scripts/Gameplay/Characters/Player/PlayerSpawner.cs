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
    public sealed class PlayerSpawner : SingletonMonoBehaviour<PlayerSpawner>
    {
        private const string PlayerAddress = "Player";

        private AddressablePrefabLoader addressableLoader;

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<PlayerSpawner>.Instance != null;

        /// <summary>
        /// PlayerSpawner の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static PlayerSpawner Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<PlayerSpawner>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<PlayerSpawner>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(PlayerSpawner));
                return obj.AddComponent<PlayerSpawner>();
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
        /// Addressables からプレイヤーアセットをロードし、指定された位置（未指定時は原点）に生成する。
        /// </summary>
        /// <param name="spawnPoint">スポーン位置として使用する Transform（null の場合は Vector3.zero を使用）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された PlayerController インスタンス</returns>
        public async Task<PlayerController> SpawnPlayerAsync(Transform spawnPoint = null, CancellationToken cancellationToken = default)
        {
            var spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            spawnPos.z = 0f;
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

            var playerData = MasterDataManager.GetPlayerMasterData();
            player.ApplyData(playerData);

            if (GameRecordTracker.HasInstance || GameRecordTracker.Instance != null)
            {
                GameRecordTracker.Instance.BindPlayer(player);
            }

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
