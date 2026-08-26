/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Runner プロジェクトで使用するプレハブを自動生成するエディタ拡張。
 */

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Runner.Editor
{
    public static class PrefabGenerator
    {
        private const string PrefabDir = "Assets/Runner/Prefabs";

        [MenuItem("Tools/Runner/Generate All Prefabs")]
        public static void GenerateAllPrefabs()
        {
            EnsureDirectoryExists(PrefabDir);
            CreateLoadingViewPrefab();
            CreateCameraManagerPrefab();
            CreateBackgroundPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabGenerator] すべてのプレハブを正常に生成しました: " + PrefabDir);
        }

        [MenuItem("Tools/Runner/Generate Background Prefab")]
        public static void CreateBackgroundPrefab()
        {
            EnsureDirectoryExists(PrefabDir);
            var path = $"{PrefabDir}/Background.prefab";

            var rootObj = new GameObject("Background");
            rootObj.AddComponent<SpriteRenderer>();
            rootObj.AddComponent<ArenaBackground>();

            PrefabUtility.SaveAsPrefabAsset(rootObj, path);
            Object.DestroyImmediate(rootObj);

            Debug.Log($"[PrefabGenerator] Background プレハブを作成しました: {path}");
        }

        [MenuItem("Tools/Runner/Generate LoadingView Prefab")]
        public static void CreateLoadingViewPrefab()
        {
            EnsureDirectoryExists(PrefabDir);
            var path = $"{PrefabDir}/LoadingView.prefab";

            // Root Canvas
            var rootObj = new GameObject("LoadingView");
            rootObj.transform.localScale = Vector3.one;
            var canvas = rootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // 最前面
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

            var scaler = rootObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            rootObj.AddComponent<GraphicRaycaster>();
            var canvasGroup = rootObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            var loadingView = rootObj.AddComponent<LoadingView>();

            // Background Blocker
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(rootObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.6f);
            bgImage.raycastTarget = true;

            // Content Container
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(rootObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(300, 300);

            // Spinner
            var spinnerObj = new GameObject("Spinner");
            spinnerObj.transform.SetParent(contentObj.transform, false);
            var spinnerRect = spinnerObj.AddComponent<RectTransform>();
            spinnerRect.anchoredPosition = new Vector2(0, 30);
            spinnerRect.sizeDelta = new Vector2(80, 80);
            var spinnerImage = spinnerObj.AddComponent<Image>();
            spinnerImage.color = Color.white;

            // Message Text (TMP)
            var textObj = new GameObject("MessageText");
            textObj.transform.SetParent(contentObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = new Vector2(0, -60);
            textRect.sizeDelta = new Vector2(300, 60);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Loading...";
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // SerializedField への参照割り当て
            var so = new SerializedObject(loadingView);
            so.FindProperty("rootCanvas").objectReferenceValue = canvas;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("spinnerIcon").objectReferenceValue = spinnerRect;
            so.FindProperty("messageText").objectReferenceValue = tmp;
            so.ApplyModifiedProperties();

            // Prefab 保存
            PrefabUtility.SaveAsPrefabAsset(rootObj, path);
            Object.DestroyImmediate(rootObj);

            Debug.Log($"[PrefabGenerator] LoadingView プレハブを作成しました: {path}");
        }

        [MenuItem("Tools/Runner/Generate CameraManager Prefab")]
        public static void CreateCameraManagerPrefab()
        {
            EnsureDirectoryExists(PrefabDir);
            var path = $"{PrefabDir}/CameraManager.prefab";

            // 1. Root: CameraManager (MainCamera)
            var rootObj = new GameObject("CameraManager");
            rootObj.tag = "MainCamera";

            var mainCam = rootObj.AddComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.Skybox;
            mainCam.cullingMask = ~0; // Everything
            mainCam.depth = 0;

            rootObj.AddComponent<AudioListener>();

            var mainCameraData = mainCam.GetUniversalAdditionalCameraData();
            if (mainCameraData != null)
            {
                mainCameraData.renderType = CameraRenderType.Base;
            }

            var cameraManager = rootObj.AddComponent<CameraManager>();

            // 2. Child: SystemCamera (Overlay Camera)
            var sysCamObj = new GameObject("SystemCamera");
            sysCamObj.transform.SetParent(rootObj.transform, false);

            var sysCam = sysCamObj.AddComponent<Camera>();
            sysCam.clearFlags = CameraClearFlags.Nothing;
            sysCam.cullingMask = 1 << 5; // UI Layer (Layer 5)

            var sysCameraData = sysCam.GetUniversalAdditionalCameraData();
            if (sysCameraData != null)
            {
                sysCameraData.renderType = CameraRenderType.Overlay;
                sysCameraData.renderShadows = false;
            }

            // 3. URP カメラスタックに SystemCamera を追加
            if (mainCameraData != null && !mainCameraData.cameraStack.Contains(sysCam))
            {
                mainCameraData.cameraStack.Add(sysCam);
            }

            // 4. SerializedField の参照割り当て
            var so = new SerializedObject(cameraManager);
            so.FindProperty("systemCamera").objectReferenceValue = sysCam;
            so.ApplyModifiedProperties();

            // Prefab 保存
            PrefabUtility.SaveAsPrefabAsset(rootObj, path);
            Object.DestroyImmediate(rootObj);

            Debug.Log($"[PrefabGenerator] CameraManager プレハブを作成しました: {path}");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
