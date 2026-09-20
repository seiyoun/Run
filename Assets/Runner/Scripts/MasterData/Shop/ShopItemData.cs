/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タイムセールショップで取り扱うアイテム種別および商品データ定義。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ショップアイテムの系統分類（強化系、攻撃系、回復系）。
    /// </summary>
    public enum ShopItemType
    {
        Enhancement = 0, // 強化系
        Attack = 1,      // 攻撃系
        Recovery = 2     // 回復系
    }

    /// <summary>
    /// ショップに陳列される商品アイテムのデータクラス。
    /// </summary>
    [Serializable]
    public class ShopItemData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/ShopData";

        public int id;
        public string itemName;
        public string description;
        public string iconEmoji;
        public int price;
        public ShopItemType itemType;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public ShopItemData()
        {
        }

        /// <summary>
        /// 商品データインスタンスを生成する。
        /// </summary>
        /// <param name="id">アイテム識別子（整数値）</param>
        /// <param name="name">アイテム名</param>
        /// <param name="desc">説明文</param>
        /// <param name="emoji">表示用絵文字/アイコンタグ</param>
        /// <param name="price">必要ポイント数</param>
        /// <param name="type">アイテム機能種別</param>
        public ShopItemData(int id, string name, string desc, string emoji, int price, ShopItemType type)
        {
            this.id = id;
            this.itemName = name;
            this.description = desc;
            this.iconEmoji = emoji;
            this.price = price;
            this.itemType = type;
        }

        /// <summary>
        /// Resources から ShopData.json を読み込み、全 ShopItemData のリストを生成する。
        /// </summary>
        /// <param name="path">リソースパス（デフォルト: Data/ShopData）</param>
        /// <returns>ShopItemData のリスト</returns>
        public static List<ShopItemData> LoadAllFromResources(string path = DefaultResourcePath)
        {
            var result = new List<ShopItemData>();
            var jsonAsset = Resources.Load<TextAsset>(path);
            if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text))
            {
                DebugLogger.Error($"[ShopItemData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<ShopMasterDataContainer>(jsonAsset.text);
                if (container != null && container.items != null && container.items.Count > 0)
                {
                    result.AddRange(container.items);
                    DebugLogger.Log($"[ShopItemData] {result.Count} 種類のショップアイテムを読み込みました。");
                }
                else
                {
                    DebugLogger.Warning($"[ShopItemData] '{path}' のパース結果が空です。");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[ShopItemData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// ShopData.json のリスト形式を JsonUtility でデシリアライズするための内部コンテナクラス。
        /// </summary>
        [Serializable]
        private class ShopMasterDataContainer
        {
            // ショップアイテムマスターデータのリスト
            public List<ShopItemData> items = new List<ShopItemData>();
        }
    }
}

