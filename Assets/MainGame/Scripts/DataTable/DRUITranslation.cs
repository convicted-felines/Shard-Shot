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
    /// 配置表。
    /// </summary>
    public class DRUITranslation : DataRowBase
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
        /// 获取中文简体。
        /// </summary>
        public string ChineseSimplified
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取英语。
        /// </summary>
        public string English
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取中文繁体。
        /// </summary>
        public string ChineseTraditional
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取韩语。
        /// </summary>
        public string Korean
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
            ChineseSimplified = columnStrings[index++];
            English = columnStrings[index++];
            ChineseTraditional = columnStrings[index++];
            Korean = columnStrings[index++];

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
                    ChineseSimplified = binaryReader.ReadString();
                    English = binaryReader.ReadString();
                    ChineseTraditional = binaryReader.ReadString();
                    Korean = binaryReader.ReadString();
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
    /// UITranslation枚举。
    /// </summary>
    public enum UITranslationEnum : int
    {
        /// <summary>
        /// 未知。
        /// <summary>
        Unknown = 0,
        /// <summary>
        /// UI翻译
        /// </summary>
        UI翻译 = 100,
    }


}
