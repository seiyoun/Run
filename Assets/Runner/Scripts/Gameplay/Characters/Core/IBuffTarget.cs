/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: バフの付与を受け付ける対象エンティティのインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// バフの付与を受け付ける対象エンティティのインターフェース。
    /// </summary>
    public interface IBuffTarget
    {
        /// <summary>対象エンティティの GameObject</summary>
        GameObject GameObject { get; }

        /// <summary>
        /// バフを付与する。
        /// </summary>
        /// <param name="buff">付与する IBuff インスタンス</param>
        void AddBuff(IBuff buff);

        /// <summary>
        /// 指定された種別のバフを解除する。
        /// </summary>
        /// <param name="type">解除するバフ種別</param>
        void RemoveBuff(BuffType type);
    }
}

