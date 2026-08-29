/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中ステート。入力コントローラーをプレイヤーに接続し、移動・アクション・死亡判定を有効化する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームプレイ中のステート。
    /// </summary>
    public sealed class GamePlayingState : IState<GamePlayState>
    {
        public GamePlayState State => GamePlayState.Playing;

        private readonly IGameContext context;

        public GamePlayingState(IGameContext context)
        {
            this.context = context;
        }

        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GamePlayingState] ゲームプレイ開始！プレイヤー入力を有効化します。");

            var player = context.Player;
            if (player != null)
            {
                // InputController のコールバックに接続して操作を有効化
                if (InputController.Instance != null)
                {
                    player.BindInput(InputController.Instance);
                }

                // プレイヤー死亡イベントを購読
                if (player.Status != null)
                {
                    player.Status.OnDead += HandlePlayerDead;
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
        /// 毎フレームのゲームプレイ更新処理を実行する。
        /// ショップモーダル表示中などのポーズ時は deltaTime を 0 にしてプレイヤー更新を停止します。
        /// </summary>
        public void Update()
        {
            // ポーズ状態の判定（ショップモーダル表示中またはTime.timeScaleが0以下）
            bool isPaused = (GameHUDView.Instance != null && GameHUDView.Instance.ShopModal != null && GameHUDView.Instance.ShopModal.IsOpen)
                            || Time.timeScale <= 0f;

            float deltaTime = isPaused ? 0f : Time.deltaTime;

            // プレイヤーのメイン更新処理を実行
            var player = context.Player;
            if (player != null)
            {
                player.OnUpdate(deltaTime);
            }
        }

        public void Exit()
        {
            var player = context.Player;
            if (player != null && player.Status != null)
            {
                player.Status.OnDead -= HandlePlayerDead;
            }

            DebugLogger.Log("[GamePlayingState] プレイ中ステートを終了しました。");
        }

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
