/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ステージごとの設定（ステージID、脱出までの時間、背景ID、出現ウェーブIDリスト）を保持・JSONシリアライズするマスターデータクラス。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ステージの基本設定（脱出制限時間、背景ID、出現ウェーブIDリスト）を表すマスターデータクラス。
    /// </summary>
    [Serializable]
    public sealed class StageMasterData
    {
        /// <summary>デフォルトのリソース配置パス</summary>
        public const string DefaultResourcePath = "Data/StageData";

        [Tooltip("ステージ識別番号（ステージ数）")]
        public int stageId = 1;

        [Tooltip("脱出までの制限時間（秒）")]
        public float escapeTime = 180f;

        [Tooltip("生成する背景プレハブのアドレス/ID")]
        public string backgroundId = "ArenaBackground";

        [Tooltip("このステージで発生する出現ウェーブIDのリスト")]
        public List<int> waveIds = new List<int>();

        /// <summary>ステージ識別番号（ステージ数）</summary>
        public int StageId => stageId;

        /// <summary>脱出までの制限時間（秒）</summary>
        public float EscapeTime => escapeTime;

        /// <summary>背景プレハブのアドレス/ID</summary>
        public string BackgroundId => backgroundId;

        /// <summary>出現ウェーブIDリスト</summary>
        public IReadOnlyList<int> WaveIds => waveIds;

        /// <summary>
        /// デフォルトコンストラクタ（JSONデシリアライズ用）。
        /// </summary>
        public StageMasterData()
        {
        }

        /// <summary>
        /// Resources から StageData.json を同期ロードし、全ステージマスターデータのリストを取得する。
        /// </summary>
        /// <param name="path">リソースパス</param>
        /// <returns>ロードされた StageMasterData のリスト</returns>
        public static List<StageMasterData> LoadAllFromResources(string path = DefaultResourcePath)
        {
            var result = new List<StageMasterData>();
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null || string.IsNullOrWhiteSpace(textAsset.text))
            {
                DebugLogger.Error($"[StageMasterData] '{path}' のロードに失敗しました。");
                return result;
            }

            try
            {
                var container = JsonUtility.FromJson<StageMasterDataContainer>(textAsset.text);
                if (container != null && container.stages != null)
                {
                    result.AddRange(container.stages);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[StageMasterData] JSONパースエラー: {ex.Message}");
            }

            return result;
        }

        [Serializable]
        private class StageMasterDataContainer
        {
            public List<StageMasterData> stages = new List<StageMasterData>();
        }
    }
}

