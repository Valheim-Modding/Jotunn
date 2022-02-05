using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jotunn.DebugUtils
{
    internal class UEInputBlocker : MonoBehaviour
    {
        private ConfigEntry<bool> _isModEnabled;
        private GameObject UnityExplorer;
        private bool Blocked;

        private void Awake()
        {
            _isModEnabled = Main.Instance.Config.Bind<bool>(nameof(UEInputBlocker), "Enabled", false, "Globally enable or disable blocking input when UnityExplorer is open");

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "start")
            {
                Logger.LogDebug("Trying to identify UnityExplorer");

                var plugin = FindObjectsOfType(typeof(BaseUnityPlugin)).Cast<BaseUnityPlugin>().ToArray()
                    .FirstOrDefault(x => x.Info?.Metadata?.GUID == "com.sinai.unityexplorer");
                
                // legacy
                UnityExplorer = plugin?.gameObject.scene.GetRootGameObjects()
                    .FirstOrDefault(x => x.name.Equals("UnityExplorer"));
                
                // 4.4.0 and up
                if (!UnityExplorer)
                {
                    UnityExplorer = plugin?.gameObject.scene.GetRootGameObjects()
                        .FirstOrDefault(x => x.transform.Find("com.sinai.unityexplorer_Root"))?.transform
                        .Find("com.sinai.unityexplorer_Root").gameObject;
                }

                if (UnityExplorer)
                {
                    Logger.LogDebug("UnityExplorer found, saving Zarb!");
                    SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
                }
                else
                {
                    Destroy(this);
                }
            }
        }

        private void Update()
        {
            if (!UnityExplorer)
            {
                return;
            }

            if (!_isModEnabled.Value)
            {
                if (Blocked)
                {
                    GUIManager.BlockInput(false);
                    Blocked = false;
                }

                return;
            }

            if (UnityExplorer.activeSelf && !Blocked)
            {
                GUIManager.BlockInput(true);
                Blocked = true;
            }

            if (!UnityExplorer.activeSelf && Blocked)
            {
                GUIManager.BlockInput(false);
                Blocked = false;
            }
        }
    }
}
