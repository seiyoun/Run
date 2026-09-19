# Shiyuan Foundation Package

`com.shiyuan.foundation` は、クライアントアプリケーション開発で利用できる汎用共通基盤システムをまとめた Unity パッケージです。
本パッケージは Unity Package Manager (UPM) を介して他のプロジェクトでもそのままインポートして再利用可能です。

---

## 包含する機能モジュール

本パッケージは以下のモジュールで構成されており、それぞれアセンブリ（`.asmdef`）が分離されています。各モジュールの詳細な仕様や使い方については、リンク先の個別ドキュメントをご参照ください。

1. **[Core](Runtime/Core/README.md)** (`Shiyuan.Foundation.Core`):
   - 共通のログ出力機能（`DebugLogger`）および共通ユーティリティ（シングルトン等）。
2. **[Addressables](Runtime/Addressables/README.md)** (`Shiyuan.Foundation.Addressables`):
   - `AddressablePrefabLoader` を用いた安全な Prefab の非同期ロードと解放、自動参照カウント管理。
3. **[LocalStorage](Runtime/LocalStorage/README.md)** (`Shiyuan.Foundation.LocalStorage`):
   - ローカル保存用サービス（`LocalStorageService`）。同期 API でのセーブ・ロード。
4. **[Localization](Runtime/Localization/README.md)** (`Shiyuan.Foundation.Localization`):
   - `LocalizationManager` による多言語リソースのロード待機、初期化、ロケールの変更。
5. **[Scenes](Runtime/Scenes/README.md)** (`Shiyuan.Foundation.Scenes`):
   - シーン切り替えと各シーンごとのライフサイクル状態管理 (`ISceneLifecycle` / `SceneStateMachineBase`)。
6. **[Firebase (Auth)](Runtime/Firebase/Auth/README.md)** (`Shiyuan.Foundation.Firebase` / `Shiyuan.Foundation.Firebase.Editor`):
   - Firebase ログインプロバイダー（Google Play Games, Apple 等）、Functions API通信用クライアント（`FunctionsApiClient`）および iOS ビルド自動化処理。
7. **[Effects](Runtime/Effects/README.md)** (`Shiyuan.Foundation.Effects`):
   - 画面クリック時などのタップエフェクト再生システム（`TouchEffectPlayer`）。

---

## 導入方法

### 1. ローカルフォルダから導入する場合 (file 参照)
別プロジェクトの `Packages/manifest.json` を開き、`dependencies` に以下のようにローカルパスを指定します。

```json
{
  "dependencies": {
    "com.shiyuan.foundation": "file:../path/to/com.shiyuan.foundation"
  }
}
```

### 2. Git 経由で導入する場合 (Git 参照)
同じく `Packages/manifest.json` の `dependencies` に Git リポジトリのURL（必要に応じて subdirectory パラメータ）を指定します。

```json
{
  "dependencies": {
    "com.shiyuan.foundation": "https://github.com/seiyoun/hero-client.git?path=Packages/com.shiyuan.foundation"
  }
}
```
