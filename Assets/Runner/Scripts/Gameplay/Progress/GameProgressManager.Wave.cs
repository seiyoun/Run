/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: GameProgressManager のエネミースポーンウェーブ管理・切り替え判定を担う partial クラス。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    public sealed partial class GameProgressManager
    {
        /// <summary>ウェーブ切り替わりイベント（引数: 新しいウェーブ設定データ）</summary>
        public event Action<SpawnWaveData> OnWaveChanged;

        [Tooltip("時間帯ごとのスポーンウェーブ設定リスト")]
        [SerializeField] private List<SpawnWaveData> waveDataList = new List<SpawnWaveData>();

        private SpawnWaveData currentWave;

        /// <summary>現在アクティブなウェーブ設定データ</summary>
        public SpawnWaveData CurrentWave => currentWave;

        /// <summary>
        /// GameLoadingState 等からロードされたウェーブ設定データを反映する。
        /// </summary>
        /// <param name="waves">設定するウェーブデータリスト</param>
        public void SetWaveData(IReadOnlyList<SpawnWaveData> waves)
        {
            waveDataList.Clear();
            if (waves != null)
            {
                waveDataList.AddRange(waves);
            }
        }

        /// <summary>
        /// ウェーブ進行状態を初期化し、初期ウェーブを判定・適用する。
        /// </summary>
        private void StartWaveProgress()
        {
            currentWave = null;
            UpdateCurrentWave();
        }

        /// <summary>
        /// 経過時間に応じたウェーブの更新を評価する。
        /// </summary>
        private void TickWave()
        {
            UpdateCurrentWave();
        }

        /// <summary>
        /// 現在の経過時間に対応するウェーブを検索し、切り替わりがあればイベントを発行する。
        /// </summary>
        private void UpdateCurrentWave()
        {
            SpawnWaveData matchingWave = null;
            if (waveDataList != null)
            {
                for (int i = 0; i < waveDataList.Count; i++)
                {
                    if (waveDataList[i].IsActive(elapsedTime))
                    {
                        matchingWave = waveDataList[i];
                        break;
                    }
                }
            }

            if (matchingWave != currentWave)
            {
                currentWave = matchingWave;
                if (currentWave != null)
                {
                    DebugLogger.Log($"[GameProgressManager] ウェーブが切り替わりました: ID {currentWave.WaveId} ({currentWave.StartTime:F0}s〜{currentWave.EndTime:F0}s, 間隔: {currentWave.SpawnInterval}s, 最大: {currentWave.MaxAliveCount})");
                    OnWaveChanged?.Invoke(currentWave);
                }
            }
        }

        /// <summary>
        /// ウェーブ関連イベントの購読を解除する。
        /// </summary>
        private void CleanupWaveEvents()
        {
            currentWave = null;
            OnWaveChanged = null;
        }
    }
}
