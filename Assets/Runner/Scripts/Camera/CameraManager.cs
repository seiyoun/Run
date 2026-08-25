/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: メインカメラおよび URP カメラスタック（システムカメラ等）を一元管理する。
 */

using Shiyuan.Foundation.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Runner
{
    /// <summary>
    /// メインカメラ（Base Camera）として動作し、Inspector またはコンポーネントから指定されたシステムカメラを URP カメラスタックに登録・管理する。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-995)]
    public sealed class CameraManager : SingletonMonoBehaviour<CameraManager>
    {
        [Header("Camera Stack Settings")]
        [Tooltip("システム UI・ローディング・通知などを描画するオーバーレイカメラ")]
        [SerializeField]
        private Camera systemCamera;

        private Camera mainCamera;
        private UniversalAdditionalCameraData mainCameraData;

        /// <summary>
        /// メインカメラ（Base Camera）を取得する。
        /// </summary>
        public Camera MainCamera => mainCamera != null ? mainCamera : (mainCamera = GetComponent<Camera>());

        /// <summary>
        /// システムカメラ（Overlay Camera）を取得する。
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
        /// メインカメラを Base Camera に設定し、登録されたシステムカメラを Overlay としてカメラスタックに自動同期する。
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

            // メインカメラを URP Base Camera に設定
            var cameraData = MainCameraData;
            if (cameraData != null)
            {
                cameraData.renderType = CameraRenderType.Base;

                // システムカメラがインスペクター等で設定されている場合
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

        /// <summary>
        /// システムカメラを動的に設定・更新する。
        /// </summary>
        /// <param name="overlayCamera">オーバーレイとして追加するシステムカメラ</param>
        public void SetSystemCamera(Camera overlayCamera)
        {
            if (systemCamera != null && mainCameraData != null)
            {
                mainCameraData.cameraStack.Remove(systemCamera);
            }

            systemCamera = overlayCamera;
            SetupCameras();
        }

        /// <summary>
        /// 任意のオーバーレイカメラをメインカメラのカメラスタックに追加する。
        /// </summary>
        /// <param name="overlayCamera">スタックに追加するカメラ</param>
        public void AddOverlayCamera(Camera overlayCamera)
        {
            if (overlayCamera == null)
            {
                return;
            }

            var cameraData = MainCameraData;
            if (cameraData == null)
            {
                return;
            }

            var overlayData = overlayCamera.GetUniversalAdditionalCameraData();
            if (overlayData != null)
            {
                overlayData.renderType = CameraRenderType.Overlay;
            }

            if (!cameraData.cameraStack.Contains(overlayCamera))
            {
                cameraData.cameraStack.Add(overlayCamera);
            }
        }

        /// <summary>
        /// 任意のオーバーレイカメラをカメラスタックから削除する。
        /// </summary>
        /// <param name="overlayCamera">スタックから削除するカメラ</param>
        public void RemoveOverlayCamera(Camera overlayCamera)
        {
            if (overlayCamera == null)
            {
                return;
            }

            var cameraData = MainCameraData;
            cameraData?.cameraStack.Remove(overlayCamera);
        }
    }
}
