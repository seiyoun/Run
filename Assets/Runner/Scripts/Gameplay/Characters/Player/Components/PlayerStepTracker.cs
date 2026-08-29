/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーの移動距離蓄積・歩数カウント・歩行によるポイ活ポイント加算を制御するコンポーネント。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// プレイヤーの移動距離から歩数を換算し、歩行に応じたポイント獲得イベントを通知するトラッカーコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStepTracker : MonoBehaviour
    {
        private const float DefaultThreshold = 0.65f;
        private const long DefaultPoints = 2;

        [Header("Step Settings")]
        [Tooltip("1歩と判定する移動距離(m)")]
        [SerializeField] private float stepDistanceThreshold = DefaultThreshold;

        [Tooltip("1歩あたりに獲得するポイント額")]
        [SerializeField] private long pointsPerStep = DefaultPoints;

        private int currentSteps;
        private float totalDistanceMoved;
        private float stepAccumulator;

        /// <summary>現在の累積総歩数</summary>
        public int CurrentSteps => currentSteps;

        /// <summary>現在の累積総移動距離(m)</summary>
        public float TotalDistanceMoved => totalDistanceMoved;

        /// <summary>1歩判定の移動距離閾値(m)</summary>
        public float StepDistanceThreshold
        {
            get => stepDistanceThreshold;
            set => stepDistanceThreshold = Mathf.Max(0.01f, value);
        }

        /// <summary>1歩あたりの獲得ポイント額</summary>
        public long PointsPerStep
        {
            get => pointsPerStep;
            set => pointsPerStep = Math.Max(0, value);
        }

        /// <summary>歩数が加算された際に発火するイベント (現在の総歩数)</summary>
        public event Action<int> OnStepsChanged;

        /// <summary>移動距離が発生した際に発火するイベント (フレーム移動距離)</summary>
        public event Action<float> OnDistanceMoved;

        /// <summary>
        /// フレームごとの移動距離を受け取り、歩数判定とポイント加算を処理する。
        /// </summary>
        /// <param name="distance">フレーム移動距離(m)</param>
        /// <param name="wallet">ポイント加算対象のウォレット（null時は加算スキップ）</param>
        public void ProcessMovementDistance(float distance, IMoneyCollector wallet = null)
        {
            if (distance <= 0f) return;

            totalDistanceMoved += distance;
            stepAccumulator += distance;

            while (stepAccumulator >= stepDistanceThreshold)
            {
                stepAccumulator -= stepDistanceThreshold;
                currentSteps++;
                wallet?.CollectMoney(pointsPerStep);
                OnStepsChanged?.Invoke(currentSteps);
            }

            OnDistanceMoved?.Invoke(distance);
        }

        /// <summary>
        /// 歩数を直接設定する。
        /// </summary>
        /// <param name="steps">設定する歩数</param>
        public void SetSteps(int steps)
        {
            currentSteps = Math.Max(0, steps);
            OnStepsChanged?.Invoke(currentSteps);
        }
    }
}

