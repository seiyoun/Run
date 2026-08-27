/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ画面（HUD）の全UI要素を統合・統括する総合HUDビューコンポーネント。
 *                ポイ活/歩数、怒りゲージ、脱出タイマー、タイムセール通知、スマホ通販モーダルを一元管理します。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// ゲーム画面の全HUD（情報表示・通知・ショップモーダル）を統括するメインビュークラス。
    /// プロシージャルUI生成にも対応し、エディタ・実行時のどちらでも完全自動構築が可能です。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHUDView : MonoBehaviour
    {
        public static GameHUDView Instance { get; private set; }

        [Header("Sub HUD Components")]
        [SerializeField] private PointStepHUD pointStepHUD;
        [SerializeField] private RageGaugeHUD rageGaugeHUD;
        [SerializeField] private EscapeTimerHUD escapeTimerHUD;
        [SerializeField] private SaleNotificationBanner saleNotificationBanner;
        [SerializeField] private SmartphoneShopModalView shopModalView;

        [Header("Auto Sale Trigger Settings")]
        [Tooltip("何ポイント貯まるごとにタイムセール通知を発生させるか")]
        [SerializeField] private long saleTriggerPointInterval = 300;
        private long nextSaleTriggerPoint = 300;

        public PointStepHUD PointStepHUD => pointStepHUD;
        public RageGaugeHUD RageGaugeHUD => rageGaugeHUD;
        public EscapeTimerHUD EscapeTimerHUD => escapeTimerHUD;
        public SaleNotificationBanner SaleBanner => saleNotificationBanner;
        public SmartphoneShopModalView ShopModal => shopModalView;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            SetupBindings();
        }

        private void Start()
        {
            // 初期バインド
            if (shopModalView != null && pointStepHUD != null)
            {
                shopModalView.BindPointHUD(pointStepHUD);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void SetupBindings()
        {
            if (saleNotificationBanner != null)
            {
                saleNotificationBanner.OnBannerClicked += HandleSaleBannerClicked;
            }

            if (shopModalView != null)
            {
                shopModalView.OnItemPurchased += HandleItemPurchased;
            }
        }

        /// <summary>
        /// 移動による歩数加算およびポイント獲得を処理する。
        /// </summary>
        public void OnPlayerMoved(float distance)
        {
            if (distance <= 0f) return;

            // ポイ活・歩数加算
            if (pointStepHUD != null)
            {
                pointStepHUD.OnDistanceMoved(distance);
            }

            // 怒りゲージの上昇（逃げることで徐々に蓄積）
            if (rageGaugeHUD != null)
            {
                rageGaugeHUD.AddRage(distance * 1.5f);
            }

            // 一定ポイント到達によるタイムセール通知のトリガー判定
            if (pointStepHUD != null && pointStepHUD.CurrentPoint >= nextSaleTriggerPoint)
            {
                TriggerSaleNotification();
                nextSaleTriggerPoint += saleTriggerPointInterval;
            }
        }

        /// <summary>
        /// タイムセール通知を発火する。
        /// </summary>
        public void TriggerSaleNotification()
        {
            if (saleNotificationBanner != null && !saleNotificationBanner.IsShowing)
            {
                saleNotificationBanner.ShowBanner();
            }
        }

        /// <summary>
        /// ジャスト回避成功時の処理
        /// </summary>
        public void OnJustDodge()
        {
            if (pointStepHUD != null)
            {
                // ボーナスポイント獲得
                pointStepHUD.TriggerJustDodge(100);
            }

            if (rageGaugeHUD != null)
            {
                // 怒りゲージ大幅上昇
                rageGaugeHUD.AddRage(25f);
            }
        }

        private void HandleSaleBannerClicked()
        {
            if (shopModalView != null)
            {
                shopModalView.OpenShop();
            }
        }

        private void HandleItemPurchased(ShopItemData item)
        {
            Debug.Log($"[GameHUDView] アイテム効果適用: {item.itemName} ({item.itemType})");

            var player = PlayerController.Instance;
            if (player == null) return;

            switch (item.itemType)
            {
                case ShopItemType.EnergyDrink:
                    player.Heal(100);
                    if (rageGaugeHUD != null) rageGaugeHUD.AddRage(30f);
                    break;

                case ShopItemType.SpeedSneakers:
                    player.MoveSpeed *= 1.25f;
                    break;

                case ShopItemType.Drone:
                case ShopItemType.Bodyguard:
                case ShopItemType.PointMagnet:
                case ShopItemType.BarrierShield:
                    // 各種アイテム効果
                    break;
            }
        }
    }
}

