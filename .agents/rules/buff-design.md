# Buff & Status Effect Design Guidelines (バフ・状態異常設計規約)

キャラクター等に付与されるバフ・デバフ・状態異常を設計・実装する際は、以下の原則とパターンを厳守してください。

## 1. Architecture & Encapsulation (アーキテクチャとカプセル化)
- **`IBuff` インターフェースの実装**:
  - 新規バフは必ず `Runner.IBuff` を実装した独立クラス（例: `SpeedBuff`）として作成する。
  - 効果の適用（`Apply`）、解除（`Remove`）、時間経過更新（`Tick`）はバフクラス自身で完結させる（カプセル化）。
- **キャラクター本体の肥大化防止**:
  - `PlayerController` や `EnemyController` などの本体クラスに、個別のバフタイマー（例: `_speedBuffTimer`, `_attackBuffTimer`）やフラグを直接追加してはならない。
  - キャラクターは `CharacterBuffHandler` を介してバフを管理し、本体の責務を汚染しないこと。

## 2. Lifecycle Management with CharacterBuffHandler (`CharacterBuffHandler` による一元管理)
- **バフの付与と解除**:
  - バフの追加は `buffHandler.AddBuff(buff)`、明示的な解除は `buffHandler.RemoveBuff(buff)` または `buffHandler.RemoveBuffsOfType<T>()` で行う。
- **フレーム更新と自動除外**:
  - `CharacterBuffHandler` の `Update()` 内で登録中の全バフの `Tick(deltaTime)` が呼び出され、持続時間終了（`!buff.IsActive`）となったバフは自動的にリストから除外される。
- **破棄時・死亡時のクリーンアップ**:
  - キャラクターの破棄時（`OnDestroy`）および死亡時には、必ず `buffHandler.ClearBuffs()` を呼び出して全バフを安全にロールバック・解除すること。

## 3. Re-application & Overwrite Policy (重複付与・上書きポリシー)
- **デフォルトは上書き・時間リセット**:
  - 同種のバフを重複付与する際の標準動作は「既存の同種バフを解除し、新しいバフを付与する（時間リセット・上書き）」とする。
  ```csharp
  buffHandler.RemoveBuffsOfType<SpeedBuff>();
  buffHandler.AddBuff(new SpeedBuff(movementComponent, multiplier, duration));
  ```
- **スタック・加算の取り扱い**:
  - 仕様として効果値のスタックや持続時間の加算延長を要求されている場合を除き、推測で勝手なスタック機構を作らない（YAGNI原則）。

## 4. Rollback Safety (安全なロールバック保証)
- **確実な通常値への復元**:
  - `Remove()` メソッドでは、バフによって変更されたパラメータ（速度倍率、攻撃力補正、コライダー設定等）を確実に元の通常値へ戻すこと。
- **二重解除の防止**:
  - すでに解除済みのバフに対して `Remove()` が再度呼ばれた場合でも例外や不整合が起きないよう、`isActive` フラグによる実行ガードを行う。

## 5. Coding Conventions & YAGNI
- バフクラスにおいても `unity-script-conventions`（メンバー記述順序、全関数へのXMLドキュメントコメント、[Tooltip]）を厳守する。
- ユーザーから明示的な指示がない限り、未要求のバフクラスを「念のため」先行作成してはならない（Minimal Viable Change）。

