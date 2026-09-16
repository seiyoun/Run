/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: AddressableManager からプレハブをロードし、エネミーをインスタンス化・初期化して生成するファクトリクラス（POCO）。
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
    /// AddressableManager ("Enemy") からプレハブを取得して生成します。
    /// </summary>
    public sealed class EnemyFactory : IEnemyFactory
    {
        public const string DefaultEnemyAddress = "Enemy";
        private const float FallbackMoveSpeed = 3.0f;

        private static GameObject cachedEnemyPrefab;

        private GameObject enemyPrefab;
        private Transform spawnContainer;
        private float defaultMoveSpeed = FallbackMoveSpeed;

        /// <summary>エネミープレハブの参照</summary>
        public GameObject EnemyPrefab
        {
            get => enemyPrefab != null ? enemyPrefab : cachedEnemyPrefab;
            set => enemyPrefab = value;
        }

        /// <summary>生成時のエネミーデフォルト移動速度</summary>
        public float DefaultMoveSpeed
        {
            get => defaultMoveSpeed;
            set => defaultMoveSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>エネミーの親オブジェクト Transform</summary>
        public Transform SpawnContainer
        {
            get => spawnContainer;
            set => spawnContainer = value;
        }

        /// <summary>
        /// EnemyFactory のデフォルトコンストラクタ。
        /// </summary>
        public EnemyFactory() : this(null, null)
        {
        }

        /// <summary>
        /// EnemyFactory のコンストラクタ。
        /// </summary>
        /// <param name="prefab">使用するプレハブ GameObject（null の場合は AddressableManager または事前キャッシュから解決）</param>
        /// <param name="container">生成先コンテナ Transform（任意）</param>
        public EnemyFactory(GameObject prefab, Transform container = null)
        {
            enemyPrefab = prefab != null ? prefab : cachedEnemyPrefab;
            spawnContainer = container;
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
        /// 設定またはキャッシュされたプレハブからエネミーを同期生成し、初期設定を行って返す。
        /// プレハブが未ロードの場合は null を返します。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public EnemyController CreateEnemy(Vector3 position)
        {
            return CreateEnemy(position, EnemyType.Salaryman);
        }

        /// <summary>
        /// 設定またはキャッシュされたプレハブからエネミーを同期生成し、指定されたエネミー種別のパラメータを適用して返す。
        /// プレハブが未ロードの場合は null を返します。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public EnemyController CreateEnemy(Vector3 position, EnemyType enemyType)
        {
            var targetPrefab = enemyPrefab != null ? enemyPrefab : cachedEnemyPrefab;
            if (targetPrefab == null)
            {
                DebugLogger.Error("[EnemyFactory] 生成対象のエネミープレハブがロードされていません。CreateEnemyAsync を使用するか、あらかじめプレハブを設定してください。");
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
            var instanceObj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity, spawnContainer);
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
            if (cachedEnemyPrefab != null) return cachedEnemyPrefab;

            try
            {
                cachedEnemyPrefab = await AddressableManager.LoadAssetAsync<GameObject>(DefaultEnemyAddress, cancellationToken);
                enemyPrefab = cachedEnemyPrefab;
                return cachedEnemyPrefab;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[EnemyFactory] AddressableManager ({DefaultEnemyAddress}) のロードに失敗しました: {ex.Message}");
                return null;
            }
        }
    }
}
