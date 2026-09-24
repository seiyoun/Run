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

        /// <summary>ウェーブ切り替わりイベント（引数: 新しいウェーブ設定データ）</summary>
        public event Action<SpawnWaveData> OnWaveChanged;

        [Tooltip("時間帯ごとのスポーンウェーブ設定リスト")]
        [SerializeField] private List<SpawnWaveData> waveDataList = new List<SpawnWaveData>();

        private float elapsedTime;
        private float escapeDuration = 180f;
        private bool isProgressing;
        private SpawnWaveData currentWave;

        /// <summary>ゲーム開始からの累計経過時間（秒）</summary>
        public float ElapsedTime => elapsedTime;

        /// <summary>ステージで設定された脱出までの制限時間（秒）</summary>
        public float EscapeDuration => escapeDuration;

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
            OnWaveChanged = null;
            base.OnDestroy();
        }

        /// <summary>Game シーン破棄時に一緒に破棄させる</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

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
        /// ゲーム進行（時間の計測）を開始し、初期ウェーブを適用する。
        /// </summary>
        public void StartProgress()
        {
            elapsedTime = 0f;
            isProgressing = true;
            currentWave = null;
            UpdateCurrentWave();

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
        /// 毎フレームの進行時間を加算し、イベント通知およびウェーブ更新を評価する。
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        public void Tick(float deltaTime)
        {
            if (!isProgressing || deltaTime <= 0f) return;

            elapsedTime += deltaTime;
            OnTimeUpdated?.Invoke(elapsedTime);
            UpdateCurrentWave();
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
    }
}
