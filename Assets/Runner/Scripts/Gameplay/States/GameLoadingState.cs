/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲーム初期化時のロードステート。背景プレハブおよびプレイヤープレハブをロード・生成し、ゲームプレイの準備を整える。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
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
        private const string BackgroundAddress = "ArenaBackground";
        private const string ResultModalAddress = "GameResultModalView";

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

            var bgObj = await LoadBackgroundAsync(cancellationToken);

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

            await LoadPlayerAsync(playerSpawnPoint, cancellationToken);
            await LoadResultModalAsync(cancellationToken);
            SetupGameHUD();

#if SANDBOX || UNITY_EDITOR
            SpawnDebugHUD();
#endif

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
        /// Addressables から背景プレハブをロード・生成する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された背景 GameObject インスタンス</returns>
        private async Task<GameObject> LoadBackgroundAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameLoadingState] Addressables から背景プレハブのロードを開始します...");

            var loader = new AddressablePrefabLoader();
            try
            {
                var bgObj = await loader.LoadAsync(BackgroundAddress, cancellationToken);
                if (bgObj != null)
                {
                    var arenaBg = bgObj.GetComponent<ArenaBackground>();
                    if (arenaBg != null)
                    {
                        arenaBg.BindLoader(loader);
                    }
                    DebugLogger.Log("[GameLoadingState] 背景プレハブのロード・生成が完了しました。");
                    return bgObj;
                }
                else
                {
                    DebugLogger.Error("[GameLoadingState] 背景プレハブのロード結果が null です。");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[GameLoadingState] 背景プレハブのロードに失敗しました: {ex.Message}");
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

            var spawner = PlayerSpawner.Instance;
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

        /// <summary>
        /// Addressables からリザルトモーダルプレハブをロード・生成し、非表示状態で待機させてローダーを委託する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        private async Task LoadResultModalAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[GameLoadingState] Addressables から GameResultModalView のロードを開始します...");

            var loader = new AddressablePrefabLoader();
            try
            {
                var modalObj = await loader.LoadAsync(ResultModalAddress, cancellationToken);
                if (modalObj != null)
                {
                    var modalView = modalObj.GetComponent<GameResultModalView>();
                    if (modalView != null)
                    {
                        modalView.BindLoader(loader);
                        modalView.Hide();
                        DebugLogger.Log("[GameLoadingState] GameResultModalView のロードおよび非表示待機化が完了しました。");
                    }
                    else
                    {
                        DebugLogger.Error("[GameLoadingState] ロードされたプレハブに GameResultModalView がアタッチされていません。");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"[GameLoadingState] GameResultModalView のロードに失敗しました: {ex.Message}");
            }
        }

        /// <summary>
        /// ステート待機非同期処理。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
        }

        /// <summary>
        /// 毎フレームの更新処理。
        /// </summary>
        public void Update()
        {
        }

        /// <summary>
        /// ステート終了時のクリーンアップ処理。
        /// </summary>
        public void Exit()
        {
            DebugLogger.Log("[GameLoadingState] ロード完了。Playing ステートへ移行しました。");
        }
    }
}
