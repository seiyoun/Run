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
        StateMachine<GamePlayState> StateMachine { get; }
        PlayerController Player { get; }
        void SetPlayerInstance(PlayerController player);
    }
}
