/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Google Fonts の Noto Sans JP から Dynamic TMP Font Asset を生成し、
 *                TMP Settings の Fallback / Default Font に登録するエディタ拡張スクリプト。
 */

#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Runner.Editor
{
    public static class JapaneseFontAssetCreator
    {
        private const string FontTtfPath = "Assets/Runner/Fonts/NotoSansJP.ttf";
        private const string OutputAssetPath = "Assets/Runner/Fonts/NotoSansJP_SDF.asset";

        [MenuItem("Tools/Runner/Generate NotoSansJP TMP Font Asset")]
        public static void GenerateFontAsset()
        {
            // 1. TTF アセットのインポート確認
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (font == null)
            {
                Debug.LogError($"[JapaneseFontAssetCreator] TTFファイルが見つかりません: {FontTtfPath}");
                return;
            }

            // 2. Dynamic TMP Font Asset の生成
            Debug.Log("[JapaneseFontAssetCreator] NotoSansJP から Dynamic TMP Font Asset を生成中...");
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 40,
                atlasPadding: 4,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true
            );

            if (fontAsset == null)
            {
                Debug.LogError("[JapaneseFontAssetCreator] TMP_FontAsset の生成に失敗しました。");
                return;
            }

            fontAsset.name = "NotoSansJP_SDF";

            // アセットとして保存
            if (File.Exists(OutputAssetPath))
            {
                AssetDatabase.DeleteAsset(OutputAssetPath);
            }

            AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                foreach (var tex in fontAsset.atlasTextures)
                {
                    if (tex != null)
                    {
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
            }

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[JapaneseFontAssetCreator] TMP Font Asset を作成・保存しました: {OutputAssetPath}");

            // 3. TMP Settings の Default Font / Fallback List に登録
            RegisterToTmpSettings(fontAsset);
        }

        private static void RegisterToTmpSettings(TMP_FontAsset fontAsset)
        {
            var tmpSettings = TMP_Settings.instance;
            if (tmpSettings == null)
            {
                Debug.LogWarning("[JapaneseFontAssetCreator] TMP_Settings が見つかりませんでした。");
                return;
            }

            var so = new SerializedObject(tmpSettings);

            // デフォルトフォントの更新
            var defaultFontProp = so.FindProperty("m_defaultFontAsset");
            if (defaultFontProp != null)
            {
                defaultFontProp.objectReferenceValue = fontAsset;
            }

            // Fallback Font List に追加
            var fallbackListProp = so.FindProperty("m_fallbackFontAssets");
            if (fallbackListProp != null)
            {
                bool alreadyExists = false;
                for (int i = 0; i < fallbackListProp.arraySize; i++)
                {
                    var elem = fallbackListProp.GetArrayElementAtIndex(i);
                    if (elem.objectReferenceValue == fontAsset)
                    {
                        alreadyExists = true;
                        break;
                    }
                }

                if (!alreadyExists)
                {
                    fallbackListProp.InsertArrayElementAtIndex(0);
                    fallbackListProp.GetArrayElementAtIndex(0).objectReferenceValue = fontAsset;
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tmpSettings);
            AssetDatabase.SaveAssets();

            Debug.Log("[JapaneseFontAssetCreator] TMP Settings に NotoSansJP_SDF をデフォルトおよびフォールバックフォントとして登録しました！");
        }
    }
}
#endif
