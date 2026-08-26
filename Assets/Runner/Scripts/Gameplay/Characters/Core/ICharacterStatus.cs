/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクターのステータス（HP値、回復、被ダメージ、死亡）を抽象化するインターフェース。
 */

using System;

namespace Runner
{
    /// <summary>
    /// キャラクターやエンティティのステータス・体力管理インターフェース。
    /// 被ダメージ (<see cref="IDamageable"/>) および回復 (<see cref="IHealable"/>) を継承・統合します。
    /// </summary>
    public interface ICharacterStatus : IDamageable, IHealable
    {
        /// <summary>現在のHP</summary>
        int CurrentHp { get; }

        /// <summary>最大HP</summary>
        int MaxHp { get; }

        /// <summary>正規化されたHP割合（0.0 〜 1.0）</summary>
        float NormalizedHp { get; }

        /// <summary>HPが変動した際に発火するイベント (現在のHP, 最大HP)</summary>
        event Action<int, int> OnHpChanged;

        /// <summary>最大HPを設定する</summary>
        /// <param name="maxHp">設定する最大HP</param>
        /// <param name="restoreCurrent">現在HPも全快にするか</param>
        void SetMaxHp(int maxHp, bool restoreCurrent = false);
    }
}
