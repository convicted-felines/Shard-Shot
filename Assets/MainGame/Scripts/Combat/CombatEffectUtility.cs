using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 命中效果：伤害、眩晕等战斗反馈的统一入口。
    /// </summary>
    public static class CombatEffectUtility
    {
        public static bool TryApplyHit(Collider collider, float damage, float stunDuration, int attackerEntityId = 0)
        {
            if (collider == null)
            {
                return false;
            }

            RagdollOwnerDamageProxy ragdollProxy = collider.GetComponent<RagdollOwnerDamageProxy>();
            if (ragdollProxy != null)
            {
                return ragdollProxy.ApplyHit(damage, stunDuration, attackerEntityId);
            }

            CharacterDamageReceiver receiver = collider.GetComponent<CharacterDamageReceiver>();
            return receiver != null && receiver.ApplyHit(damage, stunDuration, attackerEntityId);
        }
    }
}
