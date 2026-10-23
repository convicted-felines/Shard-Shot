using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 武器战斗数值计算。
    /// </summary>
    public static class WeaponCombatUtility
    {
        /// <summary>
        /// 攻击间隔 = 基础攻击速度 / 攻击速度。
        /// </summary>
        public static float GetAttackInterval(DRWeapon weapon)
        {
            if (weapon == null)
            {
                return float.MaxValue;
            }

            return GetAttackInterval(weapon, weapon.AttackSpeed);
        }

        /// <summary>
        /// 攻击间隔 = 基础攻击速度 / 指定攻击速度（加特林叠射速等动态攻速）。
        /// </summary>
        public static float GetAttackInterval(DRWeapon weapon, float attackSpeed)
        {
            if (weapon == null || attackSpeed <= 0f)
            {
                return float.MaxValue;
            }

            return weapon.BaseAttackSpeed / attackSpeed;
        }

        /// <summary>
        /// 攻击开始后，到实际产生攻击行为（如发射子弹）的延迟。
        /// </summary>
        public static float GetAttackFireDelay(DRWeapon weapon)
        {
            if (weapon == null)
            {
                return float.MaxValue;
            }

            return GetAttackFireDelay(weapon, weapon.AttackSpeed);
        }

        /// <summary>
        /// 攻击开始后，到实际产生攻击行为（如发射子弹）的延迟。
        /// </summary>
        public static float GetAttackFireDelay(DRWeapon weapon, float attackSpeed)
        {
            if (weapon == null)
            {
                return float.MaxValue;
            }

            return GetAttackInterval(weapon, attackSpeed) * Mathf.Clamp01(weapon.AttackTiming);
        }
    }
}
