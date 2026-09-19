# Core モジュール (Shiyuan.Foundation.Core)

本モジュールは、Shiyuan.Foundation パッケージで利用される基礎的な共通基盤（ログ出力、シングルトン等）および汎用ユーティリティを提供します。

---

## 主な機能

* **`DebugLogger`**:
  - 条件付きコンパイル（`DEBUG_LOG` ディレクティブなど）に対応したログ出力ラッパー。
  - リリースビルド時にはコンパイルからログを完全に除外することで、パフォーマンスへの影響と不要なログ出力を抑えます。
* **`SingletonMonoBehaviour<T>`**:
  - スレッドセーフかつライフサイクル管理が容易な MonoBehaviour 用のシングルトン基盤。
  - シーンを跨いで常駐するオブジェクトやマネージャーの設計に利用されます。

---

## 導入・使用例

### シングルトンマネージャーとログ出力

```csharp
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Shiyuan.Foundation.Sample
{
    // 常駐するシングルトンマネージャーの設計
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        protected override void Awake()
        {
            base.Awake();
            if (!IsPrimaryInstance)
            {
                return; // 重複したインスタンスは自動破棄されます
            }

            // GameManager がシーン遷移しても破棄されないように設定
            DontDestroyOnLoad(gameObject);
            
            // ログ出力のテスト（DEBUG_LOG が定義されている場合のみ出力）
            DebugLogger.Log("GameManager が正常に起動しました。");
        }

        public void StartGame()
        {
            DebugLogger.Warning("ゲームを開始します。");
        }
    }
}
```

---

## ログの制御について

本パッケージおよびアプリ内のログ出力は、パフォーマンスとセキュリティ上の観点から、不要なログ出力を制御するためのディレクティブを設定することができます。詳細は `unity-logging` カスタムスキルを参照してください。
* `DebugLogger.Log`: 通常ログ。
* `DebugLogger.Warning`: 警告ログ。
* `DebugLogger.Error`: エラーログ。
* `DebugLogger.Exception`: 例外ログ。
