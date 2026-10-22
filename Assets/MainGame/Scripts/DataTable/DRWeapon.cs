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
    /// 武器表。
    /// </summary>
    public class DRWeapon : DataRowBase
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
        /// 获取武器类型。
        /// </summary>
        public string WeaponType
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取伤害。
        /// </summary>
        public float Damage
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取基础攻击速度。
        /// </summary>
        public float BaseAttackSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取攻击速度。
        /// </summary>
        public float AttackSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取攻击时机。
        /// </summary>
        public float AttackTiming
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取子弹资源名称。
        /// </summary>
        public string BulletAssetName
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取子弹速度。
        /// </summary>
        public float BulletSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// 获取伤害倒计时。
        /// </summary>
        public float DamageCountdown
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
            WeaponType = columnStrings[index++];
            Damage = float.Parse(columnStrings[index++]);
            BaseAttackSpeed = float.Parse(columnStrings[index++]);
            AttackSpeed = float.Parse(columnStrings[index++]);
            AttackTiming = float.Parse(columnStrings[index++]);
            BulletAssetName = columnStrings[index++];
            BulletSpeed = float.Parse(columnStrings[index++]);
            DamageCountdown = float.Parse(columnStrings[index++]);

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
                    WeaponType = binaryReader.ReadString();
                    Damage = binaryReader.ReadSingle();
                    BaseAttackSpeed = binaryReader.ReadSingle();
                    AttackSpeed = binaryReader.ReadSingle();
                    AttackTiming = binaryReader.ReadSingle();
                    BulletAssetName = binaryReader.ReadString();
                    BulletSpeed = binaryReader.ReadSingle();
                    DamageCountdown = binaryReader.ReadSingle();
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
    /// Weapon枚举。
    /// </summary>
    public enum WeaponEnum : int
    {
        /// <summary>
        /// 未知。
        /// <summary>
        Unknown = 0,
        /// <summary>
        /// 
        /// </summary>
        加特林 = 1,
        /// <summary>
        /// 
        /// </summary>
        榴弹炮 = 2,
        /// <summary>
        /// 
        /// </summary>
        骑士剑 = 3,
    }


}
