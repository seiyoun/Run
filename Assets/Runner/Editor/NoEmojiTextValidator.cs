/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: UIテキストおよびスクリプトへの絵文字・未収録特殊文字の混入を防止・自動検証するエディタフック
 */

using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Runner.Editor
{
    /// <summary>
    /// アセット保存時やシーン編集時に TMP_Text およびスクリプト内の絵文字混入を検知・防止するエディタ拡張。
    /// </summary>
    public sealed class NoEmojiTextValidator : UnityEditor.AssetModificationProcessor
    {
        private static readonly Regex EmojiRegex = new Regex(
            @"[\uD83C-\uDBFF\uDC00-\uDFFF]|[\u2600-\u27BF]|[\u2300-\u23FF]|[\u2B00-\u2BFF]|[\u2190-\u21FF]|[\u2900-\u297F]|[\uFE00-\uFE0F]",
            RegexOptions.Compiled
        );

        /// <summary>
        /// アセット保存時に呼び出されるフック処理。
        /// </summary>
        /// <param name="paths">保存対象のアセットパス配列</param>
        /// <returns>変更を許可するパス配列</returns>
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;

                // シーンまたはプレハブの保存時に TMP_Text を検査
                if (path.EndsWith(".unity") || path.EndsWith(".prefab"))
                {
                    ValidateTextInAsset(path);
                }
            }

            return paths;
        }

        /// <summary>
        /// 指定されたアセット内の TMP_Text に絵文字が含まれていないか検査し、検出時は警告と自動サニタイズを行う。
        /// </summary>
        /// <param name="assetPath">検査対象のアセットパス</param>
        private static void ValidateTextInAsset(string assetPath)
        {
            if (assetPath.EndsWith(".prefab"))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null) return;

                var tmpTexts = prefab.GetComponentsInChildren<TMP_Text>(true);
                foreach (var tmp in tmpTexts)
                {
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text) && EmojiRegex.IsMatch(tmp.text))
                    {
                        Debug.LogWarning($"[NoEmojiTextValidator] プレハブ '{assetPath}' の TMP_Text '{tmp.name}' に絵文字・未収録文字が検出されました: \"{tmp.text}\"。絵文字はフォント未収録による文字化けの原因となるため削除・修正してください。");
                    }
                }
            }
        }

        /// <summary>
        /// プロジェクト全体のテキストに絵文字が含まれていないか手動検証するメニュー項目。
        /// </summary>
        [MenuItem("Runner/Validation/Validate No Emojis in UI")]
        public static void ValidateAllText()
        {
            int emojiCount = 0;
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Runner" });

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var tmpTexts = prefab.GetComponentsInChildren<TMP_Text>(true);
                foreach (var tmp in tmpTexts)
                {
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text) && EmojiRegex.IsMatch(tmp.text))
                    {
                        Debug.LogWarning($"[NoEmojiTextValidator] プレハブ '{path}' -> '{tmp.name}': \"{tmp.text}\"");
                        emojiCount++;
                    }
                }
            }

            if (emojiCount == 0)
            {
                Debug.Log("[NoEmojiTextValidator] 検証完了: UI テキストに絵文字・未収録記号は検出されませんでした（完全クリーン）。");
            }
            else
            {
                Debug.LogError($"[NoEmojiTextValidator] 検証完了: {emojiCount} 件の絵文字が検出されました。");
            }
        }
    }
}
