/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 脱出ゲートを事前ロードし、表示とAddressablesの解放を管理する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Gameシーンで脱出ゲートの生成、表示、解放を管理する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EscapePointSpawner : SingletonMonoBehaviour<EscapePointSpawner>
    {
        private const string EscapePointAddress = "EscapePoint";

        private AddressablePrefabLoader addressableLoader;
        private EscapePoint loadedPoint;

        /// <summary>Gameシーン破棄時に一緒に破棄する。</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか。</summary>
        public static bool HasInstance => SingletonMonoBehaviour<EscapePointSpawner>.Instance != null;

        /// <summary>Gameシーン上のSpawnerを取得し、存在しなければ生成する。</summary>
        public new static EscapePointSpawner Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<EscapePointSpawner>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<EscapePointSpawner>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(EscapePointSpawner));
                return obj.AddComponent<EscapePointSpawner>();
            }
        }

        /// <summary>ロード済みの脱出ゲート。</summary>
        public EscapePoint LoadedPoint => loadedPoint;

        /// <summary>シングルトンとAddressablesローダーを初期化する。</summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance) return;

            addressableLoader = new AddressablePrefabLoader();
        }

        /// <summary>Gameシーンの破棄時に脱出ゲートとAddressablesの参照を解放する。</summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance) return;

            base.OnDestroy();
            if (loadedPoint != null) loadedPoint.OnDestroyed -= HandlePointDestroyed;
            loadedPoint = null;
            var loader = addressableLoader;
            addressableLoader = null;
            loader?.Dispose();
        }

        /// <summary>脱出ゲートを生成し、開放まで非表示で待機させる。</summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>ロード済みの脱出ゲート</returns>
        public async Task<EscapePoint> PreloadAsync(CancellationToken cancellationToken)
        {
            if (loadedPoint != null) return loadedPoint;

            var instance = await addressableLoader.LoadAsync(EscapePointAddress, cancellationToken);
            var point = instance != null ? instance.GetComponent<EscapePoint>() : null;
            if (point == null)
            {
                addressableLoader.Dispose();
                DebugLogger.Error("[EscapePointSpawner] EscapePoint プレハブに EscapePoint がありません。");
                return null;
            }

            point.gameObject.SetActive(false);
            point.OnDestroyed += HandlePointDestroyed;
            loadedPoint = point;
            return loadedPoint;
        }

        /// <summary>ロード済みの脱出ゲートを指定位置に表示する。</summary>
        /// <param name="position">表示位置</param>
        public void Show(Vector3 position)
        {
            if (loadedPoint == null) return;

            loadedPoint.transform.position = position;
            loadedPoint.gameObject.SetActive(true);
        }

        /// <summary>ロード済みの脱出ゲートを非表示にする。</summary>
        public void Hide()
        {
            if (loadedPoint != null) loadedPoint.gameObject.SetActive(false);
        }

        /// <summary>
        /// 指定されたステージ境界内で、プレイヤーから一定距離離れた位置に脱出ゲートを表示する。
        /// </summary>
        /// <param name="boundary">ステージ境界コライダー</param>
        /// <param name="playerPosition">プレイヤーの現在位置</param>
        /// <param name="spawnedPoint">表示された脱出ゲート</param>
        /// <returns>配置に成功した場合は true</returns>
        public bool TryShowAtSafePosition(Collider2D boundary, Vector3 playerPosition, out EscapePoint spawnedPoint)
        {
            spawnedPoint = null;
            if (loadedPoint == null || boundary == null) return false;

            const float margin = 2f;
            const float minPlayerDistance = 8f;
            var bounds = boundary.bounds;
            float minX = bounds.min.x + margin;
            float maxX = bounds.max.x - margin;
            float minY = bounds.min.y + margin;
            float maxY = bounds.max.y - margin;

            if (minX > maxX || minY > maxY) return false;

            Vector2 position = Vector2.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                if (Vector2.Distance(position, playerPosition) >= minPlayerDistance) break;
            }

            Show(new Vector3(position.x, position.y, 0f));
            spawnedPoint = loadedPoint;
            return true;
        }

        /// <summary>脱出ゲートが先に破棄された場合もAddressablesの参照を解放する。</summary>
        private void HandlePointDestroyed()
        {
            if (loadedPoint != null) loadedPoint.OnDestroyed -= HandlePointDestroyed;
            loadedPoint = null;
            addressableLoader?.Dispose();
        }
    }
}
