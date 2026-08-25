/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game シーンのライフサイクル管理。IGameContext を実装し、Gameplay/States のステート群を用いてシーン進行を制御する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using Shiyuan.Foundation.Scenes;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Game シーンにおけるライフサイクル管理クラス。
    /// IGameContext を実装し、Gameplay/States 配下のステート群（Loading, Playing, Paused, GameOver）を通じてゲーム進行を管理します。
    /// </summary>
    public sealed class GameSceneLifecycle : SceneLifecycleBase, IGameContext
    {
        private StateMachine<GamePlayState> stateMachine;
        private PlayerController playerInstance;

        #region IGameContext Implementation

        public StateMachine<GamePlayState> StateMachine => stateMachine;
        public PlayerController Player => playerInstance;

        public void SetPlayerInstance(PlayerController player)
        {
            playerInstance = player;
        }

        #endregion

        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameScene] 通信・アセットロード待機中...");
            await Task.Yield();
        }

        protected override async Task OnInitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameScene] GameSceneLifecycle 初期化開始。ステートマシンをセットアップします。");

            stateMachine = new StateMachine<GamePlayState>();

            // 各ステートの登録
            stateMachine.AddState(new GameLoadingState(this));
            stateMachine.AddState(new GamePlayingState(this));
            stateMachine.AddState(new GamePausedState(this));
            stateMachine.AddState(new GameOverState(this));

            // Loading ステートへ遷移してプレイヤー生成・ゲームプレイ準備を開始
            await stateMachine.ChangeStateAsync(GamePlayState.Loading, cancellationToken);
        }

        protected override void OnUpdate()
        {
            if (stateMachine != null && stateMachine.HasCurrentState)
            {
                var currentState = stateMachine.GetState(stateMachine.CurrentState);
                currentState?.Update();
            }
        }

        protected override void OnDestroy()
        {
            if (stateMachine != null)
            {
                stateMachine.Dispose();
                stateMachine = null;
            }

            playerInstance = null;
            DebugLogger.Log("[GameScene] GameSceneLifecycle 破棄処理完了。");
        }
    }
}
