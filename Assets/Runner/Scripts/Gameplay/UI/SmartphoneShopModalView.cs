/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: スマホのECアプリ風「即時ネット通販」画面モーダルUI。
 *                ランダムな3つの商品（ドローン、ボディガード、回復薬など）を提示し、1つ購入すると即時反映して閉じます。
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    public enum ShopItemType
    {
        Drone,        // 自動護衛ドローン
        Bodyguard,    // 屈強なボディガード
        EnergyDrink,  // スタミナ栄養ドリンク (HP回復)
        SpeedSneakers,// 超軽量ランニングシューズ (移動速度UP)
        PointMagnet,  // 強力ポイ活マグネット (コイン/ポイント吸引)
        BarrierShield // 身代わりシールド (被弾無効)
    }

    [Serializable]
    public class ShopItemData
    {
        public string id;
        public string itemName;
        public string description;
        public string iconEmoji;
        public int price;
        public ShopItemType itemType;

        public ShopItemData(string id, string name, string desc, string emoji, int price, ShopItemType type)
        {
            this.id = id;
            this.itemName = name;
            this.description = desc;
            this.iconEmoji = emoji;
            this.price = price;
            this.itemType = type;
        }
    }

    /// <summary>
    /// スマートフォンのECアプリ風「ネット通販タイムセール」モーダルウィンドウ。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SmartphoneShopModalView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI userPointsText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform itemContainer;

        [Header("Item Card References (3 Cards)")]
        [SerializeField] private ShopItemCardUI[] itemCards = new ShopItemCardUI[3];

        private List<ShopItemData> availableItemPool = new List<ShopItemData>();
        private ShopItemData[] currentOfferedItems = new ShopItemData[3];
        private PointStepHUD pointStepHUD;

        public bool IsOpen => modalRoot != null && modalRoot.activeSelf;

        /// <summary>アイテム購入時のコールバック (購入したアイテム)</summary>
        public event Action<ShopItemData> OnItemPurchased;
        public event Action OnShopClosed;

        private void Awake()
        {
            InitializeItemPool();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }
        }

        private void InitializeItemPool()
        {
            availableItemPool.Clear();
            availableItemPool.Add(new ShopItemData("drone", "追従自律ドローン", "周囲のぶつかり屋を自動索敵して撃退する", "[DRONE]", 300, ShopItemType.Drone));
            availableItemPool.Add(new ShopItemData("bodyguard", "専属ボディガード", "プレイヤーにピッタリ密着して敵をタックルで吹き飛ばす", "[GUARD]", 500, ShopItemType.Bodyguard));
            availableItemPool.Add(new ShopItemData("energy_drink", "メガエナジードリンク", "体力を即座に全快にし、一定時間怒りゲージ上昇UP", "[DRINK]", 200, ShopItemType.EnergyDrink));
            availableItemPool.Add(new ShopItemData("sneakers", "エアジェットスニーカー", "移動速度が恒久的に25%アップし、回避しやすくなる", "[SPEED]", 350, ShopItemType.SpeedSneakers));
            availableItemPool.Add(new ShopItemData("magnet", "超電導ポイ活マグネット", "周囲に落ちているポイントやアイテムを一瞬で引き寄せる", "[MAGNET]", 250, ShopItemType.PointMagnet));
            availableItemPool.Add(new ShopItemData("shield", "ワンタイムガード保険", "ぶつかり屋との衝突ダメージを1度だけ完全に無効化する", "[SHIELD]", 400, ShopItemType.BarrierShield));
        }

        /// <summary>
        /// PointStepHUDをバインドする。
        /// </summary>
        public void BindPointHUD(PointStepHUD hud)
        {
            pointStepHUD = hud;
        }

        /// <summary>
        /// タイムセールショップを開き、ランダムな3つの商品を陳列する。
        /// </summary>
        public void OpenShop()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(true);
            }

            UpdateUserPointsDisplay();
            PickRandomItems();
            SetupCards();
        }

        /// <summary>
        /// ショップを閉じる。
        /// </summary>
        public void CloseShop()
        {
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            OnShopClosed?.Invoke();
        }

        private void PickRandomItems()
        {
            // プールから重複なしで3つ選出
            var poolCopy = new List<ShopItemData>(availableItemPool);
            for (int i = 0; i < 3; i++)
            {
                if (poolCopy.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, poolCopy.Count);
                    currentOfferedItems[i] = poolCopy[randomIndex];
                    poolCopy.RemoveAt(randomIndex);
                }
                else
                {
                    currentOfferedItems[i] = availableItemPool[i % availableItemPool.Count];
                }
            }
        }

        private void SetupCards()
        {
            long currentPoints = pointStepHUD != null ? pointStepHUD.CurrentPoint : 0;

            for (int i = 0; i < itemCards.Length && i < 3; i++)
            {
                var card = itemCards[i];
                var item = currentOfferedItems[i];

                if (card != null && item != null)
                {
                    int cardIndex = i;
                    bool canAfford = currentPoints >= item.price;
                    card.Bind(item, canAfford, () => HandleBuyItem(cardIndex));
                }
            }
        }

        private void HandleBuyItem(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= currentOfferedItems.Length) return;
            var item = currentOfferedItems[cardIndex];
            if (item == null) return;

            // ポイント消費処理
            if (pointStepHUD != null)
            {
                if (!pointStepHUD.TryConsumePoints(item.price))
                {
                    Debug.LogWarning($"[SmartphoneShop] ポイントが不足しています: 必要 {item.price} pt");
                    return;
                }
            }

            Debug.Log($"[SmartphoneShop] 商品購入完了: {item.itemName} (¥{item.price}pt)");
            OnItemPurchased?.Invoke(item);

            // 購入後、即座にショップ画面を閉じる（仕様）
            CloseShop();
        }

        private void UpdateUserPointsDisplay()
        {
            if (userPointsText != null)
            {
                long pt = pointStepHUD != null ? pointStepHUD.CurrentPoint : 0;
                userPointsText.text = $"所持ポイント: <color=#FFD700><b>¥{pt:N0} pt</b></color>";
            }
        }

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
    }

    /// <summary>
    /// 商品カード1枚分のUIコンポーネント
    /// </summary>
    [Serializable]
    public class ShopItemCardUI
    {
        public GameObject cardRoot;
        public TextMeshProUGUI iconText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI priceText;
        public Button buyButton;
        public TextMeshProUGUI buyButtonText;

        public void Bind(ShopItemData item, bool canAfford, Action onBuy)
        {
            if (iconText != null) iconText.text = item.iconEmoji;
            if (nameText != null) nameText.text = item.itemName;
            if (descText != null) descText.text = item.description;
            if (priceText != null) priceText.text = $"¥{item.price:N0} <size=70%>pt</size>";

            if (buyButton != null)
            {
                buyButton.interactable = canAfford;
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => onBuy?.Invoke());
            }

            if (buyButtonText != null)
            {
                buyButtonText.text = canAfford ? "即時購入" : "ポイント不足";
            }
        }
    }
}

