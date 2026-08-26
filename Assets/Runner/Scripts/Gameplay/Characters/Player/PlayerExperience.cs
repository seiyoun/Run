/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの経験値（EXP）およびレベル上昇を管理するコンポーネント。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの経験値獲得とレベルアップを管理するクラス。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerExperience : MonoBehaviour
    {
        [Header("Experience Settings")]
        [SerializeField] private int initialLevel = 1;
        [SerializeField] private int baseRequiredExp = 10;
        [SerializeField] private float expGrowthRate = 1.2f;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int RequiredExp { get; private set; } = 10;

        /// <summary>
        /// 現在のレベルにおける経験値の充填率（0.0 〜 1.0）
        /// </summary>
        public float NormalizedExp => RequiredExp > 0 ? Mathf.Clamp01((float)CurrentExp / RequiredExp) : 0f;

        /// <summary>
        /// 経験値が変動した際に発火するイベント (現在のEXP, 必要EXP, 現在のLevel)
        /// </summary>
        public event Action<int, int, int> OnExpChanged;

        /// <summary>
        /// レベルアップした際に発火するイベント (新Level)
        /// </summary>
        public event Action<int> OnLevelUp;

        private void Awake()
        {
            CurrentLevel = Mathf.Max(1, initialLevel);
            CurrentExp = 0;
            RequiredExp = CalculateRequiredExp(CurrentLevel);
        }

        private void Start()
        {
            // 初期状態を通知
            OnExpChanged?.Invoke(CurrentExp, RequiredExp, CurrentLevel);
        }

        /// <summary>
        /// 経験値を加算する。必要経験値に達した場合はレベルアップ処理を行う。
        /// </summary>
        /// <param name="amount">獲得経験値量</param>
        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            CurrentExp += amount;
            DebugLogger.Log($"[PlayerExperience] EXP獲得: +{amount} (合計: {CurrentExp}/{RequiredExp})");

            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                CurrentLevel++;
                RequiredExp = CalculateRequiredExp(CurrentLevel);
                DebugLogger.Log($"[PlayerExperience] レベルアップ！ Lv.{CurrentLevel} (次の必要EXP: {RequiredExp})");
                OnLevelUp?.Invoke(CurrentLevel);
            }

            OnExpChanged?.Invoke(CurrentExp, RequiredExp, CurrentLevel);
        }

        /// <summary>
        /// 指定レベルに必要な経験値量を計算する。
        /// </summary>
        private int CalculateRequiredExp(int level)
        {
            // 計算例: baseExp * (level ^ growthRate)
            return Mathf.RoundToInt(baseRequiredExp * Mathf.Pow(level, expGrowthRate));
        }

        /// <summary>
        /// デバッグ・リセット用
        /// </summary>
        public void ResetExp(int level = 1)
        {
            CurrentLevel = Mathf.Max(1, level);
            CurrentExp = 0;
            RequiredExp = CalculateRequiredExp(CurrentLevel);
            OnExpChanged?.Invoke(CurrentExp, RequiredExp, CurrentLevel);
        }
    }
}

