/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: メインカメラおよび URP カメラスタック（通常UIカメラ・システムカメラ）を一元管理する。
 */

using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Runner
{
    /// <summary>
    /// メインカメラ（Base Camera）として動作し、UICamera（通常UI）および SystemCamera（ダイアログ/ローディング）を URP カメラスタックに登録・管理する。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-995)]
    public sealed class CameraManager : SingletonMonoBehaviour<CameraManager>
    {
        [Header("Camera Stack Settings")]
        [Tooltip("ゲーム内通常 UI（ポーズボタン・経験値バー・HUD・メニュー等）を描画するオーバーレイカメラ")]
        [SerializeField]
        private Camera uiCamera;

        [Tooltip("システムダイアログ（バトル終了リザルト・ローディング・エラーポップアップ等）を描画する最前面オーバーレイカメラ")]
        [SerializeField]
        private Camera systemCamera;

        private Camera mainCamera;
        private UniversalAdditionalCameraData mainCameraData;

        /// <summary>
        /// メインカメラ（Base Camera）を取得する。ワールド空間（キャラクター・背景・頭上HPバー等）を描画。
        /// </summary>
        public Camera MainCamera => mainCamera != null ? mainCamera : (mainCamera = GetComponent<Camera>());

        /// <summary>
        /// 通常 UI カメラ（Overlay Camera）を取得する。
        /// </summary>
        public Camera UICamera => uiCamera;

        /// <summary>
        /// システムカメラ（Overlay Camera）を取得する。最前面に描画。
        /// </summary>
        public Camera SystemCamera => systemCamera;

        /// <summary>
        /// メインカメラの URP カメラデータを取得する。
        /// </summary>
        public UniversalAdditionalCameraData MainCameraData
        {
            get
            {
                if (mainCameraData == null && MainCamera != null)
                {
                    mainCameraData = MainCamera.GetUniversalAdditionalCameraData();
                }
                return mainCameraData;
            }
        }

        protected override bool ShouldDontDestroyOnLoad => true;

        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance)
            {
                return;
            }

            SetupCameras();
            DebugLogger.Log("[CameraManager] CameraManager (MainCamera) が正常に初期化されました。");
        }

        private void OnValidate()
        {
            SetupCameras();
        }

        /// <summary>
        /// メインカメラを Base Camera に設定し、UICamera および SystemCamera を Overlay としてカメラスタックに自動同期する。
        /// </summary>
        public void SetupCameras()
        {
            if (mainCamera == null)
            {
                mainCamera = GetComponent<Camera>();
            }

            if (mainCamera == null)
            {
                return;
            }

            // MainCamera タグを設定
            if (!gameObject.CompareTag("MainCamera"))
            {
                gameObject.tag = "MainCamera";
            }

            // メインカメラを URP Base Camera に設定（UI レイヤーは Overlay カメラに任せるため除外）
            mainCamera.cullingMask &= ~(1 << 5);

            var cameraData = MainCameraData;
            if (cameraData != null)
            {
                cameraData.renderType = CameraRenderType.Base;

                // 1. 通常 UI カメラ (Overlay: スタック1番目) の登録
                if (uiCamera != null)
                {
                    var uiData = uiCamera.GetUniversalAdditionalCameraData();
                    if (uiData != null)
                    {
                        uiData.renderType = CameraRenderType.Overlay;
                    }

                    if (!cameraData.cameraStack.Contains(uiCamera))
                    {
                        cameraData.cameraStack.Add(uiCamera);
                    }
                }

                // 2. システムカメラ (Overlay: スタック2番目 / 最前面) の登録
                if (systemCamera != null)
                {
                    var sysData = systemCamera.GetUniversalAdditionalCameraData();
                    if (sysData != null)
                    {
                        sysData.renderType = CameraRenderType.Overlay;
                    }

                    if (!cameraData.cameraStack.Contains(systemCamera))
                    {
                        cameraData.cameraStack.Add(systemCamera);
                    }
                }
            }
        }
    }
}
