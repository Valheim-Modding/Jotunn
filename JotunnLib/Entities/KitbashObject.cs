using System;
using Jotunn.Configs;
using UnityEngine;

namespace Jotunn.Entities
{
    public class KitbashObject
    {
        public Action OnKitbashApplied;

        public GameObject Prefab { get; internal set; }
        public KitbashConfig Config { get; internal set; }

    }
}
