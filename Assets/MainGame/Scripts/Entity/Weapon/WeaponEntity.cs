using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 武器实体基类。数值来自 Weapon 表，场景引用在子类 SerializeField 中配置。
    /// </summary>
    public abstract class WeaponEntity : Entity
    {
        private WeaponData m_WeaponData;
        private DRWeapon m_DRWeapon;
        private CharacterEntity m_Owner;

        public WeaponData WeaponData => m_WeaponData;

        public DRWeapon WeaponConfig => m_DRWeapon;

        public CharacterEntity Owner => m_Owner;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_WeaponData = userData as WeaponData;
            if (m_WeaponData == null)
            {
                Log.Error("WeaponEntity data is invalid.");
                return;
            }

            m_DRWeapon = GameEntry.DataTable.GetDataRow<DRWeapon>(TypeId);
            if (m_DRWeapon == null)
            {
                Log.Error("WeaponEntity config id '{0}' is invalid.", TypeId);
                return;
            }

            m_Owner = GameEntry.Entity.GetGameEntity(m_WeaponData.OwnerEntityId) as CharacterEntity;
            if (m_Owner == null)
            {
                Log.Error("WeaponEntity owner '{0}' not found.", m_WeaponData.OwnerEntityId);
                GameEntry.Entity.HideEntity(this);
                return;
            }

            Name = m_DRWeapon.Name;
            GameEntry.Entity.AttachEntity(this, m_Owner, m_WeaponData.AttachTransformPath);
            OnEquipped();

            Log.Info("Weapon entity '{0}' equipped on character '{1}'.", Name, m_Owner.Name);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            OnUnequipped();
            m_Owner = null;
            m_DRWeapon = null;
            m_WeaponData = null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);
            CachedTransform.localPosition = Vector3.zero;
            CachedTransform.localRotation = Quaternion.identity;
            CachedTransform.localScale = Vector3.one;
        }

        /// <summary>挂接到角色后调用，子类可扩展开火、攻击等逻辑入口。</summary>
        protected virtual void OnEquipped()
        {
        }

        /// <summary>隐藏销毁前调用。</summary>
        protected virtual void OnUnequipped()
        {
        }
    }
}
