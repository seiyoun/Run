/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IAttacker を実装し、攻撃力、攻撃間隔、クールダウン、攻撃実行を制御する汎用コンポーネント。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// キャラクターの攻撃挙動・クールダウン管理を担う攻撃コンポーネント。
    /// プレイヤーや敵モンスター等で共通して利用可能な IAttacker 実装です。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAttacker2D : MonoBehaviour, IAttacker
    {
        private const int DefaultPower = 10;
        private const float DefaultInterval = 1.0f;

        [Header("Combat Settings")]
        [Tooltip("基本攻撃力")]
        [SerializeField] private int attackPower = DefaultPower;

        [Tooltip("攻撃間隔 (秒)")]
        [SerializeField] private float attackInterval = DefaultInterval;

        private float attackCooldownTimer;
        private ICharacterAnimator characterAnimator;
        private ICharacterVisual characterVisual;
        private ICharacterStatus characterStatus;
        private IMovable characterMovement;

        /// <summary>攻撃力</summary>
        public int AttackPower
        {
            get => attackPower;
            set => attackPower = Mathf.Max(1, value);
        }

        /// <summary>攻撃間隔（秒）</summary>
        public float AttackInterval
        {
            get => attackInterval;
            set => attackInterval = Mathf.Max(0.05f, value);
        }

        /// <summary>現在攻撃可能かどうか</summary>
        public bool CanAttack => (characterStatus == null || !characterStatus.IsDead) && attackCooldownTimer <= 0f;

        /// <summary>攻撃実行時イベント</summary>
        public event Action OnAttack;

        /// <summary>
        /// 関連インターフェースの参照を取得する。
        /// </summary>
        private void Awake()
        {
            characterAnimator = GetComponent<ICharacterAnimator>() ?? GetComponentInChildren<ICharacterAnimator>();
            characterVisual = GetComponent<ICharacterVisual>() ?? GetComponentInChildren<ICharacterVisual>();
            characterStatus = GetComponent<ICharacterStatus>() ?? GetComponentInChildren<ICharacterStatus>();
            characterMovement = GetComponent<IMovable>() ?? GetComponentInChildren<IMovable>();
        }

        /// <summary>
        /// 攻撃クールダウンタイマーを更新する。
        /// </summary>
        /// <param name="deltaTime">フレーム経過時間</param>
        public void OnUpdate(float deltaTime)
        {
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= deltaTime;
            }
        }

        /// <summary>
        /// 現在の向きまたは正面に向けて攻撃を実行する。
        /// </summary>
        public void Attack()
        {
            var dir = characterMovement != null ? characterMovement.FacingDirection : Vector2.right;
            Attack(dir);
        }

        /// <summary>
        /// 指定された方向に向けて攻撃を実行する。
        /// </summary>
        /// <param name="direction">攻撃方向ベクトル</param>
        public void Attack(Vector2 direction)
        {
            if (!CanAttack) return;

            if (direction.sqrMagnitude > 0.01f)
            {
                var facingDir = new Vector2(Mathf.Sign(direction.x), 0f);
                characterVisual?.SetFacingDirection(facingDir);
            }

            attackCooldownTimer = attackInterval;
            characterAnimator?.TriggerAttack();
            OnAttack?.Invoke();
        }
    }
}

