/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中の進行ステートを定義する Enum。
 */

namespace Runner
{
    /// <summary>
    /// ゲームプレイ中の進行ステート。
    /// </summary>
    public enum GamePlayState
    {
        None = 0,
        Loading = 1,
        Playing = 2,
        GameOver = 3
    }
}
