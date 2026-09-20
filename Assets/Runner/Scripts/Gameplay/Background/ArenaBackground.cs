/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 2D アリーナの背景グリッドを描画し、プレイヤーの移動感を演出する。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ヴァンサバ風の広大なアリーナ背景（グリッド床）を生成・管理するコンポーネント。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ArenaBackground : MonoBehaviour
    {
        /// <summary>現在アクティブな ArenaBackground のインスタンス</summary>
        public static ArenaBackground Instance { get; private set; }

        [Tooltip("ステージの枠・境界を定義するコライダー")]
        [SerializeField]
        private Collider2D boundaryCollider;

        [Tooltip("プレイヤーの初期生成位置Transform")]
        [SerializeField]
        private Transform playerSpawnPoint;

        /// <summary>プレイヤーの初期生成位置Transform</summary>
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        /// <summary>ステージの枠・境界コライダー</summary>
        public Collider2D BoundaryCollider => boundaryCollider;

        /// <summary>
        /// インスタンスの登録およびコライダー参照の自動補完を行う。
        /// </summary>
        private void Awake()
        {
            Instance = this;
            if (boundaryCollider == null)
            {
                boundaryCollider = GetComponent<Collider2D>();
            }
        }

        /// <summary>
        /// インスタンスの登録を解除する。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
