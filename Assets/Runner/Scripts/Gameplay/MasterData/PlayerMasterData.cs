/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー専用のマスターパラメータ（HP、移動速度、怒りゲージなど）を保持・JSONシリアライズするデータクラス。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの初期パラメータおよび永続化マスターデータを表すデータクラス。
    /// </summary>
    [Serializable]
    public class PlayerMasterData
    {
        [Tooltip("プレイヤーのキャラクター名")]
        public string characterName;

        [Tooltip("最大HP")]
        public int maxHp;

        [Tooltip("移動速度")]
        public float moveSpeed;

        [Tooltip("攻撃力")]
        public int attackPower;

        [Tooltip("攻撃間隔 (秒)")]
        public float attackInterval;

        [Tooltip("アイテム吸い込み範囲の半径 (m)")]
        public float magnetRadius;

        [Tooltip("1歩と判定する移動距離 (m)")]
        public float stepDistanceThreshold;

        [Tooltip("1歩あたりに獲得するポイント額")]
        public long pointsPerStep;

        [Tooltip("最大怒りゲージ値")]
        public float maxRage;

        [Tooltip("怒りゲージの溜まる速度（1秒あたり）")]
        public float rageGainRate;

        [Tooltip("怒りMAX時の覚醒持続時間（秒）")]
        public float awakeningDuration;

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
    }
}
