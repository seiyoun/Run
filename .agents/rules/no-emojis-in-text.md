---
description: Unity UIテキスト（TextMeshPro / TextMeshProUGUI / スクリプト内テキスト）に絵文字・特殊記号を含めない厳格ルール
---

# UIテキストへの絵文字・特殊記号使用禁止ルール (No Emojis in Text Rule)

プロジェクト内の TextMeshPro フォント（`NotoSansJP-VariableFont_wght_SDF`）にはカラー絵文字や特殊 Unicode 記号（Dingbats、Unicode 矢印 `➔`、Emoji 全般）のグリフが含まれていません。
これらを使用すると、ランタイム時に `The character with Unicode value \uXXXX was not found in the font asset` という警告が発生し、文字化け（□ / `\u25A1`）となります。

---

## 1. 厳格ルール
1. **TextMeshPro / UI 表示文字列に絵文字を含めてはならない**:
   - `👟`, `⏳`, `🚨`, `🚪`, `⚡`, `🔥`, `✨`, `🪙`, `🧲`, `🛸`, `🕶️`, `🥫`, `🛡️`, `🔧`, `📱` 等の Unicode 絵文字は一切使用禁止。
   - `➔` (`\u2794`), `➡️` などの未収録特殊矢印記号も使用禁止。
2. **代替表現のルール**:
   - 装飾や強調には標準的な全角記号（`【 ... 】`, `▶`, `◀`, `¥`, `pt`, `x`, `:` など）またはシンプルな英語テキスト（`[DRONE]`, `[SPEED]`, `ALERT`, `STEP` 等）を使用すること。
3. **シーン・プレハブのテキスト**:
   - インスペクターやプレハブ、シーン内の `m_text`（初期テキスト）にも絵文字を残さないこと。

---

## 2. 自動検証
- エージェント作業終了時（Task Completion Validator Hook）で絵文字の混入が自動チェックされ、ブロックされます。
- Unity Editor では `Assets/Runner/Editor/NoEmojiTextValidator.cs`（メニュー: `Runner/Validation/Validate No Emojis in UI`）によって自動検査・手動検査が可能です。
