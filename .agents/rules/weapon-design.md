# Weapon & Bullet Design Guidelines (武器・弾丸設計規約)

武器、召喚物（ドローン、ビット、タレット等）、および弾丸を設計・実装する際は、以下の原則とパターンを厳守してください。

## 1. Responsibility Separation (責務の分離)
- **移動・追従コンポーネントと攻撃コンポーネントの分離**:
  - 移動や追従・浮遊ボビング（例: `FollowTarget`）と、索敵・攻撃制御（例: `IAttacker`, `IFindTarget`）は独立したコンポーネントとして分離する。
  - 移動系コンポーネントが攻撃系の向き制御（Z軸回転やスプライト反転等）を上書き・干渉してはならない。

## 2. Aiming & Orientation (エイミングと向き制御)
- **360度全方位攻撃の向き制御**:
  - ターゲット方向を向いて攻撃する武器・ビットは、`transform.rotation`（Z軸回転）で向きを制御する。
  - 銃口（FirePoint）を武器オブジェクトの子Transformとして配置することで、本体の回転に連動して正確な発射位置・弾道を維持する。
  - Z軸回転で全方位を向く場合、`SpriteRenderer.flipX` を併用すると回転と反転が二重に適用されて描画が破綻するため、スプライト反転は行わない。
- **補間回転**:
  - 向きの変更は即時反映ではなく、`Mathf.MoveTowardsAngle` 等を用いてスムーズに追従させる。
  - ターゲット不在時は正面（水平0度等）のデフォルト姿勢へ滑らかに復帰させる。

## 3. Bullet Object Pooling (弾丸のオブジェクトプール設計)
- **`UnityEngine.Pool.ObjectPool<T>` の標準採用**:
  - 高頻度で生成・破棄される弾丸は、ガベージコレクション（GC）負荷を防ぐため必ず Unity 標準の `UnityEngine.Pool.ObjectPool<T>` で管理する。
- **プールの所有とライフサイクル**:
  - 武器コンポーネント（発射元）がプールインスタンスを保持・管理する。
  - `Awake()` でプールを初期化（`createFunc`, `actionOnGet`, `actionOnRelease`, `actionOnDestroy`, `collectionCheck: false`）。
  - `OnDestroy()` で必ず `pool.Dispose()` を呼び出し、リソースリークを防止する。
- **弾丸の返却と安全性**:
  - 弾丸クラスは初期化時（`Initialize`）にプール返却用コールバック（`Action<T>` 等）を受け取る。
  - 寿命終了時やヒット時にコールバックを呼び出してプールへ返却する。
  - **多重返却防止ガード**: 寿命と衝突が同フレームで重複発生することによる二重返却（例外発生）を防ぐため、`isReleased` フラグ等による多重呼び出しガードを必須とする。
  - **フォールバック破棄**: 親の武器が破棄済みの場合やプール未設定の場合でもエラーにならず、安全に `Destroy(gameObject)` へフォールバックする耐性を持たせる。

## 4. Coding Conventions & YAGNI
- 武器・弾丸クラスにおいても `unity-script-conventions`（メンバー記述順序、全関数へのXMLドキュメントコメント、シリアライズ変数への[Tooltip]）を厳守する。
- 不要な先行実装や推測による拡張コードは作らず、要求に応じた最小限の実装（Minimal Viable Change）を徹底する。

