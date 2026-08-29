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
        [Tooltip("ポイ活・歩数HUDコンポーネント")]
        [SerializeField] private PointStepHUD pointStepHUD;

        [Tooltip("怒りゲージHUDコンポーネント")]
        [SerializeField] private RageGaugeHUD rageGaugeHUD;

        [Tooltip("脱出タイマーHUDコンポーネント")]
        [SerializeField] private EscapeTimerHUD escapeTimerHUD;

        [Tooltip("タイムセール通知バナーコンポーネント")]
        [SerializeField] private SaleNotificationBanner saleNotificationBanner;

        [Tooltip("スマホ通販ショップモーダルコンポーネント")]
        [SerializeField] private SmartphoneShopModalView shopModalView;

        [Tooltip("バーチャルジョイスティックUIコンポーネント")]
        [SerializeField] private VirtualJoystickView virtualJoystickView;

        [Header("Item Arrival Trigger Settings")]
        [Tooltip("何ポイント貯まるごとにアイテム入荷通知を発生させるか")]
        [SerializeField] private long saleTriggerPointInterval = 300;
        private long nextSaleTriggerPoint = 300;

        /// <summary>ポイ活・歩数表示HUD</summary>
        public PointStepHUD PointStepHUD => pointStepHUD;

        /// <summary>怒りゲージHUD</summary>
        public RageGaugeHUD RageGaugeHUD => rageGaugeHUD;

        /// <summary>脱出タイマーHUD</summary>
        public EscapeTimerHUD EscapeTimerHUD => escapeTimerHUD;

        /// <summary>アイテム入荷通知バナー</summary>
        public SaleNotificationBanner SaleBanner => saleNotificationBanner;

        /// <summary>スマホ通販ショップモーダル</summary>
        public SmartphoneShopModalView ShopModal => shopModalView;

        /// <summary>バーチャルジョイスティックUI</summary>
        public VirtualJoystickView VirtualJoystick => virtualJoystickView;

        /// <summary>
        /// インスタンスの初期化およびバインドを行う。
        /// </summary>
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

            EnsureVirtualJoystick();
            SetupBindings();
        }

        /// <summary>
        /// 初回フレームでの初期バインドを行う。
        /// </summary>
        private void Start()
        {
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
        /// <param name="distance">移動距離</param>
        public void OnPlayerMoved(float distance)
        {
            if (distance <= 0f) return;

            var player = PlayerController.Instance;
            if (player != null && pointStepHUD != null)
            {
                pointStepHUD.SetSteps(player.CurrentSteps);
                pointStepHUD.SetPoints(player.CurrentMoney);
            }

            // 一定ポイント到達によるアイテム入荷通知のトリガー判定
            if (pointStepHUD != null && pointStepHUD.CurrentPoint >= nextSaleTriggerPoint)
            {
                TriggerItemArrivalNotification();
                nextSaleTriggerPoint += saleTriggerPointInterval;
            }
        }

        /// <summary>
        /// アイテム入荷通知を発火する。
        /// </summary>
        public void TriggerItemArrivalNotification()
        {
            if (saleNotificationBanner != null && !saleNotificationBanner.IsShowing)
            {
                saleNotificationBanner.ShowBanner();
            }
        }

        /// <summary>
        /// アイテム入荷通知を発火する（後方互換エイリアス）。
        /// </summary>
        public void TriggerSaleNotification() => TriggerItemArrivalNotification();

        /// <summary>
        /// ジャスト回避成功時の処理
        /// </summary>
        public void OnJustDodge()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.CollectMoney(100);
            }

            if (pointStepHUD != null)
            {
                // ボーナスポイント獲得演出
                pointStepHUD.TriggerJustDodge(100);
            }

            if (rageGaugeHUD != null)
            {
                // 怒りゲージ大幅上昇
                rageGaugeHUD.AddRage(25f);
            }
        }

        /// <summary>
        /// セール通知バナーのクリックイベントを処理する。
        /// </summary>
        private void HandleSaleBannerClicked()
        {
            if (shopModalView != null)
            {
                shopModalView.OpenShop();
            }
        }

        /// <summary>
        /// ショップアイテム購入時の効果適用を処理する。
        /// </summary>
        /// <param name="item">購入されたアイテムデータ</param>
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

        /// <summary>
        /// バーチャルジョイスティックUIの参照が未設定の場合に子階層から取得する。
        /// </summary>
        private void EnsureVirtualJoystick()
        {
            if (virtualJoystickView == null)
            {
                virtualJoystickView = GetComponentInChildren<VirtualJoystickView>(true);
            }
        }
    }
}

