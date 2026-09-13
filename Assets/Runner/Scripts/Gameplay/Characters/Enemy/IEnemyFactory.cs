/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エネミーインスタンスの生成・初期化を担当するファクトリインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミーインスタンス生成ファクトリのインターフェース。
    /// </summary>
    public interface IEnemyFactory
    {
        /// <summary>
        /// 指定されたワールド座標にエネミーを生成し、初期設定を行って返す。
        /// </summary>
        /// <param name="position">生成位置</param>
        /// <param name="target">追尾対象（プレイヤー等の Transform）</param>
        /// <returns>生成された EnemyController インスタンス</returns>
        EnemyController CreateEnemy(Vector3 position, Transform target = null);
    }
}

