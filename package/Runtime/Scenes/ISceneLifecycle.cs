/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: シーン処理で使用するライフサイクル処理を定義する。
 */

using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Shiyuan.Foundation.Scenes
{
    public interface ISceneLifecycle
    {
        /// <summary>
        /// 通信完了後に、遷移パラメータを受け取り、このシーンで使用するリソースと状態を初期化する。
        /// </summary>
        Task InitializeAsync(object parameter, CancellationToken cancellationToken);

        /// <summary>
        /// シーンの初期化（InitializeAsync）前に、このシーンで必要な通信完了を待機する。
        /// </summary>
        Task WaitForCommunicationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// このシーンの毎フレーム処理を実行する。
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// このシーンが保持するリソースと状態を解放する。
        /// </summary>
        void Destroy();
    }

    public abstract class SceneLifecycleBase : ISceneLifecycle
    {
        private bool hasLoggedInitialize;
        private bool hasLoggedCommunication;
        private bool hasLoggedUpdate;
        private bool hasLoggedDestroy;

        /// <summary>
        /// 通信完了後に、遷移パラメータを受け取り、このシーンで使用するリソースと状態を初期化し、一度だけログを出力する。
        /// </summary>
        public Task InitializeAsync(object parameter, CancellationToken cancellationToken)
        {
            LogOnce(ref hasLoggedInitialize, "初期化");
            return OnInitializeAsync(parameter, cancellationToken);
        }

        /// <summary>
        /// シーンの初期化（InitializeAsync）前に、このシーンで必要な通信完了を待機し、一度だけログを出力する。
        /// </summary>
        public Task WaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            LogOnce(ref hasLoggedCommunication, "通信待機");
            return OnWaitForCommunicationAsync(cancellationToken);
        }

        /// <summary>
        /// このシーンの毎フレーム処理を実行し、最初の実行時だけログを出力する。
        /// </summary>
        public void Update(float deltaTime)
        {
            LogOnce(ref hasLoggedUpdate, "更新");
            OnUpdate(deltaTime);
        }

        /// <summary>
        /// このシーンが保持するリソースと状態を解放し、一度だけログを出力する。
        /// </summary>
        public void Destroy()
        {
            LogOnce(ref hasLoggedDestroy, "破棄");
            OnDestroy();
        }

        /// <summary>
        /// 通信完了後に、遷移パラメータを受け取り、このシーン固有の初期化処理を実行する。
        /// </summary>
        protected abstract Task OnInitializeAsync(object parameter, CancellationToken cancellationToken);

        /// <summary>
        /// シーンの初期化（InitializeAsync）前に、このシーン固有の通信待機処理を実行する。
        /// </summary>
        protected virtual Task OnWaitForCommunicationAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// このシーン固有の更新処理を実行する。
        /// </summary>
        protected virtual void OnUpdate(float deltaTime)
        {
            OnUpdate();
        }

        /// <summary>
        /// このシーン固有の更新処理を実行する。
        /// </summary>
        protected virtual void OnUpdate()
        {
        }

        /// <summary>
        /// このシーン固有の破棄処理を実行する。
        /// </summary>
        protected abstract void OnDestroy();

        /// <summary>
        /// 指定されたライフサイクル段階のログを一度だけ出力する。
        /// </summary>
        private void LogOnce(ref bool hasLogged, string lifecycleName)
        {
            if (hasLogged)
            {
                return;
            }

            hasLogged = true;
#if DEBUG_LOG && SCENE_LOG
            Debug.Log($"{GetSceneName()} シーン: {lifecycleName}");
#endif
        }

        /// <summary>
        /// ログ表示用のシーン名を取得する。
        /// </summary>
        private string GetSceneName()
        {
            const string suffix = "Scene";
            var sceneName = GetType().Name;
            if (sceneName.EndsWith(suffix))
            {
                return sceneName.Substring(0, sceneName.Length - suffix.Length);
            }

            return sceneName;
        }
    }
}
