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
    /// 声音配置表。
    /// </summary>
    public class DRSound : DataRowBase
    {
        private int m_Id = 0;

        /// <summary>
        /// 获取声音编号。
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
        /// 获取优先级（默认0，128最高，-128最低）。
        /// </summary>
        public int Priority
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取是否循环。
        /// </summary>
        public bool Loop
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取音量（0~1）。
        /// </summary>
        public float Volume
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取声音空间混合量（0为2D，1为3D，中间值混合效果）。
        /// </summary>
        public float SpatialBlend
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取声音最大距离。
        /// </summary>
        public float MaxDistance
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
            Priority = int.Parse(columnStrings[index++]);
            Loop = bool.Parse(columnStrings[index++]);
            Volume = float.Parse(columnStrings[index++]);
            SpatialBlend = float.Parse(columnStrings[index++]);
            MaxDistance = float.Parse(columnStrings[index++]);

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
                    Priority = binaryReader.Read7BitEncodedInt32();
                    Loop = binaryReader.ReadBoolean();
                    Volume = binaryReader.ReadSingle();
                    SpatialBlend = binaryReader.ReadSingle();
                    MaxDistance = binaryReader.ReadSingle();
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
    /// Sound枚举。
    /// </summary>
    public enum SoundEnum : int
    {
        /// <summary>
        /// 未知。
        /// <summary>
        Unknown = 0,
        /// <summary>
        /// 获得金币
        /// </summary>
        获得金币 = 1000,
        /// <summary>
        /// 
        /// </summary>
        加特林开火 = 1001,
        /// <summary>
        /// 
        /// </summary>
        加特林普通子弹击中 = 1002,
        /// <summary>
        /// 
        /// </summary>
        加特林巨型子弹击中 = 1003,
        /// <summary>
        /// 
        /// </summary>
        加特林巨型分裂子弹击中 = 1004,
        /// <summary>
        /// 
        /// </summary>
        榴弹炮开火 = 1005,
        /// <summary>
        /// 
        /// </summary>
        榴弹爆炸 = 1006,
        /// <summary>
        /// 
        /// </summary>
        骑士剑击中 = 1007,
        /// <summary>
        /// 
        /// </summary>
        角色死亡 = 1008,
        /// <summary>
        /// 
        /// </summary>
        大斧头击中 = 1009,
        /// <summary>
        /// 
        /// </summary>
        挥动骑士剑 = 1010,
        /// <summary>
        /// 
        /// </summary>
        挥动斧头 = 1011,
    }


}
