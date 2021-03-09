using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;
using JotunnLib.Entities;
using JotunnLib.Utils;

namespace SimpleMounts
{
    public class SaddlePrefab : PrefabConfig
    {
        public SaddlePrefab() : base("Saddle", "Wood")
        {

        }

        public override void Register()
        {
            // Configure item drop
            ItemDrop item = Prefab.GetComponent<ItemDrop>();
            item.m_itemData.m_shared.m_name = "Saddle";
            item.m_itemData.m_shared.m_description = "Use this to ride animals";
            item.m_itemData.m_dropPrefab = Prefab;
            item.m_itemData.m_shared.m_weight = 1f;
            item.m_itemData.m_shared.m_maxStackSize = 1;
            item.m_itemData.m_shared.m_variants = 1;

            Texture2D tex = AssetUtils.LoadTexture("SimpleMounts/assets/saddle.png");
            Debug.Log(tex);
            item.m_itemData.m_shared.m_icons = new Sprite[]
            {
                Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 100)
            };
        }
    }
}
