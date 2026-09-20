/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー専用のマスターパラメータ（HP、移動速度、怒りゲージなど）を保持・JSONシリアライズするデータクラス。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの初期パラメータおよび永続化マスターデータを表すデータクラス。
    /// </summary>
    [Serializable]
    public class PlayerMasterData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/PlayerData";

        // プレイヤーのキャラクター名
        public string characterName;

        // 最大HP
        public int maxHp;

        // 移動速度
        public float moveSpeed;

        // 攻撃力
        public int attackPower;

        // 攻撃間隔 (秒)
        public float attackInterval;

        // アイテム吸い込み範囲の半径 (m)
        public float magnetRadius;

        // 1歩と判定する移動距離 (m)
        public float stepDistanceThreshold;

        // 1歩あたりに獲得するポイント額
        public long pointsPerStep;

        /// <summary>
        /// JSON 文字列から PlayerMasterData を生成する。
        /// </summary>
        /// <param name="json">JSON 文字列</param>
        /// <returns>パースされた PlayerMasterData インスタンス</returns>
        public static PlayerMasterData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PlayerMasterData();
            }

            try
            {
                return JsonUtility.FromJson<PlayerMasterData>(json) ?? new PlayerMasterData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerMasterData] JSON のパースに失敗しました: {ex.Message}");
                return new PlayerMasterData();
            }
        }

        /// <summary>
        /// PlayerMasterData を JSON 文字列に変換する。
        /// </summary>
        /// <param name="prettyPrint">整形して出力するか</param>
        /// <returns>シリアライズされた JSON 文字列</returns>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// Resources から PlayerData.json を読み込み、PlayerMasterData インスタンスを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/PlayerData）</param>
        /// <returns>パースされた PlayerMasterData インスタンス（失敗時はデフォルト値）</returns>
        public static PlayerMasterData LoadFromResources(string path = DefaultResourcePath)
        {
            var jsonAsset = Resources.Load<TextAsset>(path);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[PlayerMasterData] '{path}' のロードに失敗しました。デフォルト値を使用します。");
                return new PlayerMasterData();
            }

            try
            {
                var data = FromJson(jsonAsset.text);
                DebugLogger.Log($"[PlayerMasterData] 読み込み完了: HP={data.maxHp}, Speed={data.moveSpeed}");
                return data;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[PlayerMasterData] JSONパースエラー: {ex.Message}");
                return new PlayerMasterData();
            }
        }
    }
}
