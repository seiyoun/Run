# Scenes モジュール (Shiyuan.Foundation.Scenes)

本モジュールは、Unity アプリケーションにおけるシーンのロード、アンロード、および遷移時の各シーンごとのライフサイクル状態管理を処理するための共通基盤を提供します。

---

## 主な機能

* **`ISceneLifecycle`**:
  - 各シーンの初期化・終了処理をフックするためのインターフェース。シーン遷移時に自動的に呼び出されます。
    - `OnInitializeAsync`: シーン進入時の非同期初期化処理。
    - `OnFinalizeAsync`: シーン離脱時のクリーンアップ・解放処理。
* **`SceneManagerBase` (およびその派生)**:
  - アプリケーション全体のシーン遷移や、遷移時のフェード演出、パラメータ（引数）の受け渡しなどを管理します。

---

## 導入・使用例

### シーンのライフサイクル管理クラスの実装
新規追加する Unity シーンの Root（または主要なオブジェクト）にアタッチするコンポーネントで `ISceneLifecycle` を実装します。これにより、シーン単位での依存解決（Bind）やリソース解放（Unbind）を安全に行うことができます。

```csharp
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Shiyuan.Foundation.Scenes;

public class MainSceneLifecycle : SceneLifecycleBase
{
    /// <summary>
    /// 通信完了後に遷移パラメータを受け取り、シーン固有の非同期初期化を実行する。
    /// </summary>
    protected override async Task OnInitializeAsync(object parameter, CancellationToken token)
    {
        // 1. シーンパラメータの解析とデータ復元
        if (parameter is MySceneParameter myParam)
        {
            // ...
        }

        // 2. シーン固有の依存解決や ViewModel のバインディング
        await Task.Delay(100, token); // 初期化処理のシミュレート
    }

    /// <summary>
    /// シーン固有の破棄処理。
    /// </summary>
    protected override void OnDestroy()
    {
        // 1. ViewModel のバインド解除や破棄
        // 2. ロードしたアセットリソースの解放
    }
}
```

---

## 注意事項

1. **シーンパラメータの型安全**:
   `SceneParameter` を使用して遷移先へデータを渡す場合、データの型キャスト時に安全なキャスト（`as` 演算子など）を行い、例外を防いでください。
2. **非同期のタイムアウト / キャンセル処理**:
   `OnInitializeAsync` に渡される `CancellationToken` は、ユーザーが初期化中に他の画面に遷移しようとした際などにキャンセル状態になります。非同期処理には常にこのトークンを伝搬させ、不要な処理がバックグラウンドで動き続けないようにしてください。
