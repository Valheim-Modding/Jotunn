using System;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    public class KitbashObject
    {
        public Action KitbashApplied;

        public GameObject Prefab { get; internal set; }
        public KitbashConfig Config { get; internal set; }

    }
}
