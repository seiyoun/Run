/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: バフのマスターデータ（持続時間、効果値など）を保持・JSONシリアライズするマスターデータクラス。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// バフのマスターパラメータ（持続時間、効果倍率・回復量等）を表すデータクラス。
    /// </summary>
    [Serializable]
    public class BuffMasterData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/BuffData";

        public int buffId;
        public string buffName;
        public string description;
        public float duration;
        public float value;

        /// <summary>バフ識別番号</summary>
        public int BuffId => buffId;

        /// <summary>バフ種別列挙型</summary>
        public BuffType Type => (BuffType)buffId;

        /// <summary>バフ名称</summary>
        public string BuffName => buffName;

        /// <summary>バフ説明文</summary>
        public string Description => description;

        /// <summary>効果持続時間（秒）</summary>
        public float Duration => duration;

        /// <summary>効果量（速度倍率、回復量など）</summary>
        public float Value => value;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public BuffMasterData()
        {
        }

        /// <summary>
        /// パラメータを指定してバフマスターデータを生成する。
        /// </summary>
        /// <param name="buffId">バフ識別番号</param>
        /// <param name="buffName">バフ名称</param>
        /// <param name="description">バフ説明文</param>
        /// <param name="duration">効果持続時間（秒）</param>
        /// <param name="value">効果量</param>
        public BuffMasterData(int buffId, string buffName, string description, float duration, float value)
        {
            this.buffId = buffId;
            this.buffName = buffName;
            this.description = description;
            this.duration = duration;
            this.value = value;
        }

        /// <summary>
        /// バフマスターデータの文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            return $"BuffMasterData: {buffName} (Id: {buffId}, Duration: {duration}s, Value: {value})";
        }

        /// <summary>
        /// Resources から BuffData.json を読み込み、全 BuffMasterData のリストを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/BuffData）</param>
        /// <returns>BuffMasterData のリスト</returns>
        public static List<BuffMasterData> LoadAllFromResources(string path = DefaultResourcePath)
        {
            var result = new List<BuffMasterData>();
            var jsonAsset = Resources.Load<TextAsset>(path);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[BuffMasterData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<BuffMasterDataContainer>(jsonAsset.text);
                if (container != null && container.buffs != null && container.buffs.Count > 0)
                {
                    result.AddRange(container.buffs);
                    DebugLogger.Log($"[BuffMasterData] {result.Count} 種類のバフマスターデータを読み込みました。");
                }
                else
                {
                    DebugLogger.Warning($"[BuffMasterData] '{path}' のパース結果が空です。");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[BuffMasterData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// BuffData.json のリスト形式を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class BuffMasterDataContainer
        {
            public List<BuffMasterData> buffs = new List<BuffMasterData>();
        }
    }
}

