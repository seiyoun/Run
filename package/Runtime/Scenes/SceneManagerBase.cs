/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: シーン遷移とシーンライフサイクルの Update 呼び出しを管理する基底クラスを定義する。
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Core;
using Debug = UnityEngine.Debug;

namespace Shiyuan.Foundation.Scenes
{
    public abstract class SceneManagerBase<TScene> : SingletonMonoBehaviour<SceneManagerBase<TScene>> where TScene : struct, Enum
    {
        private SceneStateMachineBase<TScene> sceneStateMachine;
        private CancellationTokenSource sceneCancellationTokenSource;

        public TScene CurrentScene => sceneStateMachine != null && sceneStateMachine.HasCurrentScene ? sceneStateMachine.CurrentScene : StartScene;
        public bool IsChangingScene => sceneStateMachine?.IsChangingScene ?? false;

        protected override bool ShouldDontDestroyOnLoad => false;
        protected abstract TScene StartScene { get; }

        /// <summary>
        /// プロジェクト固有のシーンステートマシンを作成する。
        /// </summary>
        protected abstract SceneStateMachineBase<TScene> CreateSceneStateMachine();

        /// <summary>
        /// 指定されたシーンへ遷移し、シーンライフサイクルを順番に実行する。
        /// </summary>
        public Task ChangeScene(TScene scene)
        {
            return ChangeScene(scene, null, false);
        }

        /// <summary>
        /// ローディング表示の有無を指定してシーンへ遷移する。
        /// </summary>
        public Task ChangeScene(TScene scene, bool showLoading)
        {
            return ChangeScene(scene, null, showLoading);
        }

        /// <summary>
        /// パラメータを渡して指定されたシーンへ遷移する。
        /// </summary>
        public Task ChangeScene(TScene scene, object parameter)
        {
            return ChangeScene(scene, parameter, false);
        }

        /// <summary>
        /// パラメータとローディング表示の有無を指定してシーンへ遷移する。
        /// </summary>
        public async Task ChangeScene(TScene scene, object parameter, bool showLoading)
        {
            try
            {
                await sceneStateMachine.ChangeSceneAsync(scene, parameter, showLoading, GetSceneCancellationToken());
            }
            catch (OperationCanceledException)
            {
#if DEBUG_LOG
                Debug.LogWarning($"Scene change was canceled. requested:{scene}");
#endif
            }
            catch (Exception exception)
            {
#if DEBUG_LOG
                Debug.LogError($"Scene change failed. requested:{scene} error:{exception.Message}");
#endif
                throw;
            }
        }

        /// <summary>
        /// 現在のシーンへ毎フレーム処理を委譲する。
        /// </summary>
        private void Update()
        {
            sceneStateMachine?.Update(UnityEngine.Time.deltaTime);
        }

        /// <summary>
        /// このマネージャーの破棄時に、現在のシーンとキャンセル元を解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!IsPrimaryInstance)
            {
                return;
            }

            sceneCancellationTokenSource?.Cancel();
            sceneCancellationTokenSource?.Dispose();
            sceneStateMachine?.Dispose();
            base.OnDestroy();
        }

        /// <summary>
        /// シーンステートマシンを初期化する。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance)
            {
                return;
            }

            sceneStateMachine = CreateSceneStateMachine();
        }

        /// <summary>
        /// Unity が最初に読み込んだシーンのライフサイクルを初期化する。
        /// </summary>
        private async void Start()
        {
            if (!IsPrimaryInstance)
            {
                return;
            }

            if (sceneStateMachine.HasCurrentScene || sceneStateMachine.IsChangingScene)
            {
                return;
            }

            var activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var shouldLoadStartScene = activeSceneName != StartScene.ToString();

            try
            {
                await sceneStateMachine.StartAsync(StartScene, shouldLoadStartScene, false, GetSceneCancellationToken());
            }
            catch (OperationCanceledException)
            {
#if DEBUG_LOG
                Debug.LogWarning($"Initial scene setup was canceled. scene:{StartScene}");
#endif
            }
            catch (Exception exception)
            {
#if DEBUG_LOG
                Debug.LogError($"Initial scene setup failed. scene:{StartScene} error:{exception.Message}");
#endif
                throw;
            }
        }

        /// <summary>
        /// シーン遷移用のキャンセルトークンを取得する。
        /// </summary>
        private CancellationToken GetSceneCancellationToken()
        {
            sceneCancellationTokenSource ??= new CancellationTokenSource();
            return sceneCancellationTokenSource.Token;
        }
    }
}
