using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色实体逻辑。移动等玩法基于刚体，后续与物理碎片交互。
    /// </summary>
    public class CharacterEntity : Entity
    {

        
        private CharacterData m_CharacterData;
        private DRCharacter m_DRCharacter;
        private Rigidbody m_Rigidbody;

        public CharacterData CharacterData => m_CharacterData;

        public DRCharacter CharacterConfig => m_DRCharacter;

        public Rigidbody Rigidbody => m_Rigidbody;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_CharacterData = userData as CharacterData;
            if (m_CharacterData == null)
            {
                Log.Error("CharacterEntity data is invalid.");
                return;
            }

            m_DRCharacter = GameEntry.DataTable.GetDataRow<DRCharacter>(TypeId);
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponentInChildren<Rigidbody>();
            }

            Name = m_DRCharacter != null ? m_DRCharacter.Name : Utility.Text.Format("Character_{0}", Id);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_CharacterData = null;
            m_DRCharacter = null;
            m_Rigidbody = null;
            base.OnHide(isShutdown, userData);
        }
    }
}
