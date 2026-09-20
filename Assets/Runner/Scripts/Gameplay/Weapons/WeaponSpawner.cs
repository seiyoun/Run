/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 武器オブジェクト（ドローン等）を Addressables から非同期ロード・生成し、追従対象への関連付けを行う武器スポナークラス。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Addressables から武器オブジェクト（ドローン等）を非同期生成するシングルトンスポナークラス。
    /// プレイヤーなどの対象 Transform に追従コンポーネントを関連付けます。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponSpawner : SingletonMonoBehaviour<WeaponSpawner>
    {
        private const string DroneAddress = "Drone";

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

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// オブジェクト破棄時のリソース解放を行う。
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        /// <summary>
        /// 指定した種別の武器を非同期生成し、指定したターゲットに関連付けます。
        /// </summary>
        /// <param name="weaponType">生成する武器の種別（デフォルト: Drone）</param>
        /// <param name="target">追従対象の Transform（null の場合は PlayerController.Instance を使用）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された武器 GameObject（失敗時は null）</returns>
        public async Task<GameObject> SpawnWeaponAsync(WeaponType weaponType = WeaponType.Drone, Transform target = null, CancellationToken cancellationToken = default)
        {
            if (target == null && PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
            }

            string address = GetAddressForWeaponType(weaponType);
            if (string.IsNullOrEmpty(address))
            {
                DebugLogger.Error($"[WeaponSpawner] 未対応の武器種別です: {weaponType}");
                return null;
            }

            var spawnPos = target != null ? target.position : Vector3.zero;

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
                    DebugLogger.Log($"[WeaponSpawner] 武器 ({weaponType}) を生成しました。座標: {weaponObj.transform.position}");
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

