using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 玩家角色：读取输入，通过刚体在世界平面移动。
    /// </summary>
    public class PlayerCharacterEntity : CharacterEntity, IDamageable
    {
        private static readonly int SpeedParamHash = Animator.StringToHash("Speed");

        private Animator m_Animator;
        private float m_AnimatorSpeed;
        private CharacterWeaponComponent m_WeaponComponent;
        private CharacterCombatComponent m_CombatComponent;
        private PlayerKnockdownComponent m_KnockdownComponent;

        private float m_CurrentHealth;
        private bool m_IsAlive;

        public override CharacterFaction Faction => CharacterFaction.Player;

        public bool IsAlive => m_IsAlive;

        public float CurrentHealth => m_CurrentHealth;

        public float MaxHealth => CharacterConfig != null ? CharacterConfig.Health : 0f;

        public bool IsKnockedDown => m_KnockdownComponent != null && m_KnockdownComponent.IsKnockedDown;

        public bool IsInvincible => m_KnockdownComponent != null && m_KnockdownComponent.IsInvincible;

        public Animator Animator => m_Animator;

        public CharacterWeaponComponent WeaponComponent => m_WeaponComponent;

        public WeaponEntity CurrentWeaponEntity => m_WeaponComponent?.CurrentWeaponEntity;

        public int CurrentWeaponId => m_WeaponComponent != null ? m_WeaponComponent.CurrentWeaponId : 0;

        public void EquipWeapon(WeaponEnum weaponEnum)
        {
            m_WeaponComponent?.EquipWeapon(weaponEnum);
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_CurrentHealth = MaxHealth;
            m_IsAlive = m_CurrentHealth > 0f;
            DamageableCharacterEntity.SetupDamageReceivers(this, this);

            m_WeaponComponent = GetComponent<CharacterWeaponComponent>();
            if (m_WeaponComponent == null)
            {
                m_WeaponComponent = gameObject.AddComponent<CharacterWeaponComponent>();
            }

            m_WeaponComponent.Initialize(this, PrefabConfig);

            m_Animator = GetComponentInChildren<Animator>();
            m_AnimatorSpeed = 0f;
            if (m_Animator != null)
            {
                m_Animator.keepAnimatorStateOnDisable = true;
                m_Animator.SetFloat(SpeedParamHash, 0f);
            }

            m_CombatComponent = GetComponent<CharacterCombatComponent>();
            if (m_CombatComponent == null)
            {
                m_CombatComponent = gameObject.AddComponent<CharacterCombatComponent>();
            }

            m_CombatComponent.Initialize(this, m_WeaponComponent, m_Animator);
            EquipWeapon(WeaponEnum.加特林);

            m_KnockdownComponent = GetComponent<PlayerKnockdownComponent>();
            if (m_KnockdownComponent == null)
            {
                m_KnockdownComponent = gameObject.AddComponent<PlayerKnockdownComponent>();
            }

            m_KnockdownComponent.Initialize(this);

            if (Rigidbody == null)
            {
                return;
            }

            Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            if (m_KnockdownComponent != null)
            {
                m_KnockdownComponent.Clear();
                m_KnockdownComponent = null;
            }

            if (m_WeaponComponent != null)
            {
                m_WeaponComponent.ClearCurrentWeapon();
            }

            m_Animator = null;
            m_AnimatorSpeed = 0f;
            m_CombatComponent = null;
            m_WeaponComponent = null;
            m_IsAlive = false;
            m_CurrentHealth = 0f;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (!m_IsAlive)
            {
                return;
            }

            m_KnockdownComponent?.Tick(elapseSeconds);
            if (IsKnockedDown)
            {
                return;
            }

            UpdateMovement(elapseSeconds);
            m_CombatComponent?.Tick(elapseSeconds);
        }

        /// <summary>采样起身动画第一帧骨骼姿态（用于布偶→动画的拟合目标）。</summary>
        public RagdollBonePoseSnapshot SampleGetUpAnimationFirstFrame(string getUpAnimatorStateName, string boneRootName)
        {
            if (m_Animator == null || string.IsNullOrEmpty(getUpAnimatorStateName))
            {
                return new RagdollBonePoseSnapshot();
            }

            m_AnimatorSpeed = 0f;
            m_Animator.SetFloat(SpeedParamHash, 0f);
            m_Animator.speed = 1f;
            m_Animator.enabled = true;
            m_Animator.Play(getUpAnimatorStateName, 0, 0f);
            m_Animator.Update(0f);
            return RagdollBonePoseSnapshot.CaptureFromHierarchy(CachedTransform, boneRootName);
        }

        /// <summary>骨骼拟合期间关闭 Animator，避免与手动写骨骼冲突。</summary>
        public void SetAnimatorEnabledForPoseBlend(bool enabled)
        {
            if (m_Animator != null)
            {
                m_Animator.enabled = enabled;
            }
        }

        /// <summary>布偶起身后播放趴起身/坐起身状态（骨骼已对齐第 0 帧时仅接管 Animator）。</summary>
        public void BeginGetUpAnimationPlayback(string getUpAnimatorStateName)
        {
            if (m_Animator == null || string.IsNullOrEmpty(getUpAnimatorStateName))
            {
                return;
            }

            m_AnimatorSpeed = 0f;
            m_Animator.SetFloat(SpeedParamHash, 0f);
            m_Animator.speed = 1f;
            m_Animator.enabled = true;
            m_Animator.Play(getUpAnimatorStateName, 0, 0f);
            m_Animator.Update(0f);
        }

        /// <summary>起身动画是否已结束（离开起身状态或 normalizedTime ≥ 1）。</summary>
        public bool IsGetUpAnimationComplete(bool playbackStarted, string getUpAnimatorStateName)
        {
            if (!playbackStarted || m_Animator == null || string.IsNullOrEmpty(getUpAnimatorStateName))
            {
                return false;
            }

            int getUpStateHash = Animator.StringToHash(getUpAnimatorStateName);
            AnimatorStateInfo state = m_Animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == getUpStateHash)
            {
                return state.normalizedTime >= 1f;
            }

            return true;
        }

        public void TakeDamage(float damage, int attackerEntityId = 0)
        {
            if (!m_IsAlive || damage <= 0f || IsInvincible)
            {
                return;
            }

            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);
            GameEntry.Entity.ShowWorldUIFloatingText(Id, $"-{damage:0}");

            if (m_CurrentHealth <= 0f)
            {
                OnDead(attackerEntityId);
                return;
            }

            m_KnockdownComponent?.TryEnterKnockdown(attackerEntityId);
        }

        protected override DeathRagdollSettings GetDeathRagdollSettings()
        {
            DeathRagdollSettings settings = base.GetDeathRagdollSettings();
            settings.DetachLimbs = false;
            // 玩家死亡后摄像机继续跟随布偶，不自动回收。
            settings.HideDelay = 0f;
            return settings;
        }

        private void OnDead(int attackerEntityId)
        {
            m_IsAlive = false;
            if (m_KnockdownComponent != null)
            {
                m_KnockdownComponent.Clear();
            }

            // 击飞中可能关掉了渲染/物理，死亡生成布偶前先恢复，避免 HideEntity 时状态残留。
            SetKnockdownPresentationActive(true);

            int ragdollEntityId = SpawnDeathRagdollAndHide(attackerEntityId);
            if (ragdollEntityId != 0 && TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                camera.SetFollowEntity(ragdollEntityId);
            }

            Log.Info("Player character defeated.");
        }

        private void UpdateMovement(float elapseSeconds)
        {
            if (Rigidbody == null || CharacterConfig == null || GameEntry.Control == null)
            {
                return;
            }

            Vector2 moveInput = GameEntry.Control.GetMoveVector();
            Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            Rigidbody.angularVelocity = Vector3.zero;

            float moveSpeed = CharacterConfig.MoveSpeed;
            Vector3 velocity = Rigidbody.velocity;
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
            Rigidbody.velocity = velocity;

            UpdateAnimator(moveSpeed, elapseSeconds);

            if (moveDirection.sqrMagnitude <= 0.001f || IsCombatFacingLocked())
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion newRotation = Quaternion.Slerp(
                Rigidbody.rotation,
                targetRotation,
                Mathf.Clamp01(CharacterConfig.TurnSpeed * elapseSeconds));
            Rigidbody.MoveRotation(newRotation);
        }

        private void UpdateAnimator(float moveSpeed, float elapseSeconds)
        {
            if (m_Animator == null)
            {
                return;
            }

            float horizontalSpeed = new Vector3(Rigidbody.velocity.x, 0f, Rigidbody.velocity.z).magnitude;
            float targetSpeed = moveSpeed > 0.01f ? Mathf.Clamp01(horizontalSpeed / moveSpeed) : 0f;
            m_AnimatorSpeed = Mathf.Lerp(m_AnimatorSpeed, targetSpeed, elapseSeconds * 10f);
            m_Animator.SetFloat(SpeedParamHash, m_AnimatorSpeed);
        }

        private bool IsCombatFacingLocked()
        {
            return m_CombatComponent != null && m_CombatComponent.IsAttacking;
        }
    }
}
