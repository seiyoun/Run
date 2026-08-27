/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Boot シーンのライフサイクル管理と初期化処理を定義する。
 *                SceneManager を介して AddressablePrefabLoader による LoadingView 常駐ロードを行い、Title シーンへ遷移します。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using Shiyuan.Foundation.Scenes;

namespace Runner
{
    /// <summary>
    /// Boot シーンにおける初期化および次シーンへの遷移ライフサイクル。
    /// SceneManager を通じて AddressablePrefabLoader による LoadingView ロードを行い、Title シーンへ遷移します。
    /// </summary>
    public sealed class BootSceneLifecycle : SceneLifecycleBase
    {
        /// <summary>
        /// Boot シーン初期化前の通信・データ読み込み待機処理。
        /// SceneManager 側の AddressablePrefabLoader を通じて LoadingView を非同期ロード・常駐化する。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[BootScene] SceneManager を介して LoadingView をロード中...");

            if (SceneManager.Instance != null)
            {
                await SceneManager.Instance.EnsureLoadingViewAsync(cancellationToken);
            }

            await Task.Yield();
        }

        /// <summary>
        /// Boot シーンの初期化処理を実行し、完了後に Title シーンへ遷移する。
        /// </summary>
        /// <param name="parameter">初期化パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        protected override async Task OnInitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[BootScene] 初期化完了。Title シーンへ遷移します。");

            // Title シーンへ遷移
            await SceneManager.Instance.ChangeScene(SceneType.Title, showLoading: true);
        }

        /// <summary>
        /// Boot シーン離脱時のクリーンアップ処理を行う。
        /// </summary>
        protected override void OnDestroy()
        {
            DebugLogger.Log("[BootScene] 破棄処理完了。");
        }
    }
}
