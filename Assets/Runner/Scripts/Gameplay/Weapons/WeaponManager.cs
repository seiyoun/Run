/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 武器インターフェース IWeapon を介して各武器のレベル管理およびアップグレード・解放を統括する武器マネージャー。
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
    /// 武器のレベル管理およびアップグレードを一元管理するシングルトンマネージャークラス。
    /// 各武器種別（IWeapon）を辞書で保持し、ドローン等の具象実装に直接依存することなく抽象インターフェースを通じて操作します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponManager : SingletonMonoBehaviour<WeaponManager>
    {
        private readonly Dictionary<WeaponType, IWeapon> activeWeapons = new Dictionary<WeaponType, IWeapon>();

        /// <summary>Game シーン破棄時に一緒に破棄させ、確実に状態を初期化する</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<WeaponManager>.Instance != null;

        /// <summary>
        /// WeaponManager の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static WeaponManager Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<WeaponManager>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<WeaponManager>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(WeaponManager));
                return obj.AddComponent<WeaponManager>();
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
        /// オブジェクト破棄時の状態クリアおよび全武器実体の解放を行う。
        /// </summary>
        protected override void OnDestroy()
        {
            ResetAllWeapons();
            base.OnDestroy();
        }

        /// <summary>
        /// 指定した武器種別の現在のレベルを取得する（未所持時は 0）。
        /// </summary>
        /// <param name="weaponType">対象の武器種別</param>
        /// <returns>現在の武器レベル（未所持時は 0）</returns>
        public int GetWeaponLevel(WeaponType weaponType)
        {
            return activeWeapons.TryGetValue(weaponType, out var weapon) ? weapon.CurrentLevel : 0;
        }

        /// <summary>
        /// 指定した武器種別が最大レベルに達しているかを判定する。
        /// </summary>
        /// <param name="weaponType">対象の武器種別</param>
        /// <returns>最大レベルに達していれば true</returns>
        public bool IsMaxLevel(WeaponType weaponType)
        {
            return activeWeapons.TryGetValue(weaponType, out var weapon) && weapon.IsMaxLevel;
        }

        /// <summary>
        /// 指定した武器種別をレベルアップする。未所持の場合は新規インスタンスを生成して Lv.1 にセットアップします。
        /// </summary>
        /// <param name="weaponType">レベルアップ対象の武器種別（デフォルト: Drone）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>レベルアップに成功した場合は true、最大レベル到達時や失敗時は false</returns>
        public async Task<bool> UpgradeWeaponAsync(WeaponType weaponType = WeaponType.Drone, CancellationToken cancellationToken = default)
        {
            if (!activeWeapons.TryGetValue(weaponType, out var weapon))
            {
                weapon = CreateWeapon(weaponType);
                if (weapon == null) return false;
                activeWeapons[weaponType] = weapon;
            }

            return await weapon.UpgradeAsync(cancellationToken);
        }

        /// <summary>
        /// 管理中のすべての武器を解放・破棄し、レベル状態を初期化する。
        /// </summary>
        public void ResetAllWeapons()
        {
            foreach (var weapon in activeWeapons.Values)
            {
                weapon?.Release();
            }

            activeWeapons.Clear();
            DebugLogger.Log("[WeaponManager] すべての武器をリセット・解放しました。");
        }

        /// <summary>
        /// 武器種別に対応する IWeapon インスタンスを生成する。
        /// </summary>
        /// <param name="weaponType">武器種別</param>
        /// <returns>生成された IWeapon インスタンス（未対応時は null）</returns>
        private IWeapon CreateWeapon(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Drone:
                    return new DroneWeapon();
                default:
                    DebugLogger.Error($"[WeaponManager] 未対応の武器種別です: {weaponType}");
                    return null;
            }
        }
    }
}
