/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: キャラクターに付与された IBuff のライフサイクル（付与・解除・毎フレーム更新）を一元管理するコンポーネント。
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runner
{
    /// <summary>
    /// キャラクター等に付与されるバフ（IBuff）のライフサイクルを一元管理するコンポーネント。
    /// バフの付与、解除、時間経過の更新、および終了時の自動除外を担当します。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterBuffHandler : MonoBehaviour
    {
        private readonly List<IBuff> activeBuffs = new List<IBuff>();

        /// <summary>現在アクティブなバフの読み取り専用リスト</summary>
        public IReadOnlyList<IBuff> ActiveBuffs => activeBuffs;

        /// <summary>
        /// 毎フレーム登録されたバフの効果時間を更新し、終了したバフを除外する。
        /// </summary>
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                buff.Tick(deltaTime);
                if (!buff.IsActive)
                {
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// オブジェクト破棄時に全バフを解除してクリアする。
        /// </summary>
        private void OnDestroy()
        {
            ClearBuffs();
        }

        /// <summary>
        /// バフを付与し、効果を適用して管理リストへ追加する。
        /// 同一バフ識別番号（BuffId）のバフが既に存在する場合は解除して新しいバフで上書きする。
        /// </summary>
        /// <param name="buff">付与する IBuff インスタンス</param>
        public void AddBuff(IBuff buff)
        {
            if (buff == null) return;

            int targetBuffId = buff.BuffId;
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].BuffId == targetBuffId)
                {
                    activeBuffs[i].Remove();
                    activeBuffs.RemoveAt(i);
                }
            }

            buff.Apply();
            activeBuffs.Add(buff);
        }

        /// <summary>
        /// バフ種別を指定し、BuffFactory経由でバフを生成して付与・上書きする。
        /// </summary>
        /// <param name="type">付与するバフ種別</param>
        public void AddBuff(BuffType type)
        {
            var buff = BuffFactory.Create(type, gameObject);
            if (buff != null)
            {
                AddBuff(buff);
            }
        }

        /// <summary>
        /// 指定したバフを解除し、管理リストから除外する。
        /// </summary>
        /// <param name="buff">解除する IBuff インスタンス</param>
        public void RemoveBuff(IBuff buff)
        {
            if (buff == null) return;

            buff.Remove();
            activeBuffs.Remove(buff);
        }

        /// <summary>
        /// 指定したバフ識別番号のバフを解除し、管理リストから除外する。
        /// </summary>
        /// <param name="buffId">バフ識別番号</param>
        public void RemoveBuff(int buffId)
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].BuffId == buffId)
                {
                    activeBuffs[i].Remove();
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 指定したバフ種別のバフを解除し、管理リストから除外する。
        /// </summary>
        /// <param name="type">バフ種別</param>
        public void RemoveBuff(BuffType type)
        {
            RemoveBuff((int)type);
        }

        /// <summary>
        /// 指定した型のバフをすべて解除し、管理リストから除外する。
        /// </summary>
        /// <typeparam name="T">対象のバフ型</typeparam>
        public void RemoveBuffsOfType<T>() where T : class, IBuff
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i] is T typedBuff)
                {
                    typedBuff.Remove();
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 指定したバフ識別番号の最初のアクティブバフを取得する。
        /// </summary>
        /// <param name="buffId">バフ識別番号</param>
        /// <returns>見つかったバフ（存在しない場合は null）</returns>
        public IBuff GetBuff(int buffId)
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].BuffId == buffId && activeBuffs[i].IsActive)
                {
                    return activeBuffs[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 指定したバフ種別の最初のアクティブバフを取得する。
        /// </summary>
        /// <param name="type">バフ種別</param>
        /// <returns>見つかったバフ（存在しない場合は null）</returns>
        public IBuff GetBuff(BuffType type)
        {
            return GetBuff((int)type);
        }

        /// <summary>
        /// 指定したバフ識別番号のバフが現在有効かどうかを判定する。
        /// </summary>
        /// <param name="buffId">バフ識別番号</param>
        /// <returns>バフが有効であれば true</returns>
        public bool HasBuff(int buffId)
        {
            return GetBuff(buffId) != null;
        }

        /// <summary>
        /// 指定したバフ種別のバフが現在有効かどうかを判定する。
        /// </summary>
        /// <param name="type">バフ種別</param>
        /// <returns>バフが有効であれば true</returns>
        public bool HasBuff(BuffType type)
        {
            return HasBuff((int)type);
        }

        /// <summary>
        /// 指定した型の最初のアクティブバフを取得する。
        /// </summary>
        /// <typeparam name="T">対象のバフ型</typeparam>
        /// <returns>見つかったバフ（存在しない場合は null）</returns>
        public T GetBuff<T>() where T : class, IBuff
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i] is T typedBuff && typedBuff.IsActive)
                {
                    return typedBuff;
                }
            }

            return null;
        }

        /// <summary>
        /// 指定した型のバフが現在有効かどうかを判定する。
        /// </summary>
        /// <typeparam name="T">対象のバフ型</typeparam>
        /// <returns>バフが有効であれば true</returns>
        public bool HasBuff<T>() where T : class, IBuff
        {
            return GetBuff<T>() != null;
        }

        /// <summary>
        /// 管理中のすべてのバフを解除し、リストをクリアする。
        /// </summary>
        public void ClearBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i]?.Remove();
            }

            activeBuffs.Clear();
        }
    }
}

