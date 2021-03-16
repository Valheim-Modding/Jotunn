using UnityEngine;

namespace JotunnLib.Entities
{
    public class PieceConfig
    {
        public string Name { get; set; }
        public string Description { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool AllowedInDungeons { get; set; } = false;
        public Sprite Icon { get; set; }
        public PieceRequirementConfig[] Requirements { get; set; } = new PieceRequirementConfig[0];

        public Piece.Requirement[] GetRequirements()
        {
            Piece.Requirement[] reqs = new Piece.Requirement[Requirements.Length];

            for (int i = 0; i < reqs.Length; i++)
            {
                reqs[i] = Requirements[i].GetPieceRequirement();
            }

            return reqs;
        }
    }
}
