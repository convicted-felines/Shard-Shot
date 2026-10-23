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
        private CharacterKnockdownComponent m_KnockdownComponent;
        private CharacterKnockbackComponent m_KnockbackComponent;
        private CharacterStunComponent m_StunComponent;
        private CharacterWorldHealthBarComponent m_HealthBarComponent;

        private float m_CurrentHealth;
        private bool m_IsAlive;

        public override CharacterFaction Faction => CharacterFaction.Player;

        public bool IsAlive => m_IsAlive;

        public float CurrentHealth => m_CurrentHealth;

        public float MaxHealth => CharacterConfig != null ? CharacterConfig.Health : 0f;

        public bool IsKnockedDown => m_KnockdownComponent != null && m_KnockdownComponent.IsKnockedDown;

        public bool IsInvincible => m_KnockdownComponent != null && m_KnockdownComponent.IsInvincible;

        public bool IsStunned => m_StunComponent != null && m_StunComponent.IsStunned;

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
            m_HealthBarComponent = CharacterWorldHealthBarComponent.Ensure(this);
            m_HealthBarComponent.Show(GetHealthProgress01());
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

            m_KnockdownComponent = CharacterKnockdownComponent.Ensure(this, BuildPlayerKnockdownOptions());
            m_KnockbackComponent = CharacterKnockbackComponent.Ensure(this);
            m_StunComponent = CharacterStunComponent.Ensure(this, m_Animator);

            if (Rigidbody == null)
            {
                return;
            }

            CharacterRigidbodyUtility.SetupTopDownMovement(Rigidbody);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_HealthBarComponent?.Clear();
            m_HealthBarComponent = null;

            if (m_KnockdownComponent != null)
            {
                m_KnockdownComponent.Clear();
                m_KnockdownComponent = null;
            }

            if (m_StunComponent != null)
            {
                m_StunComponent.Clear();
                m_StunComponent = null;
            }

            if (m_KnockbackComponent != null)
            {
                m_KnockbackComponent.Clear();
                m_KnockbackComponent = null;
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

            m_KnockbackComponent?.Tick(elapseSeconds);
            m_StunComponent?.Tick(elapseSeconds);
            if (IsStunned)
            {
                return;
            }

            UpdateMovement(elapseSeconds);
            m_CombatComponent?.Tick(elapseSeconds);
        }

        public void TakeDamage(float damage, int attackerEntityId = 0)
        {
            if (!m_IsAlive || damage <= 0f || IsInvincible)
            {
                return;
            }

            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);
            GameEntry.Entity.ShowWorldUIFloatingText(Id, $"-{damage:0}");
            m_HealthBarComponent?.SetProgress(GetHealthProgress01());

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
            settings.DetachLimbs = true;
            // 玩家死亡后摄像机继续跟随布偶，不自动回收。
            settings.HideDelay = 0f;
            return settings;
        }

        private void OnDead(int attackerEntityId)
        {
            m_IsAlive = false;
            m_HealthBarComponent?.Hide();

            DeathRagdollSettings settings = GetDeathRagdollSettings();
            Vector3 forceOrigin = ResolveDeathForceOrigin(attackerEntityId);

            if (m_KnockdownComponent != null
                && m_KnockdownComponent.TryConvertActiveRagdollToDeathScatter(forceOrigin, settings.ScatterForce, settings.HideDelay))
            {
                m_KnockdownComponent.ClearAfterDeathRagdollConversion();
                SetKnockdownPresentationActive(true);
                GameEntry.Entity.HideEntity(this);
                Log.Info("Player character defeated (knockdown ragdoll scattered).");
                return;
            }

            m_KnockdownComponent?.Clear();

            // 击飞中可能关掉了渲染/物理，死亡生成布偶前先恢复，避免 HideEntity 时状态残留。
            SetKnockdownPresentationActive(true);

            int ragdollEntityId = SpawnDeathRagdollAndHide(attackerEntityId, forceOrigin, settings.ScatterForce);
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

            float moveSpeed = CharacterConfig.MoveSpeed;
            Vector3 intendedVelocity = moveDirection * moveSpeed;
            if (m_KnockbackComponent != null)
            {
                m_KnockbackComponent.ApplyToRigidbody(Rigidbody, intendedVelocity);
            }
            else
            {
                CharacterRigidbodyUtility.ApplyPlanarVelocity(Rigidbody, intendedVelocity);
            }

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

            float horizontalSpeed = CharacterRigidbodyUtility.GetPlanarSpeed(Rigidbody);
            float targetSpeed = moveSpeed > 0.01f ? Mathf.Clamp01(horizontalSpeed / moveSpeed) : 0f;
            m_AnimatorSpeed = Mathf.Lerp(m_AnimatorSpeed, targetSpeed, elapseSeconds * 10f);
            m_Animator.SetFloat(SpeedParamHash, m_AnimatorSpeed);
        }

        private bool IsCombatFacingLocked()
        {
            return m_CombatComponent != null && m_CombatComponent.IsAttacking;
        }

        private float GetHealthProgress01()
        {
            return MaxHealth > 0f ? m_CurrentHealth / MaxHealth : 0f;
        }

        private CharacterKnockdownComponent.Options BuildPlayerKnockdownOptions()
        {
            CharacterPrefabConfig config = PrefabConfig;
            return new CharacterKnockdownComponent.Options
            {
                FollowCameraWithRagdoll = true,
                TransferWeaponToRagdoll = true,
                PostGetUpInvincibleDuration = config != null
                    ? Mathf.Max(0f, config.PostGetUpInvincibleDuration)
                    : 1.5f,
            };
        }
    }
}
