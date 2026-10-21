//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using GameFramework;
using UnityEngine;

namespace MainGame
{
    [Serializable]
    public abstract class EntityData : IReference
    {
        [SerializeField]
        private int m_Id = 0;
        [SerializeField]
        private int m_TypeId = 0;
        [SerializeField]
        private string m_AssetName = "";
        [SerializeField]
        private string m_EntityName = "";

        [SerializeField]
        private Vector3 m_Position = Vector3.zero;
        [SerializeField]
        private Quaternion m_Rotation = Quaternion.identity;
        [SerializeField]
        private Vector3 m_Scale = Vector3.one;

        public EntityData(int entityId, int typeId)
        {
            m_Id = entityId;
            m_TypeId = typeId;
        }

        /// <summary>
        /// 实体编号。
        /// 这个编号是创建实体时产生的编号
        /// </summary>
        public int Id
        {
            get { return m_Id; }
        }
        /// <summary>
        /// 设置实体编号,技能数据类创建时id为0,创建实体时才设置id
        /// </summary>
        /// <param name="id"></param>
        public void SetId(int id)
        {
            m_Id = id;
        }



        /// <summary>
        /// 实体类型编号。
        /// 这个编号是在txt数据表中定义的编号
        /// </summary>
        public int TypeId
        {
            get
            {
                return m_TypeId;
            }
        }

        /// <summary>
        /// 设置实体类型编号（从对象池取出后写入表 Id / 枚举值）。
        /// </summary>
        public void SetTypeId(int typeId)
        {
            m_TypeId = typeId;
        }

        /// <summary>
        /// 资源名称
        /// </summary>
        public string AssetName
        {
            get { return m_AssetName; }
            set { m_AssetName = value; }
        }
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName
        {
            get { return m_EntityName; }
            set { m_EntityName = value; }
        }

        /// <summary>
        /// 实体位置。
        /// </summary>
        public Vector3 Position
        {
            get
            {
                return m_Position;
            }
            set
            {
                m_Position = value;
            }
        }

        /// <summary>
        /// 实体朝向。
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return m_Rotation;
            }
            set
            {
                m_Rotation = value;
            }
        }

        public Vector3 Scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        public virtual void Clear()
        {
            m_Id = 0;
            m_TypeId = 0;
            m_AssetName = "";
            m_EntityName = "";
            m_Position = Vector3.zero;
            m_Rotation = Quaternion.identity;
            m_Scale = Vector3.one;
        }
    }
}
