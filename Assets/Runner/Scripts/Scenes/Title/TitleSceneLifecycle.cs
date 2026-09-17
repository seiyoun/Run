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

        /// <summary>
        /// タイトルシーン開始前の通信待機およびマスターデータの事前ロードを行う。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>待機タスク</returns>
        protected override async Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            DebugLogger.Log("[TitleScene] タイトルリソースの準備中...");
            await MasterDataManager.InitializeAsync();
        }

        /// <summary>
        /// タイトルシーンの初期化および TitleView のイベント購読を行う。
        /// </summary>
        /// <param name="parameter">初期化パラメータ</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>完了タスク</returns>
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

        /// <summary>
        /// 毎フレームの更新処理。
        /// </summary>
        protected override void OnUpdate()
        {
        }

        /// <summary>
        /// シーン破棄時のクリーンアップ処理を行う。
        /// </summary>
        protected override void OnDestroy()
        {
            if (titleView != null)
            {
                titleView.OnStartClicked -= HandleStartClicked;
                titleView = null;
            }

            DebugLogger.Log("[TitleScene] 破棄処理完了。");
        }

        /// <summary>
        /// スタートボタン押下時に Home シーンへの遷移を実行する。
        /// </summary>
        private async void HandleStartClicked()
        {
            await SceneManager.Instance.ChangeScene(SceneType.Home);
        }
    }
}
