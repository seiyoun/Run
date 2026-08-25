/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームオーバーステート。プレイヤーの入力を切断し、移動を完全停止する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;

namespace Runner
{
    /// <summary>
    /// ゲームオーバーステート。
    /// </summary>
    public sealed class GameOverState : IState<GamePlayState>
    {
        public GamePlayState State => GamePlayState.GameOver;

        private readonly IGameContext context;

        public GameOverState(IGameContext context)
        {
            this.context = context;
        }

        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameOverState] ゲームオーバー。プレイヤー入力を切断します。");

            var player = context.Player;
            if (player != null)
            {
                player.UnbindInput();
                player.Stop();
            }

            return Task.CompletedTask;
        }

        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Update() { }
        public void Exit() { }
    }
}
