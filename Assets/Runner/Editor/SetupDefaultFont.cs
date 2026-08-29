/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: デフォルトフォントをNotoSansJP-VariableFont_wghtに設定し、プロジェクト内の全TMPコンポーネントのフォントを一括置換および絵文字・特殊記号サニタイズを行うエディタ拡張
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Runner.Editor
{
    /// <summary>
    /// デフォルトフォントを NotoSansJP-VariableFont_wght に設定・更新し、全アセットのフォント参照を置換および絵文字をサニタイズするエディタユーティリティ。
    /// </summary>
    public static class SetupDefaultFont
    {
        private const string FontPath = "Assets/Runner/Fonts/NotoSansJP-VariableFont_wght.ttf";
        private const string FontAssetPath = "Assets/Runner/Fonts/NotoSansJP-VariableFont_wght_SDF.asset";
        private const string OldFontAssetPath = "Assets/Runner/Fonts/NotoSansJP_SDF.asset";
        private const string OldTtfPath = "Assets/Runner/Fonts/NotoSansJP.ttf";

        // 絵文字・フォント未収録の特殊記号（➔等）を検出する正規表現
        private static readonly Regex EmojiRegex = new Regex(
            @"[\uD83C-\uDBFF\uDC00-\uDFFF]|[\u2600-\u27BF]|[\u2300-\u23FF]|[\u2B00-\u2BFF]|[\u2190-\u21FF]|[\u2900-\u297F]|[\uFE00-\uFE0F]",
            RegexOptions.Compiled
        );

        /// <summary>
        /// NotoSansJP-VariableFont_wght から TMP Font Asset を作成/更新し、TMP Settings のデフォルトフォントに設定し、プロジェクト全体のフォントを一括置換および絵文字をサニタイズする。
        /// </summary>
        [MenuItem("Runner/Fonts/Setup NotoSansJP-VariableFont_wght as Default Font")]
        public static void Execute()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError($"[SetupDefaultFont] フォントが見つかりません: {FontPath}");
                return;
            }

            // 古い NotoSansJP_SDF.asset があればリネームまたは移行
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OldFontAssetPath) != null &&
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) == null)
            {
                AssetDatabase.MoveAsset(OldFontAssetPath, FontAssetPath);
            }

            // 古い NotoSansJP.ttf があれば削除
            if (AssetDatabase.LoadAssetAtPath<Font>(OldTtfPath) != null)
            {
                AssetDatabase.DeleteAsset(OldTtfPath);
                Debug.Log($"[SetupDefaultFont] 古いフォントファイルを削除しました: {OldTtfPath}");
            }

            // Dynamic TMP_FontAsset の生成または取得
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    font,
                    90,
                    9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024,
                    1024,
                    AtlasPopulationMode.Dynamic,
                    true
                );

                fontAsset.name = "NotoSansJP-VariableFont_wght_SDF";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "NotoSansJP-VariableFont_wght_SDF Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                {
                    foreach (var tex in fontAsset.atlasTextures)
                    {
                        if (tex != null)
                        {
                            tex.name = "NotoSansJP-VariableFont_wght_SDF Atlas";
                            AssetDatabase.AddObjectToAsset(tex, fontAsset);
                        }
                    }
                }
            }
            else
            {
                fontAsset.name = "NotoSansJP-VariableFont_wght_SDF";
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "NotoSansJP-VariableFont_wght_SDF Material";
                }
            }

            var serializedFont = new SerializedObject(fontAsset);
            var clearDynamicDataProp = serializedFont.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearDynamicDataProp != null)
            {
                clearDynamicDataProp.boolValue = true;
                serializedFont.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(fontAsset);

            // TMP Settings の更新
            var tmpSettings = TMP_Settings.instance;
            if (tmpSettings != null)
            {
                var serializedSettings = new SerializedObject(tmpSettings);
                var defaultFontProp = serializedSettings.FindProperty("m_defaultFontAsset");
                if (defaultFontProp != null)
                {
                    defaultFontProp.objectReferenceValue = fontAsset;
                }

                var fallbackListProp = serializedSettings.FindProperty("m_fallbackFontAssets");
                if (fallbackListProp != null)
                {
                    fallbackListProp.arraySize = 1;
                    fallbackListProp.GetArrayElementAtIndex(0).objectReferenceValue = fontAsset;
                }

                serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(tmpSettings);
            }

            // プレハブおよびシーン内のすべての TMP_Text のフォント置換 & 絵文字サニタイズ
            ReplaceFontsAndSanitizeInPrefabs(fontAsset);
            ReplaceFontsAndSanitizeInScenes(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupDefaultFont] デフォルトフォントを {font.name} ({FontAssetPath}) に正常に差し替え、全プレハブ・シーンのフォントとテキストをサニタイズ・更新しました。");
        }

        /// <summary>
        /// 文字列から絵文字および未収録特殊文字を除去・置換する。
        /// </summary>
        public static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            
            // ➔ (U+2794) や矢印、絵文字などを標準テキスト・記号に置換
            string sanitized = input.Replace("➔", "▶")
                                    .Replace("➡️", "▶")
                                    .Replace("⚡️", "")
                                    .Replace("⚡", "")
                                    .Replace("📱", "")
                                    .Replace("⏳", "")
                                    .Replace("🚨", "")
                                    .Replace("🚪", "")
                                    .Replace("👟", "")
                                    .Replace("🪙", "")
                                    .Replace("🧲", "")
                                    .Replace("🔥", "")
                                    .Replace("✨", "")
                                    .Replace("🛸", "")
                                    .Replace("🕶️", "")
                                    .Replace("🕶", "")
                                    .Replace("🥫", "")
                                    .Replace("🛡️", "")
                                    .Replace("🛡", "")
                                    .Replace("🔧", "");

            // 残りの Unicode 絵文字を除去
            sanitized = EmojiRegex.Replace(sanitized, "");
            return sanitized.Trim();
        }

        /// <summary>
        /// 全プレハブ内の TMP_Text コンポーネントのフォント置換および絵文字サニタイズを行う。
        /// </summary>
        private static void ReplaceFontsAndSanitizeInPrefabs(TMP_FontAsset targetFont)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int replacedCount = 0;

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tmpTexts = prefab.GetComponentsInChildren<TMP_Text>(true);
                if (tmpTexts.Length == 0) continue;

                bool modified = false;
                foreach (var tmp in tmpTexts)
                {
                    if (tmp.font != targetFont)
                    {
                        tmp.font = targetFont;
                        tmp.fontSharedMaterial = targetFont.material;
                        modified = true;
                    }

                    string originalText = tmp.text;
                    string sanitized = SanitizeText(originalText);
                    if (originalText != sanitized)
                    {
                        tmp.text = sanitized;
                        modified = true;
                    }

                    if (modified)
                    {
                        EditorUtility.SetDirty(tmp);
                        replacedCount++;
                    }
                }

                if (modified)
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            Debug.Log($"[SetupDefaultFont] プレハブ内の {replacedCount} 個の TMP_Text を置換・サニタイズしました。");
        }

        /// <summary>
        /// 全シーン内の TMP_Text コンポーネントのフォント置換および絵文字サニタイズを行う。
        /// </summary>
        private static void ReplaceFontsAndSanitizeInScenes(TMP_FontAsset targetFont)
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            int replacedCount = 0;

            var currentScene = EditorSceneManager.GetActiveScene();
            var currentScenePath = currentScene.path;

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool modified = false;

                var rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    var tmpTexts = root.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var tmp in tmpTexts)
                    {
                        bool itemModified = false;
                        if (tmp.font != targetFont)
                        {
                            tmp.font = targetFont;
                            tmp.fontSharedMaterial = targetFont.material;
                            itemModified = true;
                        }

                        string originalText = tmp.text;
                        string sanitized = SanitizeText(originalText);
                        if (originalText != sanitized)
                        {
                            tmp.text = sanitized;
                            itemModified = true;
                        }

                        if (itemModified)
                        {
                            EditorUtility.SetDirty(tmp);
                            modified = true;
                            replacedCount++;
                        }
                    }
                }

                if (modified)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[SetupDefaultFont] シーン内の {replacedCount} 個の TMP_Text を置換・サニタイズしました。");
        }
    }
}
