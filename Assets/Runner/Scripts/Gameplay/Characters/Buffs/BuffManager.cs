/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: バフの生成と対象への適用を統括する静的マネージャークラス。
 */

namespace Runner
{
    /// <summary>
    /// バフの生成と対象への適用を統括する静的マネージャークラス。
    /// </summary>
    public static class BuffManager
    {
        /// <summary>
        /// 指定されたバフ種別をファクトリから生成し、対象に適用する。
        /// </summary>
        /// <param name="target">付与対象（IBuffTarget）</param>
        /// <param name="type">適用するバフ種別</param>
        public static void ApplyBuff(IBuffTarget target, BuffType type)
        {
            if (target == null) return;

            var buff = BuffFactory.Create(type);
            if (buff != null)
            {
                target.AddBuff(buff);
            }
        }

        /// <summary>
        /// 指定されたバフ種別を対象から解除する。
        /// </summary>
        /// <param name="target">解除対象（IBuffTarget）</param>
        /// <param name="type">解除するバフ種別</param>
        public static void RemoveBuff(IBuffTarget target, BuffType type)
        {
            if (target == null) return;

            target.RemoveBuff(type);
        }
    }
}

