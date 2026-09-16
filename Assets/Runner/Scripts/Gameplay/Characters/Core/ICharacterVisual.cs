/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクターやエンティティの見た目・アニメーション表示を抽象化するインターフェース。
 */

using UnityEngine;

namespace Runner
{
    /// <summary>
    /// キャラクターやエンティティの見た目（スプライト、アニメーション、エフェクト等）を制御するインターフェース。
    /// </summary>
    public interface ICharacterVisual
    {
        /// <summary>
        /// キャラクターの向き（左右反転など）を設定する。
        /// </summary>
        /// <param name="facingDirection">向いている方向ベクトル</param>
        void SetFacingDirection(Vector2 facingDirection);

        /// <summary>
        /// 被ダメージ時の点滅（ヒットフラッシュ）エフェクトを再生する。
        /// </summary>
        /// <param name="flashDuration">点滅時間（秒）</param>
        void PlayHitFlash(float flashDuration = 0.1f);

        /// <summary>
        /// キャラクターの色合い（カラーティント）を設定する。
        /// </summary>
        /// <param name="color">適用するカラー</param>
        void SetColor(Color color);

        /// <summary>
        /// 見た目の表示・非表示を切り替える。
        /// </summary>
        /// <param name="visible">表示フラグ</param>
        void SetVisible(bool visible);

        /// <summary>
        /// スプライト画像を直接設定する。
        /// </summary>
        /// <param name="sprite">設定するスプライト</param>
        void SetSprite(Sprite sprite);

        /// <summary>
        /// 指定された画像名に基づいてキャッシュまたはリソースからスプライトを読み込み適用する。
        /// </summary>
        /// <param name="imageName">スプライト画像名（拡張子なし）</param>
        void LoadSprite(string imageName);
    }
}
