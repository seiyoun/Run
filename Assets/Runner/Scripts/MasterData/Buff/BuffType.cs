/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム内で付与されるバフの種別を定義する列挙型。
 */

namespace Runner
{
    /// <summary>
    /// バフの種別列挙型。
    /// マスターデータの buffId と一対一で対応します。
    /// </summary>
    public enum BuffType
    {
        /// <summary>移動速度アップバフ</summary>
        Speed = 1,

        /// <summary>HP継続回復バフ</summary>
        HpRegen = 2
    }
}

