/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローン武器オブジェクトの制御コンポーネント。アタッチされた IFollowTarget コンポーネントに追従対象を連携します。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ドローン武器の制御コンポーネント。
    /// 同一 GameObject にアタッチされた IFollowTarget コンポーネントに追従対象を設定・連携します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FollowTarget))]
    public sealed class DroneFollower : MonoBehaviour
    {
        private IFollowTarget followTarget;

        /// <summary>現在アタッチされている追従インターフェース</summary>
        public IFollowTarget FollowTarget => followTarget;

        /// <summary>現在の追従対象 Transform</summary>
        public Transform Target => followTarget != null ? followTarget.Target : null;

        /// <summary>
        /// 同一 GameObject の IFollowTarget コンポーネントを取得する。
        /// </summary>
        private void Awake()
        {
            EnsureFollowTarget();
        }

        /// <summary>
        /// 初回フレームで追従対象が未設定の場合、プレイヤーを自動検知してターゲットを設定する。
        /// </summary>
        private void Start()
        {
            EnsureFollowTarget();
            if (followTarget != null && followTarget.Target == null)
            {
                var player = PlayerController.Instance;
                if (player != null)
                {
                    SetTarget(player.transform);
                }
            }
        }

        /// <summary>
        /// オブジェクトの文字列表現を返す。
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString()
        {
            string targetName = Target != null ? Target.name : "None";
            return $"DroneFollower (Target: {targetName})";
        }

        /// <summary>
        /// 追従対象の Transform を設定する。
        /// </summary>
        /// <param name="newTarget">新しい追従対象</param>
        public void SetTarget(Transform newTarget)
        {
            EnsureFollowTarget();
            if (followTarget != null)
            {
                followTarget.SetTarget(newTarget);
            }
        }

        /// <summary>
        /// IFollowTarget の参照が未取得の場合に取得する。
        /// </summary>
        private void EnsureFollowTarget()
        {
            if (followTarget == null)
            {
                followTarget = GetComponent<IFollowTarget>();
            }
        }
    }
}
