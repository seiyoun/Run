/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: エネミーの種類（サラリーマンぶつかり屋、ぶつかりババア等）を識別する列挙型。
 */

namespace Runner
{
    /// <summary>
    /// エネミーの種類を識別する列挙型。
    /// </summary>
    public enum EnemyType
    {
        /// <summary>サラリーマンぶつかり屋（標準エネミー）</summary>
        Salaryman = 0,

        /// <summary>ぶつかりババア（突進おばあさん）</summary>
        Granny = 1
    }
}
