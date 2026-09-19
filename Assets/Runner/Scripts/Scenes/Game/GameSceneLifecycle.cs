/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game シーンのライフサイクル管理。IGameContext を実装し、Gameplay/States のステート群を用いてシーン進行を制御する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
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

        /// <summary>ゲームプレイ進行ステートマシン</summary>
        public StateMachine<GamePlayState> StateMachine => stateMachine;

        /// <summary>
        /// Game シーン初期化前の通信およびアセット読み込み待機処理。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameScene] 通信・アセットロード待機中...");
            await Task.Yield();
        }

        /// <summary>
        /// Game シーンの初期化処理を実行し、ステートマシンを構築して Loading ステートへ遷移する。
        /// </summary>
        /// <param name="parameter">初期化パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        protected override async Task OnInitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameScene] GameSceneLifecycle 初期化開始。ステートマシンをセットアップします。");

            // GamePlayStateFactory を用いてステートマシンを構築
            stateMachine = GamePlayStateFactory.CreateStateMachine(this);

            // Loading ステートへ遷移してプレイヤー生成・ゲームプレイ準備を開始
            await stateMachine.ChangeStateAsync(GamePlayState.Loading, cancellationToken);
        }

        /// <summary>
        /// Game シーンの毎フレーム処理を実行し、現在のアクティブステートを更新する。
        /// </summary>
        protected override void OnUpdate()
        {
            stateMachine?.Update(Time.deltaTime);
        }

        /// <summary>
        /// Game シーン離脱時のクリーンアップ処理を行い、ステートマシンを破棄する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (stateMachine != null)
            {
                stateMachine.Dispose();
                stateMachine = null;
            }

            DebugLogger.Log("[GameScene] GameSceneLifecycle 破棄処理完了。");
        }

        /// <summary>
        /// Home シーンへの復帰を要求し、シーン遷移を実行する。
        /// </summary>
        public async void RequestExitToHome()
        {
            DebugLogger.Log("[GameScene] Home シーンへの復帰が要求されました。シーン遷移を開始します。");
            if (SceneManager.Instance != null)
            {
                await SceneManager.Instance.ChangeScene(SceneType.Home, showLoading: true);
            }
        }
    }
}
