namespace MainGame
{
    /// <summary>
    /// 可承受伤害的实体接口，武器命中后通过 <see cref="CharacterDamageReceiver"/> 转发调用。
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        float CurrentHealth { get; }

        float MaxHealth { get; }

        void TakeDamage(float damage, int attackerEntityId = 0);
    }
}
