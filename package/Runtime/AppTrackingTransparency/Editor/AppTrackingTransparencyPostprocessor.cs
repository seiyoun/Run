/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: iOSビルド完了時に Xcode プロジェクトへ「AppTrackingTransparency.framework」を自動リンクするポストプロセッサ。
 */

#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Shiyuan.Foundation.Editor
{
    public static class AppTrackingTransparencyPostprocessor
    {
        /// <summary>
        /// iOS ビルド完了時に Xcode プロジェクトへ AppTrackingTransparency.framework を自動リンク（Weak Link）する。
        /// </summary>
        /// <param name="target">ビルドターゲット</param>
        /// <param name="path">ビルド出力先パス</param>
        [PostProcessBuild(2)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
                return;

            // 1. AppTrackingTransparency.framework を Weak Link として追加
            var projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromString(System.IO.File.ReadAllText(projectPath));
            
#if UNITY_2019_3_OR_NEWER
            string targetGuid = project.GetUnityFrameworkTargetGuid();
#else
            string targetGuid = project.TargetGuidByName("Unity-iPhone");
#endif
            project.AddFrameworkToProject(targetGuid, "AppTrackingTransparency.framework", true);
            System.IO.File.WriteAllText(projectPath, project.WriteToString());

            // 2. Info.plist に NSUserTrackingUsageDescription を自動設定
            string plistPath = System.IO.Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            
            string trackingKey = "NSUserTrackingUsageDescription";
            string trackingDescription = "最適化された広告を表示するためにデバイスの識別子を使用します。";
            plist.root.SetString(trackingKey, trackingDescription);
            
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
