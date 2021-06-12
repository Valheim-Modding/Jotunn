using System.Collections.Generic;

namespace Jotunn.Configs
{
    public class KitbashConfig
    {
        public List<string> boxColliderPaths = new List<string>();
        public List<KitbashSourceConfig> KitbashSources = new List<KitbashSourceConfig>(); 
        public string layer;
        public bool FixReferences { get; internal set; }
    }
}
