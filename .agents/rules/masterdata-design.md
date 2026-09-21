# Master Data Design Guidelines (マスターデータ設計規約)

ゲーム内のパラメータ設定、エネミー・プレイヤー・武器・ステージ・ショップ等の静的データを追加・改修する際は、以下の原則とパターンを厳守してください。

## 1. Storage & Placement (データの配置とフォーマット)
- **JSON 形式と配置パス**:
  - すべてのマスターデータ JSON ファイルは `Assets/Runner/Resources/Data/{Feature}Data.json` に配置する。
  - 例: `EnemyData.json`, `PlayerData.json`, `ShopData.json`, `StageData.json`, `WeaponData.json`
- **軽量データの保持原則**:
  - マスターデータには数値パラメータや識別文字列（ID、表示名、スプライトファイル名等）のみを保持し、テクスチャやプレハブ等の重いアセット参照を直接含めてはならない。

## 2. Model Structure & Deserialization (モデルクラスとJSONデシリアライズ)
- **アセンブリ定義**:
  - マスターデータ関連のクラス・Enumはすべて `Runner.MasterData.asmdef`（名前空間 `Runner`）に配置する。
- **クラス設計パターン**:
  - `[Serializable]` を付与したモデルクラスを作成する。
  - JSONマッピング用フィールドは public camelCase、外部公開用プロパティは public PascalCase（getter-only）とする。
  - リスト形式の JSON（`{ "items": [ ... ] }`）を `JsonUtility` で安全に読み込むため、内部コンテナクラス（例: `private class XxxContainer { public List<Xxx> items; }`）を定義する。
- **リソース読み込みメソッド**:
  - モデルクラス内に `public static List<T> LoadAllFromResources(string path = DefaultResourcePath)` を実装し、`Resources.Load<TextAsset>` でロード・デシリアライズする。

## 3. Centralized Cache via MasterDataManager (`MasterDataManager` による一元キャッシュ)
- **ゲーム起動時の事前一括ロード**:
  - `MasterDataManager.InitializeAsync()`（または `Initialize()`）により、ゲーム開始時（Boot/Title）に全マスターデータをロードし、内部の Dictionary / List にキャッシュする。
  - 実行中に毎フレーム `Resources.Load` や JSON パースを行ってはならない。
- **高速アクセスとフォールバック保証**:
  - 各データ型に対応する取得メソッド（例: `GetEnemyMasterData(EnemyType)`, `GetWeaponMasterData(WeaponType)` 等）を提供する。
  - 未登録のキーやロード失敗時でも例外でクラッシュしないよう、デフォルト値生成やフォールバック処理を徹底する。

## 4. Usage by Runtime Components (実行時コンポーネントでの利用)
- **データ駆動の適用**:
  - キャラクターや武器のコントローラー（`EnemyController`, `PlayerController` 等）は、生成時・リスポーン時に `MasterDataManager` から最新のパラメータを取得し、各コンポーネント（Status, Movement, Collider等）へ一括適用（`ApplyData`）する。
- **ゲームバランス調整の独立性**:
  - パラメータ調整はコード修正ではなく JSON ファイルの編集のみで完結できるように設計する。

## 5. Coding Conventions & YAGNI
- `unity-script-conventions`（メンバー記述順序、全関数へのXMLドキュメントコメント、[Tooltip]）を厳守する。
- 未使用・未要求のデータフィールドを先行して追加しない（Minimal Viable Change）。

