/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム進行（経過時間・ウェーブ）を一元管理し、時間更新やウェーブ切り替わりをイベント通知するマネージャー。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームの進捗状況（経過時間・現在の出現ウェーブ）を管理し、イベントを発行するシーン限定シングルトンマネージャー。
    /// 外部コンポーネントはこのマネージャーのイベントを購読することで、疎結合に進行状況に応じた処理を実行できます。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameProgressManager : SingletonMonoBehaviour<GameProgressManager>
    {
        private const long SaleTriggerPointInterval = 300;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<GameProgressManager>.Instance != null;

        /// <summary>GameProgressManager の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。</summary>
        public new static GameProgressManager Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<GameProgressManager>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<GameProgressManager>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(GameProgressManager));
                return obj.AddComponent<GameProgressManager>();
            }
        }

        /// <summary>経過時間更新イベント（引数: 累計経過秒数）</summary>
        public event Action<float> OnTimeUpdated;

        /// <summary>脱出残り時間更新イベント（引数: 脱出までの残り秒数）</summary>
        public event Action<float> OnRemainingTimeUpdated;

        /// <summary>非常口開放イベント</summary>
        public event Action OnExitUnlocked;

        /// <summary>ウェーブ切り替わりイベント（引数: 新しいウェーブ設定データ）</summary>
        public event Action<SpawnWaveData> OnWaveChanged;

        /// <summary>入荷進捗更新イベント（引数: 残り必要ポイント, 進捗率0~1, 初回フラグ）</summary>
        public event Action<long, float, bool> OnRestockProgressUpdated;

        /// <summary>累積ポイント到達によるショップ直接オープン要求イベント</summary>
        public event Action OnShopTriggered;

        [Tooltip("時間帯ごとのスポーンウェーブ設定リスト")]
        [SerializeField] private List<SpawnWaveData> waveDataList = new List<SpawnWaveData>();

        private float elapsedTime;
        private float escapeDuration = 180f;
        private bool isProgressing;
        private bool isExitUnlocked;
        private SpawnWaveData currentWave;
        private long nextSaleTriggerPoint = SaleTriggerPointInterval;

        /// <summary>ゲーム開始からの累計経過時間（秒）</summary>
        public float ElapsedTime => elapsedTime;

        /// <summary>ステージで設定された脱出までの制限時間（秒）</summary>
        public float EscapeDuration => escapeDuration;

        /// <summary>脱出までの残り時間（秒）</summary>
        public float RemainingEscapeTime => Mathf.Max(0f, escapeDuration - elapsedTime);

        /// <summary>非常口が開放されているかどうか</summary>
        public bool IsExitUnlocked => isExitUnlocked;

        /// <summary>ゲーム進行が現在アクティブ（計測中）であるか</summary>
        public bool IsProgressing => isProgressing;

        /// <summary>現在アクティブなウェーブ設定データ</summary>
        public SpawnWaveData CurrentWave => currentWave;

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 破棄時にイベント購読解除を促すためクリーンアップを行う。
        /// </summary>
        protected override void OnDestroy()
        {
            isProgressing = false;
            currentWave = null;
            OnTimeUpdated = null;
            OnRemainingTimeUpdated = null;
            OnExitUnlocked = null;
            OnWaveChanged = null;
            OnRestockProgressUpdated = null;
            OnShopTriggered = null;
            base.OnDestroy();
        }

        /// <summary>Game シーン破棄時に一緒に破棄させる</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>
        /// StageMasterData からステージ設定（脱出制限時間等）を一括反映する。
        /// </summary>
        /// <param name="stageData">適用するステージマスターデータ</param>
        public void ApplyStageData(StageMasterData stageData)
        {
            if (stageData == null) return;
            SetEscapeDuration(stageData.EscapeTime);
        }

        /// <summary>
        /// ステージ設定の脱出制限時間を反映する。
        /// </summary>
        /// <param name="duration">脱出制限時間（秒）</param>
        public void SetEscapeDuration(float duration)
        {
            escapeDuration = Mathf.Max(1f, duration);
        }

        /// <summary>
        /// GameLoadingState 等からロードされたウェーブ設定データを反映する。
        /// </summary>
        /// <param name="waves">設定するウェーブデータリスト</param>
        public void SetWaveData(IReadOnlyList<SpawnWaveData> waves)
        {
            waveDataList.Clear();
            if (waves != null)
            {
                waveDataList.AddRange(waves);
            }
        }

        /// <summary>
        /// ゲーム進行（時間の計測・入荷進捗）を開始し、初期ウェーブおよび初期進捗をイベント通知する。
        /// </summary>
        public void StartProgress()
        {
            elapsedTime = 0f;
            isExitUnlocked = false;
            isProgressing = true;
            currentWave = null;
            nextSaleTriggerPoint = SaleTriggerPointInterval;
            UpdateCurrentWave();
            OnRemainingTimeUpdated?.Invoke(RemainingEscapeTime);

            long initialEarned = GameRecordTracker.HasInstance ? GameRecordTracker.Instance.EarnedMoney : 0;
            long initialCycleEarned = initialEarned % SaleTriggerPointInterval;
            long initialRemaining = SaleTriggerPointInterval - initialCycleEarned;
            float initialProgress = (float)initialCycleEarned / SaleTriggerPointInterval;
            OnRestockProgressUpdated?.Invoke(initialRemaining, initialProgress, true);

            DebugLogger.Log("[GameProgressManager] ゲーム進行の計測を開始しました。");
        }

        /// <summary>
        /// ゲーム進行（時間の計測）を停止する。
        /// </summary>
        public void StopProgress()
        {
            isProgressing = false;
            DebugLogger.Log($"[GameProgressManager] ゲーム進行を停止しました。最終経過時間: {elapsedTime:F2}秒");
        }

        /// <summary>
        /// 毎フレームの進行時間を加算し、イベント通知、ウェーブ更新、および入荷・セール発火進捗を評価する。
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        public void Tick(float deltaTime)
        {
            if (!isProgressing || deltaTime <= 0f) return;

            elapsedTime += deltaTime;
            OnTimeUpdated?.Invoke(elapsedTime);
            OnRemainingTimeUpdated?.Invoke(RemainingEscapeTime);
            UpdateCurrentWave();

            if (!isExitUnlocked && elapsedTime >= escapeDuration)
            {
                isExitUnlocked = true;
                DebugLogger.Log("[GameProgressManager] 脱出制限時間に到達し、非常口が開放されました！");
                OnExitUnlocked?.Invoke();
            }

            UpdateRestockProgress();
        }

        /// <summary>
        /// 現在の経過時間に対応するウェーブを検索し、切り替わりがあればイベントを発行する。
        /// </summary>
        private void UpdateCurrentWave()
        {
            SpawnWaveData matchingWave = null;
            if (waveDataList != null)
            {
                for (int i = 0; i < waveDataList.Count; i++)
                {
                    if (waveDataList[i].IsActive(elapsedTime))
                    {
                        matchingWave = waveDataList[i];
                        break;
                    }
                }
            }

            if (matchingWave != currentWave)
            {
                currentWave = matchingWave;
                if (currentWave != null)
                {
                    DebugLogger.Log($"[GameProgressManager] ウェーブが切り替わりました: ID {currentWave.WaveId} ({currentWave.StartTime:F0}s〜{currentWave.EndTime:F0}s, 間隔: {currentWave.SpawnInterval}s, 最大: {currentWave.MaxAliveCount})");
                    OnWaveChanged?.Invoke(currentWave);
                }
            }
        }

        /// <summary>
        /// 累積獲得ポイントに基づく入荷進捗率の計算およびセール（ショップオープン）発生判定を行う。
        /// </summary>
        private void UpdateRestockProgress()
        {
            long totalEarned = GameRecordTracker.HasInstance ? GameRecordTracker.Instance.EarnedMoney : 0;
            long cycleEarned = totalEarned % SaleTriggerPointInterval;
            long remainingPoints = SaleTriggerPointInterval - cycleEarned;
            float progress = (float)cycleEarned / SaleTriggerPointInterval;

            if (totalEarned >= nextSaleTriggerPoint)
            {
                nextSaleTriggerPoint = ((totalEarned / SaleTriggerPointInterval) + 1) * SaleTriggerPointInterval;
                OnShopTriggered?.Invoke();
                OnRestockProgressUpdated?.Invoke(remainingPoints, progress, false);
            }
            else
            {
                OnRestockProgressUpdated?.Invoke(remainingPoints, progress, false);
            }
        }
    }
}
