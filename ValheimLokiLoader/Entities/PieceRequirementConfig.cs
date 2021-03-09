using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ValheimLokiLoader.Managers;

namespace ValheimLokiLoader.Entities
{
    public class PieceRequirementConfig
    {
        public string Item { get; set; }
        public int Amount { get; set; }
        public int AmountPerLevel { get; set; }
        public bool Recover { get; set; }

        public Piece.Requirement GetPieceRequirement()
        {
            return new Piece.Requirement()
            {
                m_resItem = PrefabManager.Instance.GetPrefab(Item).GetComponent<ItemDrop>(),
                m_amount = Amount,
                m_amountPerLevel = AmountPerLevel,
                m_recover = Recover
            };
        }
    }
}
