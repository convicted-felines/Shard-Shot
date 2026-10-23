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
    /// 特效实体表。
    /// </summary>
    public class DREffect : DataRowBase
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
        /// 获取特效高度。
        /// </summary>
        public float EffectHeight
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
            EffectHeight = float.Parse(columnStrings[index++]);

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
                    EffectHeight = binaryReader.ReadSingle();
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
    /// Effect枚举。
    /// </summary>
    public enum EffectEnum : int
    {
        /// <summary>
        /// 未知。
        /// <summary>
        Unknown = 0,
        /// <summary>
        /// 脚步烟雾
        /// </summary>
        脚步烟雾 = 1,
        /// <summary>
        /// 加特林开火
        /// </summary>
        加特林开火 = 2,
        /// <summary>
        /// 加特林击中
        /// </summary>
        加特林击中 = 3,
        /// <summary>
        /// 加特林子弹特效
        /// </summary>
        加特林子弹特效 = 4,
        /// <summary>
        /// 加特林枪杆闪光
        /// </summary>
        加特林枪杆闪光 = 5,
        /// <summary>
        /// 榴弹炮开火
        /// </summary>
        榴弹炮开火 = 6,
        /// <summary>
        /// 榴弹爆炸
        /// </summary>
        榴弹爆炸 = 7,
        /// <summary>
        /// 榴弹炮伤害
        /// </summary>
        榴弹炮伤害 = 8,
        /// <summary>
        /// 骑士剑击中
        /// </summary>
        骑士剑击中 = 9,
        /// <summary>
        /// 角色死亡
        /// </summary>
        角色死亡 = 10,
        /// <summary>
        /// 角色眩晕
        /// </summary>
        角色眩晕 = 11,
    }


}
