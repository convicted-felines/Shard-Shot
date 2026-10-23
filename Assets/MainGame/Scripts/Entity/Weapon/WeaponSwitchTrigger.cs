using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 场景触发器：玩家进入后切换为指定武器。Collider 需勾选 Is Trigger。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponSwitchTrigger : MonoBehaviour
    {
        [SerializeField]
        private WeaponEnum m_Weapon = WeaponEnum.加特林;

        [SerializeField]
        private bool m_OnlyOnce;

        private bool m_HasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            if (m_OnlyOnce && m_HasTriggered)
            {
                return;
            }

            PlayerCharacterEntity player = other.GetComponentInParent<PlayerCharacterEntity>();
            if (player == null)
            {
                return;
            }

            player.EquipWeapon(m_Weapon);
            m_HasTriggered = true;
        }
    }
}
