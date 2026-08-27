---
name: unity-script-conventions
description: Unity C# スクリプトの設計・コーディング規約。アセンブリ定義（asmdef）に基づいた namespace の分離設計、クラス内のメンバー記述順序（const/static → シリアライズ → private変数 → public変数 → プロパティ → Unity関数 → オーバーライド関数 → public関数 → private関数）、全関数へのドキュメントコメント（XMLコメント）必須化、変数コメントのクリーン設計（private変数はコメント不要、[SerializeField]は[Tooltip]、publicプロパティはXMLコメント）、および不要な区切りコメントの禁止を定義・強制します。C# スクリプトの新規作成、リファクタリング、コードレビュー時に必ず使用してください。
---

# Unity C# スクリプト記述規約 (Script Conventions)

このスキルは、プロジェクト内のすべての Unity C# スクリプトで一貫した設計品質、可読性、保守性を保つためのコーディング規約を定義します。

---

## 1. アセンブリ定義（asmdef）と Namespace の分離規則

スクリプトはすべて **Assembly Definition (`.asmdef`) の境界およびディレクトリ構造** に従って適切な namespace に分離します。

### Namespace 設計ルール
- ルート名前空間はプロジェクト名（`Runner` 等）から開始します。
- asmdef の名前およびフォルダ階層と 1:1 で一致させます。

| ディレクトリ / アセンブリ | Namespace 例 |
| :--- | :--- |
| `Scripts/Scenes/Boot/` (`Runner.Scenes`) | `Runner.Scenes.Boot` / `Runner.Boot` |
| `Scripts/Gameplay/` (`Runner.Gameplay`) | `Runner.Gameplay` |
| `Scripts/Gameplay/Characters/Player/` | `Runner.Gameplay.Characters.Player` または `Runner.Gameplay` |
| `Scripts/Gameplay/UI/` | `Runner.Gameplay.UI` |
| `Scripts/Gameplay/Items/` | `Runner.Gameplay.Items` |
| `Scripts/Input/` (`Runner.Input`) | `Runner.Input` |
| `Editor/` (`Runner.Editor`) | `Runner.Editor` |

---

## 2. クラス内メンバーの厳格な記述順序

すべての C# クラス・コンポーネントは、**上から下へ以下の厳格な順序** でメンバーを記述します。

```
1. const / static フィールド
2. [SerializeField] シリアライズフィールド
3. private インスタンス変数
4. public インスタンス変数
5. プロパティ & イベント (Properties & Events)
6. Unity ライフサイクル関数 (Awake, Start, Update, OnDestroy 等)
7. override 関数 (ToString や基底クラスの override)
8. public 関数 (公開メソッド)
9. private 関数 / 内部ヘルパー関数
```

> [!IMPORTANT]
> **セクション区切りコメントの禁止**:
> `// -------------------------------------------------------------` や `// 1. const / static フィールド` などの目次・セクション区切りコメントは**記述しないでください**。コードは常に清潔（Clean）でノイズのない状態に保ちます。メンバーの配置順序自体によって構造を表現します。

---

## 3. 変数コメント規約（クリーン重視スタイル）

| 変数の種類 | コメント方針 | 記述ルール |
| :--- | :--- | :--- |
| **private 変数** | **原則コメント不要（Clean）** | 変数名自体で意図・役割を自明にする。ノイズとなるコメントは付けない。 |
| **`[SerializeField]`** | **`[Tooltip("...")]` を付与** | Inspector での可読性とコード上のドキュメントを兼ねる。 |
| **public プロパティ / イベント** | **XMLコメント（`/// <summary>`）を付与** | 公開 API として IntelliSense で参照できるようにする。 |

---

## 4. 関数（メソッド）へのコメント必須ルール

**すべての関数（Unity ライフサイクル関数、override 関数、public 関数、private 関数を含む）には、必ずその役割・目的を説明するコメント（XML ドキュメントコメント `<summary>`, `<param>`, `<returns>` 等）を記述してください。**

