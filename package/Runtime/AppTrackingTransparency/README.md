# App Tracking Transparency (ATT) モジュール

iOS 14 以降でのユーザーのプライバシー保護に基づき、広告識別子（IDFA）の取得許可をユーザーに要求するための iOS ネイティブブリッジおよび C# マネージャークラスを提供するモジュールです。

## 提供機能
1. **ATTダイアログの要求**: ネイティブのトラッキング許可プロンプトを非同期で呼び出します。
2. **ステータス取得**: `NotDetermined`, `Restricted`, `Denied`, `Authorized` などの許可状態を安全に取得します。
3. **IL2CPP 対応**: AOT（IL2CPP）ビルド下でもクラッシュせずに逆参照コールバックが受け取れるように、`[MonoPInvokeCallback]` 属性付きの静的メソッドでラッピングされています。

## ディレクトリ構成
- `AppTrackingTransparencyManager.cs`: C# のマネージャークラス。
- `Plugins/iOS/AppTrackingTransparencyBridge.mm`: Objective-C++ による iOS ネイティブコード。

## 使い方

### 1. トラッキングダイアログの表示要求

アプリの起動処理が完了し、Unity の描画画面がアクティブ（かつフォーカスを取得）した段階で以下のように呼び出します。

```csharp
using Shiyuan.Foundation.AppTrackingTransparency;
using System.Threading.Tasks;

public async void Start()
{
#if UNITY_IOS && !UNITY_EDITOR
    var tcs = new TaskCompletionSource<bool>();
    AppTrackingTransparencyManager.RequestAuthorization((status) =>
    {
        tcs.SetResult(true);
    });
    await tcs.Task;
#endif

    // ATTの判断が完了した後に各種SDK（AdMob等）の初期化を走らせる
    AdMobInitializer.Initialize();
}
```

### 2. トラッキング許可ステータスの取得

現在のステータスを同期で取得したい場合は、以下を呼び出します。

```csharp
var status = AppTrackingTransparencyManager.GetStatus();
if (status == AppTrackingTransparencyManager.AuthorizationStatus.Authorized)
{
    // 許可されている場合の処理
}
```

## 注意事項
- iOS実機で正常にダイアログを表示するためには、Xcodeプロジェクトへの `AppTrackingTransparency.framework` のリンクと、`Info.plist` への `NSUserTrackingUsageDescription` キーの設定が必要です。
- 本パッケージの `Editor/XcodeProjectPostProcessor.cs` が、ビルド時に自動的にこれらの設定を構成するよう自動化されています。
