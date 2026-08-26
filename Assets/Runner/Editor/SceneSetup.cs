/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Home シーン、Game シーン等のシーン内オブジェクトおよび EventSystem の InputActions をセットアップするエディタ拡張。
 */

using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Runner.Editor
{
    public static class SceneSetup
    {
        [MenuItem("Tools/Runner/Setup All Scenes")]
        public static void SetupAllScenes()
        {
            SetupHomeScene();
            SetupGameScene();
            AddressableSetup.RegisterAllAddressables();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneSetup] すべてのシーンおよび Addressables のセットアップが完了しました。");
        }

        [MenuItem("Tools/Runner/Setup Home Scene UI")]
        public static void SetupHomeScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Runner/Scenes/Home.unity");

            // EventSystem & Input Actions セットアップ
            SetupEventSystem();

            // Canvas
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            var homeView = canvasObj.GetComponent<HomeView>();
            if (homeView == null)
            {
                homeView = canvasObj.AddComponent<HomeView>();
            }

            // Game Start Button
            var playBtnObj = GameObject.Find("GameStartButton");
            if (playBtnObj == null)
            {
                playBtnObj = new GameObject("GameStartButton");
                playBtnObj.transform.SetParent(canvasObj.transform, false);
                var rect = playBtnObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(360, 100);
                rect.anchoredPosition = Vector2.zero;

                var img = playBtnObj.AddComponent<Image>();
                img.color = new Color(0.18f, 0.65f, 0.35f, 1f);

                var btn = playBtnObj.AddComponent<Button>();

                // Button Text
                var btnTextObj = new GameObject("Text (TMP)");
                btnTextObj.transform.SetParent(playBtnObj.transform, false);
                var textRect = btnTextObj.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                var btnTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
                btnTmp.text = "GAME START";
                btnTmp.fontSize = 36;
                btnTmp.alignment = TextAlignmentOptions.Center;
                btnTmp.color = Color.white;
                btnTmp.raycastTarget = false;

                var so = new SerializedObject(homeView);
                so.FindProperty("playButton").objectReferenceValue = btn;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] Home シーンのセットアップが完了しました。");
        }

        [MenuItem("Tools/Runner/Setup Game Scene")]
        public static void SetupGameScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Runner/Scenes/Game.unity");

            // 1. InputController (入力一元管理)
            var inputCtrlObj = GameObject.Find("InputController");
            if (inputCtrlObj == null)
            {
                inputCtrlObj = new GameObject("InputController");
            }
            var inputController = inputCtrlObj.GetComponent<InputController>();
            if (inputController == null)
            {
                inputController = inputCtrlObj.AddComponent<InputController>();
            }
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Runner/Settings/Input/RunnerInputActions.inputactions");
            if (inputAsset != null)
            {
                var inputSo = new SerializedObject(inputController);
                inputSo.FindProperty("inputActionsAsset").objectReferenceValue = inputAsset;
                inputSo.ApplyModifiedProperties();
            }

            // 2. シーン上の静的 Player を削除 (動的ロード・プレハブ構成へ移行)
            var oldPlayerObj = GameObject.Find("Player");
            if (oldPlayerObj != null)
            {
                Object.DestroyImmediate(oldPlayerObj);
            }

            // 3. PlayerSpawner (Addressables 動的生成用)
            var spawnerObj = GameObject.Find("PlayerSpawner");
            if (spawnerObj == null)
            {
                spawnerObj = new GameObject("PlayerSpawner");
            }
            var spawner = spawnerObj.GetComponent<PlayerSpawner>();
            if (spawner == null)
            {
                spawner = spawnerObj.AddComponent<PlayerSpawner>();
            }

            // 4. Main Camera & CinemachineBrain
            var mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (mainCamObj == null)
            {
                mainCamObj = new GameObject("Main Camera");
                mainCamObj.tag = "MainCamera";
                mainCamObj.AddComponent<Camera>();
                mainCamObj.AddComponent<AudioListener>();
            }

            var cam = mainCamObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            mainCamObj.transform.position = new Vector3(0f, 0f, -10f);

            var brain = mainCamObj.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamObj.AddComponent<CinemachineBrain>();
            }

            // 5. CinemachineCamera (Virtual Camera)
            var vcamObj = GameObject.Find("CinemachineCamera");
            if (vcamObj == null)
            {
                vcamObj = new GameObject("CinemachineCamera");
            }

            var vcam = vcamObj.GetComponent<CinemachineCamera>();
            if (vcam == null)
            {
                vcam = vcamObj.AddComponent<CinemachineCamera>();
            }

            vcam.Target.TrackingTarget = null; // プレイヤー生成時に自動バインド
            vcam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            vcam.Lens.OrthographicSize = 6.0f;

            var composer = vcamObj.GetComponent<CinemachinePositionComposer>();
            if (composer == null)
            {
                composer = vcamObj.AddComponent<CinemachinePositionComposer>();
            }
            composer.Damping = new Vector3(0.3f, 0.3f, 0f);

            var binder = vcamObj.GetComponent<CinemachinePlayerBinder>();
            if (binder == null)
            {
                binder = vcamObj.AddComponent<CinemachinePlayerBinder>();
            }

            // 6. BackgroundSpawner (Addressables 動的生成用)
            var oldBgArenaObj = GameObject.Find("ArenaBackground");
            if (oldBgArenaObj != null)
            {
                Object.DestroyImmediate(oldBgArenaObj);
            }

            var bgSpawnerObj = GameObject.Find("BackgroundSpawner");
            if (bgSpawnerObj == null)
            {
                bgSpawnerObj = new GameObject("BackgroundSpawner");
            }
            var bgSpawner = bgSpawnerObj.GetComponent<BackgroundSpawner>();
            if (bgSpawner == null)
            {
                bgSpawner = bgSpawnerObj.AddComponent<BackgroundSpawner>();
            }

            // 7. EventSystem & Input Actions セットアップ
            SetupEventSystem();

            // 不要なスクリーン Canvas / PlayerStatusHUD が残っていれば削除
            var oldHudObj = GameObject.Find("PlayerStatusHUD");
            if (oldHudObj != null)
            {
                Object.DestroyImmediate(oldHudObj);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneSetup] Game シーンのセットアップが完了しました。");
        }

        private static void SetupEventSystem()
        {
            var eventSystemObj = GameObject.Find("EventSystem");
            if (eventSystemObj == null)
            {
                eventSystemObj = new GameObject("EventSystem");
            }

            var eventSystem = eventSystemObj.GetComponent<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = eventSystemObj.AddComponent<EventSystem>();
            }

            var uiModule = eventSystemObj.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null)
            {
                uiModule = eventSystemObj.AddComponent<InputSystemUIInputModule>();
            }

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Runner/Settings/Input/RunnerInputActions.inputactions");
            if (inputAsset != null)
            {
                var so = new SerializedObject(uiModule);
                so.FindProperty("m_ActionsAsset").objectReferenceValue = inputAsset;

                var allObjects = AssetDatabase.LoadAllAssetsAtPath("Assets/Runner/Settings/Input/RunnerInputActions.inputactions");
                foreach (var obj in allObjects)
                {
                    if (obj is InputActionReference actionRef && actionRef.action != null)
                    {
                        var name = actionRef.action.name;
                        if (name == "Point") so.FindProperty("m_PointAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "Click") so.FindProperty("m_LeftClickAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "RightClick") so.FindProperty("m_RightClickAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "MiddleClick") so.FindProperty("m_MiddleClickAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "ScrollWheel") so.FindProperty("m_ScrollWheelAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "Navigate") so.FindProperty("m_MoveAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "Submit") so.FindProperty("m_SubmitAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                        else if (name == "Cancel") so.FindProperty("m_CancelAction").FindPropertyRelative("m_Action").objectReferenceValue = actionRef;
                    }
                }

                so.ApplyModifiedProperties();
            }
        }
    }
}
