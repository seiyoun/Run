/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エネミーインスタンスの生成・初期化を担当するファクトリインターフェース。
 */

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミーインスタンス生成ファクトリのインターフェース。
    /// </summary>
    public interface IEnemyFactory
    {
        /// <summary>
        /// 指定されたワールド座標にエネミーを非同期ロード・生成し、初期設定を行って返す。
        /// </summary>
        /// <param name="position">生成位置</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス</returns>
        Task<EnemyController> CreateEnemyAsync(Vector3 position, CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定されたエネミー種別とワールド座標に基づいてエネミーを非同期ロード・生成し、パラメータを適用して返す。
        /// </summary>
        /// <param name="position">生成位置</param>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された EnemyController インスタンス</returns>
        Task<EnemyController> CreateEnemyAsync(Vector3 position, EnemyType enemyType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 指定されたワールド座標にエネミーを同期生成し、初期設定を行って返す。
        /// </summary>
        /// <param name="position">生成位置</param>
        /// <returns>生成された EnemyController インスタンス</returns>
        EnemyController CreateEnemy(Vector3 position);

        /// <summary>
        /// 指定されたエネミー種別とワールド座標に基づいてエネミーを同期生成し、パラメータを適用して返す。
        /// </summary>
        /// <param name="position">生成位置</param>
        /// <param name="enemyType">生成するエネミー種別</param>
        /// <returns>生成された EnemyController インスタンス</returns>
        EnemyController CreateEnemy(Vector3 position, EnemyType enemyType);
    }
}
