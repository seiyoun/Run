/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: enum と Unity シーン読み込みを汎用ステートマシンへ接続する基底クラスを定義する。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Shiyuan.Foundation.Scenes
{
    public abstract class SceneStateMachineBase<TScene> : IDisposable where TScene : struct, Enum
    {
        private readonly StateMachine<TScene> stateMachine = new();
        private bool hasRegisteredStates;

        public TScene CurrentScene => stateMachine.CurrentState;
        public bool HasCurrentScene => stateMachine.HasCurrentState;
        public bool IsChangingScene => stateMachine.IsChangingState;

        /// <summary>
        /// プロジェクト固有のシーンステートを登録する。
        /// </summary>
        protected abstract void RegisterStates();

        /// <summary>
        /// シーンステートを登録する。
        /// </summary>
        protected void AddState(TScene scene, Func<ISceneLifecycle> createSceneLifecycle)
        {
            stateMachine.AddState(new SceneStateBase<TScene>(
                scene,
                createSceneLifecycle,
                LoadUnitySceneAsync,
                OnShowLoading,
                OnHideLoading));
        }

        /// <summary>
        /// ローディング表示を開始する。
        /// </summary>
        protected virtual void OnShowLoading()
        {
        }

        /// <summary>
        /// ローディング表示を終了する。
        /// </summary>
        protected virtual void OnHideLoading()
        {
        }

        /// <summary>
        /// 指定された開始シーンからステートマシンを開始する。
        /// </summary>
        public Task StartAsync(TScene startScene, bool shouldLoadStartScene, bool showLoading, CancellationToken cancellationToken)
        {
            EnsureRegisteredStates();
            SetShouldLoadScene(startScene, shouldLoadStartScene);
            return ChangeSceneAsync(startScene, null, showLoading, cancellationToken);
        }

        /// <summary>
        /// 指定されたシーンへ遷移する。
        /// </summary>
        public Task ChangeSceneAsync(TScene scene, bool showLoading, CancellationToken cancellationToken)
        {
            return ChangeSceneAsync(scene, null, showLoading, cancellationToken);
        }

        /// <summary>
        /// パラメータを渡して指定されたシーンへ遷移する。
        /// </summary>
        public async Task ChangeSceneAsync(TScene scene, object parameter, bool showLoading, CancellationToken cancellationToken)
        {
            EnsureRegisteredStates();
            ThrowIfSceneNameDoesNotMatch(scene);
            SetShouldShowLoading(scene, showLoading);
            await stateMachine.ChangeStateAsync(scene, parameter, cancellationToken);
        }

        /// <summary>
        /// 現在シーンの毎フレーム処理を実行する。
        /// </summary>
        public void Update(float deltaTime)
        {
            stateMachine.Update(deltaTime);
        }

        /// <summary>
        /// 現在シーンを破棄する。
        /// </summary>
        public void Dispose()
        {
            stateMachine.Dispose();
        }

        /// <summary>
        /// 次回開始時に Unity シーンを読み込むかどうかを設定する。
        /// </summary>
        private void SetShouldLoadScene(TScene scene, bool shouldLoadScene)
        {
            if (stateMachine.GetState(scene) is SceneStateBase<TScene> sceneState)
            {
                sceneState.ShouldLoadUnityScene = shouldLoadScene;
            }
        }

        /// <summary>
        /// 次回遷移時にローディング UI を表示するかどうかを設定する。
        /// </summary>
        private void SetShouldShowLoading(TScene scene, bool shouldShowLoading)
        {
            if (stateMachine.GetState(scene) is SceneStateBase<TScene> sceneState)
            {
                sceneState.ShouldShowLoading = shouldShowLoading;
            }
        }

        /// <summary>
        /// シーンステートが未登録なら登録する。
        /// </summary>
        private void EnsureRegisteredStates()
        {
            if (hasRegisteredStates)
            {
                return;
            }

            RegisterStates();
            hasRegisteredStates = true;
        }

        /// <summary>
        /// enum 値を Unity シーン名として使用できることを検証する。
        /// </summary>
        private static void ThrowIfSceneNameDoesNotMatch(TScene scene)
        {
            if (!Enum.IsDefined(typeof(TScene), scene))
            {
                throw new InvalidOperationException($"Scene enum and scene name must match: {scene}");
            }
        }

        /// <summary>
        /// enum 値と同名の Unity シーンを読み込む。
        /// </summary>
        private static async Task LoadUnitySceneAsync(TScene scene, CancellationToken cancellationToken)
        {
            var sceneName = scene.ToString();
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException($"Scene name and enum value must match. scene:{sceneName}");
            }

            var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            if (asyncOperation == null)
            {
                throw new InvalidOperationException($"Failed to load scene: {scene}");
            }

            while (!asyncOperation.isDone)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }
    }

    public sealed class SceneStateBase<TScene> : IState<TScene> where TScene : struct, Enum
    {
        private readonly Func<ISceneLifecycle> createSceneLifecycle;
        private readonly Func<TScene, CancellationToken, Task> loadUnitySceneAsync;
        private readonly Action showLoading;
        private readonly Action hideLoading;
        private ISceneLifecycle sceneLifecycle;
        private bool isShowingLoading;
        private object cachedParameter;

        public SceneStateBase(
            TScene state,
            Func<ISceneLifecycle> createSceneLifecycle,
            Func<TScene, CancellationToken, Task> loadUnitySceneAsync,
            Action showLoading,
            Action hideLoading)
        {
            State = state;
            this.createSceneLifecycle = createSceneLifecycle;
            this.loadUnitySceneAsync = loadUnitySceneAsync;
            this.showLoading = showLoading;
            this.hideLoading = hideLoading;
        }

        public TScene State { get; }
        public bool ShouldLoadUnityScene { get; set; } = true;
        public bool ShouldShowLoading { get; set; }

        /// <summary>
        /// パラメータを受け取り、Unity シーンを読み込み、シーンの通信完了を待機する。
        /// </summary>
        public async Task EnterAsync(object parameter, CancellationToken cancellationToken)
        {
            try
            {
                if (ShouldLoadUnityScene)
                {
                    ShowLoadingIfNeeded();
                    await loadUnitySceneAsync(State, cancellationToken);
                }

                ShouldLoadUnityScene = true;
                sceneLifecycle = createSceneLifecycle();
                cachedParameter = parameter;
                await sceneLifecycle.WaitForCommunicationAsync(cancellationToken);
            }
            catch
            {
                cachedParameter = null;
                HideLoadingIfNeeded();
                throw;
            }
        }

        /// <summary>
        /// シーンライフサイクルを初期化する。
        /// </summary>
        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            try
            {
                await sceneLifecycle.InitializeAsync(cachedParameter, cancellationToken);
            }
            finally
            {
                cachedParameter = null;
                HideLoadingIfNeeded();
            }
        }

        /// <summary>
        /// シーンの毎フレーム処理を実行する。
        /// </summary>
        public void Update(float deltaTime)
        {
            sceneLifecycle?.Update(deltaTime);
        }

        /// <summary>
        /// シーンライフサイクルを破棄する。
        /// </summary>
        public void Exit()
        {
            HideLoadingIfNeeded();
            sceneLifecycle?.Destroy();
            sceneLifecycle = null;
        }

        /// <summary>
        /// 設定されている場合にローディング UI を表示する。
        /// </summary>
        private void ShowLoadingIfNeeded()
        {
            if (!ShouldShowLoading || isShowingLoading)
            {
                return;
            }

            showLoading();
            isShowingLoading = true;
        }

        /// <summary>
        /// 表示中のローディング UI を非表示にし、次回設定を初期化する。
        /// </summary>
        private void HideLoadingIfNeeded()
        {
            if (isShowingLoading)
            {
                hideLoading();
                isShowingLoading = false;
            }

            ShouldShowLoading = false;
        }
    }
}
