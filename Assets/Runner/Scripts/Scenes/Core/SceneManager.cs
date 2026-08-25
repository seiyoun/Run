/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: アプリケーション全体のシーン遷移を管理するマネージャークラスを定義する。
 */

using Shiyuan.Foundation.Scenes;

namespace Runner
{
    /// <summary>
    /// Runner プロジェクトのシーン遷移を統括するシングルトンマネージャー。
    /// </summary>
    public sealed class SceneManager : SceneManagerBase<SceneType>
    {
        /// <summary>
        /// SceneManager のインスタンスを取得する。
        /// </summary>
        public new static SceneManager Instance => (SceneManager)SceneManagerBase<SceneType>.Instance;

        /// <summary>
        /// シーン遷移後も SceneManager を常駐させる。
        /// </summary>
        protected override bool ShouldDontDestroyOnLoad => true;

        /// <summary>
        /// アプリケーション起動時に最初に読み込む初期シーン。
        /// </summary>
        protected override SceneType StartScene => SceneType.Boot;

        /// <summary>
        /// シーンステートマシンインスタンスを生成する。
        /// </summary>
        protected override SceneStateMachineBase<SceneType> CreateSceneStateMachine()
        {
            return new SceneStateMachine();
        }
    }
}
