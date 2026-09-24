/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: IDroppable エンティティからのドロップ要求を受け取り、ItemSpawner を介してアイテム生成を実行するドロップ管理マネージャー。
 */

using System;
using System.Threading;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// IDroppable エンティティのドロップ要求イベントを購読し、アイテムドロップを一元管理する Observer マネージャー。
    /// エネミー生成時に登録され、死亡・破壊時に ItemSpawner を呼び出してアイテムを生成します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropManager : SingletonMonoBehaviour<DropManager>
    {
        private const long DefaultDropCoinAmount = 10;

        [Header("Drop Settings")]
        [Tooltip("ドロップするコイン1枚あたりの金額")]
        [SerializeField] private long dropCoinAmount = DefaultDropCoinAmount;

        /// <summary>Game シーン破棄時に一緒に破棄させる</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<DropManager>.Instance != null;

        /// <summary>
        /// DropManager の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static DropManager Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<DropManager>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<DropManager>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(DropManager));
                return obj.AddComponent<DropManager>();
            }
        }

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// IDroppable エンティティのドロップ要求イベントを購読登録する。
        /// </summary>
        /// <param name="droppable">登録する IDroppable インスタンス</param>
        public void Register(IDroppable droppable)
        {
            if (droppable == null) return;

            droppable.OnDropRequested -= HandleDropRequested;
            droppable.OnDropRequested += HandleDropRequested;
        }

        /// <summary>
        /// IDroppable エンティティのドロップ要求イベント購読を解除する。
        /// </summary>
        /// <param name="droppable">解除する IDroppable インスタンス</param>
        public void Unregister(IDroppable droppable)
        {
            if (droppable == null) return;

            droppable.OnDropRequested -= HandleDropRequested;
        }

        /// <summary>
        /// ドロップ要求イベントを受信し、ItemSpawner を介して指定座標にコインを生成する。
        /// </summary>
        /// <param name="position">ドロップ発生ワールド座標</param>
        private void HandleDropRequested(Vector3 position)
        {
            if (ItemSpawner.HasInstance || ItemSpawner.Instance != null)
            {
                _ = ItemSpawner.Instance.SpawnMoneyItemAsync(position, dropCoinAmount, destroyCancellationToken);
            }
        }
    }
}

