using Jotunn.Entities;
using Jotunn.Managers;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for drops of <see cref="CustomCreature">CustomCreatures</see> used in <see cref="CreatureConfig"/>.
    /// </summary>
    public class DropConfig
    {
        /// <summary>
        ///     Name of the item prefab of this drop. Gets resolved by Jötunn at runtime.
        /// </summary>
        public string Item { get; set; } = string.Empty;

        /// <summary>
        ///     Minimum amount of this drop. Defaults to 1.
        /// </summary>
        public int MinAmount { get; set; } = 1;

        /// <summary>
        ///     Maximum amount of this drop. Defaults to 1.
        /// </summary>
        public int MaxAmount { get; set; } = 1;

        /// <summary>
        ///     Chance of this drop in percent. Defaults to 100f.
        /// </summary>
        public float Chance { get; set; } = 100f;

        /// <summary>
        ///     Should the drop be multiplied so every player gets the same amount. Defaults to false.
        /// </summary>
        public bool OnePerPlayer { get; set; } = false;

        /// <summary>
        ///     Should the drop amount be multiplied by the creature level. Defaults to true.
        /// </summary>
        public bool LevelMultiplier { get; set; } = true;

        /// <summary>
        ///     Creates a Valheim <see cref="CharacterDrop.Drop"/> from this config.
        /// </summary>
        /// <returns>The Valheim <see cref="CharacterDrop.Drop"/></returns>
        public CharacterDrop.Drop GetDrop()
        {
            return new CharacterDrop.Drop
            {
                m_prefab = MockManager.Instance.CreateMockedGameObject(Item),
                m_amountMin = MinAmount,
                m_amountMax = MaxAmount < MinAmount ? MinAmount : MaxAmount,
                m_chance = Chance / 100f,
                m_onePerPlayer = OnePerPlayer,
                m_levelMultiplier = LevelMultiplier
            };
        }
    }
}
