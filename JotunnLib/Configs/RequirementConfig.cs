using System.Collections.Generic;
using Jotunn.Entities;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for requirements needed to craft items or build pieces.
    /// </summary>
    public class RequirementConfig
    {
        /// <summary>
        ///     Name of the item prefab of this requirement.
        /// </summary>
        public string Item { get; set; } = string.Empty;

        /// <summary>
        ///     Amount that is needed to fulfill the requirement. Defaults to 1.
        /// </summary>
        public int Amount { get; set; } = 1;

        /// <summary>
        ///     How much more of this requirement is needed per item level. Defaults to 0.
        /// </summary>
        public int AmountPerLevel { get; set; } = 0;

        /// <summary>
        ///     Whether the item is dropped after deconstructing a piece. Defaults to true.
        /// </summary>
        public bool Recover { get; set; } = true;

        /// <summary>
        ///     Creates a new requirement config with default values.
        /// </summary>
        public RequirementConfig() { }

        /// <summary>
        ///     Creates a new requirement config with the given values.
        /// </summary>
        /// <param name="item">The internal item prefab id, see https://valheim-modding.github.io/Jotunn/data/objects/item-list.html or the Valheim Wiki</param>
        /// <param name="amount">The amount of items needed to craft an item/piece</param>
        /// <param name="amountPerLevel">The amount of items needed to upgrade an item. Does not apply to pieces. The basic formular is: Upgrade Amount = Item Level * Amount Per Level</param>
        /// <param name="recover">Whether the item is dropped after deconstructing a piece</param>
        public RequirementConfig(string item, int amount, int amountPerLevel = 0, bool recover = true)
        {
            Item = item;
            Amount = amount;
            AmountPerLevel = amountPerLevel;
            Recover = recover;
        }

        /// <summary>
        ///     Creates a Valheim Piece.Requirement from this config. 
        /// </summary>
        /// <returns></returns>
        public Piece.Requirement GetRequirement()
        {
            return new Piece.Requirement
            {
                m_resItem = Mock<ItemDrop>.Create(Item),
                m_amount = Amount,
                m_amountPerLevel = AmountPerLevel,
                m_recover = Recover
            };
        }

        /// <summary>
        ///     Creates a Valheim Piece.Requirement array from the given requirement configs.
        /// </summary>
        /// <param name="requirements"></param>
        /// <returns></returns>
        public static Piece.Requirement[] GetRequirements(IEnumerable<RequirementConfig> requirements)
        {
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();

            foreach (RequirementConfig requirement in requirements)
            {
                if (requirement != null && requirement.IsValid())
                {
                    reqs.Add(requirement.GetRequirement());
                }
            }

            return reqs.ToArray();
        }

        /// <summary>
        ///     Checks if the requirement has any item and amount set.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Item) && (Amount > 0 || AmountPerLevel > 0);
        }
    }
}
