/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ICharacterVisual を実装し、2D スプライト描画・向き反転・非同期シームレス画像ロード・被弾フラッシュを制御するコンポーネント。
 */

using System.Collections;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 2D スプライトの向き反転、非同期シームレス画像ロード、被弾フラッシュ等の見た目を制御するコンポーネント。
    /// スプライトの読み込みをオンデマンドかつ非同期で行うことで、起動時のメモリ負荷と描画スパイクを抑制します。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class CharacterVisual2D : MonoBehaviour, ICharacterVisual
    {
        private const string SpriteResourcePrefix = "Sprites/Characters/";

        [Header("References")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private Color baseColor = Color.white;
        private Coroutine flashCoroutine;
        private Coroutine loadSpriteCoroutine;
        private string currentImageName;

        /// <summary>現在ロード・表示されている画像名</summary>
        public string CurrentImageName => currentImageName;

        /// <summary>
        /// SpriteRenderer の参照を解決し、基本色をキャッシュする。
        /// </summary>
        private void Awake()
        {
            EnsureSpriteRenderer();
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        }

        /// <summary>
        /// 破棄時に実行中のコルーチン参照を整理する。
        /// </summary>
        private void OnDestroy()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }

            if (loadSpriteCoroutine != null)
            {
                StopCoroutine(loadSpriteCoroutine);
                loadSpriteCoroutine = null;
            }
        }

        /// <summary>
        /// キャラクターの向き（左右反転）を設定する。
        /// </summary>
        /// <param name="facingDirection">向いている方向ベクトル</param>
        public void SetFacingDirection(Vector2 facingDirection)
        {
            if (spriteRenderer == null) return;

            if (facingDirection.x < 0f)
            {
                spriteRenderer.flipX = true;
            }
            else if (facingDirection.x > 0f)
            {
                spriteRenderer.flipX = false;
            }
        }

        /// <summary>
        /// 被ダメージ時の点滅（フラッシュ）を再生する。
        /// </summary>
        /// <param name="flashDuration">点滅継続時間（秒）</param>
        public void PlayHitFlash(float flashDuration = 0.1f)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(HitFlashRoutine(flashDuration));
        }

        /// <summary>
        /// キャラクターの色合いを設定する。
        /// </summary>
        /// <param name="color">設定する色</param>
        public void SetColor(Color color)
        {
            baseColor = color;
            if (spriteRenderer != null && flashCoroutine == null)
            {
                spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// 見た目の表示・非表示を切り替える。
        /// </summary>
        /// <param name="visible">表示する場合は true</param>
        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        /// <summary>
        /// スプライト画像を直接設定する。
        /// </summary>
        /// <param name="sprite">設定するスプライト</param>
        public void SetSprite(Sprite sprite)
        {
            EnsureSpriteRenderer();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
        }

        /// <summary>
        /// 指定された画像名に基づいて Resources から非同期でスプライトを読み込み、シームレスに適用する。
        /// キャラクター生成時にオンデマンドでロードするため、ゲーム起動時のメモリ負荷を抑えられます。
        /// </summary>
        /// <param name="imageName">スプライト画像名（拡張子なし）</param>
        public void LoadSprite(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName)) return;

            if (currentImageName == imageName && spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return;
            }

            EnsureSpriteRenderer();

            if (loadSpriteCoroutine != null)
            {
                StopCoroutine(loadSpriteCoroutine);
            }

            loadSpriteCoroutine = StartCoroutine(LoadSpriteRoutine(imageName));
        }

        /// <summary>
        /// Resources からスプライトを非同期ロードして SpriteRenderer に適用するコルーチン。
        /// </summary>
        /// <param name="imageName">スプライト画像名</param>
        /// <returns>コルーチン反復子</returns>
        private IEnumerator LoadSpriteRoutine(string imageName)
        {
            var path = $"{SpriteResourcePrefix}{imageName}";
            var request = Resources.LoadAsync<Sprite>(path);

            yield return request;

            var sprite = request.asset as Sprite;
            if (sprite == null)
            {
                // 接頭辞なしのパスでもフォールバック試行
                var fallbackReq = Resources.LoadAsync<Sprite>(imageName);
                yield return fallbackReq;
                sprite = fallbackReq.asset as Sprite;
            }

            if (sprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                currentImageName = imageName;
            }
            else
            {
                Debug.LogWarning($"[CharacterVisual2D] スプライト '{imageName}' の非同期ロードに失敗しました (Path: {path})。");
            }

            loadSpriteCoroutine = null;
        }

        /// <summary>
        /// SpriteRenderer の参照を安全に取得・検証する。
        /// </summary>
        private void EnsureSpriteRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// 被弾フラッシュの実行コルーチン。
        /// </summary>
        /// <param name="duration">フラッシュ継続時間（秒）</param>
        /// <returns>コルーチン反復子</returns>
        private IEnumerator HitFlashRoutine(float duration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(duration);
                spriteRenderer.color = baseColor;
            }

            flashCoroutine = null;
        }
    }
}
