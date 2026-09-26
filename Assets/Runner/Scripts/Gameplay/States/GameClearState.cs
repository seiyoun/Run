/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 脱出成功後にプレイヤーを停止し、結果UIとホームへの復帰を管理する。
 */

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 脱出成功時のリザルト表示とホームへの復帰を管理するステート。
    /// </summary>
    public sealed class GameClearState : IState<GamePlayState>
    {
        private readonly IGameContext context;
        private GameResultModalView resultModalView;

        /// <summary>現在のステート種別</summary>
        public GamePlayState State => GamePlayState.GameClear;

        /// <summary>
        /// ゲームコンテキストを受け取る。
        /// </summary>
        /// <param name="context">ステート間で共有されるコンテキスト</param>
        public GameClearState(IGameContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 操作を停止し、脱出成功のリザルトUIを表示する。
        /// </summary>
        /// <param name="parameter">開始パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.UnbindInput();
                player.Stop();
            }

            var tracker = GameRecordTracker.HasInstance ? GameRecordTracker.Instance : null;
            var message = new StringBuilder();
            message.AppendLine("非常口から脱出成功！");
            message.AppendLine();
            message.AppendLine($"今回の歩数: {tracker?.TotalSteps ?? 0:N0} 歩");
            message.AppendLine($"獲得マネー: {tracker?.EarnedMoney ?? 0:N0} pt");
            message.Append($"倒した敵: {tracker?.TotalDefeatedCount ?? 0:N0} 体");

            resultModalView = GameResultModalView.Instance ?? Object.FindFirstObjectByType<GameResultModalView>();
            if (resultModalView != null)
            {
                resultModalView.Show("脱出成功！", message.ToString(), HandleOkClicked, new Color(0f, 1f, 0.53f));
            }
            else
            {
                DebugLogger.Error("[GameClearState] GameResultModalView.Instance が取得できませんでした。");
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
        /// クリア画面ではゲーム進行を更新しない。
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        public void Update(float deltaTime) { }

        /// <summary>
        /// ステート終了時にリザルトUIを閉じる。
        /// </summary>
        public void Exit()
        {
            if (resultModalView != null && resultModalView.IsOpen)
            {
                resultModalView.Hide();
            }
        }

        /// <summary>
        /// リザルトUIのOKボタンからホームへ戻る。
        /// </summary>
        private void HandleOkClicked()
        {
            context?.RequestExitToHome();
        }
    }
}
