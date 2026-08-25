/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: アプリケーションで使用するシーンの識別子を定義する。
 */

namespace Runner
{
    /// <summary>
    /// アプリケーション内で遷移可能なシーンの種類。
    /// ※ enum の識別子名は Unity のシーン名と一致している必要があります。
    /// </summary>
    public enum SceneType
    {
        Boot,
        Title,
        Home,
        Game,
    }
}
