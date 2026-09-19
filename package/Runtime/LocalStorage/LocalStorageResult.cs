/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ローカル保存データの読み込み結果を保持する。
 */

namespace Shiyuan.Foundation.LocalStorage
{
    public sealed class LocalStorageResult<T>
    {
        /// <summary>
        /// 読み込み結果を指定してインスタンスを作成する。
        /// </summary>
        private LocalStorageResult(LocalStorageStatus status, T value, string message)
        {
            Status = status;
            Value = value;
            Message = message;
        }

        public LocalStorageStatus Status { get; }

        public T Value { get; }

        public string Message { get; }

        public bool IsSuccess => Status == LocalStorageStatus.Success;

        /// <summary>
        /// 読み込み成功の結果を作成する。
        /// </summary>
        public static LocalStorageResult<T> Success(T value)
        {
            return new LocalStorageResult<T>(LocalStorageStatus.Success, value, string.Empty);
        }

        /// <summary>
        /// 指定したステータスの読み込み失敗結果を作成する。
        /// </summary>
        public static LocalStorageResult<T> Failure(LocalStorageStatus status, string message = "")
        {
            return new LocalStorageResult<T>(status, default, message);
        }
    }
}
