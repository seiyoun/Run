/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: スマホのECアプリ風「即時ネット通販」画面モーダルUI。
 *                MVVMパターンのViewとしてViewModelからの通知を受け取り描画を更新します。
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// スマートフォンのECアプリ風「ネット通販タイムセール」モーダルウィンドウのViewコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SmartphoneShopModalView : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("モーダルウィンドウ全体のルートGameObject")]
        [SerializeField] private GameObject modalRoot;

        [Tooltip("所持ポイント表示テキスト")]
        [SerializeField] private TextMeshProUGUI userPointsText;

        [Tooltip("ショップモーダルを閉じるボタン")]
        [SerializeField] private Button closeButton;

        [Tooltip("商品カードの親コンテナ")]
        [SerializeField] private Transform itemContainer;

        [Header("Item Card References (3 Cards)")]
        [Tooltip("商品カードUI（3スロット）")]
        [SerializeField] private ShopItemCardUI[] itemCards = new ShopItemCardUI[3];

        private SmartphoneShopViewModel viewModel;
        private bool isInitialized;

        /// <summary>紐付けられているViewModelインスタンス</summary>
        public SmartphoneShopViewModel ViewModel
        {
            get
            {
                InitializeIfNeeded();
                return viewModel;
            }
        }

        /// <summary>ショップモーダルが開いているかどうか</summary>
        public bool IsOpen => viewModel != null && viewModel.IsOpen && modalRoot != null && modalRoot.activeSelf;

        /// <summary>アイテム購入時のコールバック (購入したアイテム)</summary>
        public event Action<ShopItemData> OnItemPurchased;

        /// <summary>ショップが閉じられた際のコールバック</summary>
        public event Action OnShopClosed;

        /// <summary>
        /// コンポーネントの初期化を行う。
        /// </summary>
        private void Awake()
        {
            InitializeIfNeeded();

            if (viewModel != null && !viewModel.IsOpen && modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        /// <summary>
        /// オブジェクト破棄時のイベント購読解除およびタイムスケール復元を行う。
        /// </summary>
        private void OnDestroy()
        {
            if (viewModel != null && viewModel.IsOpen)
            {
                Time.timeScale = 1f;
            }
            UnbindViewModel();
        }

        /// <summary>
        /// PointStepHUDをバインドする。
        /// </summary>
        /// <param name="hud">バインド対象のPointStepHUD</param>
        public void BindPointHUD(PointStepHUD hud)
        {
            InitializeIfNeeded();
            viewModel.BindPointHUD(hud);
        }

        /// <summary>
        /// タイムセールショップを開く。
        /// </summary>
        public void OpenShop()
        {
            InitializeIfNeeded();
            viewModel.OpenShop();
        }

        /// <summary>
        /// ショップを閉じる。
        /// </summary>
        public void CloseShop()
        {
            if (viewModel != null)
            {
                viewModel.CloseShop();
            }
            else if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        /// <summary>
        /// プロシージャルUI生成時に参照を一括設定する。
        /// </summary>
        /// <param name="root">モーダルのルートGameObject</param>
        /// <param name="userPoints">所持ポイントテキスト</param>
        /// <param name="closeBtn">閉じるボタン</param>
        /// <param name="cards">商品カード配列</param>
        public void SetupReferences(GameObject root, TextMeshProUGUI userPoints, Button closeBtn, ShopItemCardUI[] cards)
        {
            modalRoot = root;
            userPointsText = userPoints;
            closeButton = closeBtn;
            itemCards = cards;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);
            }
        }

        /// <summary>
        /// ViewModelおよびイベントの初期化を必要に応じて実行する。
        /// </summary>
        private void InitializeIfNeeded()
        {
            if (isInitialized) return;
            isInitialized = true;

            if (viewModel == null)
            {
                SetViewModel(new SmartphoneShopViewModel());
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);
            }
        }

        /// <summary>
        /// 新しいViewModelを設定し、イベントをバインドする。
        /// </summary>
        /// <param name="targetViewModel">バインド対象のViewModel</param>
        private void SetViewModel(SmartphoneShopViewModel targetViewModel)
        {
            UnbindViewModel();
            viewModel = targetViewModel;

            if (viewModel != null)
            {
                viewModel.OnOpenStateChanged += HandleOpenStateChanged;
                viewModel.OnOffersUpdated += HandleOffersUpdated;
                viewModel.OnPointsUpdated += HandlePointsUpdated;
                viewModel.OnItemPurchased += HandleItemPurchased;
                viewModel.OnShopClosed += HandleShopClosed;
            }
        }

        /// <summary>
        /// 既存ViewModelのイベント購読を解除する。
        /// </summary>
        private void UnbindViewModel()
        {
            if (viewModel == null) return;

            viewModel.OnOpenStateChanged -= HandleOpenStateChanged;
            viewModel.OnOffersUpdated -= HandleOffersUpdated;
            viewModel.OnPointsUpdated -= HandlePointsUpdated;
            viewModel.OnItemPurchased -= HandleItemPurchased;
            viewModel.OnShopClosed -= HandleShopClosed;
        }

        /// <summary>
        /// モーダルの開閉状態変化イベントを処理する。
        /// </summary>
        /// <param name="isOpen">開いている場合true</param>
        private void HandleOpenStateChanged(bool isOpen)
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(isOpen);
            }

            Time.timeScale = isOpen ? 0f : 1f;

            if (isOpen)
            {
                PlayerController.Instance?.Stop();
            }
        }

        /// <summary>
        /// 陳列商品の更新イベントを処理する。
        /// </summary>
        /// <param name="offers">陳列商品リスト</param>
        /// <param name="currentPoints">現在所持ポイント</param>
        private void HandleOffersUpdated(IReadOnlyList<ShopItemData> offers, long currentPoints)
        {
            for (int i = 0; i < itemCards.Length && i < 3; i++)
            {
                var card = itemCards[i];
                var item = i < offers.Count ? offers[i] : null;

                if (card != null && item != null)
                {
                    int cardIndex = i;
                    bool canAfford = currentPoints >= item.price;
                    card.Bind(item, canAfford, () => viewModel.BuyItem(cardIndex));
                }
            }
        }

        /// <summary>
        /// 所持ポイント更新イベントを処理する。
        /// </summary>
        /// <param name="points">最新の所持ポイント</param>
        private void HandlePointsUpdated(long points)
        {
            if (userPointsText != null)
            {
                userPointsText.text = $"所持ポイント: <color=#FFD700><b>¥{points:N0} pt</b></color>";
            }
        }

        /// <summary>
        /// アイテム購入イベントを処理し、外部コールバックを発火する。
        /// </summary>
        /// <param name="item">購入されたアイテム</param>
        private void HandleItemPurchased(ShopItemData item)
        {
            OnItemPurchased?.Invoke(item);
        }

        /// <summary>
        /// ショップ閉鎖イベントを処理し、外部コールバックを発火する。
        /// </summary>
        private void HandleShopClosed()
        {
            OnShopClosed?.Invoke();
        }
    }
}
