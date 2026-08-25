/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Boot シーンのライフサイクル管理と初期化処理を定義する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Addressables;
using Shiyuan.Foundation.Core;
using Shiyuan.Foundation.Scenes;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Boot シーンにおける初期化および次シーンへの遷移ライフサイクル。
    /// </summary>
    public sealed class BootSceneLifecycle : SceneLifecycleBase
    {
        private const string LoadingViewAddress = "LoadingView";
        private readonly AddressablePrefabLoader addressablePrefabLoader = new();

        /// <summary>
        /// Boot シーン初期化前の通信・データ読み込み待機処理。
        /// Addressables から LoadingView プレハブを非同期ロードする。
        /// </summary>
        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[BootScene] LoadingView を Addressables からロード中...");

            // LoadingView が未生成なら Addressables からロードして常駐化
            if (LoadingView.Instance == null)
            {
                var loadingObj = await addressablePrefabLoader.LoadAsync(LoadingViewAddress, cancellationToken);
                if (loadingObj != null)
                {
                    DebugLogger.Log("[BootScene] LoadingView の Addressables ロードが完了しました。");
                }
            }

            await Task.Yield();
        }

        /// <summary>
        /// Boot シーンの初期化処理を実行し、完了後に Title シーンへ遷移する。
        /// </summary>
        protected override async Task OnInitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[BootScene] 初期化完了。Title シーンへ遷移します。");

            // Title シーンへ遷移
            await SceneManager.Instance.ChangeScene(SceneType.Title, showLoading: true);
        }

        /// <summary>
        /// Boot シーン離脱時のクリーンアップ処理。
        /// </summary>
        protected override void OnDestroy()
        {
            DebugLogger.Log("[BootScene] 破棄処理完了。");
            // ※ LoadingView はアプリ全体で常駐するため Dispose は呼び出さず維持します
        }
    }
}
