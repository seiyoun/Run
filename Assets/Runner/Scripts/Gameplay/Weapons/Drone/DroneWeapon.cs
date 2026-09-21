/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローン武器のレベル管理および機体同期・パラメータ設定を担当する武器実装クラス。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ドローン武器のレベルおよび関連する機体群を統括・管理する IWeapon 実装クラス。
    /// レベルアップに応じて不足分の機体を WeaponSpawner から追加生成し、全機体へ性能パラメータを同期します。
    /// </summary>
    public sealed class DroneWeapon : IWeapon
    {
        private const int MinLevel = 1;
        private const int DefaultMaxLevel = 5;

        private readonly List<DroneFollower> activeDrones = new List<DroneFollower>();
        private int currentLevel;

        /// <summary>武器の種別（ドローン）</summary>
        public WeaponType WeaponType => WeaponType.Drone;

        /// <summary>現在の武器レベル（未所持時は 0、所持時は 1 以上）</summary>
        public int CurrentLevel => currentLevel;

        /// <summary>最大レベルに達しているかどうか</summary>
        public bool IsMaxLevel => currentLevel >= MaxLevel;

        /// <summary>最大レベル（マスターデータより取得、未取得時はデフォルト値）</summary>
        private int MaxLevel
        {
            get
            {
                var master = MasterDataManager.GetWeaponMasterData(WeaponType.Drone);
                return master != null ? master.MaxLevel : DefaultMaxLevel;
            }
        }

        /// <summary>
        /// ドローン武器インスタンスを初期化する。
        /// </summary>
        public DroneWeapon()
        {
            currentLevel = 0;
        }

        /// <summary>
        /// ドローン武器の文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            return $"DroneWeapon (Lv.{currentLevel}/{MaxLevel}, Drones: {activeDrones.Count})";
        }

        /// <summary>
        /// ドローン武器をレベルアップし、目標機数までの追加生成および全機体の性能パラメータを同期する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>レベルアップに成功した場合は true、最大レベル到達時等は false</returns>
        public async Task<bool> UpgradeAsync(CancellationToken cancellationToken = default)
        {
            if (IsMaxLevel)
            {
                DebugLogger.Log($"[DroneWeapon] ドローンは既に最大レベル (Lv.{currentLevel}) に達しています。");
                return false;
            }

            currentLevel = Mathf.Clamp(currentLevel + 1, MinLevel, MaxLevel);

            var master = MasterDataManager.GetWeaponMasterData(WeaponType.Drone);
            var levelData = master != null
                ? master.GetLevelData(currentLevel)
                : new WeaponLevelData(currentLevel, 10 * currentLevel, 1.5f, currentLevel);

            DebugLogger.Log($"[DroneWeapon] ドローンを Lv.{currentLevel} にアップグレードします。(目標数: {levelData.Count}, Power: {levelData.AttackPower}, Interval: {levelData.AttackInterval:F1}s)");

            await SyncDronesAsync(levelData, cancellationToken);
            return true;
        }

        /// <summary>
        /// 管理中のすべてのドローン実体を解放・破棄し、レベルを初期状態（Lv.0）にリセットする。
        /// </summary>
        public void Release()
        {
            activeDrones.RemoveAll(d => d == null);

            if (WeaponSpawner.Instance != null)
            {
                for (int i = activeDrones.Count - 1; i >= 0; i--)
                {
                    var drone = activeDrones[i];
                    if (drone != null)
                    {
                        WeaponSpawner.Instance.ReleaseWeapon(drone.gameObject);
                    }
                }
            }

            activeDrones.Clear();
            currentLevel = 0;
            DebugLogger.Log("[DroneWeapon] すべてのドローンを解放し、レベルをリセットしました。");
        }

        /// <summary>
        /// 既存ドローンのパラメータ更新および新レベルの目標数に達するまでの不足機数生成を行う。
        /// </summary>
        /// <param name="levelData">適用対象の武器レベルデータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        private async Task SyncDronesAsync(WeaponLevelData levelData, CancellationToken cancellationToken)
        {
            if (levelData == null || WeaponSpawner.Instance == null) return;

            activeDrones.RemoveAll(d => d == null);

            // 既存ドローンのパラメータを一括更新
            for (int i = 0; i < activeDrones.Count; i++)
            {
                activeDrones[i].ApplyLevelData(levelData);
            }

            // 目標機数に達するまで追加生成
            int targetCount = Mathf.Max(1, levelData.Count);
            int neededCount = targetCount - activeDrones.Count;

            for (int i = 0; i < neededCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var droneObj = await WeaponSpawner.Instance.SpawnWeaponAsync(WeaponType.Drone, null, cancellationToken);
                if (droneObj != null && droneObj.TryGetComponent<DroneFollower>(out var follower))
                {
                    follower.ApplyLevelData(levelData);
                    activeDrones.Add(follower);
                }
            }
        }
    }
}

