using System.Collections.Generic;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Helper class for creating Mocks of item/piece requirements.
    /// </summary>
    public static class MockRequirement
    {
        /// <summary>
        ///     Creates a mocked Piece.Requirement
        /// </summary>
        /// <param name="name">Prefab name</param>
        /// <param name="amount">Amount</param>
        /// <param name="recover">Whether the resource is returned after deconstruction</param>
        /// <returns></returns>
        public static Piece.Requirement Create(string name, int amount = 1, bool recover = true)
        {
            var requirement = new Piece.Requirement
            {
                m_recover = recover,
                m_amount = amount
            };

            requirement.m_resItem = Mock<ItemDrop>.Create(name);

            return requirement;
        }

        /// <summary>
        ///     Creates a mocked Piece.Requirement array
        /// </summary>
        /// <param name="requirements">List of prefab names and amounts</param>
        /// <param name="recover">Whether the resources are returned after deconstruction</param>
        /// <returns></returns>
        public static Piece.Requirement[] CreateArray(Dictionary<string, int> requirements, bool recover = true)
        {
            List<Piece.Requirement> list = new List<Piece.Requirement>();

            foreach (KeyValuePair<string, int> requirement in requirements)
            {
                Piece.Requirement piece = Create(requirement.Key, requirement.Value, recover);
                piece.FixReferences();
                list.Add(piece);
            }

            return list.ToArray();
        }
    }
}
