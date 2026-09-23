/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: ゲームプレイ中の通貨（所持金・累計獲得金）、歩数、エネミー撃破数（タイプ別内訳）を一元的に記録・集計するシングルトンマネージャー。
 */

using System;
using System.Collections.Generic;
using Shiyuan.Foundation.Core;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// 1回のゲームプレイにおける成果（所持通貨、累計獲得通貨、歩数、エネミー撃破数・タイプ別内訳）を一元的に管理するマネージャー。
    /// 通貨の加算・消費の Single Source of Truth として機能し、リザルト画面等への統計データ提供を担当します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameRecordTracker : SingletonMonoBehaviour<GameRecordTracker>
    {
        private long currentMoney;
        private long earnedMoney;
        private int totalSteps;
        private int totalDefeatedCount;
        private readonly Dictionary<EnemyType, int> defeatedEnemyCounts = new();
        private PlayerController boundPlayer;

        /// <summary>現在消費可能な所持通貨・ポイント</summary>
        public long CurrentMoney => currentMoney;

        /// <summary>ゲームプレイ中に獲得した累計通貨・ポイント</summary>
        public long EarnedMoney => earnedMoney;

        /// <summary>ゲームプレイ中に歩いた累計歩数</summary>
        public int TotalSteps => totalSteps;

        /// <summary>倒したエネミーの総数</summary>
        public int TotalDefeatedCount => totalDefeatedCount;

        /// <summary>エネミー種別ごとの撃破数内訳</summary>
        public IReadOnlyDictionary<EnemyType, int> DefeatedEnemyCounts => defeatedEnemyCounts;

        /// <summary>所持金残高が変動した際に発火するイベント (現在の所持金額)</summary>
        public event Action<long> OnMoneyChanged;

        /// <summary>Game シーン破棄時に一緒に破棄させる</summary>
        protected override bool ShouldDontDestroyOnLoad => false;

        /// <summary>インスタンスが既に存在するかどうか</summary>
        public static bool HasInstance => SingletonMonoBehaviour<GameRecordTracker>.Instance != null;

        /// <summary>
        /// GameRecordTracker の正規インスタンスを取得する。シーン上に存在しない場合は動的に生成します。
        /// </summary>
        public new static GameRecordTracker Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                var baseInstance = SingletonMonoBehaviour<GameRecordTracker>.Instance;
                if (baseInstance != null) return baseInstance;

                var existing = FindFirstObjectByType<GameRecordTracker>();
                if (existing != null) return existing;

                var obj = new GameObject(nameof(GameRecordTracker));
                return obj.AddComponent<GameRecordTracker>();
            }
        }

        /// <summary>
        /// シングルトンの初期化を行う。
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// オブジェクト破棄時にプレイヤーのイベント購読を解除する。
        /// </summary>
        protected override void OnDestroy()
        {
            UnbindPlayer();
            base.OnDestroy();
        }

        /// <summary>
        /// 記録されているすべての統計データ（所持通貨・累計通貨・歩数・撃破数）をゼロクリアする。
        /// </summary>
        public void ResetRecord()
        {
            currentMoney = 0;
            earnedMoney = 0;
            totalSteps = 0;
            totalDefeatedCount = 0;
            defeatedEnemyCounts.Clear();
            OnMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// プレイヤーインスタンスをバインドし、歩数変更イベントの自動購読を開始する。
        /// </summary>
        /// <param name="player">対象の PlayerController インスタンス</param>
        public void BindPlayer(PlayerController player)
        {
            if (player == null) return;

            UnbindPlayer();
            boundPlayer = player;
            boundPlayer.OnStepsChanged += HandleStepsChanged;

            totalSteps = boundPlayer.CurrentSteps;
        }

        /// <summary>
        /// エネミーを登録し、死亡イベント（Status.OnDead）を購読して撃破記録を行う。
        /// </summary>
        /// <param name="enemy">登録する EnemyController インスタンス</param>
        public void RegisterEnemy(EnemyController enemy)
        {
            if (enemy == null || enemy.Status == null) return;

            enemy.Status.OnDead += () => RecordEnemyDefeat(enemy.EnemyType);
        }

        /// <summary>
        /// ドロップアイテムを登録し、回収イベント（OnCollected）を購読して通貨加算・集計を行う。
        /// </summary>
        /// <param name="item">登録する IItem インスタンス</param>
        public void RegisterItem(IItem item)
        {
            if (item == null) return;

            item.OnCollected += HandleItemCollected;
        }

        /// <summary>
        /// お金・ポイントを加算し、所持金と累計獲得額を更新してイベントを通知する。
        /// </summary>
        /// <param name="amount">加算額</param>
        public void AddMoney(long amount)
        {
            if (amount <= 0) return;

            currentMoney += amount;
            earnedMoney += amount;
            OnMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// お金・ポイントを消費する。残高不足時は消費を行わず false を返却する。
        /// </summary>
        /// <param name="amount">消費額</param>
        /// <returns>消費に成功したかどうか</returns>
        public bool TryConsumeMoney(long amount)
        {
            if (amount <= 0 || currentMoney < amount) return false;

            currentMoney -= amount;
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }

        /// <summary>
        /// 指定されたエネミー種別の撃破数を加算・記録する。
        /// </summary>
        /// <param name="enemyType">撃破されたエネミー種別</param>
        private void RecordEnemyDefeat(EnemyType enemyType)
        {
            totalDefeatedCount++;
            defeatedEnemyCounts[enemyType] = defeatedEnemyCounts.GetValueOrDefault(enemyType, 0) + 1;
        }

        /// <summary>
        /// アイテム回収イベントを受信し、該当する処理（コイン加算等）を実行する。
        /// </summary>
        /// <param name="item">回収された IItem インスタンス</param>
        /// <param name="collector">回収者 GameObject</param>
        private void HandleItemCollected(IItem item, GameObject collector)
        {
            if (item is MoneyItem moneyItem)
            {
                AddMoney(moneyItem.MoneyAmount);
            }
        }

        /// <summary>
        /// プレイヤーの歩数変更イベントを受信し、総歩数を更新する。
        /// </summary>
        /// <param name="steps">現在の総歩数</param>
        private void HandleStepsChanged(int steps)
        {
            totalSteps = steps;
        }

        /// <summary>
        /// バインドされているプレイヤーのイベント購読を解除する。
        /// </summary>
        private void UnbindPlayer()
        {
            if (boundPlayer != null)
            {
                boundPlayer.OnStepsChanged -= HandleStepsChanged;
                boundPlayer = null;
            }
        }
    }
}
