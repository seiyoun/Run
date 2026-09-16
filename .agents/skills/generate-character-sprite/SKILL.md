---
name: generate-character-sprite
description: Generates, transparentizes, optimizes, and imports 2D pixel art character and enemy sprites into Unity. Use when creating new player skins, enemies, NPCs, or bosses.
---

# 2D Character & Enemy Sprite Generation Skill

このスキルは、ゲーム『Runner』における2Dピクセルアートキャラクター（プレイヤー、エネミー、NPC等）のコンセプト設計、AI画像生成、透過切り抜き処理、およびUnityへのインポート・プレハブ設定を行う標準ワークフローを定義します。

## ワークフロー

### 1. プロンプト設計
- **スタイル**: 2Dピクセルアート、カジュアルアーケード・ランナーゲーム風
- **構図**: 正方形（1:1）、中央配置、全身（Full body）、右向き（または斜め右前向き、`flipX` 反転対応）
- **背景**: 純白無地背景（`solid pure white background`）
- **制約**: 影なし（`no shadows`）、透かしなし（`no watermarks`）

### 2. 画像生成 (`generate_image`)
- `AspectRatio`: `'1:1'`
- `ImageName`: 役割や特徴を表す英小文字（例: `enemy_speed_runner`, `boss_giant_robot`）

### 3. 背景透過 & クロップ最適化 (Unity C# `eval`)
Unity MCP の `eval` を用いて、外周からのBFSフラッドフィルにより白背景を除去します：
- キャラクター内部の白（服、ロゴ、目、歯）は透過させずに保護。
- アルファ値が有効なピクセルのバウンディングボックスを検出。
- 6%〜8%のパディングを加えて256×256のRGBA32テクスチャとして整形。
- `Assets/Runner/Sprites/Characters/{Name}.png` に保存。

### 4. Unity インポート設定
- **Texture Type**: `Sprite (2D and UI)`
- **Sprite Mode**: `Single`
- **Pixels Per Unit (PPU)**:
  - プレイヤー基準: `256`（ワールドサイズ約 1.0 ユニット、背景タイル1マス相当）
  - 通常エネミー: `240`〜`256`
  - 大型エネミー/ボス: `160`〜`200`
- **Filter Mode**: `Point (no filter)`（ドット絵のシャープさを維持）
- **Alpha Is Transparency**: `true`

### 5. プレハブ・当たり判定の連携
- 対象のプレハブ（`Player.prefab`, `Enemy.prefab` など）の `SpriteRenderer` にスプライトをセット。
- `color` を `Color.white` にリセット（スプライト本来のカラーを表示）。
- `CircleCollider2D` の半径をスプライトに合わせて調整（通常サイズは `0.35`〜`0.4`）。

