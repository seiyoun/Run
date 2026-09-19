/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイステート間で共有されるコンテキスト情報のインターフェース。
 */

using Shiyuan.Foundation.Core;

namespace Runner
{
    /// <summary>
    /// ゲームプレイの各ステートが必要とするインスタンスや StateMachine へのアクセスを提供するコンテキスト。
    /// </summary>
    public interface IGameContext
    {
        /// <summary>ゲームプレイのステートマシン</summary>
        StateMachine<GamePlayState> StateMachine { get; }

        /// <summary>生成されたプレイヤーコントローラー</summary>
        PlayerController Player { get; }

        /// <summary>
        /// 生成された PlayerController インスタンスを登録する。
        /// </summary>
        /// <param name="player">登録する PlayerController</param>
        void SetPlayerInstance(PlayerController player);

        /// <summary>
        /// Home シーンへの復帰・遷移を要求する。
        /// </summary>
        void RequestExitToHome();
    }
}
