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
    /// 同一 GameObject 内の ICharacterVisual / ICharacterAnimator と自動連携して被弾演出を再生します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterStatus : MonoBehaviour, ICharacterStatus
    {
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

        private ICharacterVisual characterVisual;
        private ICharacterAnimator characterAnimator;

        #region ICharacterStatus Implementation

        public int CurrentHp => currentHp;
        public int MaxHp => maxHp;
        public float NormalizedHp => maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
        public bool IsDead => currentHp <= 0;

        public event Action<int, int> OnHpChanged;
        public event Action<int> OnTakeDamage;
        public event Action<int> OnHeal;
        public event Action OnDead;

        /// <summary>
        /// ダメージを受ける。
        /// </summary>
        /// <param name="amount">ダメージ量</param>
        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            var actualDamage = Mathf.Min(amount, currentHp);
            currentHp -= actualDamage;

            DebugLogger.Log($"[{gameObject.name}] ダメージを受けました: -{actualDamage} (残HP: {currentHp}/{maxHp})");

            OnTakeDamage?.Invoke(actualDamage);
            OnHpChanged?.Invoke(currentHp, maxHp);

            // 被弾エフェクト・アニメーション再生
            characterVisual?.PlayHitFlash();

            if (currentHp <= 0)
            {
                Die();
            }
            else
            {
                characterAnimator?.TriggerHit();
            }
        }

        /// <summary>
        /// HPを回復する。
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

        #endregion

        private void Awake()
        {
            currentHp = maxHp;
            characterVisual = GetComponent<ICharacterVisual>() ?? GetComponentInChildren<ICharacterVisual>();
            characterAnimator = GetComponent<ICharacterAnimator>() ?? GetComponentInChildren<ICharacterAnimator>();
        }

        private void Start()
        {
            // 初期状態をイベント通知
            OnHpChanged?.Invoke(currentHp, maxHp);
        }

        /// <summary>
        /// 死亡時の処理。
        /// </summary>
        private void Die()
        {
            DebugLogger.Log($"[{gameObject.name}] が力尽きました。");
            characterAnimator?.PlayDie();
            OnDead?.Invoke();

            if (destroyOnDead)
            {
                Destroy(gameObject, 0.2f);
            }
        }
    }
}
