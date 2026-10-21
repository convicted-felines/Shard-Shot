using GameFramework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 一般工具类
    /// </summary>
    public static class CommonUtility
    {

        // 关于 EntityId 的约定：
        // 0 为无效
        // 正值用于和服务器通信的实体（如玩家角色、NPC、怪等，服务器只产生正值）
        // 负值用于本地生成的临时实体（如特效、FakeObject等）

        private static int PermanentEntityId = 0;
        private static int TemporaryEntityId = 0;

        /// <summary>
        /// 获取永久或者半永久实体ID
        /// </summary>
        /// <returns></returns>
        public static int PermanentId
        {
            get { return Interlocked.Increment(ref PermanentEntityId); }
        }
        /// <summary>
        /// 获取临时实体ID 例如特效等
        /// </summary>
        /// <returns></returns>
        public static int TemporaryId
        {
            get { return Interlocked.Decrement(ref TemporaryEntityId); }
        }


        /// <summary>
        /// 获得地面随机坐标
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public static Vector3 GetRandomVec3(int x, int z)
        {
            return new Vector3(Utility.Random.GetRandom(-x, x), 1, Utility.Random.GetRandom(-z, z));
        }

        public static List<List<string>> CutMultLineConfig(string data)
        {
            List<List<string>> result = new List<List<string>>();

            if (!string.IsNullOrEmpty(data))
            {
                // 使用 / 分割字符串成多行
                string[] rows = data.Split('/');

                foreach (string row in rows)
                {
                    // 使用 , 分割每一行中的元素
                    string[] elements = row.Split('.');

                    // 将这些元素添加到当前行中
                    List<string> currentRow = new List<string>(elements);
                    result.Add(currentRow);
                }
            }

            return result;
        }

        public static List<string> CutCommaConfig(string data)
        {
            List<string> result = new List<string>();
            if (!string.IsNullOrEmpty(data))
            {
                foreach (var str in data.Split(','))
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        result.Add(str);
                    }
                }
            }

            return result;
        }


        public static List<string> ParseFunctionConfig(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                Log.Warning("Function data is invalid.");
                return null;
            }
            return new List<string>(data.Split(','));
        }

        /// <summary>
        /// 解析技能数据
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, List<string>> ParseAbilityData(string ids, string data)
        {
            if (string.IsNullOrEmpty(ids) || string.IsNullOrEmpty(data))
            {
                Log.Warning("Ability data is invalid.");
                return null;
            }
            var result = new Dictionary<string, List<string>>();

            string[] idArray = ids.Split('/');
            string[] dataArray = data.Split('/');

            if (idArray.Length != dataArray.Length)
            {
                Log.Error("The number of IDs does not match the number of data entries.");
                return null;
            }

            for (int i = 0; i < idArray.Length; i++)
            {
                List<string> dataList = new List<string>();
                foreach (var str in dataArray[i].Split(':'))
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        dataList.Add(str);
                    }
                }

                result.Add(idArray[i], dataList);

            }

            return result;

        }

        /// <summary>
        /// 解析功能主动被动数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseFunctionData(string ids, string data)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(ids) || string.IsNullOrEmpty(data))
            {
                Log.Warning("Function data is invalid.");
                return result;
            }


            string[] idArray = ids.Split('/');
            string[] dataArray = data.Split('/');

            if (idArray.Length != dataArray.Length)
            {
                Log.Error("The number of IDs does not match the number of data entries.");
                return result;
            }

            for (int i = 0; i < idArray.Length; i++)
            {
                result.Add(idArray[i], dataArray[i]);
            }

            return result;
        }

        /// <summary>
        /// 将设置值类型名称归一化为内部标准值（bool、int、float、string）。
        /// </summary>
        public static string NormalizeSettingValueType(string valueType)
        {
            if (string.IsNullOrEmpty(valueType))
            {
                return "string";
            }

            switch (valueType.Trim().ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    return "bool";
                case "int":
                case "int32":
                case "integer":
                    return "int";
                case "float":
                case "single":
                case "double":
                    return "float";
                case "string":
                default:
                    return "string";
            }
        }

        /// <summary>
        /// 将常见布尔文本解析为 bool。兼容 0/1、t/f、y/n、true/false（任意大小写）、yes/no、on/off；首尾空白会忽略；无法识别时返回 false。
        /// </summary>
        public static bool ParseBoolString(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            string s = rawValue.Trim();

            if (s.Length == 1)
            {
                char c0 = s[0];
                if (c0 == '1')
                {
                    return true;
                }

                if (c0 == '0')
                {
                    return false;
                }

                char c = char.ToLowerInvariant(c0);
                if (c == 't' || c == 'y')
                {
                    return true;
                }

                if (c == 'f' || c == 'n')
                {
                    return false;
                }

                return false;
            }

            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(s, "no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(s, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(s, "off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return bool.TryParse(s, out bool boolValue) && boolValue;
        }

    }


    /// <summary>
    /// 复制相同或不同类同名属性
    /// </summary>
    /// <typeparam name="TIn"></typeparam>
    /// <typeparam name="TOut"></typeparam>
    public static class TransExpV2<TIn, TOut>
    {

        private static readonly Func<TIn, TOut> cache = GetFunc();
        private static Func<TIn, TOut> GetFunc()
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(TIn), "p");
            List<MemberBinding> memberBindingList = new List<MemberBinding>();

            foreach (var item in typeof(TOut).GetProperties())
            {
                if (!item.CanWrite)
                    continue;

                MemberExpression property = Expression.Property(parameterExpression, typeof(TIn).GetProperty(item.Name));
                MemberBinding memberBinding = Expression.Bind(item, property);
                memberBindingList.Add(memberBinding);
            }

            MemberInitExpression memberInitExpression = Expression.MemberInit(Expression.New(typeof(TOut)), memberBindingList.ToArray());
            Expression<Func<TIn, TOut>> lambda = Expression.Lambda<Func<TIn, TOut>>(memberInitExpression, new ParameterExpression[] { parameterExpression });

            return lambda.Compile();
        }

        public static TOut Trans(TIn tIn)
        {
            return cache(tIn);
        }

        //// <summary>
        ///  调用方法
        ///  SkillData data=TransExpV2<SkillData,SkillData>.Trans(DATA);//DATA 为已赋值的类
        ///// </summary>
    }


}