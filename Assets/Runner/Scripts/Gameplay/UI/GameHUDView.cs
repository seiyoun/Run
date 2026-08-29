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
    /// ゲームパラメータは保持せず、各サブHUDへの描画指示およびUIイベント中継を担当します。
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

            BindPlayerEvents();
        }

        /// <summary>
        /// 破棄時にシングルトン参照をクリアし、イベントバインドを解除する。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            UnbindPlayerEvents();
        }

        /// <summary>
        /// 移動による歩数および所持ポイント表示を更新する。
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
        /// ジャスト回避成功時の演出およびプレイヤーへのボーナス付与を行う。
        /// </summary>
        public void OnJustDodge()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.CollectMoney(100);
                player.AddRage(25f);
            }

            if (pointStepHUD != null)
            {
                pointStepHUD.ShowJustDodgePopup(100);
            }
        }

        /// <summary>
        /// PlayerController の状態変更イベントを購読する。
        /// </summary>
        public void BindPlayerEvents()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            player.OnStepsChanged += HandleStepsChanged;
            player.OnMoneyCollected += HandleMoneyCollected;
            player.OnRageChanged += HandleRageChanged;
            player.OnAwakeningChanged += HandleAwakeningChanged;

            if (pointStepHUD != null)
            {
                pointStepHUD.SetSteps(player.CurrentSteps);
                pointStepHUD.SetPoints(player.CurrentMoney, true);
            }

            if (rageGaugeHUD != null)
            {
                rageGaugeHUD.SetRage(player.CurrentRage, player.MaxRage, true);
                rageGaugeHUD.SetAwakened(player.IsAwakened, player.AwakeningRemainingTime);
            }
        }

        /// <summary>
        /// PlayerController の状態変更イベントの購読を解除する。
        /// </summary>
        public void UnbindPlayerEvents()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            player.OnStepsChanged -= HandleStepsChanged;
            player.OnMoneyCollected -= HandleMoneyCollected;
            player.OnRageChanged -= HandleRageChanged;
            player.OnAwakeningChanged -= HandleAwakeningChanged;
        }

        /// <summary>
        /// バナーやショップのクリックイベントをバインドする。
        /// </summary>
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
        /// 歩数変更時のHUD表示を更新する。
        /// </summary>
        /// <param name="steps">現在の歩数</param>
        private void HandleStepsChanged(int steps)
        {
            if (pointStepHUD != null)
            {
                pointStepHUD.SetSteps(steps);
            }
        }

        /// <summary>
        /// 所持金変更時のHUD表示を更新する。
        /// </summary>
        /// <param name="amount">加算額</param>
        private void HandleMoneyCollected(long amount)
        {
            var player = PlayerController.Instance;
            if (player != null && pointStepHUD != null)
            {
                pointStepHUD.SetPoints(player.CurrentMoney);
            }
        }

        /// <summary>
        /// 怒りゲージ変更時のHUD表示を更新する。
        /// </summary>
        /// <param name="current">現在の怒り値</param>
        /// <param name="max">最大怒り値</param>
        private void HandleRageChanged(float current, float max)
        {
            if (rageGaugeHUD != null)
            {
                rageGaugeHUD.SetRage(current, max);
            }
        }

        /// <summary>
        /// 覚醒状態変更時のHUD表示を更新する。
        /// </summary>
        /// <param name="isAwakened">覚醒中かどうか</param>
        /// <param name="remainingTime">残り持続時間(秒)</param>
        private void HandleAwakeningChanged(bool isAwakened, float remainingTime)
        {
            if (rageGaugeHUD != null)
            {
                rageGaugeHUD.SetAwakened(isAwakened, remainingTime);
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
        /// ショップアイテム購入時に ShopItemEffectApplier を介して効果を適用する。
        /// </summary>
        /// <param name="item">購入されたアイテムデータ</param>
        private void HandleItemPurchased(ShopItemData item)
        {
            ShopItemEffectApplier.ApplyEffect(item, PlayerController.Instance);
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
