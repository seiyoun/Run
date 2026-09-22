# Buff & Status Effect Design Guidelines (バフ・状態異常設計規約)

キャラクター等に付与されるバフ・デバフ・状態異常を設計・実装する際は、以下の原則とパターンを厳守してください。

## 1. Architecture & Responsibilities (アーキテクチャと責務分離)
バフシステムは疎結合なイベント駆動アーキテクチャを採用し、各コンポーネントの責務を明確に分離します。

```
[外部呼出元 (Shop/Debug等)]
      │ ApplyBuff(target, BuffType) / RemoveBuff(target, BuffType)
      ▼
[BuffManager] ── (生成) ──> [BuffFactory] ──> [IBuff (SpeedBuff等)]
      │ AddBuff(buff) / RemoveBuff(type)
      ▼
[IBuffTarget (PlayerController等)]
      │ 委譲
      ▼
[CharacterBuffHandler]
      │
      ├─ OnBuffApplied (IBuff) ──> [PlayerController] (パラメータ加算 / イベント購読)
      └─ OnBuffRemoved (IBuff) ──> [PlayerController] (パラメータ減算 / イベント解除)
```

- **`BuffManager` (静的マネージャー)**:
  - 外部呼出元（ショップ、デバッグコンソール、アイテム等）からのバフ付与・解除要求の統一エントリポイント。
  - `BuffManager.ApplyBuff(target, type)`: `BuffFactory.Create(type)` により `IBuff` を生成し、`target.AddBuff(buff)` を呼び出す。
  - `BuffManager.RemoveBuff(target, type)`: `target.RemoveBuff(type)` を呼び出す。
- **`IBuffTarget` (バフ受付インターフェース)**:
  - バフの付与を受け付けるエンティティ（`PlayerController` 等）が実装する。
  - `AddBuff(IBuff buff)` および `RemoveBuff(BuffType type)` を公開し、具体的なバフ管理コンポーネントへ委譲する。
- **`IBuff` (データモデル & ライフサイクル)**:
  - `Runner.IBuff` を実装した独立クラス（例: `SpeedBuff`, `HpRegenBuff`）として作成する。
  - **特定の対象（`PlayerController`, `ICharacterStatus`, `CharacterMovement2D` 等）への直接参照や依存を一切持たせない**。
  - 持続時間（`Duration`, `RemainingDuration`）、効果値（`Value`）、およびライフサイクルフラグ（`IsActive`）の管理に専念する。
  - 周期的な効果（リジェネ等）はイベント（例: `event Action<int> OnHealTick`）を発火し、バフ自身が直接ステータスを変更しない。
  - HUDでのゲージ描画・進捗率算出のため、`RemainingDuration` および `Duration` を正しく提供すること。
- **`BuffFactory` (純粋な生成ファクトリ)**:
  - `BuffFactory.Create(BuffType)` は `MasterDataManager` の設定値に基づき、純粋に `IBuff` インスタンスを生成して返す（ターゲット引数は不要）。
- **`CharacterBuffHandler` (一元管理 & イベント発火ハブ)**:
  - キャラクターにアタッチされ、バフリストの保持、毎フレームの `Tick` 更新、持続時間終了時の自動除外を担当する。
  - バフが有効化された際に `OnBuffApplied(IBuff)`、無効化・除外された際に `OnBuffRemoved(IBuff)` イベントを発火する。
- **キャラクター本体 (`PlayerController` 等)**:
  - `CharacterBuffHandler.OnBuffApplied` / `OnBuffRemoved` を購読し、バフ種別に応じたパラメータ増減（例: `movementComponent.MoveSpeed += speedBuff.Value`）やコールバック購読（例: `regenBuff.OnHealTick += HandleRegenTick`）を行う。
  - 個別のバフタイマー（`_speedBuffTimer` 等）や内部フラグを本体クラスに直接追加してはならない。

## 2. Lifecycle Management with CharacterBuffHandler (`CharacterBuffHandler` による管理)
- **バフの付与と解除**:
  - バフの追加は `buffHandler.AddBuff(buff)`、明示的な解除は `buffHandler.RemoveBuff(type)` または `buffHandler.RemoveBuff(buff)` で行う。
- **フレーム更新と自動除外**:
  - `CharacterBuffHandler` の `Update()` 内で登録中の全バフの `Tick(deltaTime)` が呼び出され、持続時間終了（`!buff.IsActive`）となったバフは `buff.Remove()` 実行後にリストから除外され、`OnBuffRemoved` イベントが発火する。
- **破棄時・死亡時のクリーンアップ**:
  - キャラクターの破棄時（`OnDestroy`）および死亡時には、必ず `buffHandler.ClearBuffs()` を呼び出して全バフを安全にロールバック・解除すること。

## 3. Re-application & Overwrite Policy (重複付与・上書きポリシー)
- **デフォルトは上書き・時間リセット**:
  - 同種のバフ（同一 `BuffId`）を重複付与する際の標準動作は「既存の同種バフを解除（`OnBuffRemoved` 発火）し、新しいバフを付与・有効化（`OnBuffApplied` 発火）する」とする（`CharacterBuffHandler.AddBuff` 内で自動制御）。
- **スタック・加算の取り扱い**:
  - 仕様として効果値のスタックや持続時間の加算延長を要求されている場合を除き、推測で勝手なスタック機構を作らない（YAGNI原則）。

## 4. Rollback Safety (安全なロールバック保証)
- **確実な通常値への復元**:
  - `OnBuffRemoved` のハンドラ内では、バフによって加算されたパラメータを確実に減算し、下限ガード（`Mathf.Max(0f, ...)`）を行って元の値へ戻すこと。
- **二重解除の防止**:
  - すでに解除済みのバフに対して `Remove()` が再度呼ばれた場合でも例外や不整合が起きないよう、`IBuff` 側で `isActive` フラグによる実行ガードを行う。

## 5. HUD & UI Integration Guidelines (HUD・UI連携規約)
- **純粋なView設計の厳守 (Pure View)**:
  - `BuffHUD` 等のUIコンポーネントは純粋な描画専用ビュー（View）とし、**`PlayerController` などのゲームプレイドメインオブジェクトを直接参照・ポーリング（`Update` 内での直接取得等）してはならない**。
  - HUDコンポーネントは描画用API（例: `SetBuff(float remainingDuration, float totalDuration)`, `ClearBuff()`）を公開し、外部から渡された数値やパラメータの描画、および表示/非表示の切り替えに専念すること。
- **総合HUD (`GameHUDView`) による一元中継・統括**:
  - ドメインオブジェクト（`PlayerController` や `CharacterBuffHandler`）の状態監視、および各サブHUDへの描画指示の伝達は、画面全体を統括する `GameHUDView` が一元的に担当する。
- **新規バフ追加時のHUD対応**:
  - 新たなバフを追加し、画面上にアイコンや残り時間を表示する必要がある場合は、`GameHUDView` 側で該当バフの検出と `BuffHUD` への描画伝達ロジックを更新・拡張すること。

## 6. Coding Conventions & YAGNI
- バフクラス、ハンドラクラス、UIクラスにおいても `unity-script-conventions`（メンバー記述順序、全関数へのXMLドキュメントコメント、[Tooltip]）を厳守する。
- ユーザーから明示的な指示がない限り、未要求のバフクラスやUI機能を「念のため」先行作成してはならない（Minimal Viable Change）。