- **Unity ライフサイクル関数**: `/// <summary>コンポーネントの初期化を行う。</summary>` 等
- **Public / Interface 関数**: 処理内容、引数の意味（`<param>`）、戻り値（`<returns>`）を明確に記述
- **Private ヘルパー関数**: 内部処理の目的やアルゴリズムを明確に記述

---

## 5. スクリプト構成テンプレート (標準フォーマット)

```csharp
/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: [クラスの目的・役割の簡潔な説明]
 */

using System;
using UnityEngine;

namespace Runner.Gameplay
{
    /// <summary>
    /// [クラスの概要コメント]
    /// </summary>
    public sealed class SampleController : MonoBehaviour, ISampleInterface
    {
        public static SampleController Instance { get; private set; }
        private const float DefaultSpeed = 5.0f;

        [Header("Settings")]
        [Tooltip("移動速度")]
        [SerializeField] private float moveSpeed = DefaultSpeed;

        [Tooltip("最大HP")]
        [SerializeField] private int maxHp = 100;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private int currentSteps;

        /// <summary>移動速度</summary>
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>現在の歩数</summary>
        public int CurrentSteps => currentSteps;

        /// <summary>歩数変更時イベント</summary>
        public event Action<int> OnStepsChanged;

        /// <summary>
        /// シングルトンの初期化およびコンポーネントの参照取得を行う。
        /// </summary>
        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// 初回フレームでの初期設定を行う。
        /// </summary>
        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// 毎フレームの入力処理を行う。
        /// </summary>
        private void Update()
        {
            ProcessInput();
        }

        /// <summary>
        /// 固定フレームごとの物理移動処理を行う。
        /// </summary>
        private void FixedUpdate()
        {
            ProcessPhysics();
        }

        /// <summary>
        /// オブジェクト破棄時の参照解放を行う。
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// オブジェクトの文字列表現を返す。
        /// </summary>
        public override string ToString()
        {
            return $"SampleController (Speed: {moveSpeed})";
        }

        /// <summary>
        /// 指定された方向へ移動入力を設定する。
        /// </summary>
        /// <param name="direction">移動方向ベクトル</param>
        public void Move(Vector2 direction)
        {
            moveInput = direction;
        }

        /// <summary>
        /// 歩数を加算し、変更イベントを発火する。
        /// </summary>
        /// <param name="amount">加算する歩数</param>
        public void AddSteps(int amount)
        {
            if (amount <= 0) return;
            currentSteps += amount;
            OnStepsChanged?.Invoke(currentSteps);
        }

        /// <summary>
        /// コントローラーの内部初期化を行う。
        /// </summary>
        private void Initialize()
        {
            // 初期化処理
        }

        /// <summary>
        /// 入力状態を監視・更新する。
        /// </summary>
        private void ProcessInput()
        {
            // 入力処理
        }

        /// <summary>
        /// Rigidbody2D を用いて物理挙動を反映する。
        /// </summary>
        private void ProcessPhysics()
        {
            // 物理挙動処理
        }
    }
}
```

---

## 6. コーディング規約のチェックリスト

コードを作成・修正した際は、以下を必ず確認してください：

- [ ] namespace は `.asmdef` およびディレクトリ階層に沿って正しく定義されているか
- [ ] クラスメンバーは指定された 9 段階の順序通りに並んでいるか
- [ ] **`// ------------------------------------` や `// 1. const / static` などのセクション区切りコメントが存在しないか（禁止）**
- [ ] **`private` 変数に余計なコメントを付けず、名前で自明になっているか**
- [ ] **`[SerializeField]` に `[Tooltip]`、`public` プロパティ/イベントに `/// <summary>` が記載されているか**
- [ ] **すべての関数（Unityライフサイクル、override、public、private）に説明コメント（XMLドキュメントコメント）が記載されているか**
