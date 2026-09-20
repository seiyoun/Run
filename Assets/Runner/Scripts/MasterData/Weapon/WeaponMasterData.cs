/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 武器のマスターパラメータおよびレベル別強化データ（威力、攻撃速度、数など）を保持・JSONシリアライズするマスターデータクラス。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 武器の系統種別。
    /// </summary>
    public enum WeaponType
    {
        Drone = 0
    }

    /// <summary>
    /// 武器のレベル別性能パラメータ（威力、攻撃速度、数など）を表すデータクラス。
    /// </summary>
    [Serializable]
    public class WeaponLevelData
    {
        // 武器レベル
        public int level;

        // 威力（攻撃力）
        public int attackPower;

        // 攻撃間隔（秒）
        public float attackInterval;

        // 同時展開数・発射数
        public int count;

        /// <summary>武器レベル</summary>
        public int Level => level;

        /// <summary>威力（攻撃力）</summary>
        public int AttackPower => attackPower;

        /// <summary>攻撃間隔（秒）</summary>
        public float AttackInterval => attackInterval;

        /// <summary>同時展開数・発射数</summary>
        public int Count => count;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public WeaponLevelData()
        {
        }

        /// <summary>
        /// パラメータを指定してレベルデータを生成する。
        /// </summary>
        /// <param name="level">武器レベル</param>
        /// <param name="attackPower">威力（攻撃力）</param>
        /// <param name="attackInterval">攻撃間隔（秒）</param>
        /// <param name="count">同時展開数・発射数</param>
        public WeaponLevelData(int level, int attackPower, float attackInterval, int count)
        {
            this.level = level;
            this.attackPower = attackPower;
            this.attackInterval = attackInterval;
            this.count = count;
        }

        /// <summary>
        /// レベルデータの文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            return $"Lv.{level} (Power: {attackPower}, Interval: {attackInterval}s, Count: {count})";
        }
    }

    /// <summary>
    /// 武器の基本定義およびレベル別パラメータリストを保持するマスターデータクラス。
    /// </summary>
    [Serializable]
    public class WeaponMasterData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/WeaponData";

        // 武器識別番号
        public int weaponId;

        // 武器種別数値（0: Drone 等）
        public int weaponType;

        // 武器名
        public string weaponName;

        // 武器説明文
        public string description;

        // スプライト画像名（Characters/ 配下のファイル名、拡張子なし）
        public string imageName;

        // レベル別性能パラメータのリスト
        public List<WeaponLevelData> levels = new List<WeaponLevelData>();

        /// <summary>武器識別番号</summary>
        public int WeaponId => weaponId;

        /// <summary>武器種別列挙型</summary>
        public WeaponType Type => (WeaponType)weaponType;

        /// <summary>武器名</summary>
        public string WeaponName => weaponName;

        /// <summary>武器説明文</summary>
        public string Description => description;

        /// <summary>スプライト画像名</summary>
        public string ImageName => imageName;

        /// <summary>レベル別性能パラメータのコレクション</summary>
        public IReadOnlyList<WeaponLevelData> Levels => levels;

        /// <summary>最大レベル</summary>
        public int MaxLevel => levels != null && levels.Count > 0 ? levels.Count : 1;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public WeaponMasterData()
        {
        }

        /// <summary>
        /// 武器マスターデータの文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            return $"WeaponMasterData: {weaponName} (Type: {Type}, Levels: {MaxLevel})";
        }

        /// <summary>
        /// 指定されたレベルに対応する性能パラメータを取得する。範囲外の場合は最も近いレベルのデータを安全に返す。
        /// </summary>
        /// <param name="level">取得対象のレベル（1以上）</param>
        /// <returns>該当レベルの WeaponLevelData</returns>
        public WeaponLevelData GetLevelData(int level)
        {
            if (levels == null || levels.Count == 0)
            {
                return new WeaponLevelData(1, 10, 1.5f, 1);
            }

            int clampedIndex = Mathf.Clamp(level - 1, 0, levels.Count - 1);
            return levels[clampedIndex];
        }

        /// <summary>
        /// JSON 文字列から単一の WeaponMasterData を生成する。
        /// </summary>
        /// <param name="json">JSON 文字列</param>
        /// <returns>パースされた WeaponMasterData インスタンス</returns>
        public static WeaponMasterData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new WeaponMasterData();
            }

            try
            {
                return JsonUtility.FromJson<WeaponMasterData>(json) ?? new WeaponMasterData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WeaponMasterData] JSON のパースに失敗しました: {ex.Message}");
                return new WeaponMasterData();
            }
        }

        /// <summary>
        /// WeaponMasterData を JSON 文字列に変換する。
        /// </summary>
        /// <param name="prettyPrint">整形して出力するか</param>
        /// <returns>シリアライズされた JSON 文字列</returns>
        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// Resources から WeaponData.json を読み込み、全 WeaponMasterData のリストを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/WeaponData）</param>
        /// <returns>WeaponMasterData のリスト</returns>
        public static List<WeaponMasterData> LoadAllFromResources(string path = DefaultResourcePath)
        {
            var result = new List<WeaponMasterData>();
            var jsonAsset = Resources.Load<TextAsset>(path);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[WeaponMasterData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<WeaponMasterDataContainer>(jsonAsset.text);
                if (container != null && container.weapons != null && container.weapons.Count > 0)
                {
                    result.AddRange(container.weapons);
                    DebugLogger.Log($"[WeaponMasterData] {result.Count} 種類の武器マスターデータを読み込みました。");
                }
                else
                {
                    var singleData = FromJson(jsonAsset.text);
                    if (singleData != null && singleData.weaponId > 0)
                    {
                        result.Add(singleData);
                        DebugLogger.Log("[WeaponMasterData] 単一武器マスターデータを読み込みました。");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[WeaponMasterData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// WeaponData.json のリスト形式を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class WeaponMasterDataContainer
        {
            // 武器マスターデータのリスト
            public List<WeaponMasterData> weapons = new List<WeaponMasterData>();
        }
    }
}

