using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色头顶世界血条：玩家用 <see cref="WorldUIEnum.进度条"/>，敌方用 <see cref="WorldUIEnum.进度条红"/>。
    /// </summary>
    public sealed class CharacterWorldHealthBarComponent : MonoBehaviour
    {
        private CharacterEntity m_Owner;
        private int m_HealthBarEntityId;

        public static CharacterWorldHealthBarComponent Ensure(CharacterEntity owner)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterWorldHealthBarComponent component = owner.GetComponent<CharacterWorldHealthBarComponent>();
            if (component == null)
            {
                component = owner.gameObject.AddComponent<CharacterWorldHealthBarComponent>();
            }

            component.m_Owner = owner;
            return component;
        }

        public void Show(float progress01)
        {
            if (m_Owner == null || GameEntry.Entity == null)
            {
                return;
            }

            Hide();

            WorldUIEnum barType = ResolveBarType(m_Owner.Faction);
            WorldUIData data = WorldUIData.CreateCharacterHealthBar(barType, m_Owner.Id, progress01);
            m_HealthBarEntityId = data.Id;
            GameEntry.Entity.ShowWorldUI(data);
        }

        public void SetProgress(float progress01)
        {
            if (m_HealthBarEntityId == 0 || GameEntry.Entity == null)
            {
                return;
            }

            WorldUIEntity healthBar = GameEntry.Entity.GetGameEntity(m_HealthBarEntityId) as WorldUIEntity;
            healthBar?.SetProgress(progress01);
        }

        public void Hide()
        {
            if (m_HealthBarEntityId == 0 || GameEntry.Entity == null)
            {
                m_HealthBarEntityId = 0;
                return;
            }

            GameEntry.Entity.HideEntity(m_HealthBarEntityId);
            m_HealthBarEntityId = 0;
        }

        public void Clear()
        {
            Hide();
            m_Owner = null;
        }

        private static WorldUIEnum ResolveBarType(CharacterFaction faction)
        {
            return faction == CharacterFaction.Player
                ? WorldUIEnum.进度条
                : WorldUIEnum.进度条红;
        }
    }
}
