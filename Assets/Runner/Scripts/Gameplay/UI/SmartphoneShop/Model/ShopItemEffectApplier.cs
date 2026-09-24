/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ショップで購入されたアイテムの効果（回復、速度上昇、怒り上昇など）をプレイヤーやゲーム状態へ適用するサービスクラス。
 */

using System;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ショップアイテム購入時の各種効果適用処理を担当するユーティリティクラス。
    /// UIからゲームプレイロジックを分離し、アイテム効果を一元管理します。
    /// </summary>
    public static class ShopItemEffectApplier
    {
        /// <summary>
        /// 購入されたアイテムの効果を対象のプレイヤーコントローラーへ適用する。
        /// </summary>
        /// <param name="item">購入されたアイテムデータ</param>
        /// <param name="player">効果適用対象のプレイヤー</param>
        public static void ApplyEffect(ShopItemData item, PlayerController player)
        {
            if (item == null) return;

            DebugLogger.Log($"[ShopItemEffectApplier] アイテム効果適用開始: {item.itemName} (ID: {item.ItemId})");

            if (player == null)
            {
                DebugLogger.Error("[ShopItemEffectApplier] プレイヤーが存在しないためアイテム効果を適用できません。");
                return;
            }

            switch (item.ItemId)
            {
                case ShopItemId.Drone:
                    if (WeaponManager.HasInstance || WeaponManager.Instance != null)
                    {
                        _ = WeaponManager.Instance.UpgradeWeaponAsync(WeaponType.Drone);
                    }
                    DebugLogger.Log($"[ShopItemEffectApplier] ドローン効果を発動: {item.itemName}");
                    break;

                case ShopItemId.SpeedUp:
                    BuffManager.ApplyBuff(player, BuffType.Speed);
                    DebugLogger.Log($"[ShopItemEffectApplier] 移動速度アップバフを付与: {item.itemName}");
                    break;
            }
        }
    }
}
