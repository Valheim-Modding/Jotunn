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
        ///     Determines if the used requirement will be rewarded again after dismanteling a piece. Defaults to false.
        /// </summary>
        public bool Recover { get; set; } = false;

        /// <summary>
        ///     Creates a new requirement config with default values.
        /// </summary>
        public RequirementConfig() { }

        /// <summary>
        ///     Creates a new requirement config with the given values.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        /// <param name="amountPerLevel"></param>
        /// <param name="recover"></param>
        public RequirementConfig(string item, int amount, int amountPerLevel = 0, bool recover = false)
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
    }
}
