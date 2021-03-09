using UnityEngine;

namespace JotunnLib.Entities
{
    public class ButtonConfig
    {
        public string Name { get; set; }
        public string Axis { get; set; } = null;
        public KeyCode Key { get; set; }
        public bool Inverted { get; set; } = false;
        public float RepeatDelay { get; set; } = 0.0f;
        public float RepeatInterval { get; set; } = 0.0f;
    }
}
