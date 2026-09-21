/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ドローンが発射する弾丸の移動およびエネミーへのダメージ判定を制御するコンポーネント。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ドローンが発射する弾丸の移動制御および衝突判定コンポーネント。
    /// 指定された方向へ直進移動し、エネミーに接触した際にダメージを与えて自身を破棄します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class DroneBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [Tooltip("弾の移動速度")]
        [SerializeField] private float speed = 12f;

        [Tooltip("弾の最大生存時間（秒）")]
        [SerializeField] private float lifeTime = 2f;

        private Vector2 direction;
        private int damage;
        private float elapsedLifeTime;

        /// <summary>
        /// 毎フレームの移動および生存時間の更新を行う。
        /// </summary>
        private void Update()
        {
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));

            elapsedLifeTime += Time.deltaTime;
            if (elapsedLifeTime >= lifeTime)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// コライダー接触時にプレイヤー以外のダメージ対象へダメージを与えて自身を破棄する。
        /// </summary>
        /// <param name="other">接触したコライダー</param>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) return;

            var damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(damage);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 弾丸の進行方向と攻撃力を初期化する。
        /// </summary>
        /// <param name="moveDirection">進行方向ベクトル</param>
        /// <param name="bulletDamage">与えるダメージ量</param>
        public void Initialize(Vector2 moveDirection, int bulletDamage)
        {
            direction = moveDirection.normalized;
            damage = bulletDamage;
            elapsedLifeTime = 0f;
        }
    }
}

