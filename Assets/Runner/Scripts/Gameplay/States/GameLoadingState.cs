/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム初期化時のロードステート。背景プレハブおよびプレイヤープレハブをロード・生成し、ゲームプレイの準備を整える。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Runner
{
    /// <summary>
    /// ゲーム開始前のロード・初期化ステート。
    /// 背景およびプレイヤーをロード・生成し、Playing ステートへ遷移します。
    /// </summary>
    public sealed class GameLoadingState : IState<GamePlayState>
    {
        public GamePlayState State => GamePlayState.Loading;

        private readonly IGameContext context;

        public GameLoadingState(IGameContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// ロード処理を開始し、背景およびプレイヤーの生成を行って Playing ステートへ遷移する。
        /// </summary>
        /// <param name="parameter">遷移パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        public async Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameLoadingState] ゲームプレイのロードを開始します...");

            // 1. 背景プレハブのロード・生成
            var bgObj = await LoadBackgroundAsync(cancellationToken);

            // 背景からプレイヤー生成位置 Transform を取得
            Transform playerSpawnPoint = null;
            if (bgObj != null)
            {
                var arenaBg = bgObj.GetComponent<ArenaBackground>();
                if (arenaBg != null && arenaBg.PlayerSpawnPoint != null)
                {
                    playerSpawnPoint = arenaBg.PlayerSpawnPoint;
                    DebugLogger.Log($"[GameLoadingState] ArenaBackground から PlayerSpawnPoint ({playerSpawnPoint.position}) を取得しました。");
                }
            }

            // 2. プレイヤーのロード・生成（背景のスポーン位置を優先）
            await LoadPlayerAsync(playerSpawnPoint, cancellationToken);

            // 3. ゲーム画面HUD（ポイ活・怒りゲージ・脱出タイマー・スマホ通販）のセットアップ
            SetupGameHUD();

#if SANDBOX || UNITY_EDITOR
            // 4. SANDBOX 定義時（または Unity エディタ実行時）のみ DebugCanvas を動的ロード・生成
            SpawnDebugHUD();
#endif

            // ロード完了後、Playing ステートへ遷移
            if (context.StateMachine != null)
            {
                await context.StateMachine.ChangeStateAsync(GamePlayState.Playing, cancellationToken);
            }
        }

        /// <summary>
        /// シーン上の GameHUDView を検出して確認する。
        /// </summary>
        private void SetupGameHUD()
        {
            var hud = GameHUDView.Instance ?? Object.FindFirstObjectByType<GameHUDView>();
            if (hud != null)
            {
                DebugLogger.Log("[GameLoadingState] シーン上に設定済みの GameHUDView を検出・認識しました。");
            }
            else
            {
                Debug.LogWarning("[GameLoadingState] シーン上に GameHUDView が見つかりません。GameCanvas に GameHUDView を設定してください。");
            }
        }

#if SANDBOX || UNITY_EDITOR
        /// <summary>
        /// SANDBOX 用 DebugCanvas を動的に生成する。
        /// </summary>
        private void SpawnDebugHUD()
        {
            GameDebugHUD.Create();
            DebugLogger.Log("[GameLoadingState] SANDBOX 用 DebugCanvas をコードから動的生成しました。");
        }
#endif

        /// <summary>
        /// BackgroundSpawner を通じて背景プレハブをロード・生成する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された背景 GameObject インスタンス</returns>
        private async Task<GameObject> LoadBackgroundAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameLoadingState] BackgroundSpawner を呼び出して背景プレハブ生成を開始します...");

            var bgSpawner = Object.FindFirstObjectByType<BackgroundSpawner>();
            if (bgSpawner != null)
            {
                var bg = await bgSpawner.SpawnBackgroundAsync(cancellationToken);
                if (bg != null)
                {
                    DebugLogger.Log("[GameLoadingState] BackgroundSpawner による背景プレハブ生成が完了しました。");
                    return bg;
                }
                else
                {
                    DebugLogger.Error("[GameLoadingState] BackgroundSpawner による背景プレハブ生成に失敗しました。");
                }
            }
            else
            {
                DebugLogger.Error("[GameLoadingState] シーン上に BackgroundSpawner が見つかりません。");
            }

            return null;
        }

        /// <summary>
        /// PlayerSpawner を通じてプレイヤーキャラクターをロード・生成する。
        /// </summary>
        /// <param name="spawnPoint">プレイヤーのスポーン位置 Transform</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        private async Task LoadPlayerAsync(Transform spawnPoint, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameLoadingState] PlayerSpawner を呼び出してプレイヤー生成を開始します...");

            var spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            if (spawner != null)
            {
                var player = await spawner.SpawnPlayerAsync(spawnPoint, cancellationToken);
                if (player != null)
                {
                    context.SetPlayerInstance(player);
                    DebugLogger.Log("[GameLoadingState] PlayerSpawner によるプレイヤー生成が完了しました。");
                }
                else
                {
                    DebugLogger.Error("[GameLoadingState] PlayerSpawner によるプレイヤー生成に失敗しました。");
                }
            }
            else
            {
                DebugLogger.Error("[GameLoadingState] シーン上に PlayerSpawner が見つかりません。");
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        public void Update()
        {
        }

        public void Exit()
        {
            DebugLogger.Log("[GameLoadingState] ロード完了。Playing ステートへ移行しました。");
        }
    }
}
