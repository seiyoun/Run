/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローンが発射する弾丸の移動およびエネミーへのダメージ判定を制御するコンポーネント。
 */

using System;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ドローンが発射する弾丸の移動制御および衝突判定コンポーネント。
    /// 指定された方向へ直進移動し、エネミーに接触した際にダメージを与えて自身を破棄またはプールへ返却します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class DroneBullet : MonoBehaviour
    {
        private Action<DroneBullet> returnToPool;

        [Header("Bullet Settings")]
        [Tooltip("弾の移動速度")]
        [SerializeField] private float speed = 12f;

        [Tooltip("弾の最大生存時間（秒）")]
        [SerializeField] private float lifeTime = 2f;

        [Tooltip("エネミー命中時に与えるノックバック力")]
        [SerializeField] private float knockbackForce = 5f;

        private Vector2 direction;
        private int damage;
        private float elapsedLifeTime;
        private bool isReleased;

        /// <summary>
        /// 毎フレームの移動および生存時間の更新を行う。
        /// </summary>
        private void Update()
        {
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));

            elapsedLifeTime += Time.deltaTime;
            if (elapsedLifeTime >= lifeTime)
            {
                ReleaseSelf();
            }
        }

        /// <summary>
        /// コライダー接触時にプレイヤー以外のダメージ対象へダメージを与えて自身を返却する。
        /// </summary>
        /// <param name="other">接触したコライダー</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) return;

            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);

                var knockbackable = other.GetComponent<IKnockbackable>() ?? other.GetComponentInParent<IKnockbackable>();
                if (knockbackable != null)
                {
                    knockbackable.ApplyKnockback(direction, knockbackForce);
                }

                ReleaseSelf();
            }
        }

        /// <summary>
        /// 弾丸の進行方向、攻撃力、およびプール返却用コールバックを初期化する。
        /// </summary>
        /// <param name="moveDirection">進行方向ベクトル</param>
        /// <param name="bulletDamage">与えるダメージ量</param>
        /// <param name="onRelease">オブジェクトプール返却時のコールバック（null時はDestroy）</param>
        public void Initialize(Vector2 moveDirection, int bulletDamage, Action<DroneBullet> onRelease = null)
        {
            direction = moveDirection.normalized;
            damage = bulletDamage;
            elapsedLifeTime = 0f;
            returnToPool = onRelease;
            isReleased = false;
        }

        /// <summary>
        /// 自身をオブジェクトプールに返却、または破棄する。
        /// </summary>
        private void ReleaseSelf()
        {
            if (isReleased) return;
            isReleased = true;

            if (returnToPool != null)
            {
                returnToPool(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

