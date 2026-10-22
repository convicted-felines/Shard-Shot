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



        private float m_CurrentHealth;

        private bool m_IsAlive;



        public override CharacterFaction Faction => CharacterFaction.Player;



        public bool IsAlive => m_IsAlive;



        public float CurrentHealth => m_CurrentHealth;



        public float MaxHealth => CharacterConfig != null ? CharacterConfig.Health : 0f;



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



            CharacterPrefabConfig prefabConfig = GetComponent<CharacterPrefabConfig>();
            Transform socket = prefabConfig != null && prefabConfig.WeaponSocket != null
                ? prefabConfig.WeaponSocket.transform
                : null;
            m_WeaponComponent.Initialize(this, socket);



            m_Animator = GetComponentInChildren<Animator>();

            m_AnimatorSpeed = 0f;

            if (m_Animator != null)

            {

                m_Animator.SetFloat(SpeedParamHash, 0f);

            }



            m_CombatComponent = GetComponent<CharacterCombatComponent>();

            if (m_CombatComponent == null)

            {

                m_CombatComponent = gameObject.AddComponent<CharacterCombatComponent>();

            }



            m_CombatComponent.Initialize(this, m_WeaponComponent, m_Animator);

            EquipWeapon(WeaponEnum.加特林);



            if (Rigidbody == null)

            {

                return;

            }



            Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        }



        protected override void OnHide(bool isShutdown, object userData)

        {

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

            UpdateMovement(elapseSeconds);

            m_CombatComponent?.Tick(elapseSeconds);

        }



        public void TakeDamage(float damage, int attackerEntityId = 0)

        {

            if (!m_IsAlive || damage <= 0f)

            {

                return;

            }



            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);

            GameEntry.Entity.ShowWorldUIFloatingText(Id, $"-{damage:0}");



            if (m_CurrentHealth <= 0f)

            {

                m_IsAlive = false;

                Log.Info("Player character defeated (knockdown/get-up pending).");

            }

        }



        protected override DeathRagdollSettings GetDeathRagdollSettings()

        {

            DeathRagdollSettings settings = base.GetDeathRagdollSettings();

            settings.DetachLimbs = false;

            return settings;

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


