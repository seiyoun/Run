/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 2D アリーナの背景グリッドを描画し、プレイヤーの移動感を演出する。
 */

using Shiyuan.Foundation.Addressables;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ヴァンサバ風の広大なアリーナ背景（グリッド床）を生成・管理するコンポーネント。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class ArenaBackground : MonoBehaviour
    {
        [Tooltip("プレイヤーの初期生成位置Transform")]
        [SerializeField]
        private Transform playerSpawnPoint;

        private AddressablePrefabLoader prefabLoader;

        /// <summary>プレイヤーの初期生成位置Transform</summary>
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        /// <summary>
        /// オブジェクト破棄時に AddressablePrefabLoader を解放する。
        /// </summary>
        private void OnDestroy()
        {
            prefabLoader?.Dispose();
            prefabLoader = null;
        }

        /// <summary>
        /// プレハブを生成した AddressablePrefabLoader をバインドし、破棄時のクリーンアップを委託する。
        /// </summary>
        /// <param name="loader">生成元 AddressablePrefabLoader</param>
        public void BindLoader(AddressablePrefabLoader loader)
        {
            prefabLoader = loader;
        }
    }
}
