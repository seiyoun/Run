/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ICharacterStatus を実装したキャラクター体力・ステータス管理コンポーネント。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// キャラクターのHP・ダメージ・回復・死亡処理を管理するコンポーネント。
    /// 体力の変動および被ダメージ・回復・死亡イベントの通知を担当します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterStatus : MonoBehaviour, ICharacterStatus, IDamageable, IHealable
    {
        /// <summary>HPが変動した際に発火するイベント (現在のHP, 最大HP)</summary>
        public event Action<int, int> OnHpChanged;

        /// <summary>ダメージを受けた際に発火するイベント (実際のダメージ量)</summary>
        public event Action<int> OnTakeDamage;

        /// <summary>HPが回復した際に発火するイベント (実際の回復量)</summary>
        public event Action<int> OnHeal;

        /// <summary>HPが0になり死亡した際に発火するイベント</summary>
        public event Action OnDead;

        [Header("HP Settings")]
        [Tooltip("最大HP")]
        [SerializeField]
        private int maxHp = 100;

        [Tooltip("現在のHP")]
        [SerializeField]
        private int currentHp = 100;

        [Tooltip("死亡時に GameObject を破棄するか（敵モンスター等で有効）")]
        [SerializeField]
        private bool destroyOnDead = false;

        [Tooltip("無敵状態かどうか（被ダメージを無効化する）")]
        [SerializeField]
        private bool isInvincible = false;

        /// <summary>現在のHP</summary>
        public int CurrentHp => currentHp;

        /// <summary>最大HP</summary>
        public int MaxHp => maxHp;

        /// <summary>正規化されたHP割合（0.0 〜 1.0）</summary>
        public float NormalizedHp => maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;

        /// <summary>死亡状態であるかどうか</summary>
        public bool IsDead => currentHp <= 0;

        /// <summary>無敵状態かどうか（被ダメージを無効化する）</summary>
        public bool IsInvincible
        {
            get => isInvincible;
            set => isInvincible = value;
        }

        /// <summary>
        /// コンポーネントの初期化と初期HPの設定を行う。
        /// </summary>
        private void Awake()
        {
            currentHp = maxHp;
        }

        /// <summary>
        /// 初回フレームで初期ステータスをイベント通知する。
        /// </summary>
        private void Start()
        {
            OnHpChanged?.Invoke(currentHp, maxHp);
        }

        /// <summary>
        /// ダメージを受け、HPを減少させてイベントを通知する。
        /// </summary>
        /// <param name="amount">ダメージ量</param>
        public void TakeDamage(int amount)
        {
            if (IsDead || isInvincible || amount <= 0) return;

            var actualDamage = Mathf.Min(amount, currentHp);
            currentHp -= actualDamage;

            DebugLogger.Log($"[{gameObject.name}] ダメージを受けました: -{actualDamage} (残HP: {currentHp}/{maxHp})");

            OnTakeDamage?.Invoke(actualDamage);
            OnHpChanged?.Invoke(currentHp, maxHp);

            if (currentHp <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// HPを回復し、イベントを通知する。
        /// </summary>
        /// <param name="amount">回復量</param>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;

            var prevHp = currentHp;
            currentHp = Mathf.Min(currentHp + amount, maxHp);
            var actualHealed = currentHp - prevHp;

            if (actualHealed > 0)
            {
                DebugLogger.Log($"[{gameObject.name}] HPが回復しました: +{actualHealed} (残HP: {currentHp}/{maxHp})");
                OnHeal?.Invoke(actualHealed);
                OnHpChanged?.Invoke(currentHp, maxHp);
            }
        }

        /// <summary>
        /// 最大HPを設定する。
        /// </summary>
        /// <param name="newMaxHp">設定する最大HP</param>
        /// <param name="restoreCurrent">現在HPも全快にするか</param>
        public void SetMaxHp(int newMaxHp, bool restoreCurrent = false)
        {
            maxHp = Mathf.Max(1, newMaxHp);
            if (restoreCurrent)
            {
                currentHp = maxHp;
            }
            else
            {
                currentHp = Mathf.Min(currentHp, maxHp);
            }

            OnHpChanged?.Invoke(currentHp, maxHp);
        }

        /// <summary>
        /// 死亡時の処理を実行し、イベントを通知する。
        /// </summary>
        private void Die()
        {
            DebugLogger.Log($"[{gameObject.name}] が力尽きました。");
            OnDead?.Invoke();

            if (destroyOnDead)
            {
                Destroy(gameObject, 0.2f);
            }
        }
    }
}
