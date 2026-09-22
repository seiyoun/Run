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
    /// </summary>
    public interface ICharacterStatus
    {
        /// <summary>現在のHP</summary>
        int CurrentHp { get; }

        /// <summary>最大HP</summary>
        int MaxHp { get; }

        /// <summary>正規化されたHP割合（0.0 〜 1.0）</summary>
        float NormalizedHp { get; }

        /// <summary>死亡・戦闘不能状態かどうか</summary>
        bool IsDead { get; }

        /// <summary>HPが変動した際に発火するイベント (現在のHP, 最大HP)</summary>
        event Action<int, int> OnHpChanged;

        /// <summary>ダメージを受けた際に発火するイベント (実際のダメージ量)</summary>
        event Action<int> OnTakeDamage;

        /// <summary>HPが回復した際に発火するイベント (実際の回復量)</summary>
        event Action<int> OnHeal;

        /// <summary>HPが0になり死亡した際に発火するイベント</summary>
        event Action OnDead;

        /// <summary>ダメージを受ける</summary>
        /// <param name="amount">ダメージ量</param>
        void TakeDamage(int amount);

        /// <summary>HPを回復する</summary>
        /// <param name="amount">回復量</param>
        void Heal(int amount);

        /// <summary>最大HPを設定する</summary>
        /// <param name="maxHp">設定する最大HP</param>
        /// <param name="restoreCurrent">現在HPも全快にするか</param>
        void SetMaxHp(int maxHp, bool restoreCurrent = false);
    }
}
