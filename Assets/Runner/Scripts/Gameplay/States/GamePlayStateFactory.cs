/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: GamePlayState に対応するステートインスタンスおよび StateMachine を生成するファクトリクラス。
 */

using System;
using Shiyuan.Foundation.Core;

namespace Runner
{
    /// <summary>
    /// GamePlayState に応じた各ステートインスタンスの生成およびステートマシンの構築を行うファクトリ。
    /// </summary>
    public static class GamePlayStateFactory
    {
        /// <summary>
        /// 指定された GamePlayState に対応するステートインスタンスを生成する。
        /// </summary>
        /// <param name="state">生成対象のゲームプレイステート種別</param>
        /// <param name="context">ステートに注入するゲームコンテキスト</param>
        /// <returns>生成された IState 実装インスタンス</returns>
        public static IState<GamePlayState> Create(GamePlayState state, IGameContext context)
        {
            return state switch
            {
                GamePlayState.Loading => new GameLoadingState(context),
                GamePlayState.Playing => new GamePlayingState(context),
                GamePlayState.Paused => new GamePausedState(context),
                GamePlayState.GameOver => new GameOverState(context),
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, $"未対応の GamePlayState です: {state}")
            };
        }

        /// <summary>
        /// 全ての GamePlayState を登録済みの StateMachine を構築して返す。
        /// </summary>
        /// <param name="context">ステートに注入するゲームコンテキスト</param>
        /// <returns>構築された StateMachine インスタンス</returns>
        public static StateMachine<GamePlayState> CreateStateMachine(IGameContext context)
        {
            var stateMachine = new StateMachine<GamePlayState>();
            stateMachine.AddState(Create(GamePlayState.Loading, context));
            stateMachine.AddState(Create(GamePlayState.Playing, context));
            stateMachine.AddState(Create(GamePlayState.Paused, context));
            stateMachine.AddState(Create(GamePlayState.GameOver, context));
            return stateMachine;
        }
    }
}
