/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Scripting Define Symbols（シンボル定義）の追加・削除・有効化・無効化を管理するエディタ拡張ウィンドウ。
 *                Android, iOS, Windows (Standalone) すべてのビルドプロファイルへ同時に反映します。
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Runner.Editor
{
    /// <summary>
    /// Scripting Define Symbols を GUI 上で直感的に管理・編集し、全プロファイル（Android, iOS, Windows）に一括適用するエディタウィンドウ。
    /// </summary>
    public sealed class DefineSymbolsWindow : EditorWindow
    {
        private const string PrefsKey = "Runner_DefineSymbols_List";

        /// <summary>
        /// 同時適用する対象プラットフォーム一覧 (Windows/Mac Standalone, iOS, Android)
        /// </summary>
        private static readonly NamedBuildTarget[] TargetPlatforms =
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.iOS,
            NamedBuildTarget.Android
        };

        [Serializable]
        private class SymbolItem
        {
            public bool isEnabled = true;
            public string name = string.Empty;
        }

        [SerializeField]
        private List<SymbolItem> symbols = new();

        private Vector2 scrollPosition;

        [MenuItem("Tools/Runner/Scripting Define Symbols Manager", false, 10)]
        public static void Open()
        {
            var window = GetWindow<DefineSymbolsWindow>("Define Symbols");
            window.minSize = new Vector2(420, 360);
            window.Show();
        }

        private void OnEnable()
        {
            LoadSymbols();
        }

        /// <summary>
        /// 全プラットフォームの PlayerSettings および保存済みシンボル一覧からリストを読み込む。
        /// </summary>
        private void LoadSymbols()
        {
            symbols.Clear();

            var activeSymbols = new HashSet<string>();

            // 1. 各ターゲットプロファイルから現在有効なシンボルを取得・マージ
            foreach (var target in TargetPlatforms)
            {
                var symbolsStr = PlayerSettings.GetScriptingDefineSymbols(target);
                if (!string.IsNullOrEmpty(symbolsStr))
                {
                    foreach (var s in symbolsStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        activeSymbols.Add(s.Trim());
                    }
                }
            }

            // 2. EditorPrefs に保存されている全シンボル履歴を読み込み
            var savedJson = EditorPrefs.GetString(PrefsKey, string.Empty);
            var knownSymbols = new List<string>();
            if (!string.IsNullOrEmpty(savedJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<SavedSymbolsWrapper>(savedJson);
                    if (wrapper?.savedSymbols != null)
                    {
                        knownSymbols.AddRange(wrapper.savedSymbols);
                    }
                }
                catch
                {
                    // 無視
                }
            }

            // 3. activeSymbols と knownSymbols をマージしてリストを構築
            var allSymbolNames = new HashSet<string>(activeSymbols);
            foreach (var known in knownSymbols)
            {
                if (!string.IsNullOrWhiteSpace(known))
                {
                    allSymbolNames.Add(known.Trim());
                }
            }

            // プロジェクトの基本プリセット候補（未登録なら追加）
            var defaultPresets = new[] { "DEBUG_LOG", "ADDRESSABLE_LOG" };
            foreach (var preset in defaultPresets)
            {
                allSymbolNames.Add(preset);
            }

            foreach (var name in allSymbolNames.OrderBy(s => s))
            {
                symbols.Add(new SymbolItem
                {
                    name = name,
                    isEnabled = activeSymbols.Contains(name)
                });
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("適用対象プロファイル:", EditorStyles.boldLabel, GUILayout.Width(140));
                EditorGUILayout.LabelField("Android, iOS, Windows (Standalone)", EditorStyles.label);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Define Symbols 一覧", EditorStyles.boldLabel);

            // シンボル一覧スクロールビュー
            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandHeight(true)))
            {
                scrollPosition = scroll.scrollPosition;

                for (int i = 0; i < symbols.Count; i++)
                {
                    var item = symbols[i];
                    using (new EditorGUILayout.HorizontalScope("box"))
                    {
                        // 有効/無効 チェックボックス
                        item.isEnabled = EditorGUILayout.Toggle(item.isEnabled, GUILayout.Width(22));

                        // シンボル名テキストフィールド
                        using (new EditorGUI.DisabledScope(!item.isEnabled))
                        {
                            item.name = EditorGUILayout.TextField(item.name);
                        }

                        // [-] 削除ボタン
                        var prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
                        if (GUILayout.Button("-", GUILayout.Width(26), GUILayout.Height(18)))
                        {
                            symbols.RemoveAt(i);
                            GUIUtility.ExitGUI();
                            break;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                }

                if (symbols.Count == 0)
                {
                    EditorGUILayout.HelpBox("シンボルが登録されていません。[+] ボタンを押して追加してください。", MessageType.Info);
                }
            }

            // [+] 追加ボタン
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.65f, 1f, 0.65f);
                if (GUILayout.Button("+ 追加", GUILayout.Width(90), GUILayout.Height(24)))
                {
                    symbols.Add(new SymbolItem
                    {
                        name = "NEW_SYMBOL",
                        isEnabled = true
                    });
                }
                GUI.backgroundColor = prevColor;
            }

            EditorGUILayout.Space(12);

            // 下部ボタン領域 (Cancel / Apply)
            using (new EditorGUILayout.HorizontalScope())
            {
                // Cancel ボタン
                if (GUILayout.Button("Cancel", GUILayout.Height(28)))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }

                GUILayout.Space(10);

                // Apply ボタン
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("Apply (全プロファイルに適用)", GUILayout.Height(28)))
                {
                    ApplySymbols();
                    Close();
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = prevColor;
            }

            EditorGUILayout.Space(6);
        }

        /// <summary>
        /// 有効になっているシンボルを Android, iOS, Windows (Standalone) の全 PlayerSettings に適用し、リスト全体を保存する。
        /// </summary>
        private void ApplySymbols()
        {
            // 有効なシンボルを抽出
            var activeSymbols = symbols
                .Where(s => s.isEnabled && !string.IsNullOrWhiteSpace(s.name))
                .Select(s => s.name.Trim())
                .Distinct()
                .ToList();

            var defineStr = string.Join(";", activeSymbols);

            // Android, iOS, Windows (Standalone) すべてのプロファイルに設定
            foreach (var target in TargetPlatforms)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, defineStr);
            }

            // 全シンボル名を履歴として保存（次回無効状態のまま再編集できるようにする）
            var allNames = symbols
                .Where(s => !string.IsNullOrWhiteSpace(s.name))
                .Select(s => s.name.Trim())
                .Distinct()
                .ToArray();

            var wrapper = new SavedSymbolsWrapper { savedSymbols = allNames };
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(wrapper));

            AssetDatabase.SaveAssets();
            Debug.Log($"[DefineSymbolsWindow] Scripting Define Symbols を全プロファイル (Android, iOS, Standalone) に反映しました:\n{defineStr}");
        }

        [Serializable]
        private class SavedSymbolsWrapper
        {
            public string[] savedSymbols;
        }
    }
}
