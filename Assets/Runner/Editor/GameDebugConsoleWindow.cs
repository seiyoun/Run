/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中のステータス監視および各種デバッグ操作を Unity Editor 上から直接実行するためのエディタ拡張ウィンドウ。
 */

using System;
using System.Threading;
using Shiyuan.Foundation.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Runner.Editor
{
    /// <summary>
    /// ゲーム実行中にプレイヤーのステータス情報（FPS、HP、怒りゲージ、座標等）をリアルタイムに表示し、
    /// 各種デバッグ操作（攻撃、ダメージ、回復、アイテム生成、敵スポーン等）を実行するエディタウィンドウ。
    /// </summary>
    public sealed class GameDebugConsoleWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Runner/Game Debug Console";
        private const string WindowTitle = "Game Debug Console";
        private const float MinWindowWidth = 360f;
        private const float MinWindowHeight = 480f;

        private Vector2 scrollPosition;

        /// <summary>
        /// プレイモード中のステータス変更をリアルタイムに反映するため、定期的にウィンドウの再描画を要求する。
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        /// <summary>
        /// エディタウィンドウの GUI 描画を行う。
        /// </summary>
        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                DrawNotPlayingView();
                return;
            }

            DrawPlayingView();
        }

        /// <summary>
        /// メニューから Game Debug Console ウィンドウを開く。
        /// </summary>
        [MenuItem(MenuPath, false, 20)]
        public static void Open()
        {
            var window = GetWindow<GameDebugConsoleWindow>(WindowTitle);
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.Show();
        }

        /// <summary>
        /// 非プレイモード時の待機画面を描画する。
        /// </summary>
        private void DrawNotPlayingView()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("ゲームが停止しています。\nUnity Editor を再生（Play）すると、リアルタイムステータス表示およびデバッグ操作が有効になります。", MessageType.Info);

            EditorGUILayout.Space(12);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("Play Mode 開始", GUILayout.Width(160), GUILayout.Height(32)))
                {
                    EditorApplication.isPlaying = true;
                }
                GUI.backgroundColor = prevColor;
                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// プレイモード中のリアルタイムステータスおよびデバッグ操作ボタン群を描画する。
        /// </summary>
        private void DrawPlayingView()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scroll.scrollPosition;

                DrawSystemSection();
                EditorGUILayout.Space(8);

                var player = PlayerController.Instance;
                DrawPlayerStatusSection(player);
                EditorGUILayout.Space(8);

                DrawActionsSection(player);
                EditorGUILayout.Space(12);
            }
        }

        /// <summary>
        /// システム情報（FPS、経過時間、HUD 表示トグル）を描画する。
        /// </summary>
        private void DrawSystemSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("System", EditorStyles.boldLabel);

                float fps = Time.unscaledDeltaTime > 0f ? (1f / Time.unscaledDeltaTime) : 0f;
                EditorGUILayout.LabelField($"FPS: {fps:F1}  |  Time: {Time.time:F1}s  |  TimeScale: {Time.timeScale:F1}");

                var progress = GameProgressManager.Instance;
                if (progress != null && progress.IsProgressing)
                {
                    var wave = progress.CurrentWave;
                    string waveStr = wave != null 
                        ? $"Wave {wave.WaveId} ({wave.StartTime:F0}s-{wave.EndTime:F0}s) | 進行: {progress.ElapsedTime:F1}s/{progress.EscapeDuration:F0}s | 敵上限: {wave.MaxAliveCount}"
                        : $"進行中 (Waveなし) | {progress.ElapsedTime:F1}s";
                    EditorGUILayout.LabelField("Wave", waveStr);
                }
                else
                {
                    EditorGUILayout.LabelField("Wave", "未開始 / 停止中");
                }

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var hud = GameDebugHUD.Instance;
                    string hudStatusText = hud != null ? "GameView HUD トグル (切替)" : "GameView HUD 未生成";
                    using (new EditorGUI.DisabledScope(hud == null))
                    {
                        if (GUILayout.Button(hudStatusText, GUILayout.Height(24)))
                        {
                            OnToggleHUDClicked();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// プレイヤーの各種ステータス情報（座標、速度、HP、歩数、所持金、怒りゲージ、アニメーション等）を描画する。
        /// </summary>
        /// <param name="player">対象の PlayerController インスタンス</param>
        private void DrawPlayerStatusSection(PlayerController player)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Player Status", EditorStyles.boldLabel);

                if (player == null)
                {
                    EditorGUILayout.HelpBox("PlayerController が見つかりません。ゲーム進行中（GameLoadingState 完了後）に表示されます。", MessageType.Warning);
                    return;
                }

                var pos = player.transform.position;
                var input = player.MoveInput;
                var speed = player.MoveSpeed;
                var status = player.Status;
                var animator = player.CharacterAnimator;

                EditorGUILayout.LabelField("座標 (Pos)", $"({pos.x:F2}, {pos.y:F2})");
                EditorGUILayout.LabelField("入力 (Input)", $"({input.x:F2}, {input.y:F2})");

                string speedBuffTag = player.HasSpeedBuff ? $" [バフ中: x{player.SpeedBuffMultiplier:F1} ({player.SpeedBuffRemainingDuration:F1}s)]" : "";
                EditorGUILayout.LabelField("移動速度 (Speed)", $"{speed:F1}{speedBuffTag}");

                if (status != null)
                {
                    float hpRatio = status.MaxHp > 0 ? (float)status.CurrentHp / status.MaxHp : 0f;
                    var charStatus = status as CharacterStatus;
                    string invincibleTag = charStatus != null && charStatus.IsInvincible ? " [無敵]" : "";
                    EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18), hpRatio, $"HP: {status.CurrentHp} / {status.MaxHp}{invincibleTag} {(status.IsDead ? "(Dead)" : "")}");
                }
                else
                {
                    EditorGUILayout.LabelField("HP", "Status なし");
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("ポイ活", $"¥{player.CurrentMoney:N0} pt  |  {player.CurrentSteps} 歩");

                bool magnetVisible = PlayerDebugRangeVisualizer.IsRangeVisible(player.transform);
                EditorGUILayout.LabelField("アイテム吸引範囲", $"{player.MagnetRadius:F1}m (表示: {(magnetVisible ? "ON" : "OFF")})");

                string animState = animator != null ? animator.CurrentState.ToString() : "None";
                EditorGUILayout.LabelField("アニメーション", animState);
            }
        }

        /// <summary>
        /// プレイヤー操作、武器、バフ、およびゲーム進行に関するデバッグアクションボタン群を描画する。
        /// </summary>
        /// <param name="player">対象の PlayerController インスタンス</param>
        private void DrawActionsSection(PlayerController player)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Player Actions", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(player == null))
                {
                    DrawButtonPair("攻撃", () => OnAttackClicked(player), "-10 HP", () => OnDamageClicked(player));
                    DrawButtonPair("+20 HP", () => OnHealClicked(player), "+500 pt", () => OnAddPointClicked(player));

                    bool magnetVisible = player != null && PlayerDebugRangeVisualizer.IsRangeVisible(player.transform);
                    string magnetText = magnetVisible ? "吸込範囲: ON" : "吸込範囲: OFF";
                    DrawButtonPair("コイン x5 生成", () => OnSpawnMoneyItemsClicked(player), magnetText, () => OnToggleMagnetRangeClicked(player));

                    var charStatus = player != null ? player.Status as CharacterStatus : null;
                    bool isInvincible = charStatus != null && charStatus.IsInvincible;
                    string invincibleText = isInvincible ? "無敵: ON" : "無敵: OFF";
                    if (GUILayout.Button(invincibleText, GUILayout.Height(28)))
                    {
                        OnToggleInvincibleClicked(player);
                    }
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Weapons", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(player == null))
                {
                    int droneLevel = WeaponManager.HasInstance ? WeaponManager.Instance.GetWeaponLevel(WeaponType.Drone) : 0;
                    bool isMax = WeaponManager.HasInstance && WeaponManager.Instance.IsMaxLevel(WeaponType.Drone);
                    string droneBtnText = isMax ? "ドローン (Max Lv.5)" : $"ドローン Lv.UP (Lv.{droneLevel})";
                    DrawButtonPair(droneBtnText, OnUpgradeDroneClicked, "全武器 解除", OnReleaseAllWeaponsClicked);
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Buffs", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(player == null))
                {
                    string buffBtnText = player != null && player.HasSpeedBuff
                        ? $"速度バフ (+50% {player.SpeedBuffRemainingDuration:F1}s)"
                        : "速度バフ (+50% 5s)";
                    DrawButtonPair(buffBtnText, () => OnApplySpeedBuffClicked(player), "速度バフ 解除", () => OnClearSpeedBuffClicked(player));
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Game & Event Actions", EditorStyles.boldLabel);

                DrawButtonPair("セール通知 発火", OnTriggerSaleClicked, "ジャスト回避 演出", OnJustDodgeClicked);
                DrawButtonPair("非常口 即時開放", OnOpenExitClicked, "敵スポーン x1", OnSpawnEnemyClicked);
            }
        }

        /// <summary>
        /// 1行に2つのボタンを左右均等（50% / 50%）に配置して描画する。
        /// </summary>
        /// <param name="leftText">左側ボタンの表示テキスト</param>
        /// <param name="onLeftClick">左側ボタン押下時のコールバック</param>
        /// <param name="rightText">右側ボタンの表示テキスト</param>
        /// <param name="onRightClick">右側ボタン押下時のコールバック</param>
        private void DrawButtonPair(string leftText, Action onLeftClick, string rightText, Action onRightClick)
        {
            var rect = EditorGUILayout.GetControlRect(false, 28f);
            const float spacing = 4f;
            float width = (rect.width - spacing) * 0.5f;

            var leftRect = new Rect(rect.x, rect.y, width, rect.height);
            var rightRect = new Rect(rect.x + width + spacing, rect.y, width, rect.height);

            if (GUI.Button(leftRect, leftText))
            {
                onLeftClick?.Invoke();
            }

            if (GUI.Button(rightRect, rightText))
            {
                onRightClick?.Invoke();
            }
        }

        /// <summary>
        /// 攻撃ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnAttackClicked(PlayerController player)
        {
            if (player == null) return;
            player.Attack();
        }

        /// <summary>
        /// ダメージボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnDamageClicked(PlayerController player)
        {
            if (player == null || player.Status == null) return;
            player.Status.TakeDamage(10);
        }

        /// <summary>
        /// 回復ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnHealClicked(PlayerController player)
        {
            if (player == null || player.Status == null) return;
            player.Status.Heal(20);
        }

        /// <summary>
        /// ポイント加算ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnAddPointClicked(PlayerController player)
        {
            if (player == null) return;
            player.CollectMoney(500);
        }

        /// <summary>
        /// 周辺へのコインアイテム生成ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private async void OnSpawnMoneyItemsClicked(PlayerController player)
        {
            if (player == null) return;
            await ItemDebugSpawner.SpawnMoneyItemsAroundAsync(player.transform.position, 5);
        }

        /// <summary>
        /// アイテム吸引範囲表示トグルボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnToggleMagnetRangeClicked(PlayerController player)
        {
            if (player == null) return;
            PlayerDebugRangeVisualizer.ToggleRangeVisible(player.transform, player.MagnetRadius);
        }

        /// <summary>
        /// 無敵状態トグルボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnToggleInvincibleClicked(PlayerController player)
        {
            var charStatus = player != null ? player.Status as CharacterStatus : null;
            if (charStatus != null)
            {
                charStatus.IsInvincible = !charStatus.IsInvincible;
                Repaint();
                DebugLogger.Log($"[GameDebugConsoleWindow] デバッグ操作: プレイヤーの無敵状態を {(charStatus.IsInvincible ? "ON" : "OFF")} に切り替えました。");
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
                DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: タイムセール通知を発火しました。");
            }
        }

        /// <summary>
        /// ジャスト回避演出発動ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnJustDodgeClicked()
        {
            if (GameHUDView.Instance != null)
            {
                GameHUDView.Instance.OnJustDodge();
                DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: ジャスト回避演出をトリガーしました。");
            }
        }

        /// <summary>
        /// 非常口即時開放ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private void OnOpenExitClicked()
        {
            if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
            {
                GameHUDView.Instance.EscapeTimerHUD.SetExitUnlocked(true);
                DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: 非常口を即時開放しました。");
            }
        }

        /// <summary>
        /// 敵1体スポーンボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private async void OnSpawnEnemyClicked()
        {
            if (EnemySpawner.Instance == null)
            {
                DebugLogger.Error("[GameDebugConsoleWindow] EnemySpawner が存在しないためエネミーを生成できません。");
                return;
            }

            var spawned = await EnemySpawner.Instance.SpawnEnemyAsync(CancellationToken.None);
            if (spawned != null)
            {
                DebugLogger.Log($"[GameDebugConsoleWindow] デバッグ操作: EnemySpawner からエネミーを生成しました。座標: {spawned.transform.position}");
                return;
            }

            DebugLogger.Error("[GameDebugConsoleWindow] エネミーの生成に失敗しました（最大上限到達または生成エラー）。");
        }

        /// <summary>
        /// GameView 上の GameDebugHUD の表示・非表示をトグル切り替えする。
        /// </summary>
        private void OnToggleHUDClicked()
        {
            if (GameDebugHUD.Instance != null)
            {
                GameDebugHUD.Instance.TogglePanel();
            }
        }

        /// <summary>
        /// ドローン武器のレベルアップボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        private async void OnUpgradeDroneClicked()
        {
            if (WeaponManager.Instance != null)
            {
                await WeaponManager.Instance.UpgradeWeaponAsync(WeaponType.Drone, CancellationToken.None);
            }
        }

        /// <summary>
        /// すべての武器を解放・クリアし、レベルをリセットするデバッグ操作を処理する。
        /// </summary>
        private void OnReleaseAllWeaponsClicked()
        {
            if (WeaponManager.Instance != null)
            {
                WeaponManager.Instance.ResetAllWeapons();
                DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: すべての武器レベルをリセットし、実体を解放しました。");
            }
            else if (WeaponSpawner.Instance != null)
            {
                WeaponSpawner.Instance.ReleaseAllWeapons();
                DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: すべての武器を解放しました。");
            }
        }

        /// <summary>
        /// 移動速度バフ付与ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnApplySpeedBuffClicked(PlayerController player)
        {
            if (player == null) return;
            player.ApplySpeedBuff(1.5f, 5.0f);
            DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: プレイヤーに移動速度バフ (+50%, 5秒) を付与しました。");
        }

        /// <summary>
        /// 移動速度バフ解除ボタンクリック時のデバッグ操作を処理する。
        /// </summary>
        /// <param name="player">対象の PlayerController</param>
        private void OnClearSpeedBuffClicked(PlayerController player)
        {
            if (player == null) return;
            player.ClearSpeedBuff();
            DebugLogger.Log("[GameDebugConsoleWindow] デバッグ操作: プレイヤーの移動速度バフを解除しました。");
        }
    }
}

