/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: アプリケーション全体のシーン遷移を管理するマネージャークラスを定義する。
 *                AddressablePrefabLoader を保持し、常駐 UI（LoadingView）のロードと破棄時 Dispose を一元管理します。
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
    /// Runner プロジェクトのシーン遷移を統括するシングルトンマネージャー。
    /// AddressablePrefabLoader を保持し、常駐 UI（LoadingView）の生成と解放（Dispose）を一元管理します。
    /// </summary>
    public sealed class SceneManager : SceneManagerBase<SceneType>
    {
        // -------------------------------------------------------------
        // 1. const / static フィールド
        // -------------------------------------------------------------
        private const string LoadingViewAddress = "LoadingView";

        public new static SceneManager Instance => (SceneManager)SceneManagerBase<SceneType>.Instance;

        // -------------------------------------------------------------
        // 2. [SerializeField] シリアライズフィールド
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 3. private インスタンス変数
        // -------------------------------------------------------------
        private AddressablePrefabLoader loadingViewLoader;

        // -------------------------------------------------------------
        // 4. public インスタンス変数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 5. プロパティ & イベント
        // -------------------------------------------------------------
        protected override bool ShouldDontDestroyOnLoad => true;
        protected override SceneType StartScene => SceneType.Boot;

        // -------------------------------------------------------------
        // 6. Unity ライフサイクル関数
        // -------------------------------------------------------------

        /// <summary>
        /// シングルトンの初期化を行い、プライマリインスタンスであれば AddressablePrefabLoader を生成する。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            if (IsPrimaryInstance)
            {
                loadingViewLoader = new AddressablePrefabLoader();
            }
        }

        /// <summary>
        /// マネージャー破棄時に AddressablePrefabLoader を Dispose してアセットを解放する。
        /// </summary>
        protected override void OnDestroy()
        {
            if (loadingViewLoader != null)
            {
                loadingViewLoader.Dispose();
                loadingViewLoader = null;
            }

            base.OnDestroy();
        }

        // -------------------------------------------------------------
        // 7. override 関数
        // -------------------------------------------------------------

        /// <summary>
        /// シーンステートマシンインスタンスを生成する。
        /// </summary>
        /// <returns>生成された SceneStateMachineBase</returns>
        protected override SceneStateMachineBase<SceneType> CreateSceneStateMachine()
        {
            return new SceneStateMachine();
        }

        // -------------------------------------------------------------
        // 8. public 関数
        // -------------------------------------------------------------

        /// <summary>
        /// AddressablePrefabLoader を用いて LoadingView プレハブをロード・生成し、常駐化させる。
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        public async Task EnsureLoadingViewAsync(CancellationToken cancellationToken = default)
        {
            if (LoadingView.Instance != null)
            {
                return;
            }

            loadingViewLoader ??= new AddressablePrefabLoader();

            try
            {
                var loadingObj = await loadingViewLoader.LoadAsync(LoadingViewAddress, cancellationToken);
                if (loadingObj != null)
                {
                    DontDestroyOnLoad(loadingObj);
                    DebugLogger.Log("[SceneManager] LoadingView を AddressablePrefabLoader からロード・常駐化しました。");
                }
            }
            catch (System.Exception ex)
            {
                DebugLogger.Error($"[SceneManager] LoadingView のロードに失敗しました: {ex.Message}");
            }
        }

        // -------------------------------------------------------------
        // 9. private 関数 / 内部ヘルパー
        // -------------------------------------------------------------
    }
}
