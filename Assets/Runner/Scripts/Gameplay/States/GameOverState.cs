/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームオーバーステート。プレイヤーの入力を切断・停止し、リザルト画面を表示してHome画面への遷移を管理する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームオーバーステート。
    /// プレイヤー入力・移動を停止し、リザルト画面を表示してOKボタンによるHomeシーン復帰を統括します。
    /// </summary>
    public sealed class GameOverState : IState<GamePlayState>
    {
        private readonly IGameContext context;
        private GameResultModalView resultModalView;

        /// <summary>現在のステート種別</summary>
        public GamePlayState State => GamePlayState.GameOver;

        /// <summary>
        /// GameOverState のコンストラクタ。
        /// </summary>
        /// <param name="context">ステート間で共有されるコンテキスト</param>
        public GameOverState(IGameContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// ゲームオーバーステート開始時の処理。プレイヤーの操作切断・停止を行い、リザルト画面を表示する。
        /// </summary>
        /// <param name="parameter">開始パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameOverState] ゲームオーバー。プレイヤー入力を切断し、リザルト画面を表示します。");

            var player = context.Player;
            int steps = 0;
            long money = 0;

            if (player != null)
            {
                player.UnbindInput();
                player.Stop();
                steps = player.CurrentSteps;
                money = player.TotalEarnedMoney;
            }

            resultModalView = GameResultModalView.Instance ?? Object.FindFirstObjectByType<GameResultModalView>();

            if (resultModalView != null)
            {
                string message = $"力尽きてしまった...\n\n今回の歩数: {steps:N0} 歩\n獲得マネー: {money:N0} pt";
                resultModalView.Show("GAME OVER", message, HandleOkClicked);
            }
            else
            {
                DebugLogger.Error("[GameOverState] GameResultModalView.Instance が取得できませんでした。");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// ステートの完了を待機する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// 毎フレームの更新処理。
        /// </summary>
        public void Update() { }

        /// <summary>
        /// ステート終了時のクリーンアップ処理。
        /// </summary>
        public void Exit()
        {
            if (resultModalView != null && resultModalView.IsOpen)
            {
                resultModalView.Hide();
            }
        }

        /// <summary>
        /// リザルト画面のOKボタンクリック時にHome画面へ遷移を要求する。
        /// </summary>
        private void HandleOkClicked()
        {
            DebugLogger.Log("[GameOverState] リザルト画面でOKが押されました。Home シーンへの復帰を要求します。");
            context?.RequestExitToHome();
        }
    }
}
