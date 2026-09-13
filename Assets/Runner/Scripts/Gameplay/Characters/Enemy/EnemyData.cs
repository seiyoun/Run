/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エネミー専用のパラメータ（最大HP、移動速度、当たり判定半径など）を保持・JSONシリアライズするデータクラス。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミーの初期パラメータおよびJSON設定データを表すデータクラス。
    /// </summary>
    [Serializable]
    public class EnemyData
    {
        [Tooltip("エネミーの種類・識別名")]
        public string enemyName = "StandardEnemy";

        [Tooltip("最大HP")]
        public int maxHp = 20;

        [Tooltip("移動速度")]
        public float moveSpeed = 2.0f;

        [Tooltip("当たり判定（CircleCollider2D）の半径 (m)")]
        public float colliderRadius = 0.5f;

        [Tooltip("攻撃力")]
        public int attackPower = 10;

        [Tooltip("攻撃間隔（秒）")]
        public float attackInterval = 1.0f;

        [Tooltip("攻撃射程（m）")]
        public float attackRange = 1.0f;

        /// <summary>
        /// JSON 文字列から EnemyData を生成する。
        /// </summary>
        /// <param name="json">JSON 文字列</param>
        /// <returns>パースされた EnemyData インスタンス</returns>
        public static EnemyData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new EnemyData();
            }

            try
            {
                return JsonUtility.FromJson<EnemyData>(json) ?? new EnemyData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnemyData] JSON のパースに失敗しました: {ex.Message}");
                return new EnemyData();
            }
        }

        /// <summary>
        /// EnemyData を JSON 文字列に変換する。
        /// </summary>
        /// <param name="prettyPrint">整形して出力するか</param>
        /// <returns>シリアライズされた JSON 文字列</returns>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }
    }
}

