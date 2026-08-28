/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: タッチした位置に表示され、指を離すと非表示になるフローティング対応バーチャルジョイスティックUI。
 *                インスペクター上で設定されたコンポーネント参照を使用します。
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runner
{
    /// <summary>
    /// ジョイスティックの表示・動作モード。
    /// </summary>
    public enum JoystickMode
    {
        /// <summary>固定位置で常時表示</summary>
        Fixed,
        /// <summary>タッチ位置に出現し、離すと消える</summary>
        Floating,
    }

    /// <summary>
    /// モバイル/タッチ操作用のバーチャルジョイスティックUIを管理するコンポーネント。
    /// タッチ座標への出現、ドラッグ操作、フェード制御を行います。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class VirtualJoystickView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float DefaultMovementRange = 50f;

        [Header("Mode Settings")]
        [Tooltip("ジョイスティックの動作モード")]
        [SerializeField] private JoystickMode joystickMode = JoystickMode.Floating;

        [Tooltip("スティックの移動可能半径（ピクセル）")]
        [SerializeField] private float movementRange = DefaultMovementRange;

        [Header("UI References")]
        [Tooltip("ジョイスティックのコンテナ（背景とノブをまとめたRectTransform）")]
        [SerializeField] private RectTransform containerRect;

        [Tooltip("ジョイスティックの背景イメージ")]
        [SerializeField] private Image backgroundImage;

        [Tooltip("ジョイスティックのハンドル（スティックノブ）イメージ")]
        [SerializeField] private Image handleImage;

        [Tooltip("タッチ判定を受け付ける透明なゾーンImage")]
        [SerializeField] private Image touchZoneImage;

        private RectTransform rootRectTransform;
        private RectTransform handleRectTransform;
        private CanvasGroup containerCanvasGroup;
        private Canvas parentCanvas;
        private Vector2 currentInputVector;
        private bool isPointerDown;

        /// <summary>ジョイスティックのルート RectTransform</summary>
        public RectTransform RootRectTransform => rootRectTransform != null ? rootRectTransform : (rootRectTransform = GetComponent<RectTransform>());

        /// <summary>ジョイスティックのコンテナ RectTransform</summary>
        public RectTransform ContainerRect => containerRect;

        /// <summary>背景イメージ</summary>
        public Image BackgroundImage => backgroundImage;

        /// <summary>ハンドルイメージ</summary>
        public Image HandleImage => handleImage;

        /// <summary>タッチゾーンイメージ</summary>
        public Image TouchZoneImage => touchZoneImage;

        /// <summary>ジョイスティック動作モード</summary>
        public JoystickMode Mode
        {
            get => joystickMode;
            set
            {
                joystickMode = value;
                UpdateModeVisibility();
            }
        }

        /// <summary>スティックの移動可能半径</summary>
        public float MovementRange
        {
            get => movementRange;
            set => movementRange = Mathf.Max(10f, value);
        }

        /// <summary>現在の入力ベクトル (-1.0 ~ 1.0)</summary>
        public Vector2 InputVector => currentInputVector;

        /// <summary>現在タッチ操作中かどうか</summary>
        public bool IsPointerDown => isPointerDown;

        /// <summary>入力ベクトル更新時のイベント</summary>
        public event Action<Vector2> OnInputUpdated;

        /// <summary>
        /// コンポーネントの初期化および参照のキャッシュを行う。
        /// </summary>
        private void Awake()
        {
            rootRectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            if (containerRect != null)
            {
                containerCanvasGroup = containerRect.GetComponent<CanvasGroup>();
                if (containerCanvasGroup == null)
                {
                    containerCanvasGroup = containerRect.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (handleImage != null)
            {
                handleRectTransform = handleImage.rectTransform;
            }
        }

        /// <summary>
        /// 初回フレームでの表示状態を反映する。
        /// </summary>
        private void Start()
        {
            UpdateModeVisibility();
        }

        /// <summary>
        /// オブジェクトの文字列表現を返す。
        /// </summary>
        /// <returns>ジョイスティック情報文字列</returns>
        public override string ToString()
        {
            return $"VirtualJoystickView (Mode: {joystickMode}, Input: {currentInputVector})";
        }

        /// <summary>
        /// タッチ開始時にジョイスティックを配置・表示し、ドラッグを開始する。
        /// </summary>
        /// <param name="eventData">ポインターイベントデータ</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;

            if (parentCanvas == null)
            {
                parentCanvas = GetComponentInParent<Canvas>();
            }

            var cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;

            if (joystickMode == JoystickMode.Floating && containerRect != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRectTransform, eventData.position, cam, out var localPoint))
                {
                    containerRect.anchoredPosition = localPoint;
                }

                SetContainerVisible(true);
            }

            if (handleRectTransform != null)
            {
                handleRectTransform.anchoredPosition = Vector2.zero;
            }

            OnDrag(eventData);
        }

        /// <summary>
        /// ドラッグ中に入力ベクトルを計算し、ハンドル位置と入力を更新する。
        /// </summary>
        /// <param name="eventData">ポインターイベントデータ</param>
        public void OnDrag(PointerEventData eventData)
        {
            if (!isPointerDown || containerRect == null || handleRectTransform == null) return;

            var cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(containerRect, eventData.position, cam, out var localPos))
            {
                var clampedPos = Vector2.ClampMagnitude(localPos, movementRange);
                handleRectTransform.anchoredPosition = clampedPos;

                currentInputVector = clampedPos / movementRange;
                OnInputUpdated?.Invoke(currentInputVector);

                if (InputController.Instance != null)
                {
                    InputController.Instance.SetInputDirect(currentInputVector);
                }
            }
        }

        /// <summary>
        /// タッチ終了時にハンドルをリセットし、フローティング時は非表示にする。
        /// </summary>
        /// <param name="eventData">ポインターイベントデータ</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;

            if (handleRectTransform != null)
            {
                handleRectTransform.anchoredPosition = Vector2.zero;
            }

            currentInputVector = Vector2.zero;
            OnInputUpdated?.Invoke(Vector2.zero);

            if (InputController.Instance != null)
            {
                InputController.Instance.SetInputDirect(Vector2.zero);
            }

            if (joystickMode == JoystickMode.Floating)
            {
                SetContainerVisible(false);
            }
        }

        /// <summary>
        /// 外部またはエディタから参照を手動設定する。
        /// </summary>
        /// <param name="container">コンテナRectTransform</param>
        /// <param name="bg">背景Image</param>
        /// <param name="handle">ハンドルImage</param>
        /// <param name="touchZone">タッチゾーンImage</param>
        public void SetupReferences(RectTransform container, Image bg, Image handle, Image touchZone)
        {
            containerRect = container;
            backgroundImage = bg;
            handleImage = handle;
            touchZoneImage = touchZone;

            if (handleImage != null)
            {
                handleRectTransform = handleImage.rectTransform;
            }

            if (containerRect != null)
            {
                containerCanvasGroup = containerRect.GetComponent<CanvasGroup>();
                if (containerCanvasGroup == null)
                {
                    containerCanvasGroup = containerRect.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        /// <summary>
        /// ジョイスティックコンテナの表示/非表示（アルファ値）を切り替える。
        /// </summary>
        /// <param name="visible">表示フラグ</param>
        public void SetContainerVisible(bool visible)
        {
            if (containerCanvasGroup != null)
            {
                containerCanvasGroup.alpha = visible ? 1f : 0f;
                containerCanvasGroup.blocksRaycasts = visible;
                containerCanvasGroup.interactable = visible;
            }
        }

        /// <summary>
        /// 現在のモード設定に応じた初期表示状態に更新する。
        /// </summary>
        private void UpdateModeVisibility()
        {
            if (joystickMode == JoystickMode.Floating)
            {
                SetContainerVisible(false);
            }
            else
            {
                SetContainerVisible(true);
                if (containerRect != null)
                {
                    containerRect.anchorMin = new Vector2(0f, 0f);
                    containerRect.anchorMax = new Vector2(0f, 0f);
                    containerRect.anchoredPosition = new Vector2(120f, 120f);
                }
            }
        }
    }
}
