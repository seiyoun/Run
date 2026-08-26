/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Game シーン用のデバッグ情報表示・テスト操作用 UI コンポーネント。
 *                SANDBOX またはエディタ実行時のみコード駆動でプロシージャル生成され、アセット(Resources/Prefab)を残しません。
 */

#if SANDBOX || UNITY_EDITOR
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// ゲーム実行中にプレイヤーのステータス・入力・アニメーション状態をリアルタイム表示し、
    /// デバッグ操作（攻撃テスト、被弾、回復テスト）を提供する UI クラス。
    /// C# コードから完全動的（プロシージャル）に Canvas ごと生成されるため、ROM 作成時に余計なアセットを含めません。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameDebugHUD : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private float updateInterval = 0.1f;

        private GameObject debugPanel;
        private TextMeshProUGUI statusText;
        private Button toggleButton;
        private Button attackButton;
        private Button damageButton;
        private Button healButton;

        private float nextUpdateTime;
        private float fpsTimer;
        private int frameCount;
        private float currentFps;

        /// <summary>
        /// コードから動的に DebugCanvas および全 UI をプロシージャル生成する。
        /// </summary>
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

        private void BuildUI(Transform root)
        {
            var defaultFont = TMP_Settings.defaultFontAsset;

            // 1. 開閉トグルボタン（右上）
            var toggleObj = CreateUIObject("ToggleButton", root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -45), new Vector2(120, 50));
            var toggleImg = toggleObj.AddComponent<Image>();
            toggleImg.color = new Color(0.2f, 0.2f, 0.35f, 0.85f);
            toggleButton = toggleObj.AddComponent<Button>();
            toggleButton.onClick.AddListener(TogglePanel);

            var toggleTextObj = CreateUIObject("Text", toggleObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var toggleTMP = toggleTextObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) toggleTMP.font = defaultFont;
            toggleTMP.text = "Debug UI";
            toggleTMP.fontSize = 18;
            toggleTMP.fontStyle = FontStyles.Bold;
            toggleTMP.alignment = TextAlignmentOptions.Center;
            toggleTMP.color = Color.white;

            // 2. メインパネル（左上）
            debugPanel = CreateUIObject("DebugPanel", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(240, -200), new Vector2(440, 340));
            var panelImg = debugPanel.AddComponent<Image>();
            panelImg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            // 3. ステータステキスト
            var statusObj = CreateUIObject("StatusText", debugPanel.transform, new Vector2(0, 0.25f), Vector2.one, Vector2.zero, new Vector2(-30, -20));
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) statusText.font = defaultFont;
            statusText.text = "[DEBUG HUD] Initializing...";
            statusText.fontSize = 20;
            statusText.alignment = TextAlignmentOptions.TopLeft;
            statusText.color = Color.white;

            // 4. アクションボタン群（Attack / Damage / Heal）
            // Attack ボタン（デバッグ用）
            var attackObj = CreateButton("AttackButton", debugPanel.transform, new Vector2(-130, 40), new Vector2(110, 50), new Color(0.85f, 0.25f, 0.25f, 1f), "Attack", defaultFont);
            attackButton = attackObj.GetComponent<Button>();
            attackButton.onClick.AddListener(OnAttackClicked);

            // Damage ボタン (-10 HP)
            var damageObj = CreateButton("DamageButton", debugPanel.transform, new Vector2(0, 40), new Vector2(110, 50), new Color(0.9f, 0.55f, 0.15f, 1f), "-10 HP", defaultFont);
            damageButton = damageObj.GetComponent<Button>();
            damageButton.onClick.AddListener(OnDamageClicked);

            // Heal ボタン (+20 HP)
            var healObj = CreateButton("HealButton", debugPanel.transform, new Vector2(130, 40), new Vector2(110, 50), new Color(0.2f, 0.75f, 0.35f, 1f), "+20 HP", defaultFont);
            healButton = healObj.GetComponent<Button>();
            healButton.onClick.AddListener(OnHealClicked);

            debugPanel.SetActive(showOnStart);
        }

        private GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = 5; // UI Layer
            obj.transform.SetParent(parent, false);
            var rt = (RectTransform)obj.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            rt.pivot = new Vector2(0.5f, 0.5f);
            return obj;
        }

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
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }

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

            var animStateText = animator != null 
                ? $"<color=#00D4FF>{animator.CurrentState}</color>" 
                : "None";

            statusText.text = 
                $"<color=#FFFF00><b>[GAME DEBUG HUD]</b></color>\n" +
                $"FPS: <color=#00FF88>{currentFps:F1}</color> | Time: {Time.time:F1}s\n" +
                $"------------------------\n" +
                $"<b>Position:</b> ({pos.x:F2}, {pos.y:F2})\n" +
                $"<b>Input:</b> ({input.x:F2}, {input.y:F2}) | Speed: {speed:F1}\n" +
                $"<b>HP:</b> {hpText}\n" +
                $"<b>Anim State:</b> {animStateText}";
        }

        public void TogglePanel()
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(!debugPanel.activeSelf);
            }
        }

        private void OnAttackClicked()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.Attack();
            }
        }

        private void OnDamageClicked()
        {
            var player = PlayerController.Instance;
            if (player != null && player.Status != null)
            {
                player.Status.TakeDamage(10);
            }
        }

        private void OnHealClicked()
        {
            var player = PlayerController.Instance;
            if (player != null && player.Status != null)
            {
                player.Status.Heal(20);
            }
        }
    }
}
#endif
