/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: お金アイテムプレハブ（MoneyItem.prefab）を自動生成するエディタ拡張。
 */

#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Runner.Editor
{
    public static class MoneyPrefabCreator
    {
        private const string SpritePath = "Assets/Runner/Sprites/CoinSprite.png";
        private const string PrefabPath = "Assets/Runner/Prefabs/MoneyItem.prefab";

        [MenuItem("Tools/Runner/Create MoneyItem Prefab")]
        public static void CreateMoneyPrefab()
        {
            // 1. スプライトのインポート設定
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            // 2. プレハブ用 GameObject の構築
            var root = new GameObject("MoneyItem");
            root.tag = "Untagged";
            root.layer = 0; // Default layer

            // SpriteRenderer
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingOrder = 5;
            sr.color = Color.white;

            // CircleCollider2D (Trigger)
            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            // MoneyItem Component
            var moneyComp = root.AddComponent<MoneyItem>();
            var so = new SerializedObject(moneyComp);
            so.FindProperty("moneyAmount").longValue = 50;
            so.FindProperty("initialAttractSpeed").floatValue = 6f;
            so.FindProperty("attractAcceleration").floatValue = 18f;
            so.FindProperty("maxAttractSpeed").floatValue = 25f;
            so.FindProperty("enableBobbing").boolValue = true;
            so.FindProperty("bobHeight").floatValue = 0.12f;
            so.FindProperty("bobSpeed").floatValue = 3.5f;
            so.ApplyModifiedProperties();

            // 子オブジェクト: 中央の「¥」マーク (TextMeshPro)
            var textObj = new GameObject("YenText");
            textObj.transform.SetParent(root.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var tmp = textObj.AddComponent<TextMeshPro>();
            tmp.text = "¥";
            tmp.fontSize = 5.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.45f, 0.28f, 0.05f, 0.95f); // 落ち着いたゴールドブラウン
            tmp.sortingOrder = 6;

            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) tmp.font = defaultFont;

            var textRt = textObj.GetComponent<RectTransform>();
            if (textRt != null)
            {
                textRt.sizeDelta = new Vector2(1f, 1f);
            }

            // 3. プレハブとして保存
            Directory.CreateDirectory("Assets/Runner/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MoneyPrefabCreator] MoneyItem プレハブを正常に生成・保存しました: {PrefabPath}");
        }
    }
}
#endif

