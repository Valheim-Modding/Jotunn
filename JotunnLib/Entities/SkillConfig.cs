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
            get { return _identifier; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Debug.LogError("Error, SkillConfig cannot have invalid Identifier: " + value);
                    return;
                }

                _identifier = value;
                UID = (Skills.SkillType)value.GetStableHashCode();
            }
        }
        public Skills.SkillType UID { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Sprite Icon { get; set; }
        public float IncreaseStep { get; set; }
    }
}
