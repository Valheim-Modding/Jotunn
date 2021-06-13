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
        ///     An optional name of the pasted GameObject<br/>
        ///     Defaults to the source name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Target parent of the pasted GameObject <br/>
        ///     Defaults to the root of the prefab
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
        ///     Position of the pasted GameObject<br/>
        ///     Defaults to <see cref="Vector3.zero"/>
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.zero;

        /// <summary>
        ///     Rotation of the pasted GameObject<br/>
        ///     Defaults to <see cref="Quaternion.identity"/> (no rotation)
        /// </summary>
        public Quaternion Rotation { get; set; } = Quaternion.identity;

        /// <summary>
        ///     Scale of the pasted GameObject<br/>
        ///     Defaults to <see cref="Vector3.one"/> (no rescale)
        /// </summary>
        public Vector3 Scale { get; set; } = Vector3.one;

        /// <summary>
        ///     An optional list of Materials to set on the pasted GameObject<br/>
        ///     Defaults to the original materials
        /// </summary>
        public string[] Materials { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
            {
                return Name;
            }
            return base.ToString();
        }
    }
}
