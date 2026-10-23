using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 布偶预制体配置：死亡散架时销毁指定骨骼上的 CharacterJoint（回池时由 RagdollEntity 重建）。
    /// </summary>
    public sealed class RagdollJointDetachConfig : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("死亡散架（DetachLimbs=true）时断开的关节。击飞布偶不会断开。")]
        private CharacterJoint[] m_JointsToDetachOnDeath;

        /// <summary>Inspector 配置的关节（仅用于首次解析骨骼 Transform）。</summary>
        public CharacterJoint[] JointsToDetachOnDeath => m_JointsToDetachOnDeath;

        /// <summary>运行时缓存的骨骼 Transform，避免 Joint 被 Destroy 后引用失效。</summary>
        private Transform[] m_DetachBoneTransforms;

        private void Awake()
        {
            EnsureBoneTransformsCached();
        }

        /// <summary>在实体 OnInit 时调用，保证对象池复用前已解析骨骼 Transform。</summary>
        public void EnsureBoneTransformsCached()
        {
            CacheDetachBoneTransforms();
        }

        /// <summary>将配置中的关节与父刚体断开。</summary>
        public void DetachConfiguredJoints()
        {
            EnsureBoneTransformsCached();
            if (m_DetachBoneTransforms == null)
            {
                return;
            }

            for (int i = 0; i < m_DetachBoneTransforms.Length; i++)
            {
                Transform bone = m_DetachBoneTransforms[i];
                if (bone == null)
                {
                    continue;
                }

                CharacterJoint joint = bone.GetComponent<CharacterJoint>();
                if (joint != null)
                {
                    Object.Destroy(joint);
                }
            }
        }

        private void CacheDetachBoneTransforms()
        {
            if (m_DetachBoneTransforms != null)
            {
                return;
            }

            if (m_JointsToDetachOnDeath == null || m_JointsToDetachOnDeath.Length == 0)
            {
                m_DetachBoneTransforms = System.Array.Empty<Transform>();
                return;
            }

            m_DetachBoneTransforms = new Transform[m_JointsToDetachOnDeath.Length];
            for (int i = 0; i < m_JointsToDetachOnDeath.Length; i++)
            {
                CharacterJoint joint = m_JointsToDetachOnDeath[i];
                m_DetachBoneTransforms[i] = joint != null ? joint.transform : null;
            }
        }
    }
}
