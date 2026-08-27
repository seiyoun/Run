/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー等のターゲットに吸い寄せられる（マグネット移動する）オブジェクトのインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ターゲットに向かって吸引・吸い寄せられるオブジェクトのインターフェース。
    /// </summary>
    public interface IAttractable
    {
        /// <summary>現在吸引（吸い込み）状態にあるかどうか</summary>
        bool IsAttracted { get; }

        /// <summary>吸引先のターゲット Transform</summary>
        Transform Target { get; }

        /// <summary>
        /// 指定ターゲットへの吸引を開始する。
        /// </summary>
        /// <param name="target">吸引先のターゲット</param>
        /// <param name="initialSpeed">初期吸引速度（0以下の場合はデフォルト速度）</param>
        void AttractTo(Transform target, float initialSpeed = 0f);

        /// <summary>
        /// 吸引を中止する。
        /// </summary>
        void StopAttract();
    }
}

