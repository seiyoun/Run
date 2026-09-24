/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: 画面左上に配置されるバフアイコン・残り持続時間の表示を行う純粋なHUDビューコンポーネント。
 *                複数バフが存在する場合は縦方向に動的展開して表示します。
 */

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// 画面左上に配置されるバフ情報表示HUD（View）。
    /// ゲームロジックやプレイヤー参照は保持せず、外部から渡されたバフ情報の描画に専念します。
    /// 複数のバフが付与されている場合は縦方向に並べて複数表示します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuffHUD : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("バフ表示テンプレートオブジェクト（スロットの複製元）")]
        [SerializeField] private GameObject contentRoot;

        [Tooltip("テンプレートのバフアイコン画像コンポーネント")]
        [SerializeField] private Image iconImage;

        [Tooltip("テンプレートのクールダウンオーバーレイ画像（Filledタイプ）")]
        [SerializeField] private Image cooldownOverlayImage;

        [Tooltip("テンプレートの残り効果時間テキスト")]
        [SerializeField] private TextMeshProUGUI durationText;

        private readonly List<BuffSlot> slotPool = new List<BuffSlot>();
        private int activeDisplayCount;

        /// <summary>現在バフアイコンが1つ以上表示中かどうか</summary>
        public bool IsShowing
        {
            get
            {
                for (int i = 0; i < slotPool.Count; i++)
                {
                    if (slotPool[i].Root != null && slotPool[i].Root.activeSelf)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 初期表示状態の設定および初期スロットの登録を行う。
        /// </summary>
        private void Awake()
        {
            EnsureInitialSlot();
            ClearBuffs();
        }

        /// <summary>
        /// バフ表示の更新を開始する。
        /// </summary>
        public void BeginUpdate()
        {
            EnsureInitialSlot();
            activeDisplayCount = 0;
        }

        /// <summary>
        /// バフ描画情報を1件追加・更新する。
        /// </summary>
        /// <param name="icon">バフアイコンスプライト</param>
        /// <param name="remainingDuration">残り効果時間（秒）</param>
        /// <param name="totalDuration">総効果持続時間（秒）</param>
        public void AddBuffDisplay(Sprite icon, float remainingDuration, float totalDuration)
        {
            if (remainingDuration <= 0f) return;

            while (slotPool.Count <= activeDisplayCount)
            {
                CreateNewSlot();
            }

            slotPool[activeDisplayCount].SetData(icon, remainingDuration, totalDuration);
            activeDisplayCount++;
        }

        /// <summary>
        /// バフ表示の更新を完了し、未使用のスロットを非表示にする。
        /// </summary>
        public void EndUpdate()
        {
            for (int i = activeDisplayCount; i < slotPool.Count; i++)
            {
                slotPool[i].Hide();
            }
        }

        /// <summary>
        /// 単一バフのアイコン、残り時間および総持続時間を設定し、表示を更新する。
        /// </summary>
        /// <param name="icon">バフアイコンスプライト</param>
        /// <param name="remainingDuration">残り効果時間（秒）</param>
        /// <param name="totalDuration">総効果持続時間（秒）</param>
        public void SetBuff(Sprite icon, float remainingDuration, float totalDuration)
        {
            BeginUpdate();
            if (remainingDuration > 0f)
            {
                AddBuffDisplay(icon, remainingDuration, totalDuration);
            }
            EndUpdate();
        }

        /// <summary>
        /// 単一バフの残り時間および総持続時間を設定し、表示を更新する。
        /// </summary>
        /// <param name="remainingDuration">残り効果時間（秒）</param>
        /// <param name="totalDuration">総効果持続時間（秒）</param>
        public void SetBuff(float remainingDuration, float totalDuration)
        {
            SetBuff(null, remainingDuration, totalDuration);
        }

        /// <summary>
        /// 全てのバフ表示を非表示にする。
        /// </summary>
        public void ClearBuffs()
        {
            activeDisplayCount = 0;
            for (int i = 0; i < slotPool.Count; i++)
            {
                slotPool[i].Hide();
            }

            if (contentRoot != null && contentRoot.activeSelf)
            {
                contentRoot.SetActive(false);
            }
        }

        /// <summary>
        /// バフ表示を非表示にする（後方互換エイリアス）。
        /// </summary>
        public void ClearBuff()
        {
            ClearBuffs();
        }

        /// <summary>
        /// コード生成時等のUI参照バインドを行う。
        /// </summary>
        /// <param name="root">表示ルートオブジェクト</param>
        /// <param name="icon">アイコンImage</param>
        /// <param name="cooldownOverlay">クールダウンオーバーレイImage</param>
        /// <param name="text">残り時間Text</param>
        public void SetupReferences(GameObject root, Image icon, Image cooldownOverlay, TextMeshProUGUI text)
        {
            contentRoot = root;
            iconImage = icon;
            cooldownOverlayImage = cooldownOverlay;
            durationText = text;
            slotPool.Clear();
            EnsureInitialSlot();
        }

        /// <summary>
        /// 初期テンプレートオブジェクトを最初のスロットとして登録する。
        /// </summary>
        private void EnsureInitialSlot()
        {
            if (slotPool.Count == 0 && contentRoot != null)
            {
                slotPool.Add(new BuffSlot
                {
                    Root = contentRoot,
                    Icon = iconImage,
                    CooldownOverlay = cooldownOverlayImage,
                    DurationText = durationText
                });
            }
        }

        /// <summary>
        /// テンプレートを複製して新しいバフスロットをプールへ追加する。
        /// </summary>
        private void CreateNewSlot()
        {
            if (contentRoot == null) return;

            var newRoot = Instantiate(contentRoot, transform, false);
            newRoot.name = $"BuffSlot_{slotPool.Count}";
            var icon = newRoot.transform.Find("Icon")?.GetComponent<Image>();
            var overlay = newRoot.transform.Find("Icon/CooldownOverlay")?.GetComponent<Image>();
            var text = newRoot.transform.Find("DurationText")?.GetComponent<TextMeshProUGUI>();

            var slot = new BuffSlot
            {
                Root = newRoot,
                Icon = icon,
                CooldownOverlay = overlay,
                DurationText = text
            };
            slotPool.Add(slot);
        }

        /// <summary>
        /// 内部で単一バフスロットのUI参照と更新処理を管理するクラス。
        /// </summary>
        private sealed class BuffSlot
        {
            public GameObject Root;
            public Image Icon;
            public Image CooldownOverlay;
            public TextMeshProUGUI DurationText;

            /// <summary>
            /// バフデータをスロットへ反映してアクティブ化する。
            /// </summary>
            /// <param name="icon">バフアイコンスプライト</param>
            /// <param name="remainingDuration">残り効果時間（秒）</param>
            /// <param name="totalDuration">総効果持続時間（秒）</param>
            public void SetData(Sprite icon, float remainingDuration, float totalDuration)
            {
                if (Root != null && !Root.activeSelf)
                {
                    Root.SetActive(true);
                }

                if (Icon != null && icon != null)
                {
                    Icon.sprite = icon;
                }

                if (DurationText != null)
                {
                    DurationText.text = $"{remainingDuration:F1}s";
                }

                if (CooldownOverlay != null && totalDuration > 0f)
                {
                    CooldownOverlay.fillAmount = Mathf.Clamp01(1f - (remainingDuration / totalDuration));
                }
            }

            /// <summary>
            /// スロットを非アクティブ化して非表示にする。
            /// </summary>
            public void Hide()
            {
                if (Root != null && Root.activeSelf)
                {
                    Root.SetActive(false);
                }
            }
        }
    }
}
