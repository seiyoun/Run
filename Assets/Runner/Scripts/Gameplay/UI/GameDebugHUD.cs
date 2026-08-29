/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game シーン用のデバッグ情報表示・テスト操作用 UI コンポーネント。
 *                SANDBOX またはエディタ実行時のみコード駆動でプロシージャル生成され、アセットを残しません。
 *                ウィンドウサイズの大型化、無彩色（モノトーン）ボタン、見やすいステータス表示を提供します。
 */

#if SANDBOX || UNITY_EDITOR
using System;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// ゲーム実行中にプレイヤーのステータス・入力・アニメーション状態・ポイ活をリアルタイム表示し、
    /// デバッグ操作を提供する大型 UI クラス。
    /// C# コードから動的に Canvas ごと生成され、無彩色の見やすいボタンレイアウトを備えます。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameDebugHUD : MonoBehaviour
    {
        private const string MoneyItemAddress = "MoneyItem";
        private static readonly Color NormalButtonColor = new Color(0.2f, 0.2f, 0.24f, 1f);
        private static readonly Color ActiveButtonColor = new Color(0.35f, 0.35f, 0.42f, 1f);
        private static readonly Color PanelBackgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.96f);
        private static readonly Color InfoBoxBackgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.85f);

        [Header("Settings")]
        [Tooltip("開始時にデバッグパネルを表示するかどうか")]
        [SerializeField] private bool showOnStart = false;

        [Tooltip("ステータス情報の更新間隔（秒）")]
        [SerializeField] private float updateInterval = 0.1f;

        private GameObject debugPanel;
        private TextMeshProUGUI statusText;
        private Button toggleButton;
        private Button attackButton;
        private Button damageButton;
        private Button healButton;
        private Button pointButton;
        private TextMeshProUGUI magnetRangeBtnText;
        private Image magnetRangeBtnImage;

        private float nextUpdateTime;
        private float fpsTimer;
        private int frameCount;
        private float currentFps;
        private AddressablePrefabLoader addressableLoader;

        /// <summary>
        /// AddressablePrefabLoader のインスタンスを初期化する。
        /// </summary>
        private void Awake()
        {
            addressableLoader = new AddressablePrefabLoader();
        }

        /// <summary>
        /// ショートカットキー監視、FPS 計測、およびステータステキストの定期更新を行う。
        /// </summary>
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame || keyboard.backquoteKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
                {
                    TogglePanel();
                }
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count >= 3)
            {
                if (touchscreen.touches[0].press.wasPressedThisFrame)
                {
                    TogglePanel();
                }
            }

            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                currentFps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }

            if (Time.unscaledTime >= nextUpdateTime)
            {
                nextUpdateTime = Time.unscaledTime + updateInterval;
                UpdateDebugInfo();
            }
        }

        /// <summary>
        /// オブジェクト破棄時に AddressablePrefabLoader を Dispose してリソースを解放する。
        /// </summary>
        private void OnDestroy()
        {
            if (addressableLoader != null)
            {
                addressableLoader.Dispose();
                addressableLoader = null;
            }
        }

        /// <summary>
        /// コードから動的に DebugCanvas および全 UI をプロシージャル生成する。
        /// </summary>
        /// <returns>生成された GameDebugHUD インスタンス</returns>
        public static GameDebugHUD Create()
        {
            var canvasObj = new GameObject("DebugCanvas");

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            var hud = canvasObj.AddComponent<GameDebugHUD>();

            hud.BuildUI(canvasObj.transform);
            return hud;
        }

        /// <summary>
        /// デバッグパネルの表示・非表示をトグル切り替えする。
        /// </summary>
        public void TogglePanel()
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
            }
        }

        /// <summary>
        /// デバッグ UI の階層構造および全ボタン群を動的に構築する。
        /// </summary>
        /// <param name="root">Canvas の Transform</param>
        private void BuildUI(Transform root)
        {
            var defaultFont = TMP_Settings.defaultFontAsset;

            var toggleObj = CreateUIObject("ToggleButton", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-120, 80), new Vector2(200, 75));
            var toggleImg = toggleObj.AddComponent<Image>();
            toggleImg.color = NormalButtonColor;
            toggleButton = toggleObj.AddComponent<Button>();
            toggleButton.onClick.AddListener(TogglePanel);

            var toggleTextObj = CreateUIObject("Text", toggleObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var toggleTMP = toggleTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) toggleTMP.font = defaultFont;
            toggleTMP.text = "Debug UI";
            toggleTMP.fontSize = 26;
            toggleTMP.fontStyle = FontStyles.Bold;
            toggleTMP.alignment = TextAlignmentOptions.Center;
            toggleTMP.color = Color.white;

            debugPanel = CreateUIObject("DebugPanel", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 1450));
            var panelImg = debugPanel.AddComponent<Image>();
            panelImg.color = PanelBackgroundColor;

            var headerObj = CreateUIObject("Header", debugPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(-40, 80));
            var titleObj = CreateUIObject("TitleText", headerObj.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-120, 0));
            var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) titleTMP.font = defaultFont;
            titleTMP.text = "<b>GAME DEBUG CONSOLE</b>";
            titleTMP.fontSize = 32;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            titleTMP.color = Color.white;
            titleTMP.raycastTarget = false;

            var closeBtnObj = CreateButton("CloseButton", headerObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50, 0), new Vector2(75, 75), NormalButtonColor, "×", defaultFont, 36);
            var closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(TogglePanel);

            var infoBoxObj = CreateUIObject("InfoBox", debugPanel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -420), new Vector2(-40, 640));
            var infoBoxImg = infoBoxObj.AddComponent<Image>();
            infoBoxImg.color = InfoBoxBackgroundColor;
            infoBoxImg.raycastTarget = false;

            var statusObj = CreateUIObject("StatusText", infoBoxObj.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30, -30));
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) statusText.font = defaultFont;
            statusText.text = "[DEBUG HUD] Initializing...";
            statusText.fontSize = 28;
            statusText.lineSpacing = 15;
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.color = Color.white;
            statusText.raycastTarget = false;

            BuildButtonGroup(debugPanel.transform, defaultFont);

            headerObj.transform.SetAsLastSibling();

            debugPanel.SetActive(showOnStart);
        }

        /// <summary>
        /// デバッグパネル下部の全操作ボタン群を無彩色グリッドで配置・構築する。
        /// </summary>
        /// <param name="parent">パネルの Transform</param>
        /// <param name="font">使用するフォントアセット</param>
        private void BuildButtonGroup(Transform parent, TMP_FontAsset font)
        {
            var attackObj = CreateButton("AttackButton", parent, new Vector2(-345, 270), new Vector2(215, 75), NormalButtonColor, "攻撃", font, 26);
            attackButton = attackObj.GetComponent<Button>();
            attackButton.onClick.AddListener(OnAttackClicked);

            var damageObj = CreateButton("DamageButton", parent, new Vector2(-115, 270), new Vector2(215, 75), NormalButtonColor, "-10 HP", font, 26);
            damageButton = damageObj.GetComponent<Button>();
            damageButton.onClick.AddListener(OnDamageClicked);

            var healObj = CreateButton("HealButton", parent, new Vector2(115, 270), new Vector2(215, 75), NormalButtonColor, "+20 HP", font, 26);
            healButton = healObj.GetComponent<Button>();
            healButton.onClick.AddListener(OnHealClicked);

            var pointObj = CreateButton("PointButton", parent, new Vector2(345, 270), new Vector2(215, 75), NormalButtonColor, "+500 pt", font, 26);
            pointButton = pointObj.GetComponent<Button>();
            pointButton.onClick.AddListener(OnAddPointClicked);

            var spawnItemObj = CreateButton("SpawnItemButton", parent, new Vector2(-345, 175), new Vector2(215, 75), NormalButtonColor, "コインx5", font, 26);
            var spawnItemBtn = spawnItemObj.GetComponent<Button>();
            spawnItemBtn.onClick.AddListener(OnSpawnMoneyItemsClicked);

            var rangeObj = CreateButton("MagnetRangeButton", parent, new Vector2(-115, 175), new Vector2(215, 75), NormalButtonColor, "吸込範囲: OFF", font, 24);
            magnetRangeBtnImage = rangeObj.GetComponent<Image>();
            magnetRangeBtnText = rangeObj.GetComponentInChildren<TextMeshProUGUI>();
            var rangeBtn = rangeObj.GetComponent<Button>();
            rangeBtn.onClick.AddListener(OnToggleMagnetRangeClicked);

            var saleObj = CreateButton("SaleButton", parent, new Vector2(115, 175), new Vector2(215, 75), NormalButtonColor, "セール発火", font, 26);
            var saleBtn = saleObj.GetComponent<Button>();
            saleBtn.onClick.AddListener(OnTriggerSaleClicked);

            var rageObj = CreateButton("RageButton", parent, new Vector2(345, 175), new Vector2(215, 75), NormalButtonColor, "怒り覚醒", font, 26);
            var rageBtn = rageObj.GetComponent<Button>();
            rageBtn.onClick.AddListener(OnTriggerAwakeningClicked);

            var dodgeObj = CreateButton("DodgeButton", parent, new Vector2(-230, 80), new Vector2(445, 75), NormalButtonColor, "ジャスト回避 演出", font, 26);
            var dodgeBtn = dodgeObj.GetComponent<Button>();
            dodgeBtn.onClick.AddListener(OnJustDodgeClicked);

            var exitObj = CreateButton("ExitButton", parent, new Vector2(230, 80), new Vector2(445, 75), NormalButtonColor, "非常口 即時開放", font, 26);
            var exitBtn = exitObj.GetComponent<Button>();
            exitBtn.onClick.AddListener(OnOpenExitClicked);
        }

        /// <summary>
        /// RectTransform を持つ UI GameObject を作成する。
        /// </summary>
        private GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
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

        /// <summary>
        /// デバッグ用ボタンスタイルの UI オブジェクトを作成する。
        /// </summary>
        private GameObject CreateButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color, string label, TMP_FontAsset font, float fontSize = 26f)
        {
            var btnObj = CreateUIObject(name, parent, anchorMin, anchorMax, pos, size);
            var img = btnObj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            btnObj.AddComponent<Button>();

            var textObj = CreateUIObject("Text", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btnObj;
        }

        /// <summary>
        /// デバッグパネル下部（Bottom-Center アンカー）用のボタンスタイル UI オブジェクトを作成する。
        /// </summary>
        private GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, Color color, string label, TMP_FontAsset font, float fontSize = 26f)
        {
            return CreateButton(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size, color, label, font, fontSize);
        }

        /// <summary>
        /// プレイヤー情報・FPS・HUD ステータスを最新情報に更新して描画する。
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (statusText == null) return;

            var player = PlayerController.Instance;
            if (player == null)
            {
                statusText.text = $"<b>FPS:</b> {currentFps:F1}\n\n<color=#FF7777>プレイヤーが生成されていません。</color>";
                return;
            }

            var pos = player.transform.position;
            var input = player.MoveInput;
            var speed = player.MoveSpeed;
            var status = player.Status;
            var animator = player.CharacterAnimator;

            var hpText = status != null 
                ? $"{status.CurrentHp} / {status.MaxHp} (Dead: {status.IsDead})" 
                : "N/A";

            var hud = GameHUDView.Instance;
            var pointInfoText = hud != null && hud.PointStepHUD != null 
                ? $"¥{hud.PointStepHUD.CurrentPoint:N0} pt  |  {hud.PointStepHUD.CurrentSteps} 歩" 
                : "N/A";

            var rageInfoText = hud != null && hud.RageGaugeHUD != null 
                ? $"{hud.RageGaugeHUD.CurrentRage:F0}%  (Awakened: {hud.RageGaugeHUD.IsAwakened})" 
                : "N/A";

            var animStateText = animator != null 
                ? $"{animator.CurrentState}" 
                : "None";

            var magnetVisible = PlayerDebugRangeVisualizer.IsRangeVisible(player.transform);
            var magnetInfo = $"{player.MagnetRadius:F1}m  (表示: {(magnetVisible ? "ON" : "OFF")})";

            statusText.text = 
                $"<b>[System]</b>  FPS: <b>{currentFps:F1}</b>  |  Time: <b>{Time.time:F1}s</b>\n" +
                $"────────────────────────────────────\n" +
                $"<b>座標:</b> ({pos.x:F2}, {pos.y:F2})    <b>入力:</b> ({input.x:F2}, {input.y:F2})    <b>速度:</b> {speed:F1}\n" +
                $"<b>体力 (HP):</b> {hpText}\n" +
                $"<b>ポイ活:</b> {pointInfoText}\n" +
                $"<b>怒りゲージ:</b> {rageInfoText}\n" +
                $"<b>アイテム吸引:</b> {magnetInfo}\n" +
                $"<b>アニメーション:</b> {animStateText}";
        }

        /// <summary>
        /// 攻撃ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnAttackClicked()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.Attack();
            }
        }

        /// <summary>
        /// ダメージボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnDamageClicked()
        {
            var player = PlayerController.Instance;
            if (player != null && player.Status != null)
            {
                player.Status.TakeDamage(10);
            }
        }

        /// <summary>
        /// 回復ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnHealClicked()
        {
            var player = PlayerController.Instance;
            if (player != null && player.Status != null)
            {
                player.Status.Heal(20);
            }
        }

        /// <summary>
        /// ポイント加算ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnAddPointClicked()
        {
            if (GameHUDView.Instance != null && GameHUDView.Instance.PointStepHUD != null)
            {
                GameHUDView.Instance.PointStepHUD.AddPoints(500);
            }
        }

        /// <summary>
        /// アイテム吸引範囲の GameView 表示トグルボタンクリック時の処理。
        /// </summary>
        private void OnToggleMagnetRangeClicked()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            bool nextState = PlayerDebugRangeVisualizer.ToggleRangeVisible(player.transform, player.MagnetRadius);

            if (magnetRangeBtnText != null)
            {
                magnetRangeBtnText.text = nextState ? "吸込範囲: ON" : "吸込範囲: OFF";
            }

            if (magnetRangeBtnImage != null)
            {
                magnetRangeBtnImage.color = nextState ? ActiveButtonColor : NormalButtonColor;
            }

            DebugLogger.Log($"[GameDebugHUD] デバッグ操作: アイテム吸引範囲の表示を {(nextState ? "ON" : "OFF")} に切り替えました。");
        }

        /// <summary>
        /// 非常口即時開放ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnOpenExitClicked()
        {
            if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
            {
                GameHUDView.Instance.EscapeTimerHUD.UnlockExit();
                DebugLogger.Log("[GameDebugHUD] デバッグ操作: 非常口を即時開放しました。");
            }
        }

        /// <summary>
        /// タイムセール通知発火ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnTriggerSaleClicked()
        {
            if (GameHUDView.Instance != null)
            {
                GameHUDView.Instance.TriggerSaleNotification();
                DebugLogger.Log("[GameDebugHUD] デバッグ操作: タイムセール通知を発火しました。");
            }
        }

        /// <summary>
        /// 怒りMAX覚醒発動ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnTriggerAwakeningClicked()
        {
            if (GameHUDView.Instance != null && GameHUDView.Instance.RageGaugeHUD != null)
            {
                GameHUDView.Instance.RageGaugeHUD.SetRage(100f, 100f, true);
                GameHUDView.Instance.RageGaugeHUD.TriggerAwakening(10f);
                DebugLogger.Log("[GameDebugHUD] デバッグ操作: 怒りMAX・覚醒モードを発動しました(10秒)。");
            }
        }

        /// <summary>
        /// プレイヤー周辺へのコイン生成ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private async void OnSpawnMoneyItemsClicked()
        {
            var player = PlayerController.Instance;
            if (player == null)
            {
                DebugLogger.Log("[GameDebugHUD] プレイヤーが存在しないためアイテムを生成できません。");
                return;
            }

            await ItemDebugSpawner.SpawnMoneyItemsAroundAsync(player.transform.position, 5);
        }

        /// <summary>
        /// ジャスト回避演出発動ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnJustDodgeClicked()
        {
            if (GameHUDView.Instance != null)
            {
                GameHUDView.Instance.OnJustDodge();
                DebugLogger.Log("[GameDebugHUD] デバッグ操作: ジャスト回避演出をトリガーしました。");
            }
        }
    }
}
#endif
