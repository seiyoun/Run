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
        /// 同一型のバフが既に存在する場合は解除して新しいバフで上書きする。
        /// </summary>
        /// <param name="buff">付与する IBuff インスタンス</param>
        public void AddBuff(IBuff buff)
        {
            if (buff == null) return;

            var buffType = buff.GetType();
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].GetType() == buffType)
                {
                    activeBuffs[i].Remove();
                    activeBuffs.RemoveAt(i);
                }
            }

            buff.Apply();
            activeBuffs.Add(buff);
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

