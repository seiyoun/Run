/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレハブからエネミーをインスタンス化・初期化して生成するファクトリクラス。
 */

using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミーのインスタンス化および初期化パラメータ注入を行うファクトリコンポーネント。
    /// IEnemyFactory を実装し、生成の責務を単一クラスに集約します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyFactory : MonoBehaviour, IEnemyFactory
    {
        private const float FallbackMoveSpeed = 3.0f;

        [Header("Prefab Settings")]
        [Tooltip("生成対象のエネミープレハブ")]
        [SerializeField]
        private EnemyController enemyPrefab;

        [Header("Default Parameters")]
        [Tooltip("生成時のエネミーデフォルト移動速度")]
        [SerializeField]
        private float defaultMoveSpeed = FallbackMoveSpeed;

        [Tooltip("エネミーの親オブジェクトTransform（null時はルートに生成）")]
        [SerializeField]
        private Transform spawnContainer;

        /// <summary>エネミープレハブの参照</summary>
        public EnemyController EnemyPrefab
        {
            get => enemyPrefab;
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
        /// 設定されたプレハブからエネミーを生成し、初期設定を行って返す。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public EnemyController CreateEnemy(Vector3 position)
        {
            return CreateEnemy(enemyPrefab, position);
        }

        /// <summary>
        /// 指定されたエネミー種別のパラメータを適用してエネミーを生成する。
        /// </summary>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public EnemyController CreateEnemy(Vector3 position, EnemyType enemyType)
        {
            return CreateEnemy(enemyPrefab, position, enemyType);
        }

        /// <summary>
        /// 指定されたプレハブからエネミーを生成し、初期設定を行って返す。
        /// </summary>
        /// <param name="prefab">生成に使用する EnemyController プレハブ</param>
        /// <param name="position">生成ワールド座標</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public EnemyController CreateEnemy(EnemyController prefab, Vector3 position)
        {
            return CreateEnemy(prefab, position, EnemyType.Salaryman);
        }

        /// <summary>
        /// 指定されたプレハブとエネミー種別に基づいてエネミーを生成し、パラメータを適用して返す。
        /// </summary>
        /// <param name="prefab">生成に使用する EnemyController プレハブ</param>
        /// <param name="position">生成ワールド座標</param>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <returns>生成された EnemyController インスタンス（失敗時は null）</returns>
        public EnemyController CreateEnemy(EnemyController prefab, Vector3 position, EnemyType enemyType)
        {
            if (prefab == null)
            {
                DebugLogger.Error("[EnemyFactory] 生成対象のエネミープレハブが指定されていません。");
                return null;
            }

            var instance = Instantiate(prefab, position, Quaternion.identity, spawnContainer);

            // MasterDataManager のキャッシュからデータを取得して注入
            var data = MasterDataManager.GetEnemyMasterData(enemyType);
            instance.ApplyData(data);

            DebugLogger.Log($"[EnemyFactory] エネミーを生成しました: {instance.name} (Type: {enemyType}, Position: {position})");
            return instance;
        }
    }
}

