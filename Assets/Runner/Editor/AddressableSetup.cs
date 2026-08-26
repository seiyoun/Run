/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: Addressables の初期設定およびプレハブのアドレス登録を行うエディタ拡張。
 */

using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Runner.Editor
{
    public static class AddressableSetup
    {
        private const string LoadingViewPrefabPath = "Assets/Runner/Prefabs/LoadingView.prefab";
        private const string LoadingViewAddress = "LoadingView";

        private const string PlayerPrefabPath = "Assets/Runner/Prefabs/Player.prefab";
        private const string PlayerAddress = "Player";

        private const string BackgroundPrefabPath = "Assets/Runner/Prefabs/Background.prefab";
        private const string BackgroundAddress = "Background";

        [MenuItem("Tools/Runner/Setup All Addressables")]
        public static void RegisterAllAddressables()
        {
            RegisterAddressable(LoadingViewPrefabPath, LoadingViewAddress);
            RegisterAddressable(PlayerPrefabPath, PlayerAddress);
            RegisterAddressable(BackgroundPrefabPath, BackgroundAddress);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AddressableSetup] すべての Addressables 登録が完了しました。");
        }

        [MenuItem("Tools/Runner/Setup Addressables For LoadingView")]
        public static void RegisterLoadingViewAddressable()
        {
            RegisterAddressable(LoadingViewPrefabPath, LoadingViewAddress);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Runner/Setup Addressables For Player")]
        public static void RegisterPlayerAddressable()
        {
            RegisterAddressable(PlayerPrefabPath, PlayerAddress);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Runner/Setup Addressables For Background")]
        public static void RegisterBackgroundAddressable()
        {
            RegisterAddressable(BackgroundPrefabPath, BackgroundAddress);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RegisterAddressable(string assetPath, string address)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[AddressableSetup] AddressableAssetSettings の取得に失敗しました。");
                return;
            }

            var group = settings.DefaultGroup;
            if (group == null)
            {
                group = settings.CreateGroup("Default Local Group", false, false, true, null);
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[AddressableSetup] プレハブが見つかりません: {assetPath}");
                return;
            }

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            Debug.Log($"[AddressableSetup] '{assetPath}' を Addressables (Address: '{address}') に登録しました。");
        }
    }
}
