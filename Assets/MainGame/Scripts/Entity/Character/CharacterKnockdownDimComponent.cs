using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 击飞布偶与起身过程中，将可见模型（布偶或角色）持续压暗。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterKnockdownDimComponent : MonoBehaviour
    {
        private CharacterEntity m_Owner;
        private CharacterKnockdownComponent m_Knockdown;
        private CharacterSustainedDimComponent m_OwnerDim;
        private CharacterSustainedDimComponent m_RagdollDim;
        private GameObject m_ActiveRagdollObject;

        public static CharacterKnockdownDimComponent Ensure(CharacterEntity owner)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterKnockdownDimComponent component = owner.GetComponent<CharacterKnockdownDimComponent>();
            if (component == null)
            {
                component = owner.gameObject.AddComponent<CharacterKnockdownDimComponent>();
            }

            component.m_Owner = owner;
            return component;
        }

        public void Clear()
        {
            ClearOwnerDim();
            ClearRagdollDim();
            m_ActiveRagdollObject = null;
        }

        public void Tick()
        {
            if (m_Owner == null)
            {
                return;
            }

            if (m_Knockdown == null)
            {
                m_Knockdown = m_Owner.GetComponent<CharacterKnockdownComponent>();
            }

            if (m_Knockdown == null || !m_Knockdown.DimDuringKnockdown)
            {
                Clear();
                return;
            }

            if (!m_Knockdown.IsKnockedDown)
            {
                Clear();
                return;
            }

            if (m_Knockdown.IsInRagdollPhase)
            {
                ApplyRagdollDim();
                return;
            }

            ApplyOwnerDim();
        }

        private void ApplyRagdollDim()
        {
            ClearOwnerDim();

            RagdollEntity ragdoll = CharacterEntity.FindActiveRagdollEntityFor(m_Owner);
            GameObject ragdollObject = ragdoll != null ? ragdoll.gameObject : null;
            if (ragdollObject == null)
            {
                ClearRagdollDim();
                return;
            }

            if (m_ActiveRagdollObject != ragdollObject)
            {
                ClearRagdollDim();
                m_ActiveRagdollObject = ragdollObject;
                m_RagdollDim = CharacterSustainedDimComponent.Ensure(ragdollObject);
            }

            m_RagdollDim?.ApplyDim();
        }

        private void ApplyOwnerDim()
        {
            if (m_ActiveRagdollObject != null)
            {
                ClearRagdollDim();
            }

            if (m_OwnerDim == null)
            {
                m_OwnerDim = CharacterSustainedDimComponent.Ensure(m_Owner.gameObject);
            }

            m_OwnerDim.ApplyDim();
        }

        private void ClearOwnerDim()
        {
            m_OwnerDim?.Clear();
            m_OwnerDim = null;
        }

        private void ClearRagdollDim()
        {
            m_RagdollDim?.Clear();
            m_RagdollDim = null;
            m_ActiveRagdollObject = null;
        }
    }
}
