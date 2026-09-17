/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ICharacterVisual を実装し、2D スプライト描画・向き反転・被弾フラッシュ等の見た目を制御する描画コンポーネント。
 */

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 2D スプライトの描画、向き反転、被弾フラッシュ等の見た目を制御する描画コンポーネント。
    /// スプライトの直接指定、および GameAssetLoader を介したスプライト名からの非同期ロード表示に対応します。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class CharacterVisual2D : MonoBehaviour, ICharacterVisual
    {
        [Header("References")]
        [Tooltip("描画対象の SpriteRenderer")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private Color baseColor = Color.white;
        private Coroutine flashCoroutine;

        /// <summary>現在設定されているスプライト名</summary>
        public string CurrentImageName => (spriteRenderer != null && spriteRenderer.sprite != null) ? spriteRenderer.sprite.name : null;

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
        /// スプライト画像名を指定して、GameAssetLoader 経由で非同期ロードし適用する。
        /// キャッシュが存在すれば即座に適用され、未ロードの場合はバックグラウンドでロード完了時に適用されます。
        /// </summary>
        /// <param name="spriteName">スプライト画像名（Addressables アドレス）</param>
        public void SetSprite(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName)) return;

            EnsureSpriteRenderer();

            if (GameAssetLoader.Instance != null && GameAssetLoader.Instance.TryGetLoadedSprite(spriteName, out var cachedSprite))
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = cachedSprite;
                }
                return;
            }

            _ = LoadAndSetSpriteAsync(spriteName, destroyCancellationToken);
        }

        /// <summary>
        /// スプライト画像名を指定して、GameAssetLoader 経由で非同期ロードし適用する。
        /// </summary>
        /// <param name="spriteName">スプライト画像名（Addressables アドレス）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public async Task SetSpriteAsync(string spriteName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(spriteName)) return;

            EnsureSpriteRenderer();

            if (GameAssetLoader.Instance != null && GameAssetLoader.Instance.TryGetLoadedSprite(spriteName, out var cachedSprite))
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = cachedSprite;
                }
                return;
            }

            await LoadAndSetSpriteAsync(spriteName, cancellationToken);
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

        /// <summary>
        /// スプライトを GameAssetLoader から非同期ロードし、完了時に適用する。
        /// </summary>
        /// <param name="spriteName">スプライト画像名</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        private async Task LoadAndSetSpriteAsync(string spriteName, CancellationToken cancellationToken)
        {
            if (GameAssetLoader.Instance == null) return;

            try
            {
                var loaded = await GameAssetLoader.Instance.LoadSpriteAsync(spriteName, cancellationToken);
                if (loaded != null && spriteRenderer != null && !cancellationToken.IsCancellationRequested)
                {
                    spriteRenderer.sprite = loaded;
                }
            }
            catch (OperationCanceledException)
            {
                // オブジェクト破棄やキャンセル時は中断
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[CharacterVisual2D] スプライトロード失敗 ({spriteName}): {ex.Message}");
            }
        }
    }
}
