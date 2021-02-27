using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader.Managers
{
    public static class ObjectManager
    {
        public static event EventHandler ObjectLoad;
        internal static List<GameObject> Items = new List<GameObject>();

        private static bool loaded = false;

        internal static void LoadItems()
        {
            Debug.Log("---- Registering custom items ----");

            ObjectLoad?.Invoke(null, EventArgs.Empty);

            foreach (GameObject obj in Items)
            {
                ObjectDB.instance.m_items.Add(obj);
                Debug.Log("Added item: " + obj.name);
            }

            Util.InvokePrivate(ObjectDB.instance, "UpdateItemHashes");
            loaded = true;
        }

        public static void AddItem(string prefabName)
        {
            Items.Add(PrefabManager.GetPrefab(prefabName));
        }

        public static void AddItem(GameObject item)
        {
            Items.Add(item);
        }
    }
}
