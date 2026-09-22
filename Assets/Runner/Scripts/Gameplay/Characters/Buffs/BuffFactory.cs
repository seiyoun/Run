/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: BuffTypeおよびマスターデータに基づいて IBuff インスタンスを生成するファクトリクラス。
 */

namespace Runner
{
    /// <summary>
    /// BuffTypeおよびマスターデータに基づいて対応する IBuff インスタンスを生成するファクトリクラス。
    /// </summary>
    public static class BuffFactory
    {
        /// <summary>
        /// 指定されたバフ種別のマスターデータに基づいた IBuff インスタンスを生成する。
        /// </summary>
        /// <param name="type">生成するバフ種別</param>
        /// <returns>生成された IBuff インスタンス</returns>
        public static IBuff Create(BuffType type)
        {
            var master = MasterDataManager.GetBuffMasterData((int)type);
            switch (type)
            {
                case BuffType.Speed:
                {
                    float speedBonus = master.Value > 0f ? master.Value : 2.5f;
                    float dur = master.Duration > 0f ? master.Duration : 5.0f;
                    return new SpeedBuff(dur, speedBonus);
                }
                case BuffType.HpRegen:
                {
                    int amount = master.Value > 0f ? (int)master.Value : 5;
                    float dur = master.Duration > 0f ? master.Duration : 10.0f;
                    return new HpRegenBuff(dur, amount);
                }
                default:
                    return null;
            }
        }
    }
}
