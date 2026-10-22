namespace MainGame
{
    /// <summary>
    /// 角色阵营，用于目标选取与敌对判定。
    /// </summary>
    public enum CharacterFaction : byte
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2,
    }

    public static class CharacterFactionUtility
    {
        public static bool IsHostile(CharacterFaction self, CharacterFaction other)
        {
            if (self == CharacterFaction.Neutral || other == CharacterFaction.Neutral)
            {
                return false;
            }

            return self != other;
        }
    }
}
