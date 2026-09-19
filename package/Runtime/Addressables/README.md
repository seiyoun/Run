# Addressables モジュール (Shiyuan.Foundation.Addressables)

本モジュールは、Addressables を用いたアセット（特に Prefab）の非同期ロードと解放処理をカプセル化し、呼び出し側での安全なリソースライフサイクル管理を提供します。

---

## 主な機能

* **`AddressablePrefabLoader`**:
  - Prefab の非同期ロード (`LoadAsync`) とインスタンス生成。
  - ロードしたアセットの参照カウント管理。
  - インスタンス破棄およびメモリ解放処理 (`Dispose`)。

---

## 導入・使用例

### Prefab のロードと自動解放
`AddressablePrefabLoader` を使用して Prefab を非同期でロードし、不要になった段階で `Dispose` を呼び出すことで、ロードされたすべてのリソースが安全にメモリから解放されます。

```csharp
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Shiyuan.Foundation.Addressables;

public class SampleView : MonoBehaviour
{
    // アカウント（参照）管理用のローダーインスタンスを保持
    private readonly AddressablePrefabLoader prefabLoader = new AddressablePrefabLoader();

    private async Task InitializeViewAsync(CancellationToken token)
    {
        // Addressables アドレスを指定して Prefab を非同期ロード
        GameObject uiRoot = await prefabLoader.LoadAsync("Assets/Prefabs/MyScreen.prefab", token);
        
        // 生成したオブジェクトの初期化処理
        if (uiRoot != null)
        {
            uiRoot.transform.SetParent(this.transform, false);
        }
    }

    private void OnDestroy()
    {
        // ロードした Prefab インスタンスを自動破棄＆メモリ解放
        prefabLoader?.Dispose();
    }
}
```

---

## 注意事項

1. **`Dispose` の呼び出し漏れ防止**:
   `AddressablePrefabLoader` でロードしたアセットは、明示的に `Dispose` を呼び出すまでメモリ上に維持されます。メモリリークを防ぐため、それを保持するクラスの破棄時（`OnDestroy` など）に必ず `Dispose` を呼び出してください。
2. **キャンセルトークンの伝搬**:
   ロード処理中に画面遷移などが発生した場合に備え、`LoadAsync` には必ず有効な `CancellationToken` を渡してください。
