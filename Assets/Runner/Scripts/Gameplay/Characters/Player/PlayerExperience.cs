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

        [Header("Movement EXP Settings")]
        [Tooltip("移動による経験値獲得を有効にするか")]
        [SerializeField] private bool gainExpByMoving = true;
        [Tooltip("経験値を獲得するのに必要な移動距離 (ワールド座標単位)")]
        [SerializeField] private float distancePerExp = 2.0f;
        [Tooltip("距離達成時に獲得する経験値量")]
        [SerializeField] private int expPerDistance = 1;
        [Tooltip("テレポートやリスポーン時の急激な移動を無視する閾値距離")]
        [SerializeField] private float teleportThreshold = 5.0f;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExp { get; private set; } = 0;
        public int RequiredExp { get; private set; } = 10;

        /// <summary>
        /// 現在のレベルにおける経験値の充填率（0.0 〜 1.0）
        /// </summary>
        public float NormalizedExp => RequiredExp > 0 ? Mathf.Clamp01((float)CurrentExp / RequiredExp) : 0f;

        /// <summary>
        /// 移動による経験値獲得の有効/無効
        /// </summary>
        public bool GainExpByMoving
        {
            get => gainExpByMoving;
            set => gainExpByMoving = value;
        }

        /// <summary>
        /// 経験値が変動した際に発火するイベント (現在のEXP, 必要EXP, 現在のLevel)
        /// </summary>
        public event Action<int, int, int> OnExpChanged;

        /// <summary>
        /// レベルアップした際に発火するイベント (新Level)
        /// </summary>
        public event Action<int> OnLevelUp;

        private Vector3 lastPosition;
        private float accumulatedDistance;

        private void Awake()
        {
            CurrentLevel = Mathf.Max(1, initialLevel);
            CurrentExp = 0;
            RequiredExp = CalculateRequiredExp(CurrentLevel);
        }

        private void Start()
        {
            lastPosition = transform.position;
            accumulatedDistance = 0f;

            // 初期状態を通知
            OnExpChanged?.Invoke(CurrentExp, RequiredExp, CurrentLevel);
        }

        private void Update()
        {
            UpdateMovementExp();
        }

        /// <summary>
        /// 移動距離を監視し、一定距離を移動するごとに経験値を加算する。
        /// </summary>
        private void UpdateMovementExp()
        {
            if (!gainExpByMoving)
            {
                lastPosition = transform.position;
                return;
            }

            Vector3 currentPos = transform.position;
            float distance = Vector3.Distance(currentPos, lastPosition);

            if (distance > 0f)
            {
                // テレポートや初期化時の急激な移動を無視
                if (distance <= teleportThreshold)
                {
                    accumulatedDistance += distance;

                    // HUDにプレイヤー移動（歩数・ポイ活・怒りゲージ）を通知
                    if (GameHUDView.Instance != null)
                    {
                        GameHUDView.Instance.OnPlayerMoved(distance);
                    }

                    // 設定された基準移動距離に達しているか判定
                    if (distancePerExp > 0f && accumulatedDistance >= distancePerExp)
                    {
                        // 基準移動距離を満たした回数を算出
                        int count = Mathf.FloorToInt(accumulatedDistance / distancePerExp);
                        // 余剰分の移動距離を保持（端数を繰り越し）
                        accumulatedDistance %= distancePerExp;
                        // 移動回数に応じた経験値を加算
                        AddExp(count * expPerDistance);
                    }
                }

                lastPosition = currentPos;
            }
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
            accumulatedDistance = 0f;
            lastPosition = transform.position;
            RequiredExp = CalculateRequiredExp(CurrentLevel);
            OnExpChanged?.Invoke(CurrentExp, RequiredExp, CurrentLevel);
        }
    }
}

