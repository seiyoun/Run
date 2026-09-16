/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エネミー専用のパラメータ（エネミー種別、最大HP、移動速度、当たり判定半径など）を保持・JSONシリアライズするデータクラス群。
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミーの初期パラメータおよびJSON設定データを表すデータクラス。
    /// </summary>
    [Serializable]
    public class EnemyData
    {
        [Tooltip("エネミーの種類数値（0: Salaryman, 1: Granny 等）")]
        public int enemyType = 0;

        [Tooltip("エネミーの表示名・識別名")]
        public string enemyName = "Salaryman";

        [Tooltip("エネミーのスプライト画像名（Resources/Sprites/Characters/ 配下のファイル名、拡張子なし）")]
        public string imageName = "Enemy";

        [Tooltip("最大HP")]
        public int maxHp = 20;

        [Tooltip("移動速度")]
        public float moveSpeed = 2.0f;

        [Tooltip("当たり判定（CircleCollider2D）の半径 (m)")]
        public float colliderRadius = 0.4f;

        [Tooltip("攻撃力")]
        public int attackPower = 10;

        [Tooltip("攻撃間隔（秒）")]
        public float attackInterval = 1.0f;

        [Tooltip("攻撃射程（m）")]
        public float attackRange = 1.0f;

        /// <summary>
        /// 数値 enemyType からキャストされた EnemyType 列挙型。
        /// </summary>
        public EnemyType Type => (EnemyType)enemyType;

        /// <summary>
        /// JSON 文字列から単一の EnemyData を生成する。
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

    /// <summary>
    /// EnemyData のリストをラップし、JsonUtility で複数件デシリアライズ可能にするコンテナクラス。
    /// </summary>
    [Serializable]
    public class EnemyDataList
    {
        [Tooltip("エネミー設定データのリスト")]
        public List<EnemyData> enemies = new List<EnemyData>();

        /// <summary>
        /// JSON 文字列から EnemyDataList を生成する。
        /// </summary>
        /// <param name="json">JSON 文字列</param>
        /// <returns>パースされた EnemyDataList インスタンス</returns>
        public static EnemyDataList FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new EnemyDataList();
            }

            try
            {
                return JsonUtility.FromJson<EnemyDataList>(json) ?? new EnemyDataList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnemyDataList] JSON のパースに失敗しました: {ex.Message}");
                return new EnemyDataList();
            }
        }

        /// <summary>
        /// EnemyDataList を JSON 文字列に変換する。
        /// </summary>
        /// <param name="prettyPrint">整形して出力するか</param>
        /// <returns>シリアライズされた JSON 文字列</returns>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }
    }
}
