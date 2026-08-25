/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Runner プロジェクトのシーン遷移ステートマシンを定義する。
 */

using Shiyuan.Foundation.Scenes;

namespace Runner
{
    /// <summary>
    /// 各シーンのライフサイクル管理クラスを登録し、シーン遷移を制御するステートマシン。
    /// </summary>
    public sealed class SceneStateMachine : SceneStateMachineBase<SceneType>
    {
        /// <summary>
        /// アプリケーションで使用する各シーンのステートとライフサイクル生成処理を登録する。
        /// </summary>
        protected override void RegisterStates()
        {
            AddState(SceneType.Boot, () => new BootSceneLifecycle());
            AddState(SceneType.Title, () => new TitleSceneLifecycle());
            AddState(SceneType.Home, () => new HomeSceneLifecycle());
            AddState(SceneType.Game, () => new GameSceneLifecycle());
        }

        /// <summary>
        /// シーン遷移時のローディング表示を開始する。
        /// </summary>
        protected override void OnShowLoading()
        {
            base.OnShowLoading();
            LoadingView.Instance?.Show();
        }

        /// <summary>
        /// シーン遷移時のローディング表示を終了する。
        /// </summary>
        protected override void OnHideLoading()
        {
            base.OnHideLoading();
            LoadingView.Instance?.Hide();
        }
    }
}