/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 武器の基本仕様（種別、現在レベル、最大レベル判定、レベルアップ、解放）を規定するインターフェース。
 */

using System.Threading;
using System.Threading.Tasks;

namespace Runner
{
    /// <summary>
    /// 武器の基本機能（種別、現在レベル、レベルアップ、解放）を定義するインターフェース。
    /// 各武器種別（ドローン等）はこのインターフェースを実装してレベル管理と実体同期を行います。
    /// </summary>
    public interface IWeapon
    {
        /// <summary>武器の種別</summary>
        WeaponType WeaponType { get; }

        /// <summary>現在の武器レベル（未所持時は 0、所持時は 1 以上）</summary>
        int CurrentLevel { get; }

        /// <summary>最大レベルに達しているかどうか</summary>
        bool IsMaxLevel { get; }

        /// <summary>
        /// 武器をレベルアップし、性能パラメータや関連実体を同期・更新する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>レベルアップに成功した場合は true、最大レベル到達時等は false</returns>
        Task<bool> UpgradeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// この武器が管理するすべての実体オブジェクトを解放・破棄し、状態をリセットする。
        /// </summary>
        void Release();
    }
}

