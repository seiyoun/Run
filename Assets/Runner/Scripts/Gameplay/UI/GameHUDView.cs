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

        #region Procedural UI Builder

        /// <summary>
        /// 独立した ScreenSpaceOverlay Canvas を生成し、その直下に全HUD要素を動的に構築・初期化する。
        /// </summary>
        public static GameHUDView Create(Canvas targetCanvas = null)
        {
            // 既存のScreenSpace Canvasを探すか、無ければ専用のCanvasを生成
            if (targetCanvas == null || targetCanvas.renderMode == RenderMode.WorldSpace)
            {
                // シーン内の既存 ScreenSpace Canvas を検索
                var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    if (c.renderMode != RenderMode.WorldSpace && c.gameObject.name != "DebugCanvas")
                    {
                        targetCanvas = c;
                        break;
                    }
                }

                // 見つからなければ専用の GameHUDCanvas を生成
                if (targetCanvas == null)
                {
                    var canvasObj = new GameObject("GameHUDCanvas");
                    var canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 10;

                    var scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080, 1920);
                    scaler.matchWidthOrHeight = 0.5f;

                    canvasObj.AddComponent<GraphicRaycaster>();
                    targetCanvas = canvas;
                }
            }

            return BuildHUD(targetCanvas);
        }

        /// <summary>
        /// 指定された ScreenSpace Canvas 直下に全HUD要素を動的に構築・初期化する。
        /// </summary>
        public static GameHUDView BuildHUD(Canvas targetCanvas)
        {
            if (targetCanvas == null)
            {
                return Create();
            }

            var defaultFont = TMP_Settings.defaultFontAsset;
            var rootTransform = targetCanvas.transform;

            var hudObj = new GameObject("GameHUDController");
            hudObj.transform.SetParent(rootTransform, false);
            var hud = hudObj.AddComponent<GameHUDView>();

            // 1. Point & Step HUD (右上)
            var pointStepObj = CreateUIObject("PointStepHUD", rootTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -45), new Vector2(260, 70));
            var pointStepBg = pointStepObj.AddComponent<Image>();
            pointStepBg.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);
            var pointStepComp = pointStepObj.AddComponent<PointStepHUD>();

            var ptTextObj = CreateUIObject("PointText", pointStepObj.transform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-10, 0));
            var ptTmp = ptTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) ptTmp.font = defaultFont;
            ptTmp.fontSize = 20;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.Right;

            var stTextObj = CreateUIObject("StepText", pointStepObj.transform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(-10, 0));
            var stTmp = stTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) stTmp.font = defaultFont;
            stTmp.fontSize = 15;
            stTmp.alignment = TextAlignmentOptions.Right;
            stTmp.color = new Color(0.8f, 0.9f, 1f, 0.9f);

            // ジャスト回避ポップアップ
            var dodgeObj = CreateUIObject("JustDodgePopup", pointStepObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -35), new Vector2(200, 50));
            var dodgeGroup = dodgeObj.AddComponent<CanvasGroup>();
            var dodgeTmp = dodgeObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) dodgeTmp.font = defaultFont;
            dodgeTmp.fontSize = 22;
            dodgeTmp.fontStyle = FontStyles.Bold;
            dodgeTmp.alignment = TextAlignmentOptions.Center;

            pointStepComp.SetupReferences(ptTmp, stTmp, dodgeTmp, dodgeGroup);
            hud.pointStepHUD = pointStepComp;

            // 2. Escape Timer HUD (中央上部)
            var timerObj = CreateUIObject("EscapeTimerHUD", rootTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -45), new Vector2(200, 60));
            var timerBg = timerObj.AddComponent<Image>();
            timerBg.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);
            var timerComp = timerObj.AddComponent<EscapeTimerHUD>();

            var timeTextObj = CreateUIObject("TimerText", timerObj.transform, new Vector2(0, 0.35f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var timeTmp = timeTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) timeTmp.font = defaultFont;
            timeTmp.fontSize = 22;
            timeTmp.fontStyle = FontStyles.Bold;
            timeTmp.alignment = TextAlignmentOptions.Center;

            var timerStatusObj = CreateUIObject("StatusText", timerObj.transform, new Vector2(0, 0), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            var timerStatusTmp = timerStatusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) timerStatusTmp.font = defaultFont;
            timerStatusTmp.fontSize = 13;
            timerStatusTmp.alignment = TextAlignmentOptions.Center;
            timerStatusTmp.text = "非常口 開放まで";
            timerStatusTmp.color = Color.yellow;

            // 非常口アラートバナー
            var alertBannerObj = CreateUIObject("ExitAlertBanner", rootTransform, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(500, 50));
            var alertBannerBg = alertBannerObj.AddComponent<Image>();
            alertBannerBg.color = new Color(0.9f, 0.2f, 0.1f, 0.9f);
            var alertTextObj = CreateUIObject("AlertText", alertBannerObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var alertTmp = alertTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) alertTmp.font = defaultFont;
            alertTmp.fontSize = 20;
            alertTmp.fontStyle = FontStyles.Bold;
            alertTmp.alignment = TextAlignmentOptions.Center;
            alertTmp.color = Color.white;
            alertBannerObj.SetActive(false);

            timerComp.SetupReferences(timeTmp, timerStatusTmp, alertBannerObj, alertTmp, null, null);
            hud.escapeTimerHUD = timerComp;

            // 3. Rage Gauge HUD (画面下部)
            var rageObj = CreateUIObject("RageGaugeHUD", rootTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 55), new Vector2(480, 48));
            var rageBg = rageObj.AddComponent<Image>();
            rageBg.color = new Color(0.1f, 0.08f, 0.08f, 0.85f);
            var rageComp = rageObj.AddComponent<RageGaugeHUD>();

            var rageFillObj = CreateUIObject("Fill", rageObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6, -6));
            var rageFillImg = rageFillObj.AddComponent<Image>();
            rageFillImg.color = new Color(1f, 0.45f, 0.1f, 1f);
            rageFillImg.type = Image.Type.Filled;
            rageFillImg.fillMethod = Image.FillMethod.Horizontal;
            rageFillImg.fillAmount = 0f;

            var rageEffectObj = CreateUIObject("EffectGroup", rageObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rageEffectGroup = rageEffectObj.AddComponent<CanvasGroup>();
            var rageEffectImg = rageEffectObj.AddComponent<Image>();
            rageEffectImg.color = new Color(1f, 1f, 0.5f, 0.4f);

            var rageTextObj = CreateUIObject("RageText", rageObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rageTmp = rageTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) rageTmp.font = defaultFont;
            rageTmp.fontSize = 18;
            rageTmp.fontStyle = FontStyles.Bold;
            rageTmp.alignment = TextAlignmentOptions.Center;
            rageTmp.color = Color.white;

            var rageStatusObj = CreateUIObject("StatusLabel", rageObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 16), new Vector2(300, 25));
            var rageStatusTmp = rageStatusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) rageStatusTmp.font = defaultFont;
            rageStatusTmp.fontSize = 14;
            rageStatusTmp.alignment = TextAlignmentOptions.Center;
            rageStatusTmp.color = new Color(1f, 0.8f, 0.4f, 1f);

            rageComp.SetupReferences(rageFillImg, rageTmp, rageStatusTmp, rageEffectGroup);
            hud.rageGaugeHUD = rageComp;

            // 4. Sale Notification Banner (上部プッシュ通知)
            var bannerObj = CreateUIObject("SaleNotificationBanner", rootTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 180), new Vector2(460, 75));
            var bannerImg = bannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.12f, 0.15f, 0.28f, 0.95f);
            var bannerBtn = bannerObj.AddComponent<Button>();
            var bannerComp = bannerObj.AddComponent<SaleNotificationBanner>();

            var bannerTitleObj = CreateUIObject("Title", bannerObj.transform, new Vector2(0, 0.5f), new Vector2(0.75f, 1), new Vector2(15, 0), Vector2.zero);
            var bannerTitleTmp = bannerTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) bannerTitleTmp.font = defaultFont;
            bannerTitleTmp.fontSize = 16;
            bannerTitleTmp.fontStyle = FontStyles.Bold;
            bannerTitleTmp.color = new Color(1f, 0.85f, 0.2f, 1f);
            bannerTitleTmp.text = "⚡️ タイムセール開催中！";

            var bannerTimerObj = CreateUIObject("Timer", bannerObj.transform, new Vector2(0.75f, 0.5f), new Vector2(1, 1), new Vector2(-15, 0), Vector2.zero);
            var bannerTimerTmp = bannerTimerObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) bannerTimerTmp.font = defaultFont;
            bannerTimerTmp.fontSize = 13;
            bannerTimerTmp.alignment = TextAlignmentOptions.Right;
            bannerTimerTmp.color = Color.white;

            var bannerMsgObj = CreateUIObject("Message", bannerObj.transform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(15, 0), new Vector2(-30, 0));
            var bannerMsgTmp = bannerMsgObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) bannerMsgTmp.font = defaultFont;
            bannerMsgTmp.fontSize = 14;
            bannerMsgTmp.color = Color.white;
            bannerMsgTmp.text = "限定アイテム入荷！タップしてチェック ➔";

            bannerComp.SetupReferences((RectTransform)bannerObj.transform, bannerBtn, bannerTitleTmp, bannerMsgTmp, bannerTimerTmp);
            hud.saleNotificationBanner = bannerComp;

            // 5. Smartphone Shop Modal View (中央モーダル)
            var modalObj = CreateUIObject("SmartphoneShopModal", rootTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 620));
            var modalImg = modalObj.AddComponent<Image>();
            modalImg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);
            var shopModal = modalObj.AddComponent<SmartphoneShopModalView>();

            // ヘッダー
            var shopHeaderObj = CreateUIObject("Header", modalObj.transform, new Vector2(0, 0.88f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var shopHeaderBg = shopHeaderObj.AddComponent<Image>();
            shopHeaderBg.color = new Color(0.15f, 0.18f, 0.3f, 1f);

            var shopTitleObj = CreateUIObject("Title", shopHeaderObj.transform, new Vector2(0.05f, 0), new Vector2(0.7f, 1), Vector2.zero, Vector2.zero);
            var shopTitleTmp = shopTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) shopTitleTmp.font = defaultFont;
            shopTitleTmp.fontSize = 20;
            shopTitleTmp.fontStyle = FontStyles.Bold;
            shopTitleTmp.text = "📱 ゲリラタイムセール ⚡️";
            shopTitleTmp.alignment = TextAlignmentOptions.Left;
            shopTitleTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

            var closeBtnObj = CreateUIObject("CloseBtn", shopHeaderObj.transform, new Vector2(0.85f, 0.15f), new Vector2(0.97f, 0.85f), Vector2.zero, Vector2.zero);
            var closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            var closeBtn = closeBtnObj.AddComponent<Button>();
            var closeBtnTextObj = CreateUIObject("Text", closeBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var closeBtnTmp = closeBtnTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) closeBtnTmp.font = defaultFont;
            closeBtnTmp.fontSize = 18;
            closeBtnTmp.text = "✕";
            closeBtnTmp.alignment = TextAlignmentOptions.Center;

            var userPtObj = CreateUIObject("UserPoints", modalObj.transform, new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.87f), Vector2.zero, Vector2.zero);
            var userPtTmp = userPtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) userPtTmp.font = defaultFont;
            userPtTmp.fontSize = 17;
            userPtTmp.text = "所持ポイント: ¥0 pt";

            // 3つのカード作成
            var cards = new ShopItemCardUI[3];
            float cardHeight = 135f;
            float startY = 320f;

            for (int i = 0; i < 3; i++)
            {
                var cardObj = CreateUIObject($"Card_{i}", modalObj.transform, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.5f), new Vector2(0, startY - (i * 155f)), new Vector2(0, cardHeight));
                var cardBg = cardObj.AddComponent<Image>();
                cardBg.color = new Color(0.14f, 0.16f, 0.24f, 1f);

                // アイコン
                var iconObj = CreateUIObject("Icon", cardObj.transform, new Vector2(0.02f, 0.5f), new Vector2(0.02f, 0.5f), new Vector2(30, 0), new Vector2(50, 50));
                var iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) iconTmp.font = defaultFont;
                iconTmp.fontSize = 32;
                iconTmp.alignment = TextAlignmentOptions.Center;

                // 商品名
                var nameObj = CreateUIObject("Name", cardObj.transform, new Vector2(0.18f, 0.6f), new Vector2(0.68f, 0.95f), Vector2.zero, Vector2.zero);
                var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) nameTmp.font = defaultFont;
                nameTmp.fontSize = 16;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.color = Color.white;

                // 説明
                var descObj = CreateUIObject("Desc", cardObj.transform, new Vector2(0.18f, 0.05f), new Vector2(0.68f, 0.6f), Vector2.zero, Vector2.zero);
                var descTmp = descObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) descTmp.font = defaultFont;
                descTmp.fontSize = 12;
                descTmp.color = new Color(0.8f, 0.85f, 0.9f, 0.9f);

                // 価格
                var priceObj = CreateUIObject("Price", cardObj.transform, new Vector2(0.7f, 0.55f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
                var priceTmp = priceObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) priceTmp.font = defaultFont;
                priceTmp.fontSize = 15;
                priceTmp.fontStyle = FontStyles.Bold;
                priceTmp.alignment = TextAlignmentOptions.Right;
                priceTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

                // 購入ボタン
                var buyBtnObj = CreateUIObject("BuyBtn", cardObj.transform, new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.5f), Vector2.zero, Vector2.zero);
                var buyBtnImg = buyBtnObj.AddComponent<Image>();
                buyBtnImg.color = new Color(0.15f, 0.65f, 0.95f, 1f);
                var buyBtn = buyBtnObj.AddComponent<Button>();

                var buyBtnTextObj = CreateUIObject("Text", buyBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var buyBtnTmp = buyBtnTextObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) buyBtnTmp.font = defaultFont;
                buyBtnTmp.fontSize = 14;
                buyBtnTmp.fontStyle = FontStyles.Bold;
                buyBtnTmp.text = "即時購入";
                buyBtnTmp.alignment = TextAlignmentOptions.Center;
                buyBtnTmp.color = Color.white;

                cards[i] = new ShopItemCardUI
                {
                    cardRoot = cardObj,
                    iconText = iconTmp,
                    nameText = nameTmp,
                    descText = descTmp,
                    priceText = priceTmp,
                    buyButton = buyBtn,
                    buyButtonText = buyBtnTmp
                };
            }

            modalObj.SetActive(false);
            shopModal.SetupReferences(modalObj, userPtTmp, closeBtn, cards);
            hud.shopModalView = shopModal;

            hud.SetupBindings();
            return hud;
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = 5;
            obj.transform.SetParent(parent, false);
            var rt = (RectTransform)obj.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return obj;
        }

        #endregion
    }
}

