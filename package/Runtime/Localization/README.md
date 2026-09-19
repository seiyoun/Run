# Localization モジュール (Shiyuan.Foundation.Localization)

本モジュールは、Unity の Localization システムをラップし、多言語対応リソースの初期化、同期・非同期による言語の切り替え、およびローカライズされたテキストの自動反映を提供します。

---

## 主な機能

* **`LocalizationManager`**:
  - 多言語データの非同期初期化および読込待機 (`InitializeAsync`)。
  - キーを指定した翻訳テキストの取得 (`GetText`)。
  - 言語コードを指定したロケールの切り替え (`ChangeLocale`)。
* **`LocalizedTMPText`**:
  - TextMesh Pro (uGUI) コンポーネントにアタッチすることで、現在選択されているロケール（言語）の翻訳結果を自動で反映するユーティリティコンポーネント。

---

## 導入・使用例

### 1. 初期化の待機
アプリ起動時やシーンロード前に、多言語リソースが完全にロードされるのを待機します（シングルトンの `Instance` を使用します）。

```csharp
using System.Threading;
using System.Threading.Tasks;
using Shiyuan.Foundation.Localization;

public class AppInitializer
{
    public async Task InitializeAsync(CancellationToken token)
    {
        // 言語リソースの初期化完了を待つ
        await LocalizationManager.Instance.InitializeAsync(token);
    }
}
```

### 2. コードからの翻訳テキスト取得
翻訳キーを指定して、現在設定されている言語の文字列を同期的に取得します。

```csharp
using Shiyuan.Foundation.Localization;
using UnityEngine;

public class LocalizedMessagePresenter : MonoBehaviour
{
    public void ShowWelcomeMessage()
    {
        // "WELCOME_MESSAGE" というキーの翻訳を取得
        string welcomeText = LocalizationManager.Instance.GetText("WELCOME_MESSAGE");
        Debug.Log(welcomeText);
    }
}
```

---

## 注意事項

1. **初期化の完了確認**:
   翻訳テキストを正しく取得するためには、`LocalizationManager.Instance.InitializeAsync` の完了後に呼び出す必要があります。初期化前に呼び出そうとすると、翻訳前のキー文字列自体がそのまま返されます。
2. **テーブルとキーの設定**:
   本モジュールでは、インスペクター等で指定したテーブル名（デフォルトは `"Localization"`）を基準に文字列を取得します。Unity の Localization テーブルに定義されたキーと完全に一致するキーを指定してください。
