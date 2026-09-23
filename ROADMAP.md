# Runner プロジェクト 開発ロードマップ & 進捗管理

本ドキュメントは、ゲーム完成に向けたタスクおよび進行状況を管理するためのロードマップです。

---

## 🎮 ゲーム概要 & コアループ
> **「ぶつかり屋を回避しながら歩いてポイ活 ➔ ショップで強化 ➔ 180秒生存して非常口（改札）から脱出！」**

1. **歩行・ポイ活**: 移動距離に応じて歩数とマネー（ポイント）が蓄積。ジャスト回避でもボーナス獲得。
2. **アイテム入荷 & タイムセール**: 300pt 蓄積ごとにスマホ通販ショップの通知が発火し、自律ドローンや回復・バフアイテムを購入。
3. **サバイバル**: 制限時間（180秒）の間、増え続けるエネミー（サラリーマン、おばあちゃん等）のタックルをしのぐ。
4. **非常口開放 & 脱出**: タイマー終了で非常口（改札）がアンロック。誘導矢印に従って改札へ到達し、脱出成功（ゲームクリア）。

---

## 📌 開発フェーズ & タスク進捗

### Phase 1: コアループの完結 (MVP: 最初から最後まで遊べる状態)
ゲームとしてプレイが成立し、開始からゲームクリア/ゲームオーバーまで一連のサイクルが完結する状態を目指します。

- [x] **1. エネミー撃破時のコイン (MoneyItem) ドロップ（疎結合サバイバー方式）**
  - [x] エネミー撃破イベント（`EnemyController.OnEnemyDefeated`）による位置通知（エネミーのアイテム完全非依存化）
  - [x] 独立ドロップ管理クラス（`ItemDropDirector`）および `MoneyItem.SpawnAsync` による一律コインドロップの実装
  - [x] プレイヤーのマグネット（`PlayerMagnet`）による吸引回収・ポイント加算連携
- [ ] **2. ショップ購入効果の本格接続**
  - [ ] ショップで「追従自律ドローン」購入時に [`WeaponManager.cs`](file:///Users/jinshiyuan/Documents/Project/GitHub/Runner/Assets/Runner/Scripts/Gameplay/Weapons/WeaponManager.cs) の `UpgradeWeaponAsync()` を呼び出し、ドローンを出現・強化
  - [ ] [`ShopItemEffectApplier.cs`](file:///Users/jinshiyuan/Documents/Project/GitHub/Runner/Assets/Runner/Scripts/Gameplay/UI/SmartphoneShop/Model/ShopItemEffectApplier.cs) において各アイテムIDごとの効果適用を実装（超電導ポイ活マグネットの範囲拡大、ワンタイムガード保険のシールド付与等）
- [ ] **3. 非常口（改札）オブジェクトと脱出クリア判定**
  - [ ] 非常口（改札ゲート）プレハブの作成およびステージ上への配置
  - [ ] 180秒経過時の非常口開放イベントと [`EscapeTimerHUD.cs`](file:///Users/jinshiyuan/Documents/Project/GitHub/Runner/Assets/Runner/Scripts/Gameplay/UI/EscapeTimerHUD.cs) のナビゲーション矢印連携 (`SetExitTarget`)
  - [ ] プレイヤー接触時の脱出成功（ゲームクリア）判定およびリザルト画面（[`GameResultModalView.cs`](file:///Users/jinshiyuan/Documents/Project/GitHub/Runner/Assets/Runner/Scripts/Gameplay/UI/GameResultModalView.cs)）のクリア文言表示

---

### Phase 2: 基礎クオリティ & ゲーム体験の向上
ゲームとしての手触り、臨場感、操作性を製品水準へ引き上げます。

- [ ] **4. ポーズメニュー（一時停止）機能**
  - [ ] ゲームプレイ中に任意でポーズできるUIとEscキー等のバインド
  - [ ] ポーズメニューから「ゲーム再開」「設定」「リタイアしてHomeへ戻る」の実行
- [ ] **5. サウンド (BGM / SE) の導入**
  - [ ] サウンド管理基盤（AudioManager 等）の設計・導入
  - [ ] BGM 実装（タイトル、ホーム、ゲームプレイ通常、脱出アラート緊迫BGM、リザルト）
  - [ ] SE 実装（足音/歩行、コイン回収、ドローン射撃・着弾、敵撃破、被ダメージ、ジャスト回避、セールチャイム、ボタンUI音）
- [ ] **6. 演出・ビジュアルフィードバック (VFX)**
  - [ ] ジャスト回避成功時のスロー/エフェクト演出
  - [ ] 敵被弾・撃破時のポップ演出やパーティクル
  - [ ] 非常口開放時のスポットライト・アラート演出
- [ ] **7. 初見向けチュートリアル・ルール説明UI**
  - [ ] ゲーム開始時やタイトル画面での操作方法（歩行ポイ活、セール通知、脱出ルール）の簡単な案内表示

---

### Phase 3: アウトゲームとリプレイ性 (メタゲーム)
繰り返し遊びたくなる成長要素ややりこみ要素を構築します。

- [ ] **8. セーブデータ管理 (`LocalStorageService`)**
  - [ ] 最高記録（ベスト生存時間、最高歩数、最大獲得マネー）のローカル保存
  - [ ] 永続アップグレード状態のセーブ・ロード
- [ ] **9. ホーム画面での永続アップグレード**
  - [ ] [`HomeView.cs`](file:///Users/jinshiyuan/Documents/Project/GitHub/Runner/Assets/Runner/Scripts/UI/HomeView.cs) にアップグレードUIを追加
  - [ ] 持ち帰ったポイントを消費して基礎HP、初期移動速度、初期マグネット範囲などを恒久強化
- [ ] **10. コンテンツ拡張（ステージ・エネミー・武器の追加）**
  - [ ] 新エネミーの追加（突進系、遠距離妨害系など）
  - [ ] 新武器・護衛（ボディガード等）の実装
  - [ ] 難易度の異なる新ステージ（`StageData.json`, `WaveData.json`）の拡張

---

## 🛠️ 進捗ステータス凡例
- [ ] 未着手
- [x] 完了

