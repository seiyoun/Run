/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム進行（経過時間・ウェーブ・入荷進捗）のライフサイクルを一元管理・オーケストレーションするマネージャー。
 */

using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームの進捗状況（経過時間・現在の出現ウェーブ・ショップ入荷進捗）を管理し、イベントを発行するシーン限定シングルトンマネージャー。
    /// 外部コンポーネントはこのマネージャーのイベントを購読することで、疎結合に進行状況に応じた処理を実行できます。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class GameProgressManager : SingletonMonoBehaviour<GameProgressManager>
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

        private bool isProgressing;

        /// <summary>ゲーム進行が現在アクティブ（計測中）であるか</summary>
        public bool IsProgressing => isProgressing;

        /// <summary>Game シーン破棄時に一緒に破棄させる</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 破棄時に各ドメインのイベント購読解除を促すためクリーンアップを行う。
        /// </summary>
        protected override void OnDestroy()
        {
            isProgressing = false;
            CleanupEscapeEvents();
            CleanupWaveEvents();
            CleanupRestockEvents();
            base.OnDestroy();
        }

        /// <summary>
        /// ゲーム進行（時間の計測・ウェーブ・入荷進捗）を一括開始し、初期状態をイベント通知する。
        /// </summary>
        public void StartProgress()
        {
            isProgressing = true;
            StartEscapeProgress();
            StartWaveProgress();
            StartRestockProgress();

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
        /// 毎フレームの各ドメイン進行処理（経過時間・脱出、ウェーブ、入荷進捗）を順次実行する。
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        public void Tick(float deltaTime)
        {
            if (!isProgressing || deltaTime <= 0f) return;

            TickEscape(deltaTime);
            TickWave();
            TickRestock();
        }
    }
}
