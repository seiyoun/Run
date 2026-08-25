/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー専用のパラメータ（HP、移動速度など）を保持・JSONシリアライズするデータクラス。
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
