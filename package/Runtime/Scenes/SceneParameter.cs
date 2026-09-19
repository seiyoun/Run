/*
 * 作成者: shiyuan.jin
 * 連絡先: shiyuan0106bot@gmail.com
 * スクリプト説明: シーン遷移で渡されたパラメータの型検証を補助する。
 */

using System;

namespace Shiyuan.Foundation.Scenes
{
    public static class SceneParameter
    {
        /// <summary>
        /// 指定された型のシーンパラメータを取得し、型が合わない場合は例外を送出する。
        /// </summary>
        public static T Require<T>(object parameter)
        {
            if (parameter == null)
            {
                if (IsNonNullableValueType(typeof(T)))
                {
                    throw new InvalidOperationException($"Scene parameter is required. expected:{typeof(T).Name}");
                }

                return default;
            }

            if (parameter is T typedParameter)
            {
                return typedParameter;
            }

            throw new InvalidOperationException($"Scene parameter type mismatch. expected:{typeof(T).Name} actual:{parameter.GetType().Name}");
        }

        /// <summary>
        /// 指定された型のシーンパラメータを取得できるか判定する。
        /// </summary>
        public static bool TryGet<T>(object parameter, out T value)
        {
            if (parameter == null)
            {
                value = default;
                return !IsNonNullableValueType(typeof(T));
            }

            if (parameter is T typedParameter)
            {
                value = typedParameter;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// null を許容しない値型かどうかを判定する。
        /// </summary>
        private static bool IsNonNullableValueType(Type type)
        {
            return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
        }
    }
}
