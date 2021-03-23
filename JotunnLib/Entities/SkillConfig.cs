using UnityEngine;

namespace JotunnLib.Entities
{
    /// <summary>
    /// Configuration class for custom skills
    /// </summary>
    public class SkillConfig
    {
        private string _identifier;
        public string Identifier
        {
            get { return this._identifier; }
            set
            {
                this._identifier = value;
                UID = (Skills.SkillType)value.GetStableHashCode();
            }
        }
        public Skills.SkillType UID { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Sprite Icon { get; set; }
        public float IncreaseStep { get; set; }

        // BaseSkill and JSON support targets v0.2.0
        //private Skills.SkillType BaseSkill { get; set; }
        //private static SkillConfig FromJson(string json)
        //{
        //    return null; // TODO: Make this work
        //}
    }
}
