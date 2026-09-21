/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローン武器オブジェクトの制御コンポーネント。番号（インデックス）に応じたオフセットで IFollowTarget に追従対象を連携します。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ドローン武器の制御コンポーネント。
    /// 生成時に割り当てられた番号（インデックス）に応じて追従オフセットを設定し、
    /// アタッチされた IFollowTarget コンポーネントを介して追従移動を行います。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FollowTarget))]
    public sealed class DroneFollower : MonoBehaviour
    {
        private static readonly Vector3[] DefaultSlotOffsets = new Vector3[]
        {
            new Vector3(-0.60f, 0.60f, 0f),
            new Vector3( 0.60f, 0.60f, 0f),
            new Vector3(-0.95f, 0.45f, 0f),
            new Vector3( 0.95f, 0.45f, 0f),
            new Vector3( 0.00f, 0.90f, 0f),
            new Vector3(-0.45f, 0.95f, 0f),
            new Vector3( 0.45f, 0.95f, 0f),
            new Vector3(-0.85f, 0.85f, 0f),
            new Vector3( 0.85f, 0.85f, 0f),
        };

        [Header("Drone Slot Settings")]
        [Tooltip("ドローンのスロット番号（0から開始）")]
        [SerializeField] private int droneIndex = 0;

        private IFollowTarget followTarget;
        private DroneAttacker2D attacker;
        private int currentLevel = 1;
        private int attackPower = 10;
        private float attackInterval = 1.5f;

        /// <summary>ドローンのスロット識別番号</summary>
        public int DroneIndex => droneIndex;

        /// <summary>ドローンの現在の武器レベル</summary>
        public int CurrentLevel => currentLevel;

        /// <summary>ドローンの攻撃力</summary>
        public int AttackPower => attacker != null ? attacker.AttackPower : attackPower;

        /// <summary>ドローンの攻撃間隔（秒）</summary>
        public float AttackInterval => attacker != null ? attacker.AttackInterval : attackInterval;

        /// <summary>現在アタッチされている追従インターフェース</summary>
        public IFollowTarget FollowTarget => followTarget;

        /// <summary>現在の追従対象 Transform</summary>
        public Transform Target => followTarget != null ? followTarget.Target : null;

        /// <summary>
        /// 同一 GameObject の IFollowTarget コンポーネントを取得し、初期オフセットを適用する。
        /// </summary>
        private void Awake()
        {
            attacker = GetComponent<DroneAttacker2D>();
            EnsureFollowTarget();
            ApplyOffsetByIndex(droneIndex);
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
            return $"DroneFollower (Index: {droneIndex}, Lv.{currentLevel}, Power: {attackPower}, Interval: {attackInterval:F1}s, Target: {targetName})";
        }

        /// <summary>
        /// 武器レベルデータを受け取り、性能パラメータ（レベル・攻撃力・攻撃間隔）を設定・更新する。
        /// </summary>
        /// <param name="data">設定対象の武器レベルデータ</param>
        public void ApplyLevelData(WeaponLevelData data)
        {
            if (data == null) return;

            currentLevel = data.Level;
            attackPower = data.AttackPower;
            attackInterval = data.AttackInterval;

            if (attacker == null)
            {
                attacker = GetComponent<DroneAttacker2D>();
            }

            if (attacker != null)
            {
                attacker.AttackPower = data.AttackPower;
                attacker.AttackInterval = data.AttackInterval;
                attacker.SearchRadius = data.SearchRadius;
            }

            DebugLogger.Log($"[DroneFollower] ドローン #{droneIndex} に Lv.{currentLevel} を適用しました。(Power: {attackPower}, Interval: {attackInterval:F1}s, Radius: {data.SearchRadius:F1}m)");
        }

        /// <summary>
        /// ドローンのスロット番号を設定し、番号に応じた固有の追従オフセットを適用する。
        /// </summary>
        /// <param name="index">スロット番号（0以上）</param>
        public void SetIndex(int index)
        {
            droneIndex = Mathf.Max(0, index);
            ApplyOffsetByIndex(droneIndex);
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

        /// <summary>
        /// ドローン識別番号に応じたスロット位置オフセットを followTarget に適用する。
        /// </summary>
        /// <param name="index">スロット識別番号</param>
        private void ApplyOffsetByIndex(int index)
        {
            EnsureFollowTarget();
            if (followTarget == null) return;

            int clampedIndex = Mathf.Clamp(index, 0, DefaultSlotOffsets.Length - 1);
            followTarget.FollowOffset = DefaultSlotOffsets[clampedIndex];
            followTarget.FlipOffsetWithFacing = false;
        }
    }
}
