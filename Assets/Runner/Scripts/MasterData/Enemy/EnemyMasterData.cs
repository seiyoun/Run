/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エネミー専用のマスターパラメータ（エネミー種別、最大HP、移動速度、当たり判定半径など）を保持・JSONシリアライズするデータクラス群。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// エネミーの初期パラメータおよびJSON設定データを表すマスターデータクラス。
    /// </summary>
    [Serializable]
    public class EnemyMasterData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/EnemyData";

        [Tooltip("エネミーの種類数値（0: Salaryman, 1: Granny 等）")]
        public int enemyType;

        [Tooltip("エネミーの表示名・識別名")]
        public string enemyName;

        [Tooltip("エネミーのスプライト画像名（Resources/Sprites/Characters/ 配下のファイル名、拡張子なし）")]
        public string imageName;

        [Tooltip("最大HP")]
        public int maxHp;

        [Tooltip("移動速度")]
        public float moveSpeed;

        [Tooltip("当たり判定（CircleCollider2D）の半径 (m)")]
        public float colliderRadius;

        [Tooltip("攻撃力")]
        public int attackPower;

        [Tooltip("攻撃間隔（秒）")]
        public float attackInterval;

        [Tooltip("攻撃射程（m）")]
        public float attackRange;

        /// <summary>
        /// 数値 enemyType からキャストされた EnemyType 列挙型。
        /// </summary>
        public EnemyType Type => (EnemyType)enemyType;

        /// <summary>
        /// JSON 文字列から単一の EnemyMasterData を生成する。
        /// </summary>
        /// <param name="json">JSON 文字列</param>
        /// <returns>パースされた EnemyMasterData インスタンス</returns>
        public static EnemyMasterData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new EnemyMasterData();
            }

            try
            {
                return JsonUtility.FromJson<EnemyMasterData>(json) ?? new EnemyMasterData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnemyMasterData] JSON のパースに失敗しました: {ex.Message}");
                return new EnemyMasterData();
            }
        }

        /// <summary>
        /// EnemyMasterData を JSON 文字列に変換する。
        /// </summary>
        /// <param name="prettyPrint">整形して出力するか</param>
        /// <returns>シリアライズされた JSON 文字列</returns>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// Resources から EnemyData.json を読み込み、全 EnemyMasterData のリストを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/EnemyData）</param>
        /// <returns>EnemyMasterData のリスト</returns>
        public static List<EnemyMasterData> LoadAllFromResources(string path = DefaultResourcePath)
        {
            var result = new List<EnemyMasterData>();
            var jsonAsset = Resources.Load<TextAsset>(path);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[EnemyMasterData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<EnemyMasterDataContainer>(jsonAsset.text);
                if (container != null && container.enemies != null && container.enemies.Count > 0)
                {
                    result.AddRange(container.enemies);
                    DebugLogger.Log($"[EnemyMasterData] {result.Count} 種類のエネミーパラメータを読み込みました。");
                }
                else
                {
                    var singleData = FromJson(jsonAsset.text);
                    if (singleData != null)
                    {
                        result.Add(singleData);
                        DebugLogger.Log("[EnemyMasterData] 単一エネミーパラメータを読み込みました。");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[EnemyMasterData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// EnemyData.json のリスト形式を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class EnemyMasterDataContainer
        {
            [Tooltip("エネミーマスターデータのリスト")]
            public List<EnemyMasterData> enemies = new List<EnemyMasterData>();
        }
    }
}
