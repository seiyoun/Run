/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 武器オブジェクト（ドローン等）を Addressables から非同期ロード・生成し、スロット番号の割り当てと生成数管理を行う武器スポナークラス。
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Addressables から武器オブジェクト（ドローン等）を非同期生成するシングルトンスポナークラス。
    /// ドローン生成時は現在の機体数を管理し、各機体にスロット番号（インデックス）を渡して固有オフセットを適用します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponSpawner : SingletonMonoBehaviour<WeaponSpawner>
    {
        private const string DroneAddress = "Drone";

        private readonly List<DroneFollower> activeDrones = new List<DroneFollower>();
        private readonly List<GameObject> activeWeapons = new List<GameObject>();

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実にリソースを解放する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<WeaponSpawner>.Instance != null;

        /// <summary>
        /// WeaponSpawner の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static WeaponSpawner Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<WeaponSpawner>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<WeaponSpawner>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(WeaponSpawner));
                return obj.AddComponent<WeaponSpawner>();
            }
        }

        /// <summary>現在アクティブなドローンの生成数</summary>
        public int ActiveDroneCount
        {
            get
            {
                activeDrones.RemoveAll(d => d == null);
                return activeDrones.Count;
            }
        }

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// オブジェクト破棄時のリソース解放および全管理武器の破棄を行う。
        /// </summary>
        protected override void OnDestroy()
        {
            ReleaseAllWeapons();
            base.OnDestroy();
        }

        /// <summary>
        /// 指定した種別の武器を非同期生成する。ドローン生成時は現在のスロット番号を渡して固有オフセットを設定します。
        /// </summary>
        /// <param name="weaponType">生成する武器の種別（デフォルト: Drone）</param>
        /// <param name="position">生成位置（null の場合は PlayerController.Instance の座標または原点を使用）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された武器 GameObject（失敗時は null）</returns>
        public async Task<GameObject> SpawnWeaponAsync(WeaponType weaponType = WeaponType.Drone, Vector3? position = null, CancellationToken cancellationToken = default)
        {
            var spawnPos = position ?? (PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero);

            string address = GetAddressForWeaponType(weaponType);
            if (string.IsNullOrEmpty(address))
            {
                DebugLogger.Error($"[WeaponSpawner] 未対応の武器種別です: {weaponType}");
                return null;
            }

            int assignedDroneIndex = 0;
            if (weaponType == WeaponType.Drone)
            {
                activeDrones.RemoveAll(d => d == null);
                assignedDroneIndex = activeDrones.Count;
            }

            try
            {
                var weaponObj = await AddressableManager.InstantiatePrefabAsync(
                    address,
                    spawnPos,
                    Quaternion.identity,
                    null,
                    cancellationToken);

                if (weaponObj != null)
                {
                    activeWeapons.Add(weaponObj);

                    if (weaponType == WeaponType.Drone)
                    {
                        var droneFollower = weaponObj.GetComponent<DroneFollower>();
                        if (droneFollower != null)
                        {
                            droneFollower.SetIndex(assignedDroneIndex);
                            activeDrones.Add(droneFollower);
                            DebugLogger.Log($"[WeaponSpawner] ドローン武器 #{assignedDroneIndex} を生成・番号割り当てしました。座標: {weaponObj.transform.position}");
                        }
                    }
                    else
                    {
                        DebugLogger.Log($"[WeaponSpawner] 武器 ({weaponType}) を生成しました。座標: {weaponObj.transform.position}");
                    }

                    return weaponObj;
                }
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log($"[WeaponSpawner] 武器 ({weaponType}) の生成がキャンセルされました。");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[WeaponSpawner] 武器 ({weaponType}) の生成に失敗しました: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 指定した武器インスタンスを解放・破棄する。ドローンの場合は残ったドローンのスロット番号を再計算します。
        /// </summary>
        /// <param name="weaponObj">解放対象の武器 GameObject</param>
        public void ReleaseWeapon(GameObject weaponObj)
        {
            if (weaponObj == null) return;

            activeWeapons.Remove(weaponObj);

            if (weaponObj.TryGetComponent<DroneFollower>(out var drone))
            {
                activeDrones.Remove(drone);
                RefreshDroneIndices();
            }

            AddressableManager.ReleaseInstance(weaponObj);
        }

        /// <summary>
        /// 管理中のすべての武器インスタンスを解放・破棄し、リストをクリアする。
        /// </summary>
        public void ReleaseAllWeapons()
        {
            for (int i = activeWeapons.Count - 1; i >= 0; i--)
            {
                var weapon = activeWeapons[i];
                if (weapon != null)
                {
                    AddressableManager.ReleaseInstance(weapon);
                }
            }

            activeWeapons.Clear();
            activeDrones.Clear();
        }

        /// <summary>
        /// 残っているアクティブなドローンのスロット番号を再振り分けする。
        /// </summary>
        private void RefreshDroneIndices()
        {
            activeDrones.RemoveAll(d => d == null);
            for (int i = 0; i < activeDrones.Count; i++)
            {
                activeDrones[i].SetIndex(i);
            }
        }

        /// <summary>
        /// 武器種別に対応する Addressables のアドレス文字列を取得する。
        /// </summary>
        /// <param name="weaponType">対象の武器種別</param>
        /// <returns>Addressables アドレス文字列（未対応時は null）</returns>
        private string GetAddressForWeaponType(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Drone:
                    return DroneAddress;
                default:
                    return null;
            }
        }
    }
}
