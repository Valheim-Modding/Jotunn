using UnityEngine;

namespace Jotunn.Configs
{ 
    public class KitbashSourceConfig
    {
        public string name;
        public string targetParentPath;
        public string sourcePrefab;
        public string sourcePath; 
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;
        public string[] materials;

        public override string ToString()
        {
            return $"KitBashSource(name={name},sourcePrefab={sourcePrefab},sourcePath={sourcePath})";
        }
    } 
}
