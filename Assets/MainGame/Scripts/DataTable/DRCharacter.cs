//------------------------------------------------------------
// Game Framework
//------------------------------------------------------------
// 此文件由工具自动生成，请勿直接修改。
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色表。
    /// </summary>
    public class DRCharacter : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取编号。
        /// </summary>
        public override int Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// 获取名称。
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取资源名称。
        /// </summary>
        public string AssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取布偶资源名称。
        /// </summary>
        public string RagdollName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取移动速度。
        /// </summary>
        public float MoveSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取转身速度。
        /// </summary>
        public float TurnSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取生命值。
        /// </summary>
        public float Health
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取攻击距离。
        /// </summary>
        public float AttackRange
        {
            get;
            private set;
        }

        public override bool ParseDataRow(string dataRowString, object userData)
        {
            string[] columnStrings = dataRowString.Split(DataTableExtension.DataSplitSeparators);
            for (int i = 0; i < columnStrings.Length; i++)
            {
                columnStrings[i] = columnStrings[i].Trim(DataTableExtension.DataTrimSeparators);
            }

            int index = 0;
            index++;
            m_Id = int.Parse(columnStrings[index++]);
            index++;
            Name = columnStrings[index++];
            AssetName = columnStrings[index++];
            RagdollName = columnStrings[index++];
            MoveSpeed = float.Parse(columnStrings[index++]);
            TurnSpeed = float.Parse(columnStrings[index++]);
            Health = float.Parse(columnStrings[index++]);
            AttackRange = float.Parse(columnStrings[index++]);

            GeneratePropertyArray();
            return true;
        }

        public override bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
        {
            using (MemoryStream memoryStream = new MemoryStream(dataRowBytes, startIndex, length, false))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream, Encoding.UTF8))
                {
                    m_Id = binaryReader.Read7BitEncodedInt32();
                    Name = binaryReader.ReadString();
                    AssetName = binaryReader.ReadString();
                    RagdollName = binaryReader.ReadString();
                    MoveSpeed = binaryReader.ReadSingle();
                    TurnSpeed = binaryReader.ReadSingle();
                    Health = binaryReader.ReadSingle();
                    AttackRange = binaryReader.ReadSingle();
                }
            }

            GeneratePropertyArray();
            return true;
        }

        private void GeneratePropertyArray()
        {

        }
    }

    /// <summary>
    /// Character枚举。
    /// </summary>
    public enum CharacterEnum : int
    {
        /// <summary>
        /// 未知。
        /// <summary>
        Unknown = 0,
        /// <summary>
        /// 
        /// </summary>
        玩家角色 = 1,
        /// <summary>
        /// 
        /// </summary>
        骑士机器人 = 2,
    }


}
