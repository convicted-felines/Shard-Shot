using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色武器切换：通过 UGF 实体创建/隐藏管理武器，挂接由 <see cref="WeaponEntity"/> 完成。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterWeaponComponent : MonoBehaviour
    {
        private CharacterEntity m_Owner;
        private Transform m_WeaponSocket;
        private int m_CurrentWeaponId;
        private int m_CurrentWeaponEntityId;

        public int CurrentWeaponId => m_CurrentWeaponId;

        public int CurrentWeaponEntityId => m_CurrentWeaponEntityId;

        public WeaponEntity CurrentWeaponEntity
        {
            get
            {
                return m_CurrentWeaponEntityId == 0
                    ? null
                    : GameEntry.Entity.GetGameEntity(m_CurrentWeaponEntityId) as WeaponEntity;
            }
        }

        public DRWeapon CurrentWeaponConfig
        {
            get
            {
                return m_CurrentWeaponId == 0
                    ? null
                    : GameEntry.DataTable.GetDataRow<DRWeapon>(m_CurrentWeaponId);
            }
        }

        public void Initialize(CharacterEntity owner, Transform weaponSocket)
        {
            m_Owner = owner;
            m_WeaponSocket = weaponSocket;
        }

        public void EquipWeapon(WeaponEnum weaponEnum)
        {
            EquipWeapon((int)weaponEnum);
        }

        public void EquipWeapon(int weaponId)
        {
            if (weaponId <= 0)
            {
                ClearCurrentWeapon();
                return;
            }

            if (weaponId == m_CurrentWeaponId && IsCurrentWeaponAlive())
            {
                return;
            }

            DRWeapon config = GameEntry.DataTable.GetDataRow<DRWeapon>(weaponId);
            if (config == null)
            {
                Log.Warning("CharacterWeaponComponent: weapon id '{0}' not found.", weaponId);
                return;
            }

            if (m_Owner == null)
            {
                Log.Warning("CharacterWeaponComponent: owner is missing.");
                return;
            }

            if (m_WeaponSocket == null)
            {
                Log.Warning("CharacterWeaponComponent: weapon socket is missing.");
                return;
            }

            ClearCurrentWeapon();

            string attachPath = TransformUtility.GetRelativeTransformPath(m_Owner.CachedTransform, m_WeaponSocket);
            WeaponData data = WeaponData.Create(
                (WeaponEnum)weaponId,
                m_Owner.Id,
                attachPath,
                m_Owner.CachedTransform.position,
                m_Owner.CachedTransform.rotation);

            m_CurrentWeaponId = weaponId;
            m_CurrentWeaponEntityId = data.Id;
            GameEntry.Entity.ShowWeapon(data);
        }

        public void ClearCurrentWeapon()
        {
            if (m_CurrentWeaponEntityId != 0 && GameEntry.Entity.HasEntity(m_CurrentWeaponEntityId))
            {
                GameEntry.Entity.HideEntity(m_CurrentWeaponEntityId);
            }

            m_CurrentWeaponEntityId = 0;
            m_CurrentWeaponId = 0;
        }

        private bool IsCurrentWeaponAlive()
        {
            return m_CurrentWeaponEntityId != 0 && GameEntry.Entity.HasEntity(m_CurrentWeaponEntityId);
        }

        private void OnDestroy()
        {
            ClearCurrentWeapon();
            m_Owner = null;
            m_WeaponSocket = null;
        }
    }
}
