using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 远程武器实体。枪口、弹壳抛出点等在 Prefab 上直接配置。
    /// </summary>
    public class RangedWeaponEntity : WeaponEntity
    {
        [SerializeField]
        private Transform m_Muzzle;

        [SerializeField]
        private Transform m_ShellEjectPoint;

        public Transform Muzzle => m_Muzzle;

        public Transform ShellEjectPoint => m_ShellEjectPoint;
    }
}
