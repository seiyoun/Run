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

        [Tooltip("脱出タイマーHUDコンポーネント")]
        [SerializeField] private EscapeTimerHUD escapeTimerHUD;

        [Tooltip("タイムセール通知バナーコンポーネント")]
        [SerializeField] private SaleNotificationBanner saleNotificationBanner;

        [Tooltip("スマホ通販ショップモーダルコンポーネント")]
        [SerializeField] private SmartphoneShopModalView shopModalView;

        [Tooltip("バーチャルジョイスティックUIコンポーネント")]
        [SerializeField] private VirtualJoystickView virtualJoystickView;

        [Tooltip("バフ表示HUDコンポーネント")]
        [SerializeField] private BuffHUD buffHUD;

        [Header("Buff Icons")]
        [Tooltip("速度バフアイコンスプライト")]
        [SerializeField] private Sprite speedBuffIcon;

        [Tooltip("HP回復バフアイコンスプライト")]
        [SerializeField] private Sprite hpRegenBuffIcon;

        /// <summary>ポイ活・歩数表示HUD</summary>
        public PointStepHUD PointStepHUD => pointStepHUD;

        /// <summary>脱出タイマーHUD</summary>
        public EscapeTimerHUD EscapeTimerHUD => escapeTimerHUD;

        /// <summary>アイテム入荷通知バナー</summary>
        public SaleNotificationBanner SaleBanner => saleNotificationBanner;

        /// <summary>スマホ通販ショップモーダル</summary>
        public SmartphoneShopModalView ShopModal => shopModalView;

        /// <summary>バーチャルジョイスティックUI</summary>
        public VirtualJoystickView VirtualJoystick => virtualJoystickView;

        /// <summary>バフ表示HUD</summary>
        public BuffHUD BuffHUD => buffHUD;

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
            EnsureBuffHUD();
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
        /// 毎フレームのHUD表示更新を行う。
        /// </summary>
        private void Update()
        {
            UpdateBuffHUD();
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
        /// アイテム入荷までの進捗情報（残りポイント・進捗率）を描画HUDへ通知・更新する。
        /// </summary>
        /// <param name="remainingPoints">入荷までの残り必要ポイント</param>
        /// <param name="progress">進捗率 (0.0 〜 1.0)</param>
        /// <param name="instant">アニメーションせず即時反映するかどうか</param>
        public void UpdateRestockProgress(long remainingPoints, float progress, bool instant = false)
        {
            if (pointStepHUD != null)
            {
                pointStepHUD.SetRestockProgress(remainingPoints, progress, instant);
            }
        }

        /// <summary>
        /// ジャスト回避成功時の演出およびプレイヤーへのボーナス付与を行う。
        /// </summary>
        public void OnJustDodge()
        {
            if (GameRecordTracker.HasInstance)
            {
                GameRecordTracker.Instance.AddMoney(100);
            }

            if (pointStepHUD != null)
            {
                pointStepHUD.ShowJustDodgePopup(100);
            }
        }

        /// <summary>
        /// GameRecordTracker の状態変更イベント（歩数・所持金）を購読する。
        /// </summary>
        public void BindPlayerEvents()
        {
            if (GameRecordTracker.HasInstance)
            {
                var tracker = GameRecordTracker.Instance;
                tracker.OnStepsChanged += HandleStepsChanged;
                tracker.OnMoneyChanged += HandleMoneyChanged;

                if (pointStepHUD != null)
                {
                    pointStepHUD.SetSteps(tracker.TotalSteps);
                    pointStepHUD.SetPoints(tracker.CurrentMoney, true);
                }
            }
        }

        /// <summary>
        /// GameRecordTracker の状態変更イベント（歩数・所持金）の購読を解除する。
        /// </summary>
        public void UnbindPlayerEvents()
        {
            if (GameRecordTracker.HasInstance)
            {
                var tracker = GameRecordTracker.Instance;
                tracker.OnStepsChanged -= HandleStepsChanged;
                tracker.OnMoneyChanged -= HandleMoneyChanged;
            }
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
        /// 所持金残高変更時のHUD表示を更新する。
        /// </summary>
        /// <param name="currentMoney">現在の所持金残高</param>
        private void HandleMoneyChanged(long currentMoney)
        {
            if (pointStepHUD != null)
            {
                pointStepHUD.SetPoints(currentMoney);
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

        /// <summary>
        /// バフ表示HUDの参照が未設定の場合に子階層から取得する。
        /// </summary>
        private void EnsureBuffHUD()
        {
            if (buffHUD == null)
            {
                buffHUD = GetComponentInChildren<BuffHUD>(true);
            }
        }

        /// <summary>
        /// プレイヤーのバフ状態を監視し、BuffHUDへ描画指示を伝達する。
        /// </summary>
        private void UpdateBuffHUD()
        {
            if (buffHUD == null) return;

            var player = PlayerController.Instance;
            if (player == null)
            {
                buffHUD.ClearBuffs();
                return;
            }

            buffHUD.BeginUpdate();

            var speedBuff = player.Buffs?.GetBuff<SpeedBuff>();
            if (speedBuff != null && speedBuff.IsActive)
            {
                float total = speedBuff.Duration > 0f ? speedBuff.Duration : 5.0f;
                buffHUD.AddBuffDisplay(speedBuffIcon, speedBuff.RemainingDuration, total);
            }

            var hpRegenBuff = player.Buffs?.GetBuff<HpRegenBuff>();
            if (hpRegenBuff != null && hpRegenBuff.IsActive)
            {
                float total = hpRegenBuff.Duration > 0f ? hpRegenBuff.Duration : 5.0f;
                buffHUD.AddBuffDisplay(hpRegenBuffIcon, hpRegenBuff.RemainingDuration, total);
            }

            buffHUD.EndUpdate();
        }
    }
}
