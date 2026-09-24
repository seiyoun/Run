# Architecture & Loose Coupling Principles (アーキテクチャ・疎結合設計原則)

このドキュメントは、プロジェクト全体におけるアーキテクチャ設計、コンポーネント間の疎結合化、およびフォルダ配置の基本方針を定めます。

---

## 1. イベント駆動アーキテクチャと疎結合 (Event-Driven & Loose Coupling)

コンポーネント間の直接的な密結合を避け、イベント（`Action` / `event`）による疎結合設計を徹底します。

### ① ポーリング監視の厳禁
- **`Update()` 等の毎フレーム処理で他クラスの状態・変数を監視（ポーリング）する実装は禁止**とします。
- 状態変化やデータ更新は、発行側が `event Action` を発火し、受信側がイベントを受け取って必要な瞬間だけ再描画・処理を行う購読モデル（Pub/Sub）を採用してください。

```csharp
// ✕ NG: 毎フレーム状態を監視（密結合・無駄なCPU消費）
private void Update()
{
    if (GameProgressManager.Instance.CurrentSteps != lastSteps)
    {
        UpdateUI();
    }
}

// ◯ OK: イベント駆動式（疎結合・高パフォーマンス）
private void Awake()
{
    GameProgressManager.Instance.OnStepsChanged += HandleStepsChanged;
}

private void OnDestroy()
{
    if (GameProgressManager.Instance != null)
    {
        GameProgressManager.Instance.OnStepsChanged -= HandleStepsChanged;
    }
}
```

### ② 依存の単方向化 (発行側は受信側を知らない)
- マネージャやロジック層（Publisher）は、UI（HUD、モーダル等）やエフェクト（Subscriber）の存在を直接知ってはなりません。
- 「イベントを通知するだけ」にとどめ、画面表示や演出の差し替えがコアロジックに影響を与えないようにします。

### ③ 確実な購読解除とライフサイクル管理
- イベントを購読する場合は、必ず `OnDestroy()` または `OnDisable()` で購読解除（`-=`）を行い、メモリリークや破棄済みオブジェクトへの参照呼び出しを防止してください。

---

## 2. 曖昧な置き場（Utils等）の徹底排除とディレクトリ設計

コードの責務を常に明確にし、何でも入るゴミ箱フォルダを排除します。

### ① `Utils` フォルダの禁止
- `Utils` や `Misc` といった曖昧なディレクトリ・ファイルは作成してはなりません。
- すべてのクラスは「その機能がゲームループやUIのどこに位置づくか」に基づき、責務に応じた具体的なディレクトリへ配置してください。
  - エネミー制御 → `Gameplay/Characters/Enemy/`
  - ゲームスコア・記録 → `Gameplay/Record/`
  - UI → 目的別に `Gameplay/UI/HUD/`, `Gameplay/UI/Modal/` へ分離

### ② 共通処理の独立 asmdef 化
- 特定のドメインに依存しない真に汎用的な共通コンポーネント（例: `SafeArea.cs` 等）は、`Common/` 配下に配置し、独立した Assembly Definition（例: `Runner.Common.asmdef`）を付与して依存関係をクリーンに保ちます。

---

## 3. 実用的なファイル分割 (`partial class` の活用)

設計のための設計（オーバーエンジニアリング）を避け、シンプルさと凝集度を両立します。

- 単一のマネージャやクラスが肥大化した場合、無理に無関係なインターフェースを切ったり、小粒なサブマネージャを乱立させて参照関係を複雑にしてはなりません。
- Unity のインスペクター連携やクラスの凝集度を保ったまま可読性を高めるため、**関心ごと（機能軸）に応じた `partial class` によるファイル分割**を第一選択とします。
  - 例: `GameProgressManager.cs`（メイン・ライフサイクル）
  - 例: `GameProgressManager.Escape.cs`（脱出・制限時間管理）
  - 例: `GameProgressManager.Wave.cs`（ウェーブ管理）
  - 例: `GameProgressManager.Restock.cs`（ショップ補充管理）

---

## 4. マスターデータの一元管理と同期アクセスの徹底

データ駆動型アーキテクチャの統一性を保ちます。

- すべてのマスターデータ（Enemy, Player, Stage, Wave, Weapon, Buff, Shop 等）は、ゲーム起動時（Boot/Title）に `MasterDataManager.Initialize()` 内で一括ロードし、メモリ上にキャッシュします。
- 実行時の各コンポーネントや State からのデータ取得は、キャッシュ辞書から O(1) で引ける同期メソッドを提供してください。
- 既にメモリキャッシュされているデータに対して、不要な `async Task` やキャンセレーショントークンを持ち回してはなりません。

---

## 5. YAGNI原則とクリーンコード規約の遵守

- **YAGNI原則**: 今この瞬間に呼び出し元が存在しないメソッド・フィールド・先行実装は一切作らない。
- **視覚的ノイズの排除**: `// ------------------` などのセクション区切りコメントは記述せず、メンバー配置順序で構造を表現する。
- **ドキュメントコメント**: すべての関数（Unity関数、private含む）に必ず正確な XML コメントを付与する。
