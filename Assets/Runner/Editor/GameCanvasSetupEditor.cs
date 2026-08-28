/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game.unity シーン内の GameCanvas に全ゲームHUD（PointStepHUD, RageGaugeHUD, EscapeTimerHUD, 
 *                SaleNotificationBanner, SmartphoneShopModalView, GameHUDView）をあらかじめ構築・アタッチ・設定するエディタ拡張。
 */

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Runner.Editor
{
    public static class GameCanvasSetupEditor
    {
        private const string GameScenePath = "Assets/Runner/Scenes/Game.unity";
        private const string FontAssetPath = "Assets/Runner/Fonts/NotoSansJP_SDF.asset";
        private const string SpritePath = "Assets/Runner/Sprites/WhiteSquare.png";
        private const string KnobSpritePath = "Assets/Runner/Sprites/JoystickKnob.png";
        private const string RingBgSpritePath = "Assets/Runner/Sprites/JoystickRingBg.png";

        [MenuItem("Tools/Runner/Setup GameCanvas HUD in Scene")]
        public static void SetupGameCanvasHUD()
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.path != GameScenePath)
            {
                EditorSceneManager.OpenScene(GameScenePath);
            }

            // 0. スプライトのインポート設定確認
            EnsureSpriteImporter(SpritePath);
            EnsureSpriteImporter(KnobSpritePath);
            EnsureSpriteImporter(RingBgSpritePath);

            var whiteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (whiteSprite == null)
            {
                whiteSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }

            var knobSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KnobSpritePath);
            if (knobSprite == null)
            {
                knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            }

            var ringBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RingBgSpritePath);
            if (ringBgSprite == null)
            {
                ringBgSprite = knobSprite;
            }

            var defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (defaultFont == null)
            {
                defaultFont = TMP_Settings.defaultFontAsset;
            }

            // 1. シーン内の Canvas を検索または生成
            Canvas targetCanvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.renderMode != RenderMode.WorldSpace)
                {
                    targetCanvas = c;
                    break;
                }
            }

            if (targetCanvas == null)
            {
                var canvasObj = new GameObject("GameCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 10;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                targetCanvas.gameObject.name = "GameCanvas";
            }

            var canvasRoot = targetCanvas.transform;

            // 2. 既存の GameHUDController または古いHUD（ExpBarRoot含む）があれば一度クリーンアップ
            var existingController = canvasRoot.Find("GameHUDController");
            if (existingController != null)
            {
                Object.DestroyImmediate(existingController.gameObject);
            }

            var existingExp = canvasRoot.Find("ExpBarRoot");
            if (existingExp != null) Object.DestroyImmediate(existingExp.gameObject);

            var existingPoint = canvasRoot.Find("PointStepHUD");
            if (existingPoint != null) Object.DestroyImmediate(existingPoint.gameObject);

            var existingRage = canvasRoot.Find("RageGaugeHUD");
            if (existingRage != null) Object.DestroyImmediate(existingRage.gameObject);

            var existingTimer = canvasRoot.Find("EscapeTimerHUD");
            if (existingTimer != null) Object.DestroyImmediate(existingTimer.gameObject);

            var existingBanner = canvasRoot.Find("SaleNotificationBanner");
            if (existingBanner != null) Object.DestroyImmediate(existingBanner.gameObject);

            var existingModal = canvasRoot.Find("SmartphoneShopModal");
            if (existingModal != null) Object.DestroyImmediate(existingModal.gameObject);

            // 3. GameHUDView を GameCanvas にアタッチ（または取得）
            var hudView = targetCanvas.GetComponent<GameHUDView>();
            if (hudView == null)
            {
                hudView = targetCanvas.gameObject.AddComponent<GameHUDView>();
            }

            var hudSo = new SerializedObject(hudView);

            // --- A. PointStepHUD (右上) ---
            var pointStepObj = CreateUIObject("PointStepHUD", canvasRoot, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-190, -65), new Vector2(350, 100));
            var pointStepBg = pointStepObj.AddComponent<Image>();
            pointStepBg.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);
            var pointStepComp = pointStepObj.AddComponent<PointStepHUD>();

            var ptTextObj = CreateUIObject("PointText", pointStepObj.transform, new Vector2(0, 0.45f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(-15, 0));
            var ptTmp = ptTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) ptTmp.font = defaultFont;
            ptTmp.fontSize = 30;
            ptTmp.fontStyle = FontStyles.Bold;
            ptTmp.alignment = TextAlignmentOptions.Right;

            var stTextObj = CreateUIObject("StepText", pointStepObj.transform, new Vector2(0, 0), new Vector2(1, 0.45f), new Vector2(0, 0), new Vector2(-15, 0));
            var stTmp = stTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) stTmp.font = defaultFont;
            stTmp.fontSize = 22;
            stTmp.alignment = TextAlignmentOptions.Right;
            stTmp.color = new Color(0.8f, 0.9f, 1f, 0.9f);

            var dodgeObj = CreateUIObject("JustDodgePopup", pointStepObj.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, -45), new Vector2(280, 65));
            var dodgeGroup = dodgeObj.AddComponent<CanvasGroup>();
            var dodgeTmp = dodgeObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) dodgeTmp.font = defaultFont;
            dodgeTmp.fontSize = 28;
            dodgeTmp.fontStyle = FontStyles.Bold;
            dodgeTmp.alignment = TextAlignmentOptions.Center;

            pointStepComp.SetupReferences(ptTmp, stTmp, dodgeTmp, dodgeGroup);
            var pSo = new SerializedObject(pointStepComp);
            pSo.FindProperty("pointText").objectReferenceValue = ptTmp;
            pSo.FindProperty("stepText").objectReferenceValue = stTmp;
            pSo.FindProperty("justDodgePopupText").objectReferenceValue = dodgeTmp;
            pSo.FindProperty("justDodgeCanvasGroup").objectReferenceValue = dodgeGroup;
            pSo.ApplyModifiedProperties();

            // --- B. EscapeTimerHUD (中央上部) ---
            var timerObj = CreateUIObject("EscapeTimerHUD", canvasRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -65), new Vector2(280, 85));
            var timerBg = timerObj.AddComponent<Image>();
            timerBg.color = new Color(0.08f, 0.1f, 0.16f, 0.85f);
            var timerComp = timerObj.AddComponent<EscapeTimerHUD>();

            var timeTextObj = CreateUIObject("TimerText", timerObj.transform, new Vector2(0, 0.35f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var timeTmp = timeTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) timeTmp.font = defaultFont;
            timeTmp.fontSize = 32;
            timeTmp.fontStyle = FontStyles.Bold;
            timeTmp.alignment = TextAlignmentOptions.Center;

            var timerStatusObj = CreateUIObject("StatusText", timerObj.transform, new Vector2(0, 0), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            var timerStatusTmp = timerStatusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) timerStatusTmp.font = defaultFont;
            timerStatusTmp.fontSize = 18;
            timerStatusTmp.alignment = TextAlignmentOptions.Center;
            timerStatusTmp.text = "非常口 開放まで";
            timerStatusTmp.color = Color.yellow;

            var alertBannerObj = CreateUIObject("ExitAlertBanner", canvasRoot, new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(680, 75));
            var alertBannerBg = alertBannerObj.AddComponent<Image>();
            alertBannerBg.color = new Color(0.9f, 0.2f, 0.1f, 0.9f);
            var alertTextObj = CreateUIObject("AlertText", alertBannerObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var alertTmp = alertTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) alertTmp.font = defaultFont;
            alertTmp.fontSize = 26;
            alertTmp.fontStyle = FontStyles.Bold;
            alertTmp.alignment = TextAlignmentOptions.Center;
            alertTmp.color = Color.white;
            alertBannerObj.SetActive(false);

            timerComp.SetupReferences(timeTmp, timerStatusTmp, alertBannerObj, alertTmp, null, null);
            var tSo = new SerializedObject(timerComp);
            tSo.FindProperty("timerText").objectReferenceValue = timeTmp;
            tSo.FindProperty("statusText").objectReferenceValue = timerStatusTmp;
            tSo.FindProperty("exitAlertBanner").objectReferenceValue = alertBannerObj;
            tSo.FindProperty("exitAlertText").objectReferenceValue = alertTmp;
            tSo.ApplyModifiedProperties();

            // --- C. RageGaugeHUD (画面下部 HPバー同様の左から伸びるプログレスバー) ---
            var rageObj = CreateUIObject("RageGaugeHUD", canvasRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 75), new Vector2(680, 65));
            var rageBg = rageObj.AddComponent<Image>();
            rageBg.sprite = whiteSprite;
            rageBg.color = new Color(0.1f, 0.08f, 0.08f, 0.85f);
            var rageComp = rageObj.AddComponent<RageGaugeHUD>();

            // Fill (左から右へ水平に伸びる)
            var rageFillObj = CreateUIObject("Fill", rageObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-8, -8));
            var rageFillImg = rageFillObj.AddComponent<Image>();
            rageFillImg.sprite = whiteSprite;
            rageFillImg.color = new Color(1f, 0.45f, 0.1f, 1f);
            rageFillImg.type = Image.Type.Filled;
            rageFillImg.fillMethod = Image.FillMethod.Horizontal;
            rageFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            rageFillImg.fillAmount = 0f;

            // 覚醒エフェクトグループ
            var rageEffectObj = CreateUIObject("EffectGroup", rageObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rageEffectGroup = rageEffectObj.AddComponent<CanvasGroup>();
            var rageEffectImg = rageEffectObj.AddComponent<Image>();
            rageEffectImg.color = new Color(1f, 1f, 0.5f, 0.4f);

            // テキスト
            var rageTextObj = CreateUIObject("RageText", rageObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var rageTmp = rageTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) rageTmp.font = defaultFont;
            rageTmp.fontSize = 24;
            rageTmp.fontStyle = FontStyles.Bold;
            rageTmp.alignment = TextAlignmentOptions.Center;
            rageTmp.color = Color.white;

            var rageStatusObj = CreateUIObject("StatusLabel", rageObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 22), new Vector2(400, 32));
            var rageStatusTmp = rageStatusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) rageStatusTmp.font = defaultFont;
            rageStatusTmp.fontSize = 20;
            rageStatusTmp.alignment = TextAlignmentOptions.Center;
            rageStatusTmp.color = new Color(1f, 0.8f, 0.4f, 1f);

            rageComp.SetupReferences(rageFillImg, rageTmp, rageStatusTmp, rageEffectGroup);
            var rSo = new SerializedObject(rageComp);
            rSo.FindProperty("fillImage").objectReferenceValue = rageFillImg;
            rSo.FindProperty("rageText").objectReferenceValue = rageTmp;
            rSo.FindProperty("statusLabelText").objectReferenceValue = rageStatusTmp;
            rSo.FindProperty("awakeningEffectGroup").objectReferenceValue = rageEffectGroup;
            rSo.ApplyModifiedProperties();

            // --- D. SaleNotificationBanner (上部) ---
            var bannerObj = CreateUIObject("SaleNotificationBanner", canvasRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 220), new Vector2(650, 110));
            var bannerImg = bannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.12f, 0.15f, 0.28f, 0.95f);
            var bannerBtn = bannerObj.AddComponent<Button>();
            var bannerComp = bannerObj.AddComponent<SaleNotificationBanner>();

            var bannerTitleObj = CreateUIObject("Title", bannerObj.transform, new Vector2(0, 0.5f), new Vector2(0.72f, 1), new Vector2(20, 0), Vector2.zero);
            var bannerTitleTmp = bannerTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) bannerTitleTmp.font = defaultFont;
            bannerTitleTmp.fontSize = 22;
            bannerTitleTmp.fontStyle = FontStyles.Bold;
            bannerTitleTmp.color = new Color(1f, 0.85f, 0.2f, 1f);
            bannerTitleTmp.text = "⚡️ タイムセール開催中！";

            var bannerTimerObj = CreateUIObject("Timer", bannerObj.transform, new Vector2(0.72f, 0.5f), new Vector2(1, 1), new Vector2(-20, 0), Vector2.zero);
            var bannerTimerTmp = bannerTimerObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) bannerTimerTmp.font = defaultFont;
            bannerTimerTmp.fontSize = 18;
            bannerTimerTmp.alignment = TextAlignmentOptions.Right;
            bannerTimerTmp.color = Color.white;

            var bannerMsgObj = CreateUIObject("Message", bannerObj.transform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(20, 0), new Vector2(-40, 0));
            var bannerMsgTmp = bannerMsgObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) bannerMsgTmp.font = defaultFont;
            bannerMsgTmp.fontSize = 19;
            bannerMsgTmp.color = Color.white;
            bannerMsgTmp.text = "限定アイテム入荷！タップしてチェック ➔";

            bannerComp.SetupReferences((RectTransform)bannerObj.transform, bannerBtn, bannerTitleTmp, bannerMsgTmp, bannerTimerTmp);
            var bSo = new SerializedObject(bannerComp);
            bSo.FindProperty("bannerRoot").objectReferenceValue = (RectTransform)bannerObj.transform;
            bSo.FindProperty("bannerButton").objectReferenceValue = bannerBtn;
            bSo.FindProperty("titleText").objectReferenceValue = bannerTitleTmp;
            bSo.FindProperty("messageText").objectReferenceValue = bannerMsgTmp;
            bSo.FindProperty("timerText").objectReferenceValue = bannerTimerTmp;
            bSo.FindProperty("visiblePosition").vector2Value = new Vector2(0, -95);
            bSo.FindProperty("hiddenPosition").vector2Value = new Vector2(0, 220);
            bSo.ApplyModifiedProperties();

            // --- E. SmartphoneShopModal (中央) ---
            var modalObj = CreateUIObject("SmartphoneShopModal", canvasRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 840));
            var modalImg = modalObj.AddComponent<Image>();
            modalImg.color = new Color(0.08f, 0.09f, 0.14f, 0.98f);
            var shopModal = modalObj.AddComponent<SmartphoneShopModalView>();

            var shopHeaderObj = CreateUIObject("Header", modalObj.transform, new Vector2(0, 0.88f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var shopHeaderBg = shopHeaderObj.AddComponent<Image>();
            shopHeaderBg.color = new Color(0.15f, 0.18f, 0.3f, 1f);

            var shopTitleObj = CreateUIObject("Title", shopHeaderObj.transform, new Vector2(0.05f, 0), new Vector2(0.7f, 1), Vector2.zero, Vector2.zero);
            var shopTitleTmp = shopTitleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) shopTitleTmp.font = defaultFont;
            shopTitleTmp.fontSize = 26;
            shopTitleTmp.fontStyle = FontStyles.Bold;
            shopTitleTmp.text = "📱 ゲリラタイムセール ⚡️";
            shopTitleTmp.alignment = TextAlignmentOptions.Left;
            shopTitleTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

            var closeBtnObj = CreateUIObject("CloseBtn", shopHeaderObj.transform, new Vector2(0.86f, 0.12f), new Vector2(0.97f, 0.88f), Vector2.zero, Vector2.zero);
            var closeBtnImg = closeBtnObj.AddComponent<Image>();
            closeBtnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            var closeBtn = closeBtnObj.AddComponent<Button>();
            var closeBtnTextObj = CreateUIObject("Text", closeBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var closeBtnTmp = closeBtnTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) closeBtnTmp.font = defaultFont;
            closeBtnTmp.fontSize = 24;
            closeBtnTmp.text = "✕";
            closeBtnTmp.alignment = TextAlignmentOptions.Center;

            var userPtObj = CreateUIObject("UserPoints", modalObj.transform, new Vector2(0.05f, 0.8f), new Vector2(0.95f, 0.87f), Vector2.zero, Vector2.zero);
            var userPtTmp = userPtObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) userPtTmp.font = defaultFont;
            userPtTmp.fontSize = 24;
            userPtTmp.text = "所持ポイント: ¥0 pt";

            var cards = new ShopItemCardUI[3];
            float cardHeight = 180f;
            float startY = 440f;

            for (int i = 0; i < 3; i++)
            {
                var cardObj = CreateUIObject($"Card_{i}", modalObj.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.5f), new Vector2(0, startY - (i * 205f)), new Vector2(0, cardHeight));
                var cardBg = cardObj.AddComponent<Image>();
                cardBg.color = new Color(0.14f, 0.16f, 0.24f, 1f);

                var iconObj = CreateUIObject("Icon", cardObj.transform, new Vector2(0.02f, 0.5f), new Vector2(0.02f, 0.5f), new Vector2(40, 0), new Vector2(65, 65));
                var iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) iconTmp.font = defaultFont;
                iconTmp.fontSize = 44;
                iconTmp.alignment = TextAlignmentOptions.Center;

                var nameObj = CreateUIObject("Name", cardObj.transform, new Vector2(0.18f, 0.6f), new Vector2(0.68f, 0.95f), Vector2.zero, Vector2.zero);
                var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) nameTmp.font = defaultFont;
                nameTmp.fontSize = 22;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.color = Color.white;

                var descObj = CreateUIObject("Desc", cardObj.transform, new Vector2(0.18f, 0.05f), new Vector2(0.68f, 0.6f), Vector2.zero, Vector2.zero);
                var descTmp = descObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) descTmp.font = defaultFont;
                descTmp.fontSize = 16;
                descTmp.color = new Color(0.8f, 0.85f, 0.9f, 0.9f);

                var priceObj = CreateUIObject("Price", cardObj.transform, new Vector2(0.7f, 0.55f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero);
                var priceTmp = priceObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) priceTmp.font = defaultFont;
                priceTmp.fontSize = 20;
                priceTmp.fontStyle = FontStyles.Bold;
                priceTmp.alignment = TextAlignmentOptions.Right;
                priceTmp.color = new Color(1f, 0.85f, 0.2f, 1f);

                var buyBtnObj = CreateUIObject("BuyBtn", cardObj.transform, new Vector2(0.7f, 0.1f), new Vector2(0.98f, 0.52f), Vector2.zero, Vector2.zero);
                var buyBtnImg = buyBtnObj.AddComponent<Image>();
                buyBtnImg.color = new Color(0.15f, 0.65f, 0.95f, 1f);
                var buyBtn = buyBtnObj.AddComponent<Button>();

                var buyBtnTextObj = CreateUIObject("Text", buyBtnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var buyBtnTmp = buyBtnTextObj.AddComponent<TextMeshProUGUI>();
                if (defaultFont != null) buyBtnTmp.font = defaultFont;
                buyBtnTmp.fontSize = 18;
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
            var mSo = new SerializedObject(shopModal);
            mSo.FindProperty("modalRoot").objectReferenceValue = modalObj;
            mSo.FindProperty("userPointsText").objectReferenceValue = userPtTmp;
            mSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
            mSo.ApplyModifiedProperties();

            // --- F. VirtualJoystick (フローティングタッチスティック) ---
            var existingJoystick = canvasRoot.Find("VirtualJoystick");
            if (existingJoystick != null)
            {
                Object.DestroyImmediate(existingJoystick.gameObject);
            }

            var joystickRootObj = CreateUIObject("VirtualJoystick", canvasRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var touchZoneImg = joystickRootObj.AddComponent<Image>();
            touchZoneImg.color = Color.clear;
            touchZoneImg.raycastTarget = true;
            var joystickView = joystickRootObj.AddComponent<VirtualJoystickView>();

            var containerObj = CreateUIObject("JoystickContainer", joystickRootObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            var containerGroup = containerObj.AddComponent<CanvasGroup>();
            containerGroup.alpha = 0f;

            var bgObj = CreateUIObject("JoystickBackground", containerObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = ringBgSprite;
            bgImg.color = new Color(1f, 1f, 1f, 0.85f);
            bgImg.raycastTarget = false;

            var handleObj = CreateUIObject("JoystickHandle", containerObj.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(65, 65));
            var handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = knobSprite;
            handleImg.color = new Color(1f, 1f, 1f, 0.95f);
            handleImg.raycastTarget = false;

            joystickView.SetupReferences((RectTransform)containerObj.transform, bgImg, handleImg, touchZoneImg);

            var jSo = new SerializedObject(joystickView);
            jSo.FindProperty("joystickMode").enumValueIndex = (int)JoystickMode.Floating;
            jSo.FindProperty("movementRange").floatValue = 50f;
            jSo.FindProperty("containerRect").objectReferenceValue = (RectTransform)containerObj.transform;
            jSo.FindProperty("backgroundImage").objectReferenceValue = bgImg;
            jSo.FindProperty("handleImage").objectReferenceValue = handleImg;
            jSo.FindProperty("touchZoneImage").objectReferenceValue = touchZoneImg;
            jSo.ApplyModifiedProperties();

            // VirtualJoystick は TouchZone を持つため、他のボタンUI（ショップ・バナー等）の邪魔にならないよう一番手前（描画最背面）に配置
            joystickRootObj.transform.SetAsFirstSibling();

            // --- G. GameHUDView の SerializedField をバインド ---
            hudSo.FindProperty("pointStepHUD").objectReferenceValue = pointStepComp;
            hudSo.FindProperty("rageGaugeHUD").objectReferenceValue = rageComp;
            hudSo.FindProperty("escapeTimerHUD").objectReferenceValue = timerComp;
            hudSo.FindProperty("saleNotificationBanner").objectReferenceValue = bannerComp;
            hudSo.FindProperty("shopModalView").objectReferenceValue = shopModal;
            hudSo.FindProperty("virtualJoystickView").objectReferenceValue = joystickView;
            hudSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(joystickView);
            EditorUtility.SetDirty(joystickRootObj);
            EditorUtility.SetDirty(targetCanvas.gameObject);
            EditorSceneManager.MarkSceneDirty(targetCanvas.gameObject.scene);
            EditorSceneManager.SaveScene(targetCanvas.gameObject.scene);

            Debug.Log("[GameCanvasSetupEditor] GameCanvas への HUD & VirtualJoystick オブジェクト配置・設定・保存が完了しました！");
        }

        private static void EnsureSpriteImporter(string assetPath)
        {
            if (!System.IO.File.Exists(assetPath)) return;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || !importer.alphaIsTransparency))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
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
    }
}
#endif

