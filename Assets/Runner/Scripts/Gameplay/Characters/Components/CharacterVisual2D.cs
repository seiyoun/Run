/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ICharacterVisual を実装した 2D スプライト描画・アニメーションコンポーネント。
 */

using System.Collections;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 2D スプライトの向き反転、歩行ボビングアニメーション、被弾フラッシュ等の表示を制御するクラス。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class CharacterVisual2D : MonoBehaviour, ICharacterVisual
    {
        [Header("Animation Settings")]
        [Tooltip("歩行時のぷにぷに上下揺れ（ボビング演出）を有効にするか")]
        [SerializeField]
        private bool enableBobbing = true;

        [Tooltip("ボビングの揺れ幅")]
        [SerializeField]
        private float bobbingIntensity = 0.08f;

        [Header("References")]
        [SerializeField]
        private SpriteRenderer spriteRenderer;

        private float bobTimer;
        private Vector3 initialScale = Vector3.one;
        private Color baseColor = Color.white;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            initialScale = transform.localScale;
            baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            EnsureDefaultSprite();
        }

        #region ICharacterVisual Implementation

        /// <summary>
        /// キャラクターの向き（左右反転）を設定する。
        /// </summary>
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
        /// 移動状態・速度に応じたボビング（上下揺れ）演出を更新する。
        /// </summary>
        public void UpdateMovementVisuals(Vector2 moveInput, float moveSpeed, float deltaTime)
        {
            if (!enableBobbing) return;

            if (moveInput.sqrMagnitude > 0.01f)
            {
                bobTimer += deltaTime * moveSpeed * 3f;
                var squishX = 1f + Mathf.Sin(bobTimer) * bobbingIntensity;
                var squishY = 1f - Mathf.Sin(bobTimer) * bobbingIntensity;
                transform.localScale = new Vector3(
                    initialScale.x * squishX,
                    initialScale.y * squishY,
                    initialScale.z
                );
            }
            else
            {
                // 待機時は滑らかに基準スケールへ戻す
                transform.localScale = Vector3.Lerp(transform.localScale, initialScale, deltaTime * 10f);
                bobTimer = 0f;
            }
        }

        /// <summary>
        /// 被ダメージ時の点滅（白フラッシュ）を再生する。
        /// </summary>
        public void PlayHitFlash(float flashDuration = 0.1f)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(HitFlashRoutine(flashDuration));
        }

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
        /// キャラクターの色合いを設定する。
        /// </summary>
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
        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = visible;
            }
        }

        #endregion

        /// <summary>
        /// スプライトが未設定の場合に、向きが分かりやすい仮のプレイヤースプライトを自動生成する。
        /// </summary>
        private void EnsureDefaultSprite()
        {
            if (spriteRenderer != null && spriteRenderer.sprite == null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };

                var colors = new Color[size * size];
                var center = new Vector2(size / 2f, size / 2f);
                var bodyColor = new Color(0.2f, 0.8f, 0.95f, 1f); // 水色のヒーローキャラクター
                var outlineColor = new Color(0.08f, 0.35f, 0.45f, 1f);
                var eyeColor = new Color(0.1f, 0.1f, 0.15f, 1f);
                var eyeGlint = Color.white;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist > 28f)
                        {
                            colors[y * size + x] = Color.clear;
                        }
                        else if (dist > 25f)
                        {
                            colors[y * size + x] = outlineColor;
                        }
                        else
                        {
                            colors[y * size + x] = bodyColor;

                            // 目の描画（右向きデフォルト）
                            if (x >= 36 && x <= 42 && y >= 32 && y <= 42)
                            {
                                colors[y * size + x] = eyeColor;
                            }
                            if (x >= 38 && x <= 40 && y >= 38 && y <= 40)
                            {
                                colors[y * size + x] = eyeGlint;
                            }
                        }
                    }
                }

                texture.SetPixels(colors);
                texture.Apply();

                spriteRenderer.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f),
                    64f
                );
            }
        }
    }
}
