/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: CinemachineCamera にプレイヤーの Transform を自動バインドするコンポーネント。
 */

using Unity.Cinemachine;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// シーン開始時に PlayerController の Transform を CinemachineCamera の TrackingTarget に自動設定する。
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    [DisallowMultipleComponent]
    public sealed class CinemachinePlayerBinder : MonoBehaviour
    {
        private CinemachineCamera vcam;

        private void Awake()
        {
            vcam = GetComponent<CinemachineCamera>();
        }

        private void Start()
        {
            BindTarget();
        }

        private void Update()
        {
            if (vcam != null && vcam.Target.TrackingTarget == null && PlayerController.Instance != null)
            {
                BindTarget();
            }
        }

        /// <summary>
        /// プレイヤーを追従対象として設定する。
        /// </summary>
        public void BindTarget()
        {
            if (vcam != null && PlayerController.Instance != null)
            {
                vcam.Target.TrackingTarget = PlayerController.Instance.transform;
            }
        }
    }
}
