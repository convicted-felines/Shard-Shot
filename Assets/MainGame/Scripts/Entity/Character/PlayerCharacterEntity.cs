using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 玩家角色：读取输入，通过刚体在世界平面移动。
    /// </summary>
    public class PlayerCharacterEntity : CharacterEntity
    {
        [SerializeField]
        private GameObject m_WeaponSocket;

        private static readonly int SpeedParamHash = Animator.StringToHash("Speed");

        private Animator m_Animator;
        private float m_AnimatorSpeed;
        private CharacterWeaponComponent m_WeaponComponent;

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

            m_WeaponComponent = GetComponent<CharacterWeaponComponent>();
            if (m_WeaponComponent == null)
            {
                m_WeaponComponent = gameObject.AddComponent<CharacterWeaponComponent>();
            }

            Transform socket = m_WeaponSocket != null ? m_WeaponSocket.transform : null;
            m_WeaponComponent.Initialize(this, socket);

            m_Animator = GetComponentInChildren<Animator>();
            m_AnimatorSpeed = 0f;
            if (m_Animator != null)
            {
                m_Animator.SetFloat(SpeedParamHash, 0f);
            }

            if (Rigidbody == null)
            {
                return;
            }

            Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
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
            m_WeaponComponent = null;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            UpdateMovement(elapseSeconds);
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
            Vector3 velocity = Rigidbody.velocity;
            velocity.x = moveDirection.x * moveSpeed;
            velocity.z = moveDirection.z * moveSpeed;
            Rigidbody.velocity = velocity;

            UpdateAnimator(moveSpeed, elapseSeconds);

            if (moveDirection.sqrMagnitude <= 0.001f)
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
    }
}
