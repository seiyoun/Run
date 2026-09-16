/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: AddressableManager からプレハブをロードし、エネミーをインスタンス化・初期化して非同期生成するファクトリクラス（POCO）。
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
    /// エネミーのインスタンス化およびマスターデータ注入を行う純粋な C# ファクトリクラス。
    /// AddressableManager ("Enemy") からプレハブを取得して非同期生成し、破棄時に参照を解放します。
    /// </summary>
    public sealed class EnemyFactory : IEnemyFactory, IDisposable
    {
        public const string DefaultEnemyAddress = "Enemy";

        private GameObject enemyPrefab;
        private bool isAssetLoaded;

        /// <summary>
        /// EnemyFactory のデフォルトコンストラクタ。
        /// </summary>
        public EnemyFactory()
        {
        }

        /// <summary>
        /// ロードしたエネミープレハブ参照を AddressableManager へ返却し、リソースを解放する。
        /// </summary>
        public void Dispose()
        {
            if (isAssetLoaded)
            {
                AddressableManager.ReleaseAsset(DefaultEnemyAddress);
                isAssetLoaded = false;
                enemyPrefab = null;
                DebugLogger.Log($"[EnemyFactory] Addressables ({DefaultEnemyAddress}) のプレハブ参照を解放しました。");
            }
        }

        /// <summary>
        /// AddressableManager からエネミープレハブを非同期ロードしてエネミーを生成し、初期設定を行って返す。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public Task<EnemyController> CreateEnemyAsync(Vector3 position, CancellationToken cancellationToken = default)
        {
            return CreateEnemyAsync(position, EnemyType.Salaryman, cancellationToken);
        }

        /// <summary>
        /// AddressableManager からエネミープレハブを非同期ロードし、指定されたエネミー種別のパラメータを適用して生成する。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public async Task<EnemyController> CreateEnemyAsync(Vector3 position, EnemyType enemyType, CancellationToken cancellationToken = default)
        {
            var targetPrefab = await GetOrLoadPrefabAsync(cancellationToken);
            if (targetPrefab == null)
            {
                DebugLogger.Error("[EnemyFactory] 生成対象のエネミープレハブのロードに失敗しました。");
                return null;
            }

            return InstantiateEnemy(targetPrefab, position, enemyType);
        }

        /// <summary>
        /// プレハブからエネミーをインスタンス化し、マスターデータを適用して初期化する。
        /// </summary>
        /// <param name="prefab">対象プレハブ GameObject</param>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="enemyType">エネミー種別</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        private EnemyController InstantiateEnemy(GameObject prefab, Vector3 position, EnemyType enemyType)
        {
            var instanceObj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            var enemyController = instanceObj.GetComponent<EnemyController>();
            if (enemyController == null)
            {
                DebugLogger.Error("[EnemyFactory] 生成されたプレハブに EnemyController がアタッチされていません。");
                return null;
            }

            var data = MasterDataManager.GetEnemyMasterData(enemyType);
            enemyController.ApplyData(data);

            DebugLogger.Log($"[EnemyFactory] エネミーを生成しました: {enemyController.name} (Type: {enemyType}, Position: {position})");
            return enemyController;
        }

        /// <summary>
        /// エネミープレハブを取得する。未ロードの場合は AddressableManager から非同期ロードしてキャッシュする。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>エネミープレハブ GameObject</returns>
        private async Task<GameObject> GetOrLoadPrefabAsync(CancellationToken cancellationToken)
        {
            if (enemyPrefab != null) return enemyPrefab;

            try
            {
                enemyPrefab = await AddressableManager.LoadAssetAsync<GameObject>(DefaultEnemyAddress, cancellationToken);
                isAssetLoaded = true;
                return enemyPrefab;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[EnemyFactory] AddressableManager ({DefaultEnemyAddress}) のロードに失敗しました: {ex.Message}");
                return null;
            }
        }
    }
}
