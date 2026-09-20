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
    /// シーン開始時に PlayerController の Transform を CinemachineCamera の TrackingTarget に、
    /// および ArenaBackground の枠コライダーを CinemachineConfiner2D に自動設定する。
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    [DisallowMultipleComponent]
    public sealed class CinemachinePlayerBinder : MonoBehaviour
    {
        private CinemachineCamera vcam;
        private CinemachineConfiner2D confiner;

        /// <summary>
        /// コンポーネント参照の初期化を行う。
        /// </summary>
        private void Awake()
        {
            vcam = GetComponent<CinemachineCamera>();
            confiner = GetComponent<CinemachineConfiner2D>();
        }

        /// <summary>
        /// 初期バインドを実行する。
        /// </summary>
        private void Start()
        {
            BindTarget();
            BindConfiner();
        }

        /// <summary>
        /// 毎フレーム未バインド対象の有無を監視し、存在すればバインドを実行する。
        /// </summary>
        private void Update()
        {
            if (vcam != null && vcam.Target.TrackingTarget == null && PlayerController.Instance != null)
            {
                BindTarget();
            }

            if (confiner != null && confiner.BoundingShape2D == null && ArenaBackground.Instance != null && ArenaBackground.Instance.BoundaryCollider != null)
            {
                BindConfiner();
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

        /// <summary>
        /// 背景の枠コライダーをカメラの境界制限（Confiner2D）として設定する。
        /// </summary>
        public void BindConfiner()
        {
            if (confiner != null && ArenaBackground.Instance != null && ArenaBackground.Instance.BoundaryCollider != null)
            {
                confiner.BoundingShape2D = ArenaBackground.Instance.BoundaryCollider;
                confiner.InvalidateBoundingShapeCache();
            }
        }
    }
}
