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

        [Header("击飞起身")]
        [SerializeField]
        [Tooltip("受击布偶后等待起身的时间（秒）。")]
        private float m_KnockdownRecoverDelay = 2f;

        [SerializeField]
        [Tooltip("击飞冲量大小。")]
        private float m_KnockdownScatterForce = 12f;

        [SerializeField]
        [Tooltip("起身后无敌持续时间（秒）；击飞过程中始终无敌。")]
        private float m_PostGetUpInvincibleDuration = 1.5f;

        [SerializeField]
        [Tooltip("布偶姿态缓动拟合到起身动画第一帧的时长（秒）。")]
        private float m_KnockdownPoseBlendDuration = 0.4f;

        [Header("默认武器")]
        [SerializeField]
        private GameObject m_WeaponSocket;

        [SerializeField]
        private WeaponEnum m_DefaultWeapon = WeaponEnum.Unknown;

        /// <summary>布偶姿态对齐用的骨骼根（通常为 hips）。</summary>
        public GameObject RagBoneRoot => m_RagBoneRoot;

        public GameObject WeaponSocket => m_WeaponSocket;

        public Transform WeaponSocketTransform => m_WeaponSocket != null ? m_WeaponSocket.transform : null;

        public WeaponEnum DefaultWeapon => m_DefaultWeapon;

        /// <summary>
        /// 获取武器挂点相对实体根节点的路径（角色与布偶变体共用同一配置引用）。
        /// </summary>
        public bool TryGetWeaponAttachPath(Transform entityRoot, out string attachPath)
        {
            attachPath = null;
            Transform socket = WeaponSocketTransform;
            if (entityRoot == null || socket == null)
            {
                return false;
            }

            attachPath = TransformUtility.GetRelativeTransformPath(entityRoot, socket);
            return !string.IsNullOrEmpty(attachPath);
        }

        /// <summary>死亡时启用散架模式（由布偶 RagdollJointDetachConfig 脚本断开关节；回池时还原连接）。</summary>
        public bool DetachLimbsOnDeath => m_DetachLimbsOnDeath;

        public float DeathRagdollHideDelay => m_DeathRagdollHideDelay;

        public float DeathScatterForce => m_DeathScatterForce;

        public float KnockdownRecoverDelay => m_KnockdownRecoverDelay;

        public float KnockdownScatterForce => m_KnockdownScatterForce;

        public float PostGetUpInvincibleDuration => m_PostGetUpInvincibleDuration;

        public float KnockdownPoseBlendDuration => m_KnockdownPoseBlendDuration;
    }
}
