using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 角色预制体配置：布偶骨骼根与死亡散架参数。
    /// </summary>
    public sealed class CharacterPrefabConfig : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_RagBoneRoot;

        [Header("死亡布偶")]
        [SerializeField]
        private bool m_DetachLimbsOnDeath;

        [SerializeField]
        private float m_DeathRagdollHideDelay = 3f;

        [SerializeField]
        private float m_DeathScatterForce = 12f;

        [Header("默认武器")]
        [SerializeField]
        private GameObject m_WeaponSocket;

        [SerializeField]
        private WeaponEnum m_DefaultWeapon = WeaponEnum.Unknown;

        /// <summary>布偶姿态对齐用的骨骼根（通常为 hips）。</summary>
        public GameObject RagBoneRoot => m_RagBoneRoot;

        public GameObject WeaponSocket => m_WeaponSocket;

        public WeaponEnum DefaultWeapon => m_DefaultWeapon;

        /// <summary>死亡时启用散架模式（依赖预制体关节 breakForce/breakTorque 配置；回池前仅重置骨骼默认 local 位姿）。</summary>
        public bool DetachLimbsOnDeath => m_DetachLimbsOnDeath;

        public float DeathRagdollHideDelay => m_DeathRagdollHideDelay;

        public float DeathScatterForce => m_DeathScatterForce;
    }
}
