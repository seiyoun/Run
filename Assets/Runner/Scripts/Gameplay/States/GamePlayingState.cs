/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中ステート。入力コントローラーの接続、脱出タイマー・セール通知トリガーの管理、死亡判定を統括する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームプレイ中のステート。
    /// プレイヤー入力・アクション、制限時間（脱出タイマー）、一定ポイント到達によるセール発火判定を統合管理します。
    /// </summary>
    public sealed class GamePlayingState : IState<GamePlayState>
    {
        private const float DefaultEscapeDurationSeconds = 180f;
        private const long SaleTriggerPointInterval = 300;

        private readonly IGameContext context;
        private float remainingEscapeTime;
        private bool isExitUnlocked;
        private long nextSaleTriggerPoint = SaleTriggerPointInterval;

        public GamePlayState State => GamePlayState.Playing;

        /// <summary>脱出制限時間の残り秒数</summary>
        public float RemainingEscapeTime => remainingEscapeTime;

        /// <summary>非常口が開放されているかどうか</summary>
        public bool IsExitUnlocked => isExitUnlocked;

        /// <summary>
        /// GamePlayingState のコンストラクタ。
        /// </summary>
        /// <param name="context">ステート間で共有されるコンテキスト</param>
        public GamePlayingState(IGameContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// プレイ中ステート開始時の初期化、入力バインド、および脱出タイマーのリセットを行う。
        /// </summary>
        /// <param name="parameter">開始パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GamePlayingState] ゲームプレイ開始！プレイヤー入力を有効化し、脱出タイマーを開始します。");

            remainingEscapeTime = DefaultEscapeDurationSeconds;
            isExitUnlocked = false;
            nextSaleTriggerPoint = SaleTriggerPointInterval;

            var player = context.Player;
            if (player != null)
            {
                if (InputController.Instance != null)
                {
                    player.BindInput(InputController.Instance);
                }

                if (player.Status != null)
                {
                    player.Status.OnDead += HandlePlayerDead;
                }
            }

            if (GameHUDView.Instance != null)
            {
                GameHUDView.Instance.BindPlayerEvents();

                if (GameHUDView.Instance.EscapeTimerHUD != null)
                {
                    GameHUDView.Instance.EscapeTimerHUD.SetRemainingTime(remainingEscapeTime);
                    GameHUDView.Instance.EscapeTimerHUD.SetExitUnlocked(false);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ステートの完了を待機する。ゲームオーバーや遷移イベントが発生するまで待機します。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>待機タスク</returns>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 毎フレームのゲームプレイ更新処理（プレイヤー更新、脱出タイマー減算、セール発火判定）を実行する。
        /// ショップモーダル表示中などのポーズ時は deltaTime を 0 にして更新を一時停止します。
        /// </summary>
        public void Update()
        {
            bool isPaused = (GameHUDView.Instance != null && GameHUDView.Instance.ShopModal != null && GameHUDView.Instance.ShopModal.IsOpen)
                            || Time.timeScale <= 0f;

            float deltaTime = isPaused ? 0f : Time.deltaTime;

            var player = context.Player;
            if (player != null)
            {
                player.OnUpdate(deltaTime);

                // 一定ポイント到達によるアイテム入荷・タイムセール通知の判定
                if (player.CurrentMoney >= nextSaleTriggerPoint)
                {
                    nextSaleTriggerPoint += SaleTriggerPointInterval;
                    if (GameHUDView.Instance != null)
                    {
                        GameHUDView.Instance.TriggerItemArrivalNotification();
                    }
                }
            }

            // 脱出タイマーの減算および非常口開放判定
            if (!isPaused && !isExitUnlocked && remainingEscapeTime > 0f)
            {
                remainingEscapeTime -= deltaTime;
                if (remainingEscapeTime <= 0f)
                {
                    remainingEscapeTime = 0f;
                    UnlockExit();
                }

                if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
                {
                    GameHUDView.Instance.EscapeTimerHUD.SetRemainingTime(remainingEscapeTime);
                }
            }
        }

        /// <summary>
        /// プレイ中ステート終了時のクリーンアップ処理を行う。
        /// </summary>
        public void Exit()
        {
            var player = context.Player;
            if (player != null && player.Status != null)
            {
                player.Status.OnDead -= HandlePlayerDead;
            }

            DebugLogger.Log("[GamePlayingState] プレイ中ステートを終了しました。");
        }

        /// <summary>
        /// 非常口を開放し、HUDへ開放通知を行う。
        /// </summary>
        public void UnlockExit()
        {
            if (isExitUnlocked) return;

            isExitUnlocked = true;
            DebugLogger.Log("[GamePlayingState] 非常口が開放されました！改札へ脱出可能になります。");

            if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
            {
                GameHUDView.Instance.EscapeTimerHUD.SetExitUnlocked(true);
            }
        }

        /// <summary>
        /// プレイヤー死亡時のGameOverステート遷移ハンドラ。
        /// </summary>
        private void HandlePlayerDead()
        {
            DebugLogger.Log("[GamePlayingState] プレイヤーが死亡しました。GameOver ステートへ遷移します。");
            if (context.StateMachine != null)
            {
                _ = context.StateMachine.ChangeStateAsync(GamePlayState.GameOver, CancellationToken.None);
            }
        }
    }
}
