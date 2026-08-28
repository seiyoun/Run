/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー専用のパラメータ（HP、移動速度、怒りゲージなど）を保持・JSONシリアライズするデータクラス。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの初期パラメータおよび永続化データを表すデータクラス。
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [Tooltip("プレイヤーのキャラクター名")]
        public string characterName = "Hero";

        [Tooltip("最大HP")]
        public int maxHp = 100;

        [Tooltip("移動速度")]
        public float moveSpeed = 6.0f;

        [Tooltip("攻撃力")]
        public int attackPower = 10;

        [Tooltip("攻撃間隔 (秒)")]
        public float attackInterval = 1.0f;

        [Tooltip("アイテム吸い込み範囲の半径 (m)")]
        public float magnetRadius = 3.5f;

        [Tooltip("1歩と判定する移動距離 (m)")]
        public float stepDistanceThreshold = 0.65f;

        [Tooltip("1歩あたりに獲得するポイント額")]
        public long pointsPerStep = 2;

        [Tooltip("最大怒りゲージ値")]
        public float maxRage = 100f;

        [Tooltip("怒りゲージの溜まる速度（1秒あたり）")]
        public float rageGainRate = 10f;

        [Tooltip("怒りゲージの減る速度（1秒あたり）")]
        public float rageDecayRate = 5f;

        /// <summary>
        /// JSON 文字列から PlayerData を生成する。
        /// </summary>
        public static PlayerData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new PlayerData();
            }

            try
            {
                return JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerData] JSON のパースに失敗しました: {ex.Message}");
                return new PlayerData();
            }
        }

        /// <summary>
        /// PlayerData を JSON 文字列に変換する。
        /// </summary>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }
    }
}
