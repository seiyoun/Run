/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中ステート。入力コントローラーの接続、脱出タイマー・セール通知トリガーの管理、死亡判定を統括する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// ゲームプレイ中のステート。
    /// プレイヤー入力・アクション、制限時間（脱出タイマー）、一定ポイント到達によるセール発火判定を統合管理します。
    /// </summary>
    public sealed class GamePlayingState : IState<GamePlayState>
    {
        private readonly IGameContext context;
        private bool isEnding;

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
        /// プレイ中ステート開始時の初期化、入力バインド、および進行管理イベントの購読を行う。
        /// </summary>
        /// <param name="parameter">開始パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
        public Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            isEnding = false;
            DebugLogger.Log("[GamePlayingState] ゲームプレイ開始！プレイヤー入力を有効化し、脱出タイマーを開始します。");

            var player = PlayerController.Instance;
            if (player != null)
            {
                // inputをプレイヤーにバインド
                if (InputController.Instance != null)
                {
                    player.BindInput(InputController.Instance);
                }

                if (player.Status != null)
                {
                    player.Status.OnDead += HandlePlayerDead;
                }

                // 記録処理（歩数・所持金管理）とプレイヤーをバインド
                if (GameRecordTracker.HasInstance || GameRecordTracker.Instance != null)
                {
                    GameRecordTracker.Instance.BindPlayer(player);
                }
            }

            if (GameHUDView.Instance != null)
            {
                // HUD表示と記録処理をバインド
                if (GameRecordTracker.HasInstance || GameRecordTracker.Instance != null)
                {
                    GameHUDView.Instance.BindRecordTracker(GameRecordTracker.Instance);
                }

                if (GameHUDView.Instance.EscapeTimerHUD != null)
                {
                    GameHUDView.Instance.EscapeTimerHUD.SetExitUnlocked(false);
                }
            }

            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.OnRemainingTimeUpdated += HandleRemainingTimeUpdated;
                GameProgressManager.Instance.OnExitUnlocked += HandleExitUnlocked;
                GameProgressManager.Instance.OnRestockProgressUpdated += HandleRestockProgressUpdated;
                GameProgressManager.Instance.OnShopTriggered += HandleShopTriggered;
                GameProgressManager.Instance.StartProgress();
            }

            if (EnemySpawnDirector.Instance != null)
            {
                EnemySpawnDirector.Instance.StartSpawning();
            }

            _ = DropManager.Instance;
            if (GameRecordTracker.HasInstance || GameRecordTracker.Instance != null)
            {
                GameRecordTracker.Instance.ResetRecord();
            }

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
            bool isPaused = (GameHUDView.Instance != null && GameHUDView.Instance.ShopView != null && GameHUDView.Instance.ShopView.IsOpen)
                            || Time.timeScale <= 0f;
            float actualDeltaTime = isPaused ? 0f : deltaTime;

            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.Tick(actualDeltaTime);
            }
        }

        /// <summary>
        /// プレイ中ステート終了時のクリーンアップ処理を行う。
        /// </summary>
        public void Exit()
        {
            isEnding = true;
            var escapePoint = EscapePoint.Instance;
            if (escapePoint != null)
            {
                escapePoint.OnPlayerEntered -= HandlePlayerEscaped;
            }
            if (EscapePointSpawner.HasInstance) EscapePointSpawner.Instance.Hide();

            if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
            {
                GameHUDView.Instance.EscapeTimerHUD.SetExitTarget(null, null);
            }

            var player = PlayerController.Instance;
            if (player != null && player.Status != null)
            {
                player.Status.OnDead -= HandlePlayerDead;
            }

            if (EnemySpawnDirector.HasInstance && EnemySpawnDirector.Instance != null)
            {
                EnemySpawnDirector.Instance.StopSpawning();
            }

            if (GameProgressManager.HasInstance && GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.OnRemainingTimeUpdated -= HandleRemainingTimeUpdated;
                GameProgressManager.Instance.OnExitUnlocked -= HandleExitUnlocked;
                GameProgressManager.Instance.OnRestockProgressUpdated -= HandleRestockProgressUpdated;
                GameProgressManager.Instance.OnShopTriggered -= HandleShopTriggered;
                GameProgressManager.Instance.StopProgress();
            }

            if (GameAssetLoader.HasInstance)
            {
                GameAssetLoader.Instance.ReleaseAll();
            }

            DebugLogger.Log("[GamePlayingState] プレイ中ステートを終了しました。");
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
        /// 脱出残り時間の更新イベントハンドラ。HUD のタイマー表示を更新する。
        /// </summary>
        /// <param name="remainingTime">残り秒数</param>
        private void HandleRemainingTimeUpdated(float remainingTime)
        {
            if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
            {
                GameHUDView.Instance.EscapeTimerHUD.SetRemainingTime(remainingTime);
            }
        }

        /// <summary>
        /// 非常口開放イベントハンドラ。HUD の非常口開放表示を有効化する。
        /// </summary>
        private void HandleExitUnlocked()
        {
            var player = PlayerController.Instance;
            var boundary = ArenaBackground.Instance != null ? ArenaBackground.Instance.BoundaryCollider : null;
            var escapePoint = EscapePoint.Instance;
            var escapePointSpawner = EscapePointSpawner.HasInstance ? EscapePointSpawner.Instance : null;
            if (isEnding || player == null || boundary == null || escapePointSpawner == null || escapePoint == null)
            {
                DebugLogger.Error("[GamePlayingState] 脱出ポイントを表示できません。ロード済みゲート、プレイヤー、またはステージ境界がありません。");
                return;
            }

            const float margin = 2f;
            const float minPlayerDistance = 8f;
            var bounds = boundary.bounds;
            float minX = bounds.min.x + margin;
            float maxX = bounds.max.x - margin;
            float minY = bounds.min.y + margin;
            float maxY = bounds.max.y - margin;
            if (minX > maxX || minY > maxY)
            {
                DebugLogger.Error("[GamePlayingState] ステージ境界が狭すぎるため脱出ポイントを配置できません。");
                return;
            }

            Vector2 position = Vector2.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                position = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
                if (Vector2.Distance(position, player.transform.position) >= minPlayerDistance) break;
            }

            escapePoint.OnPlayerEntered += HandlePlayerEscaped;
            escapePointSpawner.Show(new Vector3(position.x, position.y, 0f));

            if (GameHUDView.Instance != null && GameHUDView.Instance.EscapeTimerHUD != null)
            {
                GameHUDView.Instance.EscapeTimerHUD.SetExitUnlocked(true);
                GameHUDView.Instance.EscapeTimerHUD.SetExitTarget(escapePoint.transform, player.transform);
            }

            DebugLogger.Log($"[GamePlayingState] ロード済み脱出ポイントを {position} に表示しました。");
        }

        /// <summary>
        /// 脱出ポイントに到達したときにクリアステートへ遷移する。
        /// </summary>
        private void HandlePlayerEscaped()
        {
            if (isEnding || PlayerController.Instance == null || PlayerController.Instance.Status == null || PlayerController.Instance.Status.IsDead) return;
            if (!GameProgressManager.HasInstance || !GameProgressManager.Instance.IsExitUnlocked) return;

            isEnding = true;
            DebugLogger.Log("[GamePlayingState] プレイヤーが脱出ポイントに到達しました。");
            if (context.StateMachine != null)
            {
                _ = context.StateMachine.ChangeStateAsync(GamePlayState.GameClear, CancellationToken.None);
            }
        }

        /// <summary>
        /// 入荷進捗更新イベントハンドラ。HUD の入荷ゲージ表示を更新する。
        /// </summary>
        /// <param name="remainingPoints">次の入荷までに必要な残りポイント</param>
        /// <param name="progress">進捗率（0.0〜1.0）</param>
        /// <param name="isInitial">初期化呼び出しであるかどうか</param>
        private void HandleRestockProgressUpdated(long remainingPoints, float progress, bool isInitial)
        {
            if (GameHUDView.Instance != null)
            {
                GameHUDView.Instance.UpdateRestockProgress(remainingPoints, progress, isInitial);
            }
        }

        /// <summary>
        /// ショップ発生イベントハンドラ。ショップ画面を開く。
        /// </summary>
        private void HandleShopTriggered()
        {
            if (GameHUDView.Instance != null)
            {
                GameHUDView.Instance.OpenShop();
            }
        }
    }
}
