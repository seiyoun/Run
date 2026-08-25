/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Title シーンのライフサイクル管理処理を定義する。
 */

using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using Shiyuan.Foundation.Scenes;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// Title シーンにおけるライフサイクル管理。TitleView のボタンイベントを購読して Home シーンへ遷移する。
    /// </summary>
    public sealed class TitleSceneLifecycle : SceneLifecycleBase
    {
        private TitleView titleView;

        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[TitleScene] タイトルリソースの準備中...");
            await Task.Yield();
        }

        protected override async Task OnInitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            DebugLogger.Log("[TitleScene] 初期化完了。Title 画面を表示します。");

            titleView = Object.FindFirstObjectByType<TitleView>();
            if (titleView != null)
            {
                titleView.OnStartClicked += HandleStartClicked;
            }

            await Task.Yield();
        }

        private async void HandleStartClicked()
        {
            await SceneManager.Instance.ChangeScene(SceneType.Home);
        }

        protected override void OnUpdate()
        {
        }

        protected override void OnDestroy()
        {
            if (titleView != null)
            {
                titleView.OnStartClicked -= HandleStartClicked;
                titleView = null;
            }

            DebugLogger.Log("[TitleScene] 破棄処理完了。");
        }
    }
}
