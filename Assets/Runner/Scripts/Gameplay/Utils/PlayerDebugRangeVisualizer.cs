/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: プレイヤーなどの特定オブジェクトの吸引範囲・検知範囲を GameView 上で可視化するデバッグ描画ユーティリティ。
 *                SANDBOX またはエディタ環境でのみ動作し、LineRenderer を用いた円形リング描画を動的に管理します。
 */

#if SANDBOX || UNITY_EDITOR
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// GameView 上でプレイヤーなどの吸引範囲（円形リング）を可視化するデバッグ描画ユーティリティクラス。
    /// 対象 Transform の子オブジェクトとして LineRenderer を動的アタッチし、表示・非表示・半径更新を制御します。
    /// </summary>
    public static class PlayerDebugRangeVisualizer
    {
        // -------------------------------------------------------------
        // 1. const / static フィールド
        // -------------------------------------------------------------
        private const string VisualObjectName = "__DebugMagnetRangeVisual";
        private const int CircleSegments = 64;
        private const float DefaultLineWidth = 0.06f;

        private static readonly Color RangeColor = new Color(0f, 0.9f, 1f, 0.8f);

        // -------------------------------------------------------------
        // 2. [SerializeField] シリアライズフィールド
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 3. private インスタンス変数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 4. public インスタンス変数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 5. プロパティ & イベント
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 6. Unity ライフサイクル関数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 7. override 関数
        // -------------------------------------------------------------

        // -------------------------------------------------------------
        // 8. public 関数
        // -------------------------------------------------------------

        /// <summary>
        /// 対象 Transform の吸引範囲デバッグ表示の有効・無効を切り替える。
        /// </summary>
        /// <param name="target">追従対象の Transform</param>
        /// <param name="radius">描画する円の半径</param>
        /// <param name="visible">表示する場合は true</param>
        public static void SetRangeVisible(Transform target, float radius, bool visible)
        {
            if (target == null) return;

            var visualObj = GetOrCreateVisualObject(target);
            var lineRenderer = visualObj.GetComponent<LineRenderer>();

            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
                if (visible)
                {
                    UpdateCirclePositions(lineRenderer, radius);
                }
            }
        }

        /// <summary>
        /// 対象 Transform の吸引範囲デバッグ表示の表示状態をトグル反転する。
        /// </summary>
        /// <param name="target">追従対象の Transform</param>
        /// <param name="radius">描画する円の半径</param>
        /// <returns>切り替え後の表示状態（表示中なら true）</returns>
        public static bool ToggleRangeVisible(Transform target, float radius)
        {
            if (target == null) return false;

            bool isCurrentlyVisible = IsRangeVisible(target);
            bool nextState = !isCurrentlyVisible;
            SetRangeVisible(target, radius, nextState);
            return nextState;
        }

        /// <summary>
        /// 現在、対象 Transform の吸引範囲デバッグ表示が有効かどうかを取得する。
        /// </summary>
        /// <param name="target">対象の Transform</param>
        /// <returns>表示中であれば true</returns>
        public static bool IsRangeVisible(Transform target)
        {
            if (target == null) return false;

            var child = target.Find(VisualObjectName);
            if (child == null) return false;

            var lineRenderer = child.GetComponent<LineRenderer>();
            return lineRenderer != null && lineRenderer.enabled;
        }

        /// <summary>
        /// 吸引範囲の半径サイズを最新値に更新する。
        /// </summary>
        /// <param name="target">対象の Transform</param>
        /// <param name="radius">新しい半径</param>
        public static void UpdateRadius(Transform target, float radius)
        {
            if (target == null) return;

            var child = target.Find(VisualObjectName);
            if (child == null) return;

            var lineRenderer = child.GetComponent<LineRenderer>();
            if (lineRenderer != null && lineRenderer.enabled)
            {
                UpdateCirclePositions(lineRenderer, radius);
            }
        }

        // -------------------------------------------------------------
        // 9. private 関数 / 内部ヘルパー
        // -------------------------------------------------------------

        /// <summary>
        /// 対象 Transform 配下のデバッグ描画用 GameObject を取得、未作成なら生成して初期化する。
        /// </summary>
        /// <param name="target">親となる Transform</param>
        /// <returns>デバッグ用 GameObject</returns>
        private static GameObject GetOrCreateVisualObject(Transform target)
        {
            var child = target.Find(VisualObjectName);
            if (child != null)
            {
                return child.gameObject;
            }

            var visualObj = new GameObject(VisualObjectName);
            visualObj.transform.SetParent(target, false);
            visualObj.transform.localPosition = Vector3.zero;

            var lineRenderer = visualObj.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = CircleSegments;
            lineRenderer.startWidth = DefaultLineWidth;
            lineRenderer.endWidth = DefaultLineWidth;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null)
            {
                lineRenderer.material = new Material(shader);
            }

            lineRenderer.startColor = RangeColor;
            lineRenderer.endColor = RangeColor;
            lineRenderer.sortingOrder = 10;
            lineRenderer.enabled = false;

            return visualObj;
        }

        /// <summary>
        /// 指定された半径に基づいて LineRenderer の円周頂点座標配列を更新する。
        /// </summary>
        /// <param name="lineRenderer">対象の LineRenderer</param>
        /// <param name="radius">円の半径</param>
        private static void UpdateCirclePositions(LineRenderer lineRenderer, float radius)
        {
            if (lineRenderer == null || radius <= 0f) return;

            var positions = new Vector3[CircleSegments];
            float deltaAngle = (Mathf.PI * 2f) / CircleSegments;

            for (int i = 0; i < CircleSegments; i++)
            {
                float angle = i * deltaAngle;
                positions[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            lineRenderer.positionCount = CircleSegments;
            lineRenderer.SetPositions(positions);
        }
    }
}
#endif

