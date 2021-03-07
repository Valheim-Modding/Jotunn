using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ValheimLokiLoader.Entities
{
    public class ButtonConfig
    {
        public string Name { get; set; }
        public string Axis { get; set; }
        public KeyCode Key { get; set; }
        public bool Inverted { get; set; }
        public float RepeatDelay { get; set; }
        public float RepeatInterval { get; set; }
    }
}
