/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game シーン用のデバッグ情報表示・テスト操作用 UI コンポーネント。
 *                SANDBOX またはエディタ実行時のみコード駆動でプロシージャル生成され、アセット(Resources/Prefab)を残しません。
 *                アイテム吸引範囲の GameView 表示トグルボタン、コイン生成、各アクションテストを完備します。
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
    /// デバッグ操作（攻撃、被弾、回復、コイン生成、吸引範囲表示トグル、覚醒、セール、出口開放テスト）を提供する UI クラス。
    /// C# コードから完全動的（プロシージャル）に Canvas ごと生成されます。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameDebugHUD : MonoBehaviour
    {
        private const string MoneyItemAddress = "MoneyItem";
        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;
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
            // ショートカットキー（F1 / Backquote / Tab）でのパネル開閉
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f1Key.wasPressedThisFrame || keyboard.backquoteKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame)
                {
                    TogglePanel();
                }
            }

            // モバイル用 3本指タップでのパネル開閉
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count >= 3)
            {
                if (touchscreen.touches[0].press.wasPressedThisFrame)
                {
                    TogglePanel();
                }
            }

            // FPS 計測
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                currentFps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }

            // 情報テキストの定期更新
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

            // 1. 開閉トグルボタン（右下）
            var toggleObj = CreateUIObject("ToggleButton", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-95, 55), new Vector2(160, 60));
            var toggleImg = toggleObj.AddComponent<Image>();
            toggleImg.color = new Color(0.2f, 0.2f, 0.35f, 0.9f);
            toggleButton = toggleObj.AddComponent<Button>();
            toggleButton.onClick.AddListener(TogglePanel);

            var toggleTextObj = CreateUIObject("Text", toggleObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var toggleTMP = toggleTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) toggleTMP.font = defaultFont;
            toggleTMP.text = "🔧 Debug UI";
            toggleTMP.fontSize = 22;
            toggleTMP.fontStyle = FontStyles.Bold;
            toggleTMP.alignment = TextAlignmentOptions.Center;
            toggleTMP.color = Color.white;

            // 2. メインパネル（右下トグルボタンの上）
            debugPanel = CreateUIObject("DebugPanel", root, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-335, 440), new Vector2(650, 720));
            var panelImg = debugPanel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

            // 3. ステータステキスト
            var statusObj = CreateUIObject("StatusText", debugPanel.transform, new Vector2(0, 0.35f), Vector2.one, Vector2.zero, new Vector2(-30, -20));
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) statusText.font = defaultFont;
            statusText.text = "[DEBUG HUD] Initializing...";
            statusText.fontSize = 24;
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.color = Color.white;

            // 4. アクションボタン群（3段レイアウト）
            // 1段目: プレイヤー基本操作 (Y: 160)
            var attackObj = CreateButton("AttackButton", debugPanel.transform, new Vector2(-230, 160), new Vector2(140, 55), new Color(0.85f, 0.25f, 0.25f, 1f), "Attack", defaultFont);
            attackButton = attackObj.GetComponent<Button>();
            attackButton.onClick.AddListener(OnAttackClicked);

            var damageObj = CreateButton("DamageButton", debugPanel.transform, new Vector2(-78, 160), new Vector2(140, 55), new Color(0.9f, 0.55f, 0.15f, 1f), "-10 HP", defaultFont);
            damageButton = damageObj.GetComponent<Button>();
            damageButton.onClick.AddListener(OnDamageClicked);

            var healObj = CreateButton("HealButton", debugPanel.transform, new Vector2(78, 160), new Vector2(140, 55), new Color(0.2f, 0.75f, 0.35f, 1f), "+20 HP", defaultFont);
            healButton = healObj.GetComponent<Button>();
            healButton.onClick.AddListener(OnHealClicked);

            var pointObj = CreateButton("PointButton", debugPanel.transform, new Vector2(230, 160), new Vector2(140, 55), new Color(0.15f, 0.65f, 0.95f, 1f), "+500 pt", defaultFont);
            var pointBtn = pointObj.GetComponent<Button>();
            pointBtn.onClick.AddListener(OnAddPointClicked);

            // 2段目: アイテム生成 & 範囲表示 & イベント (Y: 95)
            var spawnItemObj = CreateButton("SpawnItemButton", debugPanel.transform, new Vector2(-230, 95), new Vector2(140, 55), new Color(1f, 0.75f, 0.1f, 1f), "🪙 コインx5", defaultFont);
            var spawnItemBtn = spawnItemObj.GetComponent<Button>();
            spawnItemBtn.onClick.AddListener(OnSpawnMoneyItemsClicked);

            var rangeObj = CreateButton("MagnetRangeButton", debugPanel.transform, new Vector2(-78, 95), new Vector2(140, 55), new Color(0.1f, 0.65f, 0.85f, 1f), "🧲 範囲表示", defaultFont);
            magnetRangeBtnImage = rangeObj.GetComponent<Image>();
            magnetRangeBtnText = rangeObj.GetComponentInChildren<TextMeshProUGUI>();
            var rangeBtn = rangeObj.GetComponent<Button>();
            rangeBtn.onClick.AddListener(OnToggleMagnetRangeClicked);

            var saleObj = CreateButton("SaleButton", debugPanel.transform, new Vector2(78, 95), new Vector2(140, 55), new Color(0.95f, 0.7f, 0.1f, 1f), "⚡ セール", defaultFont);
            var saleBtn = saleObj.GetComponent<Button>();
            saleBtn.onClick.AddListener(OnTriggerSaleClicked);

            var rageObj = CreateButton("RageButton", debugPanel.transform, new Vector2(230, 95), new Vector2(140, 55), new Color(1f, 0.25f, 0.15f, 1f), "🔥 覚醒", defaultFont);
            var rageBtn = rageObj.GetComponent<Button>();
            rageBtn.onClick.AddListener(OnTriggerAwakeningClicked);

            // 3段目: 回避 & 非常口即時開放 (Y: 30)
            var dodgeObj = CreateButton("DodgeButton", debugPanel.transform, new Vector2(-150, 30), new Vector2(180, 55), new Color(0.7f, 0.3f, 0.9f, 1f), "✨ ジャスト回避", defaultFont);
            var dodgeBtn = dodgeObj.GetComponent<Button>();
            dodgeBtn.onClick.AddListener(OnJustDodgeClicked);

            var exitObj = CreateButton("ExitButton", debugPanel.transform, new Vector2(120, 30), new Vector2(240, 55), new Color(0.1f, 0.8f, 0.4f, 1f), "🚪 出口即時開放", defaultFont);
            var exitBtn = exitObj.GetComponent<Button>();
            exitBtn.onClick.AddListener(OnOpenExitClicked);

            debugPanel.SetActive(showOnStart);
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
        private GameObject CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, Color color, string label, TMP_FontAsset font)
        {
            var btnObj = CreateUIObject(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            var img = btnObj.AddComponent<Image>();
            img.color = color;
            btnObj.AddComponent<Button>();

            var textObj = CreateUIObject("Text", btnObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
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
                statusText.text = $"<color=#FFFF00>[DEBUG HUD]</color>\nFPS: {currentFps:F1}\n\n<color=#FF6666>Player: Not Spawned</color>";
                return;
            }

            var pos = player.transform.position;
            var input = player.MoveInput;
            var speed = player.MoveSpeed;
            var status = player.Status;
            var animator = player.CharacterAnimator;

            var hpText = status != null 
                ? $"<color=#00FF88>{status.CurrentHp}</color> / {status.MaxHp} (Dead: {status.IsDead})" 
                : "N/A";

            var hud = GameHUDView.Instance;
            var pointInfoText = hud != null && hud.PointStepHUD != null 
                ? $"¥<color=#FFD700>{hud.PointStepHUD.CurrentPoint:N0}</color> pt | <color=#00D4FF>{hud.PointStepHUD.CurrentSteps}</color> 歩" 
                : "N/A";

            var rageInfoText = hud != null && hud.RageGaugeHUD != null 
                ? $"<color=#FF5522>{hud.RageGaugeHUD.CurrentRage:F0}</color>% (Awake: {hud.RageGaugeHUD.IsAwakened})" 
                : "N/A";

            var animStateText = animator != null 
                ? $"<color=#00D4FF>{animator.CurrentState}</color>" 
                : "None";

            var magnetVisible = PlayerDebugRangeVisualizer.IsRangeVisible(player.transform);
            var magnetInfo = $"<b>吸込範囲:</b> <color=#00D4FF>{player.MagnetRadius:F1}m</color> (表示: {(magnetVisible ? "ON" : "OFF")})";

            statusText.text = 
                $"<color=#FFFF00><b>[GAME DEBUG HUD]</b></color>\n" +
                $"FPS: <color=#00FF88>{currentFps:F1}</color> | Time: {Time.time:F1}s\n" +
                $"------------------------\n" +
                $"<b>Position:</b> ({pos.x:F2}, {pos.y:F2})\n" +
                $"<b>Input:</b> ({input.x:F2}, {input.y:F2}) | Speed: {speed:F1}\n" +
                $"<b>HP:</b> {hpText}\n" +
                $"<b>ポイ活:</b> {pointInfoText}\n" +
                $"<b>怒り:</b> {rageInfoText}\n" +
                $"{magnetInfo}\n" +
                $"<b>Anim:</b> {animStateText}";
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
                magnetRangeBtnText.text = nextState ? "🧲 範囲非表示" : "🧲 範囲表示";
            }

            if (magnetRangeBtnImage != null)
            {
                magnetRangeBtnImage.color = nextState 
                    ? new Color(0.1f, 0.85f, 0.65f, 1f) 
                    : new Color(0.1f, 0.65f, 0.85f, 1f);
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
