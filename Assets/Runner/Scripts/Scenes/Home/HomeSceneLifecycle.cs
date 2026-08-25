/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Home（メイン）シーンのライフサイクル管理処理を定義する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using Shiyuan.Foundation.Scenes;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Home（メイン）シーンにおけるライフサイクル管理。HomeView のボタンイベントを購読して Game シーンへ遷移する。
    /// </summary>
    public sealed class HomeSceneLifecycle : SceneLifecycleBase
    {
        private HomeView homeView;

        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[HomeScene] 通信・ホームデータの読み込み待機中...");
            await Task.Yield();
        }

        protected override async Task OnInitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[HomeScene] 初期化完了。Home（メイン）画面を表示します。");

            homeView = Object.FindFirstObjectByType<HomeView>();
            if (homeView != null)
            {
                homeView.OnPlayClicked += HandlePlayClicked;
            }

            await Task.Yield();
        }

        private async void HandlePlayClicked()
        {
            await SceneManager.Instance.ChangeScene(SceneType.Game, showLoading: true);
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            if (homeView != null)
            {
                homeView.OnPlayClicked -= HandlePlayClicked;
                homeView = null;
            }

            DebugLogger.Log("[HomeScene] 破棄処理完了。");
        }
    }
}
