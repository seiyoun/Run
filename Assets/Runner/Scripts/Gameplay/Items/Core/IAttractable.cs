/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤー等のターゲットに吸い寄せられる（マグネット移動する）オブジェクトのインターフェース。
 */

using System;
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

        /// <summary>現在の吸引移動速度</summary>
        float CurrentAttractSpeed { get; }

        /// <summary>吸引開始時イベント</summary>
        event Action<Transform> OnAttractStarted;

        /// <summary>ターゲット到達時イベント</summary>
        event Action<Transform> OnAttractReached;

        /// <summary>吸引停止時イベント</summary>
        event Action OnAttractStopped;

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

        /// <summary>
        /// 吸引パラメータを設定する。
        /// </summary>
        /// <param name="initialSpeed">初速</param>
        /// <param name="acceleration">加速度</param>
        /// <param name="maxSpeed">最大速度</param>
        void Configure(float initialSpeed, float acceleration, float maxSpeed);
    }
}

