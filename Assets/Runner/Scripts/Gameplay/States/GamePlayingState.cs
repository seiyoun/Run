/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中ステート。プレイライフサイクルとクリア・死亡時のステート遷移を統括する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームプレイ中のステート。
    /// プレイ中のライフサイクル管理およびクリア・死亡によるステート遷移を統括します。
    /// </summary>
    public sealed class GamePlayingState : IState<GamePlayState>
    {
        private readonly IGameContext context;
        private bool isEnding;

        /// <summary>対応するステート列挙値</summary>
        public GamePlayState State => GamePlayState.Playing;

        /// <summary>
        /// GamePlayingState のコンストラクタ。
        /// </summary>
        /// <param name="context">ステート間で共有されるコンテキスト</param>
        public GamePlayingState(IGameContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// プレイ中ステート開始時の初期化、各サブシステムの接続、および進行開始を行う。
        /// </summary>
        /// <param name="parameter">開始パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            isEnding = false;
            DebugLogger.Log("[GamePlayingState] ゲームプレイ開始！プレイヤー入力を有効化し、ゲーム進行を開始します。");

            var tracker = GameRecordTracker.Instance;
            var progress = GameProgressManager.Instance;
            var hud = GameHUDView.Instance;

            SetupPlayer(tracker);
            SetupHUD(hud, tracker, progress);
            StartGameplay(progress, tracker);

            DebugLogger.Log("[GamePlayingState] ゲームプレイ準備が完了しました。");
            return Task.CompletedTask;
        }

        /// <summary>
        /// プレイ中ステート待機非同期処理。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>待機タスク</returns>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 毎フレームのゲームプレイ更新処理を実行する。
        /// ショップモーダル表示中などのポーズ時は deltaTime を 0 にして進行を一時停止します。
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        public void Update(float deltaTime)
        {
            var hud = GameHUDView.Instance;
            var shop = hud != null ? hud.ShopView : null;
            bool isPaused = (shop != null && shop.IsOpen) || Time.timeScale <= 0f;
            float actualDeltaTime = isPaused ? 0f : deltaTime;

            var progress = GameProgressManager.HasInstance ? GameProgressManager.Instance : null;
            if (progress != null) progress.Tick(actualDeltaTime);
        }

        /// <summary>
        /// プレイ中ステート終了時のクリーンアップ処理を行う。
        /// </summary>
        public void Exit()
        {
            isEnding = true;
            CleanupEscapePoint();
            CleanupPlayer();
            StopGameplay();

            DebugLogger.Log("[GamePlayingState] プレイ中ステートを終了しました。");
        }

        /// <summary>
        /// プレイヤーの入力と死亡イベントを接続し、記録対象に設定する。
        /// </summary>
        /// <param name="tracker">プレイ記録の管理者</param>
        private void SetupPlayer(GameRecordTracker tracker)
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            var input = InputController.Instance;
            if (input != null) player.BindInput(input);
            if (player.Status != null) player.Status.OnDead += HandlePlayerDead;
            if (tracker != null) tracker.BindPlayer(player);
        }

        /// <summary>
        /// HUDにプレイ記録およびゲーム進行イベントをバインドし、脱出表示を初期化する。
        /// </summary>
        /// <param name="hud">対象の GameHUDView</param>
        /// <param name="tracker">プレイ記録の管理者</param>
        /// <param name="progress">ゲーム進行マネージャー</param>
        private void SetupHUD(GameHUDView hud, GameRecordTracker tracker, GameProgressManager progress)
        {
            if (hud == null) return;

            if (tracker != null) hud.BindRecordTracker(tracker);
            if (progress != null) hud.BindProgressManager(progress);
            if (hud.EscapeTimerHUD != null) hud.EscapeTimerHUD.SetExitUnlocked(false);
        }

        /// <summary>
        /// ゲーム進行と敵のスポーンを開始し、プレイ記録をリセットする。
        /// </summary>
        /// <param name="progress">ゲーム進行マネージャー</param>
        /// <param name="tracker">プレイ記録の管理者</param>
        private void StartGameplay(GameProgressManager progress, GameRecordTracker tracker)
        {
            if (progress != null)
            {
                progress.OnExitUnlocked += HandleExitUnlocked;
                progress.StartProgress();
            }

            var enemyDirector = EnemySpawnDirector.Instance;
            if (enemyDirector != null) enemyDirector.StartSpawning();

            _ = DropManager.Instance;
            if (tracker != null) tracker.ResetRecord();
        }

        /// <summary>
        /// 脱出ゲートのイベント購読とHUDの案内表示を解除する。
        /// </summary>
        private void CleanupEscapePoint()
        {
            var escapePoint = EscapePoint.Instance;
            if (escapePoint != null) escapePoint.OnPlayerEntered -= HandlePlayerEscaped;
            if (EscapePointSpawner.HasInstance) EscapePointSpawner.Instance.Hide();

            var timerHUD = GameHUDView.Instance != null ? GameHUDView.Instance.EscapeTimerHUD : null;
            if (timerHUD != null) timerHUD.SetExitTarget(null, null);
        }

        /// <summary>
        /// プレイヤーの死亡イベント購読を解除する。
        /// </summary>
        private void CleanupPlayer()
        {
            var player = PlayerController.Instance;
            if (player != null && player.Status != null) player.Status.OnDead -= HandlePlayerDead;
        }

        /// <summary>
        /// 敵と進行管理を停止し、HUDのイベントバインド解除およびプレイ中のアセット解放を行う。
        /// </summary>
        private void StopGameplay()
        {
            var enemyDirector = EnemySpawnDirector.HasInstance ? EnemySpawnDirector.Instance : null;
            if (enemyDirector != null) enemyDirector.StopSpawning();

            var progress = GameProgressManager.HasInstance ? GameProgressManager.Instance : null;
            var hud = GameHUDView.Instance;
            if (progress != null)
            {
                progress.OnExitUnlocked -= HandleExitUnlocked;
                if (hud != null) hud.UnbindProgressManager(progress);
                progress.StopProgress();
            }

            if (GameAssetLoader.HasInstance) GameAssetLoader.Instance.ReleaseAll();
        }

        /// <summary>
        /// プレイヤー死亡時のGameOverステート遷移ハンドラ。
        /// </summary>
        private void HandlePlayerDead()
        {
            if (isEnding) return;
            isEnding = true;
            DebugLogger.Log("[GamePlayingState] プレイヤーが死亡しました。GameOver ステートへ遷移します。");
            if (context.StateMachine != null)
            {
                _ = context.StateMachine.ChangeStateAsync(GamePlayState.GameOver, CancellationToken.None);
            }
        }

        /// <summary>
        /// 非常口開放イベントハンドラ。脱出ゲートを安全な位置に表示し、HUD の案内を開始する。
        /// </summary>
        private void HandleExitUnlocked()
        {
            if (isEnding) return;

            var player = PlayerController.Instance;
            var boundary = ArenaBackground.Instance != null ? ArenaBackground.Instance.BoundaryCollider : null;
            var spawner = EscapePointSpawner.HasInstance ? EscapePointSpawner.Instance : null;

            if (player == null || boundary == null || spawner == null)
            {
                DebugLogger.Error("[GamePlayingState] 脱出ポイントを表示できません。プレイヤー、ステージ境界、または Spawner がありません。");
                return;
            }

            if (spawner.TryShowAtSafePosition(boundary, player.transform.position, out var escapePoint))
            {
                escapePoint.OnPlayerEntered += HandlePlayerEscaped;

                var timerHUD = GameHUDView.Instance != null ? GameHUDView.Instance.EscapeTimerHUD : null;
                if (timerHUD != null)
                {
                    timerHUD.SetExitUnlocked(true);
                    timerHUD.SetExitTarget(escapePoint.transform, player.transform);
                }

                DebugLogger.Log($"[GamePlayingState] 脱出ポイントを {escapePoint.transform.position} に表示しました。");
            }
            else
            {
                DebugLogger.Error("[GamePlayingState] 脱出ポイントの安全配置に失敗しました。");
            }
        }

        /// <summary>
        /// 脱出ポイントに到達したときにクリアステートへ遷移する。
        /// </summary>
        private void HandlePlayerEscaped()
        {
            var player = PlayerController.Instance;
            if (isEnding || player == null || player.Status == null || player.Status.IsDead) return;
            var progress = GameProgressManager.HasInstance ? GameProgressManager.Instance : null;
            if (progress == null || !progress.IsExitUnlocked) return;

            isEnding = true;
            DebugLogger.Log("[GamePlayingState] プレイヤーが脱出ポイントに到達しました。GameClear ステートへ遷移します。");
            if (context.StateMachine != null)
            {
                _ = context.StateMachine.ChangeStateAsync(GamePlayState.GameClear, CancellationToken.None);
            }
        }
    }
}
