using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色击飞布偶与起身流程（玩家/怪物共用）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterKnockdownComponent : MonoBehaviour
    {
        private const float DefaultRecoverDelay = 2f;
        private const float DefaultPoseBlendDuration = 0.4f;
        private const float DefaultGroundSnapDuration = 0.5f;
        private const float GroundSnapHeightThreshold = 0.05f;

        private enum KnockdownPhase
        {
            None,
            Ragdoll,
            PoseBlending,
            GettingUp,
        }

        public struct Options
        {
            public bool FollowCameraWithRagdoll;
            public bool TransferWeaponToRagdoll;
            public bool DimDuringKnockdown;
            public float PostGetUpInvincibleDuration;
        }

        private CharacterEntity m_Owner;
        private Options m_Options;
        private float m_RecoverDelay;
        private float m_PoseBlendDuration;
        private float m_Elapsed;
        private float m_PoseBlendElapsed;
        private float m_InvincibleRemaining;
        private float m_InvincibleTotalDuration;
        private int m_ActiveRagdollEntityId;
        private KnockdownPhase m_Phase;
        private string m_GetUpAnimatorStateName;
        private bool m_GetUpPlaybackStarted;
        private RagdollBonePoseSnapshot m_RagdollPoseSnapshot;
        private RagdollBonePoseSnapshot m_AnimFirstFramePoseSnapshot;
        private bool m_WeaponTransferredToRagdoll;
        private float m_GroundSnapStartY;
        private float m_GroundSnapTargetY;
        private float m_GroundSnapElapsed;
        private float m_GroundSnapDuration;
        private bool m_GroundSnapActive;

        /// <summary>击飞布偶中、骨骼拟合或起身动画播放中。</summary>
        public bool IsKnockedDown => m_Phase == KnockdownPhase.Ragdoll
            || m_Phase == KnockdownPhase.PoseBlending
            || m_Phase == KnockdownPhase.GettingUp;

        /// <summary>仅处于物理布偶阶段（可再次受爆炸冲量）。</summary>
        public bool IsInRagdollPhase => m_Phase == KnockdownPhase.Ragdoll;

        /// <summary>骨骼拟合或起身动画播放中（可自当前姿态重新击飞）。</summary>
        public bool IsInRecoveryPhase => m_Phase == KnockdownPhase.PoseBlending
            || m_Phase == KnockdownPhase.GettingUp;

        public bool IsInvincible => IsKnockedDown || m_InvincibleRemaining > 0f;

        public bool DimDuringKnockdown => m_Options.DimDuringKnockdown;

        /// <summary>起身动画结束后的短暂无敌（不含击飞/起身过程）。</summary>
        public bool IsInPostGetUpInvincibility => m_Phase == KnockdownPhase.None && m_InvincibleRemaining > 0f;

        public float PostGetUpInvincibleRemaining => m_InvincibleRemaining;

        public float PostGetUpInvincibleTotal => m_InvincibleTotalDuration;

        public static CharacterKnockdownComponent Ensure(CharacterEntity owner, Options options)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterKnockdownComponent knockdown = owner.GetComponent<CharacterKnockdownComponent>();
            if (knockdown == null)
            {
                knockdown = owner.gameObject.AddComponent<CharacterKnockdownComponent>();
            }

            knockdown.Initialize(owner, options);
            return knockdown;
        }

        public void Initialize(CharacterEntity owner, Options options)
        {
            m_Owner = owner;
            m_Options = options;
            RefreshTimingFromConfig();
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

        /// <summary>击飞布偶已就地散架后仅重置组件状态，不隐藏布偶、不归还武器。</summary>
        public void ClearAfterDeathRagdollConversion()
        {
            m_ActiveRagdollEntityId = 0;
            ResetState();
        }

        /// <summary>击飞布偶阶段死亡时，就地断开关节并散架，避免在隐藏角色原位重生布偶。</summary>
        public bool TryConvertActiveRagdollToDeathScatter(Vector3 forceOriginWorld, float scatterForce, float hideDelay)
        {
            if (!IsInRagdollPhase || m_Owner == null)
            {
                return false;
            }

            RagdollEntity ragdoll = ResolveActiveRagdoll();
            if (ragdoll == null)
            {
                return false;
            }

            ragdoll.ConvertToDeathScatter(forceOriginWorld, scatterForce, hideDelay);
            m_ActiveRagdollEntityId = 0;
            return true;
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
                    SyncOwnerPositionWithActiveRagdoll();
                    TryTransferWeaponToRagdoll();
                    TickRagdollRecover(elapseSeconds);
                    break;
                case KnockdownPhase.PoseBlending:
                    TickPoseBlending(elapseSeconds);
                    break;
                case KnockdownPhase.GettingUp:
                    TickGetUpAnimation(elapseSeconds);
                    break;
            }
        }

        /// <summary>进入击飞布偶；scatterForce &lt;= 0 时使用 PrefabConfig.KnockdownScatterForce。</summary>
        public bool TryEnterKnockdown(int attackerEntityId, float scatterForce = 0f, Vector3? forceOriginWorld = null)
        {
            if (m_Owner == null || IsKnockedDown)
            {
                return false;
            }

            RefreshTimingFromConfig();
            CharacterPrefabConfig config = m_Owner.PrefabConfig;
            if (scatterForce <= 0f)
            {
                scatterForce = config != null ? config.KnockdownScatterForce : 12f;
            }

            m_Phase = KnockdownPhase.Ragdoll;
            m_Elapsed = 0f;
            m_PoseBlendElapsed = 0f;
            m_InvincibleRemaining = 0f;
            m_InvincibleTotalDuration = 0f;
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_WeaponTransferredToRagdoll = false;
            ClearPoseSnapshots();

            m_Owner.SetKnockdownPresentationActive(false);
            int ragdollEntityId = m_Owner.SpawnKnockdownRagdoll(attackerEntityId, scatterForce, forceOriginWorld);
            m_ActiveRagdollEntityId = ragdollEntityId;
            TryTransferWeaponToRagdoll();

            if (m_Options.FollowCameraWithRagdoll
                && ragdollEntityId != 0
                && TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                camera.SetFollowEntity(ragdollEntityId);
            }

            return ragdollEntityId != 0;
        }

        /// <summary>击飞布偶阶段将隐藏的角色根节点同步到布偶位置（爆炸范围、飘字等依赖实体坐标）。</summary>
        public void SyncOwnerPositionWithActiveRagdoll()
        {
            if (m_Owner == null || !IsInRagdollPhase)
            {
                return;
            }

            RagdollEntity ragdoll = ResolveActiveRagdoll();
            if (ragdoll == null || !ragdoll.TryGetHipsWorldPosition(out Vector3 hipsWorld))
            {
                return;
            }

            Vector3 hipLocalOffset = CharacterRagdollRecoveryUtility.ResolveHipLocalOffsetFromRoot(
                m_Owner.CachedTransform,
                m_Owner.PrefabConfig);
            Vector3 rootWorld = hipsWorld - m_Owner.CachedTransform.rotation * hipLocalOffset;
            m_Owner.SetCharacterWorldPose(rootWorld, m_Owner.CachedTransform.rotation);
        }

        /// <summary>布偶阶段再次受爆炸：施加冲量并重置起身倒计时。</summary>
        public void ApplyRagdollExplosionForce(Vector3 forceOriginWorld, float scatterForce)
        {
            if (!IsInRagdollPhase || m_Owner == null || scatterForce <= 0f)
            {
                return;
            }

            RagdollEntity ragdoll = ResolveActiveRagdoll();
            ragdoll?.ApplyExplosionScatterForce(forceOriginWorld, scatterForce);
            m_Elapsed = 0f;
        }

        /// <summary>起身拟合/起身动画阶段再次受击：从当前姿态重新进入布偶。</summary>
        public bool TryReenterKnockdownFromRecovery(int attackerEntityId, float scatterForce = 0f, Vector3? forceOriginWorld = null)
        {
            if (m_Owner == null || !IsInRecoveryPhase)
            {
                return false;
            }

            RefreshTimingFromConfig();
            CharacterPrefabConfig config = m_Owner.PrefabConfig;
            if (scatterForce <= 0f)
            {
                scatterForce = config != null ? config.KnockdownScatterForce : 12f;
            }

            ClearPoseSnapshots();
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_WeaponTransferredToRagdoll = false;
            m_Phase = KnockdownPhase.Ragdoll;
            m_Elapsed = 0f;
            m_PoseBlendElapsed = 0f;
            m_InvincibleRemaining = 0f;
            m_InvincibleTotalDuration = 0f;

            m_Owner.SetAnimatorEnabledForPoseBlend(false);
            m_Owner.SetKnockdownPresentationActive(false);
            int ragdollEntityId = m_Owner.SpawnKnockdownRagdoll(attackerEntityId, scatterForce, forceOriginWorld);
            m_ActiveRagdollEntityId = ragdollEntityId;
            TryTransferWeaponToRagdoll();

            if (m_Options.FollowCameraWithRagdoll
                && ragdollEntityId != 0
                && TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                camera.SetFollowEntity(ragdollEntityId);
            }

            return ragdollEntityId != 0;
        }

        private void RefreshTimingFromConfig()
        {
            CharacterPrefabConfig config = m_Owner != null ? m_Owner.PrefabConfig : null;
            m_RecoverDelay = config != null && config.KnockdownRecoverDelay > 0f
                ? config.KnockdownRecoverDelay
                : DefaultRecoverDelay;
            m_PoseBlendDuration = config != null && config.KnockdownPoseBlendDuration > 0f
                ? config.KnockdownPoseBlendDuration
                : DefaultPoseBlendDuration;
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

            if (m_Options.FollowCameraWithRagdoll
                && TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                camera.SetFollowEntity(m_Owner.Id);
            }

            BeginGroundSnapEase();

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
            ApplyGroundSnapEase(0f);
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
            m_GroundSnapElapsed += elapseSeconds;
            float groundSnap01 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_GroundSnapElapsed / m_GroundSnapDuration));
            CharacterRagdollRecoveryUtility.TryApplyRecoverPoseBlend(
                m_Owner,
                m_RagdollPoseSnapshot,
                m_AnimFirstFramePoseSnapshot,
                smoothBlend);
            ApplyGroundSnapEase(groundSnap01);
            if (groundSnap01 >= 1f)
            {
                m_GroundSnapActive = false;
            }

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

            ApplyGroundSnapEase(1f);
            m_GroundSnapActive = false;
            m_Owner.BeginGetUpAnimationPlayback(m_GetUpAnimatorStateName);
            m_GetUpPlaybackStarted = true;
            m_Phase = KnockdownPhase.GettingUp;
            ClearPoseSnapshots();
        }

        private void TickGetUpAnimation(float elapseSeconds)
        {
            if (m_Owner == null)
            {
                return;
            }

            TickGroundSnapEase(elapseSeconds);

            if (!m_Owner.IsGetUpAnimationComplete(m_GetUpPlaybackStarted, m_GetUpAnimatorStateName))
            {
                return;
            }

            FinishGetUp();
        }

        private void BeginGroundSnapEase()
        {
            m_GroundSnapElapsed = 0f;
            m_GroundSnapDuration = m_PoseBlendDuration > 0f ? m_PoseBlendDuration : DefaultGroundSnapDuration;
            m_GroundSnapStartY = m_Owner.CachedTransform.position.y;
            m_GroundSnapTargetY = 0f;
            m_GroundSnapActive = m_GroundSnapStartY > GroundSnapHeightThreshold;
        }

        private void TickGroundSnapEase(float elapseSeconds)
        {
            if (!m_GroundSnapActive || m_Owner == null)
            {
                return;
            }

            m_GroundSnapElapsed += elapseSeconds;
            float eased01 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_GroundSnapElapsed / m_GroundSnapDuration));
            ApplyGroundSnapEase(eased01);

            if (eased01 >= 1f)
            {
                m_GroundSnapActive = false;
            }
        }

        private void ApplyGroundSnapEase(float eased01)
        {
            if (m_Owner == null)
            {
                return;
            }

            Vector3 position = m_Owner.CachedTransform.position;
            position.y = Mathf.Lerp(m_GroundSnapStartY, m_GroundSnapTargetY, eased01);
            m_Owner.SetCharacterWorldPose(position, m_Owner.CachedTransform.rotation);
        }

        private void FinishGroundSnap()
        {
            if (m_Owner == null)
            {
                m_GroundSnapActive = false;
                return;
            }

            Vector3 position = m_Owner.CachedTransform.position;
            position.y = 0f;
            m_Owner.SetCharacterWorldPose(position, m_Owner.CachedTransform.rotation);
            m_GroundSnapActive = false;
        }

        private void FinishGetUp()
        {
            FinishGroundSnap();
            m_Phase = KnockdownPhase.None;
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_InvincibleRemaining = Mathf.Max(0f, m_Options.PostGetUpInvincibleDuration);
            m_InvincibleTotalDuration = m_InvincibleRemaining;
            ClearPoseSnapshots();

            if (m_InvincibleRemaining > 0f && m_Owner != null)
            {
                CharacterInvincibilityFlashComponent.Ensure(m_Owner)?.OnPostGetUpInvincibilityBegan();
            }
        }

        private void TryTransferWeaponToRagdoll()
        {
            if (!m_Options.TransferWeaponToRagdoll || m_WeaponTransferredToRagdoll || m_Owner == null)
            {
                return;
            }

            RagdollEntity ragdoll = ResolveActiveRagdoll();
            if (ragdoll == null)
            {
                return;
            }

            CharacterWeaponComponent weaponComponent = m_Owner.GetComponent<CharacterWeaponComponent>();
            if (weaponComponent != null && weaponComponent.TryTransferWeaponToRagdoll(ragdoll))
            {
                m_WeaponTransferredToRagdoll = true;
            }
        }

        private void TransferWeaponBackToCharacter()
        {
            if (!m_Options.TransferWeaponToRagdoll || m_Owner == null)
            {
                return;
            }

            CharacterWeaponComponent weaponComponent = m_Owner.GetComponent<CharacterWeaponComponent>();
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
            m_InvincibleTotalDuration = 0f;
            m_ActiveRagdollEntityId = 0;
            m_Phase = KnockdownPhase.None;
            m_GetUpAnimatorStateName = null;
            m_GetUpPlaybackStarted = false;
            m_WeaponTransferredToRagdoll = false;
            m_GroundSnapActive = false;
            m_GroundSnapElapsed = 0f;
            m_GroundSnapDuration = 0f;
            ClearPoseSnapshots();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
