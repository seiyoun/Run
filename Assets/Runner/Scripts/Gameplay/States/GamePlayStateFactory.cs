/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: GamePlayState に対応する StateMachine を生成するファクトリクラス。
 */

using Shiyuan.Foundation.Core;

namespace Runner
{
    /// <summary>
    /// GamePlayState のステートマシン構築を行うファクトリ。
    /// </summary>
    public static class GamePlayStateFactory
    {
        /// <summary>
        /// 全ての GamePlayState を登録済みの StateMachine を構築して返す。
        /// </summary>
        /// <param name="context">ステートに注入するゲームコンテキスト</param>
        /// <returns>構築された StateMachine インスタンス</returns>
        public static StateMachine<GamePlayState> CreateStateMachine(IGameContext context)
        {
            var stateMachine = new StateMachine<GamePlayState>();
            stateMachine.AddState(new GameLoadingState(context));
            stateMachine.AddState(new GamePlayingState(context));
            stateMachine.AddState(new GamePausedState(context));
            stateMachine.AddState(new GameOverState(context));
            return stateMachine;
        }
    }
}
