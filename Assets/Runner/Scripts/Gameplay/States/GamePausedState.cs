/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 一時停止ステート。プレイヤーの移動を停止する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;

namespace Runner
{
    /// <summary>
    /// ゲーム一時停止ステート。
    /// </summary>
    public sealed class GamePausedState : IState<GamePlayState>
    {
        public GamePlayState State => GamePlayState.Paused;

        private readonly IGameContext context;

        public GamePausedState(IGameContext context)
        {
            this.context = context;
        }

        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GamePausedState] ゲームを一時停止しました。");
            context.Player?.Stop();
            return Task.CompletedTask;
        }

        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Update() { }
        public void Exit() => DebugLogger.Log("[GamePausedState] 一時停止を解除しました。");
    }
}
