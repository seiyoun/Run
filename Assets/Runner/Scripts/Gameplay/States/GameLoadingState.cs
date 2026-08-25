/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム初期化時のロードステート。PlayerSpawner を呼び出してプレイヤーを生成し、ゲームプレイの準備を整える。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲーム開始前のロード・初期化ステート。
    /// PlayerSpawner を呼び出してプレイヤーを生成し、Playing ステートへ遷移します。
    /// </summary>
    public sealed class GameLoadingState : IState<GamePlayState>
    {
        public GamePlayState State => GamePlayState.Loading;

        private readonly IGameContext context;

        public GameLoadingState(IGameContext context)
        {
            this.context = context;
        }

        public async Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameLoadingState] PlayerSpawner を呼び出してプレイヤー生成を開始します...");

            var spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            if (spawner != null)
            {
                var player = await spawner.SpawnPlayerAsync(cancellationToken);
                if (player != null)
                {
                    context.SetPlayerInstance(player);
                    DebugLogger.Log("[GameLoadingState] PlayerSpawner によるプレイヤー生成が完了しました。");
                }
                else
                {
                    DebugLogger.Error("[GameLoadingState] PlayerSpawner によるプレイヤー生成に失敗しました。");
                }
            }
            else
            {
                DebugLogger.Error("[GameLoadingState] シーン上に PlayerSpawner が見つかりません。");
            }

            // ロード完了後、Playing ステートへ遷移
            if (context.StateMachine != null)
            {
                await context.StateMachine.ChangeStateAsync(GamePlayState.Playing, cancellationToken);
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        public void Update()
        {
        }

        public void Exit()
        {
            DebugLogger.Log("[GameLoadingState] ロード完了。Playing ステートへ移行しました。");
        }
    }
}
