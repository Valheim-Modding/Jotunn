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
        public string Item { get; set; }

        /// <summary>
        ///     Amount that is needed to fulfill the requirement.
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        ///     How much more of this requirement is needed per item level.
        /// </summary>
        public int AmountPerLevel { get; set; }

        /// <summary>
        ///     Determines if the used requirement will be rewarded again after dismanteling a piece.
        /// </summary>
        public bool Recover { get; set; }

        /// <summary>
        ///     Creates a Valheim Piece.Requirement from this config. 
        /// </summary>
        /// <returns></returns>
        public Piece.Requirement GetRequirement()
        {
            return new Piece.Requirement()
            {
                m_resItem = Mock<ItemDrop>.Create(Item),
                m_amount = Amount,
                m_amountPerLevel = AmountPerLevel,
                m_recover = Recover
            };
        }
    }
}
