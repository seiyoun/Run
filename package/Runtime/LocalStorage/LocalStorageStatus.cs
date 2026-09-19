/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ローカル保存データの読み込み結果種別を定義する。
 */

namespace Shiyuan.Foundation.LocalStorage
{
    public enum LocalStorageStatus
    {
        Success,
        NotFound,
        InvalidData,
        CryptoError
    }
}
