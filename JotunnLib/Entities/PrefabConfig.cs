using UnityEngine;

namespace JotunnLib.Entities
{
    public abstract class PrefabConfig
    {
        public string Name { get; private set; }
        public string BasePrefabName { get; private set; }
        public GameObject Prefab { get; set; }

        public PrefabConfig(string name)
        {
            Name = name;
        }

        public PrefabConfig(string name, string baseName)
        {
            Name = name;
            BasePrefabName = baseName;
        }

        public abstract void Register();
    }
}
