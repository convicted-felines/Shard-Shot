using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 玩家受击击飞：隐藏角色表现 → 生成布偶 → 倒计时后骨骼拟合到起身第一帧 → 播放趴/坐起身动画，击飞与起身后短时无敌。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerKnockdownComponent : MonoBehaviour
    {
        private const float DefaultRecoverDelay = 2f;
        private const float DefaultPostGetUpInvincible = 1.5f;
        private const float DefaultPoseBlendDuration = 0.4f;

        private enum KnockdownPhase
        {
            None,
            Ragdoll,
            PoseBlending,
            GettingUp,
        }

        private PlayerCharacterEntity m_Owner;
        private float m_RecoverDelay;
        private float m_PostGetUpInvincibleDuration;
        private float m_PoseBlendDuration;
        private float m_Elapsed;
        private float m_PoseBlendElapsed;
        private float m_InvincibleRemaining;
        private int m_ActiveRagdollEntityId;
        private KnockdownPhase m_Phase;
        private string m_GetUpAnimatorStateName;
        private bool m_GetUpPlaybackStarted;
        private RagdollBonePoseSnapshot m_RagdollPoseSnapshot;
        private RagdollBonePoseSnapshot m_AnimFirstFramePoseSnapshot;
        private bool m_WeaponTransferredToRagdoll;

        /// <summary>击飞布偶中、骨骼拟合或起身动画播放中，均不可操控。</summary>
        public bool IsKnockedDown => m_Phase == KnockdownPhase.Ragdoll
            || m_Phase == KnockdownPhase.PoseBlending
            || m_Phase == KnockdownPhase.GettingUp;

        public bool IsInvincible => IsKnockedDown || m_InvincibleRemaining > 0f;

        public void Initialize(PlayerCharacterEntity owner)
        {
            m_Owner = owner;
            CharacterPrefabConfig config = owner != null ? owner.PrefabConfig : null;
            m_RecoverDelay = config != null && config.KnockdownRecoverDelay > 0f
                ? config.KnockdownRecoverDelay
                : DefaultRecoverDelay;
            m_PostGetUpInvincibleDuration = config != null
                ? Mathf.Max(0f, config.PostGetUpInvincibleDuration)
                : DefaultPostGetUpInvincible;
            m_PoseBlendDuration = config != null && config.KnockdownPoseBlendDuration > 0f
                ? config.KnockdownPoseBlendDuration
                : DefaultPoseBlendDuration;
            ResetState();
        }

        public void Clear()
        {
            if (IsKnockedDown && m_Owner != null)
            {
                TransferWeaponBackToCharacter();
                m_Owner.SetKnockdownPresentationActive(true);
                m_Owner.SetAnimatorEnabledForPoseBlend(true);
            }

            HideActiveRagdollIfAny();
            ResetState();
            m_Owner = null;
        }

        public void Tick(float elapseSeconds)
        {
            if (m_InvincibleRemaining > 0f)
            {
                m_InvincibleRemaining = Mathf.Max(0f, m_InvincibleRemaining - elapseSeconds);
            }

            if (m_Owner == null)
            {
                return;
            }

            switch (m_Phase)
            {
                case KnockdownPhase.Ragdoll:
                    TryTransferWeaponToRagdoll();
                    TickRagdollRecover(elapseSeconds);
                    break;
                case KnockdownPhase.PoseBlending:
                    TickPoseBlending(elapseSeconds);
                    break;
                case KnockdownPhase.GettingUp:
                    TickGetUpAnimation();
                    break;
            }
        }

        /// <summary>受击进入击飞；已击飞或无敌时由调用方拦截。</summary>
        public bool TryEnterKnockdown(int attackerEntityId)
        {
            if (m_Owner == null || IsKnockedDown)
            {
                return false;
            }

            CharacterPrefabConfig config = m_Owner.PrefabConfig;
            float scatterForce = config != null ? config.KnockdownScatterForce : 12f;
            m_RecoverDelay = config != null && config.KnockdownRecoverDelay > 0f
                ? config.KnockdownRecoverDelay
                : DefaultRecoverDelay;
            m_PostGetUpInvincibleDuration = config != null
                ? Mathf.Max(0f, config.PostGetUpInvincibleDuration)
                : DefaultPostGetUpInvincible;
            m_PoseBlendDuration = config != null && config.KnockdownPoseBlendDuration > 0f
                ? config.KnockdownPoseBlendDuration
                : DefaultPoseBlendDuration;

            m_Phase = KnockdownPhase.Ragdoll;
            m_Elapsed = 0f;
            m_PoseBlendElapsed = 0f;
            m_InvincibleRemaining = 0f;
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_WeaponTransferredToRagdoll = false;
            ClearPoseSnapshots();

            m_Owner.SetKnockdownPresentationActive(false);
            int ragdollEntityId = m_Owner.SpawnKnockdownRagdoll(attackerEntityId, scatterForce);
            m_ActiveRagdollEntityId = ragdollEntityId;
            TryTransferWeaponToRagdoll();

            if (ragdollEntityId != 0 && TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                camera.SetFollowEntity(ragdollEntityId);
            }

            return true;
        }

        private void TickRagdollRecover(float elapseSeconds)
        {
            m_Elapsed += elapseSeconds;
            if (m_Elapsed < m_RecoverDelay)
            {
                return;
            }

            BeginGetUpFromRagdoll();
        }

        private void BeginGetUpFromRagdoll()
        {
            if (m_Phase != KnockdownPhase.Ragdoll || m_Owner == null)
            {
                return;
            }

            RagdollEntity ragdoll = ResolveActiveRagdoll();
            m_GetUpAnimatorStateName = CharacterRagdollRecoveryUtility.ResolveGetUpAnimatorStateName(ragdoll);
            TransferWeaponBackToCharacter();
            bool canBlend = CharacterRagdollRecoveryUtility.TryPrepareRecoverPoseBlend(
                m_Owner,
                ragdoll,
                m_GetUpAnimatorStateName,
                out m_RagdollPoseSnapshot,
                out m_AnimFirstFramePoseSnapshot);

            m_ActiveRagdollEntityId = 0;

            if (TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                camera.SetFollowEntity(m_Owner.Id);
            }

            if (!canBlend || m_PoseBlendDuration <= 0f)
            {
                StartGetUpAnimationPlayback();
                return;
            }

            m_PoseBlendElapsed = 0f;
            m_Phase = KnockdownPhase.PoseBlending;
            CharacterRagdollRecoveryUtility.TryApplyRecoverPoseBlend(
                m_Owner,
                m_RagdollPoseSnapshot,
                m_AnimFirstFramePoseSnapshot,
                0f);
        }

        private void TickPoseBlending(float elapseSeconds)
        {
            if (m_Owner == null)
            {
                return;
            }

            m_PoseBlendElapsed += elapseSeconds;
            float blend01 = Mathf.Clamp01(m_PoseBlendElapsed / m_PoseBlendDuration);
            float smoothBlend = Mathf.SmoothStep(0f, 1f, blend01);
            CharacterRagdollRecoveryUtility.TryApplyRecoverPoseBlend(
                m_Owner,
                m_RagdollPoseSnapshot,
                m_AnimFirstFramePoseSnapshot,
                smoothBlend);

            if (blend01 < 1f)
            {
                return;
            }

            StartGetUpAnimationPlayback();
        }

        private void StartGetUpAnimationPlayback()
        {
            if (m_AnimFirstFramePoseSnapshot != null && m_AnimFirstFramePoseSnapshot.IsValid)
            {
                CharacterRagdollRecoveryUtility.TryApplyRecoverPoseBlend(
                    m_Owner,
                    m_RagdollPoseSnapshot,
                    m_AnimFirstFramePoseSnapshot,
                    1f);
            }

            m_Owner.BeginGetUpAnimationPlayback(m_GetUpAnimatorStateName);
            m_GetUpPlaybackStarted = true;
            m_Phase = KnockdownPhase.GettingUp;
            ClearPoseSnapshots();
        }

        private void TickGetUpAnimation()
        {
            if (m_Owner == null)
            {
                return;
            }

            if (!m_Owner.IsGetUpAnimationComplete(m_GetUpPlaybackStarted, m_GetUpAnimatorStateName))
            {
                return;
            }

            FinishGetUp();
        }

        private void FinishGetUp()
        {
            m_Phase = KnockdownPhase.None;
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_InvincibleRemaining = m_PostGetUpInvincibleDuration;
            ClearPoseSnapshots();
        }

        private void TryTransferWeaponToRagdoll()
        {
            if (m_WeaponTransferredToRagdoll || m_Owner == null)
            {
                return;
            }

            RagdollEntity ragdoll = ResolveActiveRagdoll();
            if (ragdoll == null)
            {
                return;
            }

            CharacterWeaponComponent weaponComponent = m_Owner.WeaponComponent;
            if (weaponComponent != null && weaponComponent.TryTransferWeaponToRagdoll(ragdoll))
            {
                m_WeaponTransferredToRagdoll = true;
            }
        }

        private void TransferWeaponBackToCharacter()
        {
            if (m_Owner == null)
            {
                return;
            }

            CharacterWeaponComponent weaponComponent = m_Owner.WeaponComponent;
            if (weaponComponent != null)
            {
                weaponComponent.TryTransferWeaponBackToCharacter();
            }

            m_WeaponTransferredToRagdoll = false;
        }

        private RagdollEntity ResolveActiveRagdoll()
        {
            if (m_ActiveRagdollEntityId != 0)
            {
                RagdollEntity byId = GameEntry.Entity.GetGameEntity(m_ActiveRagdollEntityId) as RagdollEntity;
                if (byId != null)
                {
                    return byId;
                }
            }

            return m_Owner != null ? CharacterEntity.FindActiveRagdollEntityFor(m_Owner) : null;
        }

        private void HideActiveRagdollIfAny()
        {
            RagdollEntity ragdoll = ResolveActiveRagdoll();
            if (ragdoll != null)
            {
                GameEntry.Entity.HideEntity(ragdoll);
            }

            m_ActiveRagdollEntityId = 0;
        }

        private void ClearPoseSnapshots()
        {
            m_RagdollPoseSnapshot = null;
            m_AnimFirstFramePoseSnapshot = null;
        }

        private void ResetState()
        {
            m_Elapsed = 0f;
            m_PoseBlendElapsed = 0f;
            m_InvincibleRemaining = 0f;
            m_ActiveRagdollEntityId = 0;
            m_Phase = KnockdownPhase.None;
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_WeaponTransferredToRagdoll = false;
            ClearPoseSnapshots();
        }
    }
}
