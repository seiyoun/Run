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

            int targetStageId = parameter is int stageId ? stageId : 1;
            var stageData = MasterDataManager.GetStageMasterData(targetStageId);
            DebugLogger.Log($"[GameLoadingState] ステージ {targetStageId} のマスターデータをロードしました: Background={stageData.BackgroundId}, EscapeTime={stageData.EscapeTime}s, WaveIds=[{string.Join(", ", stageData.WaveIds)}]");

            var bgObj = await LoadBackgroundAsync(stageData.BackgroundId, cancellationToken);

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
            await LoadStageProgressAsync(stageData, cancellationToken);
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
        /// BackgroundSpawner を通じて背景プレハブをロード・生成する。
        /// </summary>
        /// <param name="backgroundId">背景プレハブのアドレス/ID</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>生成された背景 GameObject インスタンス</returns>
        private async Task<GameObject> LoadBackgroundAsync(string backgroundId, CancellationToken cancellationToken)
        {
            DebugLogger.Log($"[GameLoadingState] BackgroundSpawner を呼び出して背景プレハブ ({backgroundId}) の生成を開始します...");

            var spawner = BackgroundSpawner.Instance;
            if (spawner != null)
            {
                var bg = await spawner.SpawnBackgroundAsync(backgroundId, cancellationToken);
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
                DebugLogger.Error("[GameLoadingState] BackgroundSpawner のインスタンス取得に失敗しました。");
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
        /// ステージデータに基づき、指定された Wave 設定および脱出制限時間を GameProgressManager へ反映する。
        /// </summary>
        /// <param name="stageData">ステージマスターデータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        private async Task LoadStageProgressAsync(StageMasterData stageData, CancellationToken cancellationToken)
        {
            if (stageData == null) return;

            DebugLogger.Log($"[GameLoadingState] ステージ {stageData.StageId} の進行データ（Wave, 脱出時間）のロードを開始します...");

            var waves = await SpawnWaveData.LoadByIdsAsync(stageData.WaveIds, cancellationToken: cancellationToken);
            if (GameProgressManager.Instance != null)
            {
                GameProgressManager.Instance.SetEscapeDuration(stageData.EscapeTime);
                GameProgressManager.Instance.SetWaveData(waves);
                DebugLogger.Log($"[GameLoadingState] GameProgressManager に ステージ {stageData.StageId} の設定（脱出時間: {stageData.EscapeTime}s, ウェーブ数: {waves.Count}）を反映しました。");
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
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        public void Update(float deltaTime)
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
