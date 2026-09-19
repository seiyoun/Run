# LocalStorage モジュール (Shiyuan.Foundation.LocalStorage)

本モジュールは、クライアント側でのローカルデータの保存（セーブデータなど）を管理するための共通ストレージ基盤を提供します。

---

## 主な機能

* **`LocalStorageService`**:
  - オブジェクトを JSON 形式にシリアライズしてローカルに保存 (`Save`)。
  - ローカルファイルをデシリアライズしてオブジェクトを復元 (`Load`)。
  - 保存・読込結果ステータス (`LocalStorageResult` / `LocalStorageStatus`) の確認。
  - 同期 API による統一的な書き込み・読み込みインターフェース。

---

## 導入・使用例

### データのセーブとロード

任意のオブジェクト（Serializable なクラス）を指定したファイル名で保存・復元できます。

```csharp
using Shiyuan.Foundation.LocalStorage;
using UnityEngine;

[System.Serializable]
public class UserSettings
{
    public float BGMVolume = 1.0f;
    public float SEVolume = 1.0f;
}

public class SettingsController
{
    private const string SettingsFileName = "user_settings";
    private readonly LocalStorageService localStorageService = new LocalStorageService();

    // 保存処理
    public void SaveSettings(UserSettings settings)
    {
        localStorageService.Save(SettingsFileName, settings);
        // 保存処理は例外が発生しない限り void で処理されます（内部でディレクトリ生成等も自動実行）
        Debug.Log("設定を保存しました。");
    }

    // 読込処理
    public UserSettings LoadSettings()
    {
        var result = localStorageService.Load<UserSettings>(SettingsFileName);
        if (result.IsSuccess)
        {
            return result.Value; // 読み込んだデータ
        }
        else if (result.Status == LocalStorageStatus.NotFound)
        {
            // ファイルが存在しない場合は初期データを返す
            return new UserSettings();
        }
        else
        {
            Debug.LogError($"読み込み中にエラーが発生しました。Status: {result.Status}, Message: {result.Message}");
            return new UserSettings();
        }
    }
}
```

---

## 注意事項

1. **暗号化とセキュリティ**:
   `LocalStorageService` は、保存データを自動的に AES-CBC (256-bit) で暗号化し、さらに HMAC-SHA256 によるデジタル署名を付与して保存します。これにより、ローカルのセーブデータが平文で露出するのを防ぐとともに、改ざんを検知した場合は読み込み時に復号エラー（`LocalStorageStatus.CryptoError` / `LocalStorageStatus.InvalidData`）として弾く高度なセキュリティ機能が標準で備わっています。
2. **エラーハンドリング**:
   セーブデータが存在しない場合の対応や、何らかの原因によるデータ破損（復号エラー含む）に対処するため、必ず `result.IsSuccess` または `result.Status` を確認したうえでデータを取り出すようにしてください。
