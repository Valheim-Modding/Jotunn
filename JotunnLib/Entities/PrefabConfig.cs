using UnityEngine;
using JotunnLib.Managers;

namespace JotunnLib.Entities
{
    public abstract class PrefabConfig
    {
        public string Name { get; private set; }
        public string BasePrefabName { get; private set; }
        public GameObject Prefab { get; private set; }

        public PrefabConfig()
        {
            // Do nothing, initialize and register your own prefabs
        }

        public PrefabConfig(string name)
        {
            Name = name;
            Prefab = PrefabManager.Instance.CreatePrefab(Name);
        }

        public PrefabConfig(string name, string baseName)
        {
            Name = name;
            BasePrefabName = baseName;
            Prefab = PrefabManager.Instance.CreatePrefab(Name, BasePrefabName);
        }

        public PrefabConfig(AssetBundle assetBundle, string assetName)
        {
            Name = assetName;
            Prefab = (GameObject)assetBundle.LoadAsset(assetName);
        }

        public virtual void Register()
        {
            // Override this to make it do things
        }

        public Piece AddPiece(PieceConfig pieceConfig)
        {
            Piece piece = Prefab.AddComponent<Piece>();
            piece.m_name = pieceConfig.Name;
            piece.m_description = pieceConfig.Description;
            piece.m_resources = pieceConfig.GetRequirements();
            piece.m_enabled = pieceConfig.Enabled;
            piece.m_icon = pieceConfig.Icon;
            piece.m_allowedInDungeons = pieceConfig.AllowedInDungeons;
            return piece;
        }
    }
}
