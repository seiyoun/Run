/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タイムセールショップの商品カード1スロット分のUIバインディングを担当するクラス。
 */

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 商品カード1枚分のUI要素参照およびデータ反映を担うUIコンポーネント。
    /// </summary>
    [Serializable]
    public class ShopItemCardUI
    {
        [Tooltip("商品カード全体のルートGameObject")]
        public GameObject cardRoot;

        [Tooltip("アイコン・絵文字テキスト")]
        public TextMeshProUGUI iconText;

        [Tooltip("アイテム名テキスト")]
        public TextMeshProUGUI nameText;

        [Tooltip("アイテム説明テキスト")]
        public TextMeshProUGUI descText;

        [Tooltip("アイテム価格テキスト")]
        public TextMeshProUGUI priceText;

        [Tooltip("購入ボタン")]
        public Button buyButton;

        [Tooltip("購入ボタン内のラベルテキスト")]
        public TextMeshProUGUI buyButtonText;

        /// <summary>
        /// 商品データおよび購入可否状態をカードUIにバインドする。
        /// </summary>
        /// <param name="item">表示対象の商品データ</param>
        /// <param name="canAfford">購入可能（ポイントが足りている）かどうか</param>
        /// <param name="onBuy">購入ボタン押下時のコールバック</param>
        public void Bind(ShopItemData item, bool canAfford, Action onBuy)
        {
            if (item == null)
            {
                if (cardRoot != null) cardRoot.SetActive(false);
                return;
            }

            if (cardRoot != null) cardRoot.SetActive(true);
            if (iconText != null)
            {
                iconText.text = item.iconEmoji;
                iconText.enableAutoSizing = true;
                iconText.fontSizeMin = 14f;
                iconText.fontSizeMax = 32f;
                iconText.textWrappingMode = TextWrappingModes.NoWrap;
            }
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
