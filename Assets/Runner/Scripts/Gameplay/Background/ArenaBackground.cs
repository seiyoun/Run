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
        [Header("Arena Settings")]
        [Tooltip("アリーナのサイズ")]
        [SerializeField]
        private Vector2 arenaSize = new(100f, 100f);

        [Tooltip("グリッドの色A")]
        [SerializeField]
        private Color gridColorA = new(0.12f, 0.14f, 0.18f, 1f);

        [Tooltip("グリッドの色B")]
        [SerializeField]
        private Color gridColorB = new(0.15f, 0.17f, 0.22f, 1f);

        [Header("Spawn Points")]
        [Tooltip("プレイヤーの初期生成位置Transform")]
        [SerializeField]
        private Transform playerSpawnPoint;

        /// <summary>プレイヤーの初期生成位置Transform</summary>
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        /// <summary>
        /// コンポーネントの初期化時に背景グリッドをセットアップする。
        /// </summary>
        private void Awake()
        {
            SetupBackground();
        }

        /// <summary>
        /// 背景テクスチャおよび SpriteRenderer の初期化と設定を行う。
        /// </summary>
        private void SetupBackground()
        {
            var sr = GetComponent<SpriteRenderer>();
            sr.sortingOrder = -100; // 最背面

            // 64x64 のチェック柄プロシージャルテクスチャを生成
            const int texSize = 64;
            var texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var colors = new Color[texSize * texSize];
            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    bool isEven = ((x / (texSize / 2)) + (y / (texSize / 2))) % 2 == 0;
                    colors[y * texSize + x] = isEven ? gridColorA : gridColorB;
                }
            }

            texture.SetPixels(colors);
            texture.Apply();

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f),
                32f // 1ユニットあたり2タイル
            );

            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = arenaSize;
            transform.position = new Vector3(0f, 0f, 1f);
        }
    }
}
