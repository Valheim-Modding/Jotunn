using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace Jotunn.Entities
{
    public class CustomVegetation : CustomEntity
    {
        public GameObject Prefab { get; set; }
        public ZoneSystem.ZoneVegetation Vegetation { get; private set; }
        public string Name { get; internal set; }

        public CustomVegetation(GameObject prefab, VegetationConfig config)
        {
            Prefab = prefab;
            Name = prefab.name;
            Vegetation = config.ToVegetation();
            Vegetation.m_prefab = prefab;
        }
    }
}
