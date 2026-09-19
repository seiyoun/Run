/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: MonoBehaviour のシングルトン基底クラスを定義する。
 */

using UnityEngine;

namespace Shiyuan.Foundation.Core
{
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        public static T Instance { get; private set; }

        protected bool IsPrimaryInstance => Instance == this;

        /// <summary>
        /// 正規インスタンスをシーン遷移後も保持するかどうかを取得する。
        /// </summary>
        protected virtual bool ShouldDontDestroyOnLoad => true;

        /// <summary>
        /// インスタンスを登録し、重複インスタンスを破棄する。
        /// </summary>
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            if (ShouldDontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// 破棄時に静的参照を解除する。
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
