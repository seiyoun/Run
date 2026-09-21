# Enemy Design Guidelines (エネミー設計規約)

エネミーの新規追加、カスタマイズ、およびAI行動を設計・実装する際は、以下の原則とパターンを厳守してください。

## 1. Data-Driven Enemy Definition (データドリブンなエネミー定義)
- **新規エネミー追加の標準フロー**:
  1. **`EnemyType` 列挙型の追加**: `Assets/Runner/Scripts/MasterData/Enemy/EnemyType.cs` に新しいエネミー識別子を追加する。
  2. **マスターデータ定義 (`EnemyData.json`)**: `Assets/Runner/Resources/Data/EnemyData.json` にパラメータ（`enemyType`, `enemyName`, `imageName`, `maxHp`, `moveSpeed`, `colliderRadius`, `attackPower`, `attackInterval`, `attackRange`）を追加する。
  3. **スプライトアセットの配置**: `Assets/Runner/Art/Sprites/Characters/{imageName}.png` にスプライトを配置する。
- **データ駆動による共通プレハブ活用**:
  - パラメータや見た目のみが異なる通常エネミーは、個別の専用Prefabを乱立させず、共通プレハブ（`Enemy.prefab`）に対して `EnemyController.ApplyData(data)` でスプライト・ステータス・コライダー半径を動的適用する。

## 2. Prefab & Component Structure (プレハブとコンポーネント構成)
- **必須コンポーネント構成**:
  - `CharacterMovement2D`: 2D移動制御
  - `CharacterStatus`: HP管理および被ダメージ判定（`IDamageable` 実装）
  - `CircleCollider2D`: 接触・被弾判定
  - `BehaviorGraphAgent`: Unity Behavior によるAI行動制御
  - `EnemyController`: 上記コンポーネントの統括、マスターデータの動的適用、Blackboard変数同期
- **責務の分離**:
  - 移動制御、体力管理、AI判断をすべて `EnemyController` に詰め込まず、各専用コンポーネントに委譲すること。

## 3. Unity Behavior & Blackboard Binding (AI行動とBlackboard連携)
- **Blackboard 変数同期**:
  - `EnemyController` は、マスターデータから取得した攻撃力・攻撃間隔・射程、および追尾ターゲットを `BehaviorGraphAgent` の Blackboard に同期する（`Self`, `Target`, `AttackPower`, `AttackInterval`, `AttackRange` 等）。
- **死亡割り込みの保証 (Die2DAction)**:
  - エネミーの死亡遷移を監視する `ConditionalGuardModifier` は、追尾や攻撃などの並行アクションを即座に中断できるよう、Observer Type を **`2` (LowerPriority)** に設定すること（None の場合は割り込みが発生せずエネミーが残り続ける）。
  - 死亡時の移動停止、当たり判定無効化、死亡アニメーション再生、遅延後のプール返却は `Die2DAction` で一元処理する。

## 4. Object Pooling & Lifecycle Management (オブジェクトプールとライフサイクル)
- **`EnemySpawner` によるプール管理**:
  - エネミーの生成・破棄は必ず `EnemySpawner` の `UnityEngine.Pool.ObjectPool<EnemyController>` を介して行う。
- **リスポーン時の初期化 (`ResetForPool`)**:
  - プールから再取得されたエネミーは、`ResetForPool(position, enemyType, target)` を呼び出して以下の状態を完全にリセットする:
    - 座標の設定と死亡フラグ（`isDeathHandled`）の初期化
    - コライダー（`colliderComponent.enabled = true`）の復帰
    - マスターデータの再適用（`ApplyData`）によるHP・速度・スプライト・コライダーの再設定
    - 移動の停止（`movementComponent.Stop()`）
    - AIエージェントの再起動（`behaviorAgent.Restart()`）

## 5. Coding Conventions & YAGNI
- エネミー関連スクリプトにおいても `unity-script-conventions`（メンバー記述順序、全関数へのXMLドキュメントコメント、[Tooltip]）を厳守する。
- ユーザーから明示的な指示がない限り、未要求の特殊能力や複雑な中間クラスを先行作成してはならない（Minimal Viable Change）。

