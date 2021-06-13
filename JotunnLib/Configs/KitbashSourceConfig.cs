using UnityEngine;
using Jotunn.Managers;

namespace Jotunn.Configs
{
    /// <summary>
    ///     Configuration class for defining kitbash parts to add to a prefab with <see cref="KitbashManager"/>
    /// </summary>
    public class KitbashSourceConfig
    {
        /// <summary>
        ///     Name of the pasted GameObject
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///     Target parent of the pasted GameObject
        /// </summary>
        public string TargetParentPath { get; set; }
        /// <summary>
        ///     Source prefab that contains the GameObject to copy
        /// </summary>
        public string SourcePrefab { get; set; }
        /// <summary>
        ///     Location of the GameObject to copy from the source prefab
        /// </summary>
        public string SourcePath { get; set; }
        /// <summary>
        ///     Position of the pasted GameObject
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.zero;
        /// <summary>
        ///     Rotation of the pasted GameObject
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        /// <summary>
        ///     Scale of the pasted GameObject
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.one;
        /// <summary>
        ///     A list of Materials to set on the pasted GameObject
        /// </summary>
        public string[] Materials { get; set; } 
    }
}
